using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimPrisonBuilder.PrisonLabor;

namespace RimPrisonBuilder.UI
{
    // [UNREVIEWED] Haven't reviewed carefully
    public class Dialog_PrisonerManagement : Window
    {
        private int curTab;
        private List<TabRecord> tabs;
        private PrisonerGroupManager groupManager;

        // Scroll state per tab
        private Vector2 workScrollPos;
        private Vector2 schedScrollPos;
        private Vector2 prisonerScrollPos;

        // Cached filtered work types (no Warden, no Hunting)
        private static List<WorkTypeDef> cachedWorkTypes;
        private static int cachedWorkTypesGameTick = -1;

        private const float TabVisualHeight = 35f;
        private const float WorkCellSize = 25f;
        private const float WorkHeaderHeight = 22f;
        private const float WorkLabelWidth = 160f;
        private const float WorkColPadding = 8f;
        private const float SchedLabelWidth = 120f;
        private const float SchedSelectorHeight = 65f;
        private const float PrisonerRowHeight = 28f;
        private const float GroupButtonWidth = 140f;

        private List<Pawn> PawnList =>
            Find.CurrentMap.mapPawns.PrisonersOfColony
                .Where(p => p.IsLaborEnabled()).ToList();

        public Dialog_PrisonerManagement()
        {
            forcePause = false;
            doCloseX = true;
            doCloseButton = false;
            closeOnClickedOutside = true;
            draggable = false;
            absorbInputAroundWindow = true;
            resizeable = true;
            layer = WindowLayer.Dialog;
        }

        public override Vector2 InitialSize => new Vector2(1400f, 750f);

        public override void PostOpen()
        {
            base.PostOpen();
            BuildTabs();
            groupManager = Find.CurrentMap.GetComponent<PrisonerGroupManager>();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect tabBaseRect = new Rect(inRect.x, inRect.y + 32f,
                inRect.width, inRect.height - 32f);
            Rect tabBgRect = new Rect(inRect.x, inRect.y, inRect.width, TabVisualHeight);
            Widgets.DrawMenuSection(tabBgRect);
            TabDrawer.DrawTabs(tabBaseRect, tabs);

            // Make only the tab bar draggable so drag-painting works in content area.
            // Tabs are drawn at y=-32f to y=35f (group coords); cover the full zone.
            Rect dragRect = new Rect(inRect.x, inRect.y - 32f, inRect.width, 32f + TabVisualHeight);
            GUI.DragWindow(dragRect);

            Rect contentRect = new Rect(
                inRect.x, inRect.y + TabVisualHeight,
                inRect.width, inRect.height - TabVisualHeight);

            if (curTab == 0)
                DrawScheduleTab(contentRect);
            else if (curTab == 1)
                DrawWorkTab(contentRect);
            else
                DrawPrisonerManageTab(contentRect);
        }

        // ======== Schedule tab (group-level) ========

        private void DrawScheduleTab(Rect rect)
        {
            if (groupManager == null)
            {
                groupManager = Find.CurrentMap.GetComponent<PrisonerGroupManager>();
                if (groupManager == null) return;
            }

            var groups = groupManager.groups;
            if (groups.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimPrisonBuilder.NoGroups".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Time assignment selector at top (vanilla)
            TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid(
                new Rect(rect.x, rect.y, 191f, SchedSelectorHeight));

            // Header: hour numbers
            float gridY = rect.y + SchedSelectorHeight + 4f;
            // Narrower cells (2/3 width), integer pixel math avoids floating point gaps
            float schedTotalW = (rect.width - SchedLabelWidth - 16f) * 2f / 3f;

            Rect hourHeaderRect = new Rect(rect.x + SchedLabelWidth, gridY,
                schedTotalW, 16f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            for (int h = 0; h < 24; h++)
            {
                float xLeft = Mathf.FloorToInt(h * schedTotalW / 24f);
                float xRight = Mathf.FloorToInt((h + 1) * schedTotalW / 24f);
                Rect hr = new Rect(hourHeaderRect.x + xLeft, hourHeaderRect.y,
                    xRight - xLeft, hourHeaderRect.height);
                Widgets.Label(hr, h.ToString());
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            gridY += 18f;

            // Group rows
            float rowHeight = 24f;
            float viewHeight = groups.Count * rowHeight;
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Rect scrollRect = new Rect(rect.x, gridY, rect.width,
                rect.height - (gridY - rect.y));

            Widgets.BeginScrollView(scrollRect, ref schedScrollPos, viewRect);

            for (int g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                float y = g * rowHeight;

                // Group label
                Rect labelRect = new Rect(0f, y, SchedLabelWidth, rowHeight);
                Widgets.Label(labelRect, group.name);

                // 24 hour blocks — integer pixel math to eliminate gaps
                for (int h = 0; h < 24; h++)
                {
                    float xLeft = Mathf.FloorToInt(h * schedTotalW / 24f);
                    float xRight = Mathf.FloorToInt((h + 1) * schedTotalW / 24f);
                    Rect cellRect = new Rect(SchedLabelWidth + xLeft + 1f, y + 1f,
                        xRight - xLeft - 2f, rowHeight - 2f);
                    DrawSchedCell(cellRect, group, h);
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawSchedCell(Rect rect, PrisonerGroup group, int hour)
        {
            TimeAssignmentDef assignment = group.GetAssignment(hour);
            GUI.DrawTexture(rect, assignment.ColorTexture);

            if (!Mouse.IsOver(rect))
                return;

            Widgets.DrawBox(rect, 2);

            if (Input.GetMouseButton(0) &&
                TimeAssignmentSelector.selectedAssignment != null &&
                assignment != TimeAssignmentSelector.selectedAssignment)
            {
                group.SetAssignment(hour, TimeAssignmentSelector.selectedAssignment);
                groupManager.SyncGroupToAllPawns(group);
                SoundDefOf.Designate_DragStandard_Changed_NoCam.PlayOneShotOnCamera();
            }
        }

        // ======== Work tab (group-level) ========

        private void DrawWorkTab(Rect rect)
        {
            if (groupManager == null)
            {
                groupManager = Find.CurrentMap.GetComponent<PrisonerGroupManager>();
                if (groupManager == null) return;
            }

            var groups = groupManager.groups;
            var workTypes = GetFilteredWorkTypes();

            if (groups.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimPrisonBuilder.NoGroups".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Dynamic column width to fit the widest label
            float maxLabelW = 0f;
            Text.Font = GameFont.Tiny;
            for (int w = 0; w < workTypes.Count; w++)
            {
                float lw = Text.CalcSize(workTypes[w].labelShort.CapitalizeFirst()).x;
                if (lw > maxLabelW) maxLabelW = lw;
            }
            Text.Font = GameFont.Small;
            float colWidth = Mathf.Max(WorkCellSize, maxLabelW + WorkColPadding);

            float totalWidth = WorkLabelWidth + workTypes.Count * colWidth;
            float totalHeight = WorkHeaderHeight + groups.Count * WorkCellSize;

            Rect viewRect = new Rect(0f, 0f, totalWidth - 16f, totalHeight);
            Widgets.BeginScrollView(rect, ref workScrollPos, viewRect);

            // Column headers — horizontal text, centered
            Text.Font = GameFont.Tiny;
            for (int w = 0; w < workTypes.Count; w++)
            {
                Rect headerRect = new Rect(
                    WorkLabelWidth + w * colWidth, 0f, colWidth, WorkHeaderHeight);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(headerRect, workTypes[w].labelShort.CapitalizeFirst());
            }
            // Vertical divider line after headers
            GUI.color = new Color(1f, 1f, 1f, 0.3f);
            Widgets.DrawLineHorizontal(0f, WorkHeaderHeight, totalWidth);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Group rows
            for (int g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                float y = WorkHeaderHeight + g * WorkCellSize;

                // Group label
                Rect labelRect = new Rect(0f, y, WorkLabelWidth, WorkCellSize);
                Widgets.Label(labelRect, group.name);

                // Priority cells
                for (int w = 0; w < workTypes.Count; w++)
                {
                    Rect cellRect = new Rect(
                        WorkLabelWidth + w * colWidth, y, colWidth, WorkCellSize);
                    DrawWorkCell(cellRect, group, workTypes[w]);
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawWorkCell(Rect rect, PrisonerGroup group, WorkTypeDef wt)
        {
            int priority = group.GetPriority(wt);
            // Center the box within the column (match header text center axis)
            float boxX = rect.x + (rect.width - WorkCellSize) / 2f;
            Rect boxRect = new Rect(boxX, rect.y, WorkCellSize, WorkCellSize);

            if (priority > 0)
            {
                Widgets.DrawBoxSolid(boxRect, new Color(0.25f, 0.25f, 0.25f));
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(boxRect.ContractedBy(-2f), priority.ToString());
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                Widgets.DrawBox(boxRect);
            }

            if (!Mouse.IsOver(boxRect))
                return;

            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0)
                {
                    // Left click: decrease number (higher priority, vanilla behaviour)
                    int next = priority - 1;
                    if (next < 0) next = 4;
                    group.SetPriority(wt, next);
                    groupManager.SyncWorkPriority(group, wt, next);
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    Event.current.Use();
                }
                else if (Event.current.button == 1)
                {
                    // Right click: increase number (lower priority)
                    int prev = priority + 1;
                    if (prev > 4) prev = 0;
                    group.SetPriority(wt, prev);
                    groupManager.SyncWorkPriority(group, wt, prev);
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    Event.current.Use();
                }
            }
        }

        // ======== Prisoner management tab ========

        private void DrawPrisonerManageTab(Rect rect)
        {
            var pawns = PawnList;
            if (pawns.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimPrisonBuilder.NoLaborPrisoners".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float viewHeight = pawns.Count * PrisonerRowHeight;
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Widgets.BeginScrollView(rect, ref prisonerScrollPos, viewRect);

            float y = 0f;
            for (int i = 0; i < pawns.Count; i++)
            {
                Rect rowRect = new Rect(0f, y, viewRect.width, PrisonerRowHeight);
                if (i % 2 == 1)
                    Widgets.DrawLightHighlight(rowRect);
                DrawPrisonerRow(rowRect, pawns[i]);
                y += PrisonerRowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawPrisonerRow(Rect rect, Pawn pawn)
        {
            float nameWidth = 200f;
            float gap = 8f;

            Rect nameRect = new Rect(rect.x, rect.y, nameWidth, rect.height);
            Widgets.Label(nameRect, pawn.LabelShortCap);

            Rect groupRect = new Rect(rect.x + nameWidth + gap, rect.y + 2f,
                GroupButtonWidth, rect.height - 4f);
            DrawGroupButton(groupRect, pawn);

            var comp = pawn.TryGetComp<CompWorkTracker>();
            int coupons = comp?.earnedCoupons ?? 0;
            float couponX = rect.x + nameWidth + GroupButtonWidth + gap * 2;
            Rect couponRect = new Rect(couponX, rect.y, 200f, rect.height);
            Widgets.Label(couponRect, RimPrisonBuilderMod.Settings.WorkCouponName + ": " + coupons);

            // Grant button
            float grantBtnW = 60f;
            Rect grantRect = new Rect(couponX + 200f + gap, rect.y + 2f,
                grantBtnW, rect.height - 4f);
            if (Widgets.ButtonText(grantRect, "RimPrisonBuilder.GrantCoupons".Translate()))
            {
                Find.WindowStack.Add(new Dialog_GrantCoupons(pawn));
            }
        }

        private void DrawGroupButton(Rect rect, Pawn pawn)
        {
            if (groupManager == null)
                groupManager = Find.CurrentMap.GetComponent<PrisonerGroupManager>();

            var currentGroup = groupManager?.GetGroupFor(pawn);
            string label = currentGroup != null
                ? currentGroup.name
                : "RimPrisonBuilder.NoGroup".Translate();

            if (!Widgets.ButtonText(rect, label))
                return;

            List<FloatMenuOption> options = new List<FloatMenuOption>();

            options.Add(new FloatMenuOption("RimPrisonBuilder.NoGroup".Translate(), delegate
            {
                groupManager?.RemoveFromAllGroups(pawn);
            }));

            if (groupManager != null)
            {
                for (int i = 0; i < groupManager.groups.Count; i++)
                {
                    var g = groupManager.groups[i];
                    options.Add(new FloatMenuOption(g.name, delegate
                    {
                        groupManager.SetGroup(pawn, g);
                    }));
                }
            }

            options.Add(new FloatMenuOption("RimPrisonBuilder.ManageGroups".Translate(), delegate
            {
                if (groupManager != null)
                    Find.WindowStack.Add(new Dialog_ManagePrisonerGroups(groupManager));
            }));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        // ======== Helpers ========

        private static List<WorkTypeDef> GetFilteredWorkTypes()
        {
            int tick = Find.TickManager?.TicksGame ?? 0;
            if (cachedWorkTypes != null && cachedWorkTypesGameTick == tick)
                return cachedWorkTypes;

            cachedWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(wt => wt.defName != "Warden" && wt.defName != "Hunting")
                .ToList();
            cachedWorkTypesGameTick = tick;
            return cachedWorkTypes;
        }

        private void BuildTabs()
        {
            tabs = new List<TabRecord>
            {
                new TabRecord("RimPrisonBuilder.ScheduleTab".Translate(),
                    delegate { curTab = 0; }, () => curTab == 0),
                new TabRecord("RimPrisonBuilder.WorkTab".Translate(),
                    delegate { curTab = 1; }, () => curTab == 1),
                new TabRecord("RimPrisonBuilder.PrisonerManageTab".Translate(),
                    delegate { curTab = 2; }, () => curTab == 2)
            };
        }
    }
}
