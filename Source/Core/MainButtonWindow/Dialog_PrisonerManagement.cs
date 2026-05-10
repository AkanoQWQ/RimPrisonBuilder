using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using RimPrison.Core;

namespace RimPrison
{
    // [UNREVIEWED] Haven't reviewed carefully
    public class Dialog_PrisonerManagement : Window
    {
        private int curTab;
        private List<TabRecord> tabs;
        private PawnTable scheduleTable;
        private PawnTable workTable;

        private const float TimeSelectorHeight = 65f;
        private const float CouponColumnWidth = 80f;
        private const float CouponColumnGap = 8f;
        private const float TabVisualHeight = 35f;

        private List<Pawn> PawnList =>
            Find.CurrentMap.mapPawns.PrisonersOfColony
                .Where(p => p.IsLaborEnabled()).ToList();

        public Dialog_PrisonerManagement()
        {
            forcePause = false;
            doCloseX = true;
            doCloseButton = false;
            closeOnClickedOutside = true;
            draggable = true;
            absorbInputAroundWindow = true;
            resizeable = true;
            layer = WindowLayer.Dialog;
        }

        public override Vector2 InitialSize => new Vector2(1400f, 750f);

        public override void PostOpen()
        {
            base.PostOpen();
            BuildTabs();
            RebuildTables();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (scheduleTable == null || workTable == null)
            {
                RebuildTables();
            }

            // TabDrawer internally shifts y up by 32px (rect.y -= 32f).
            // Offset baseRect by 32px so tabs render within the clip region.
            Rect tabBaseRect = new Rect(inRect.x, inRect.y + 32f,
                inRect.width, inRect.height - 32f);

            // Background behind the tab area
            Rect tabBgRect = new Rect(inRect.x, inRect.y, inRect.width, TabVisualHeight);
            Widgets.DrawMenuSection(tabBgRect);
            TabDrawer.DrawTabs(tabBaseRect, tabs);

            // Content starts below the visual tab area
            Rect contentRect = new Rect(
                inRect.x, inRect.y + TabVisualHeight,
                inRect.width, inRect.height - TabVisualHeight);

            if (curTab == 0)
            {
                DrawScheduleTab(contentRect);
            }
            else
            {
                DrawWorkTab(contentRect);
            }
        }

        public override void Notify_ResolutionChanged()
        {
            base.Notify_ResolutionChanged();
            RebuildTables();
        }

        // ---- Schedule tab ----

        private void DrawScheduleTab(Rect rect)
        {
            scheduleTable.SetDirty();
            scheduleTable.PawnTableOnGUI(new Vector2(rect.x, rect.y));

            // Overlay time assignment grid on top of schedule table (vanilla pattern)
            TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid(
                new Rect(rect.x, rect.y, 191f, TimeSelectorHeight));

            DrawCouponColumn(rect);
        }

        private void DrawCouponColumn(Rect rect)
        {
            var pawns = scheduleTable.PawnsListForReading;
            if (pawns.Count == 0)
            {
                return;
            }

            string label = RimPrisonMod.Settings.WorkCouponName;
            float headerHeight = scheduleTable.HeaderHeight;
            float rowAreaHeight = scheduleTable.Size.y - headerHeight;
            float rowHeight = rowAreaHeight / pawns.Count;

            float colX = rect.x + scheduleTable.Size.x + CouponColumnGap;
            Rect headerRect = new Rect(colX, rect.y, CouponColumnWidth, headerHeight);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(headerRect, label);

            for (int i = 0; i < pawns.Count; i++)
            {
                var comp = pawns[i].TryGetComp<CompWorkTracker>();
                int count = comp?.earnedCoupons ?? 0;
                Rect rowRect = new Rect(colX, rect.y + headerHeight + i * rowHeight,
                    CouponColumnWidth, rowHeight);
                Widgets.Label(rowRect, count.ToString());
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        // ---- Work tab ----

        private void DrawWorkTab(Rect rect)
        {
            workTable.SetDirty();
            workTable.PawnTableOnGUI(new Vector2(rect.x, rect.y));
        }

        // ---- Init ----

        private void BuildTabs()
        {
            tabs = new List<TabRecord>
            {
                new TabRecord("RimPrison.ScheduleTab".Translate(),
                    delegate { curTab = 0; }, () => curTab == 0),
                new TabRecord("RimPrison.WorkTab".Translate(),
                    delegate { curTab = 1; }, () => curTab == 1)
            };
        }

        private void RebuildTables()
        {
            Func<IEnumerable<Pawn>> getter = () => PawnList;
            int w = (int)InitialSize.x - (int)(Margin * 2f);
            int h = (int)(InitialSize.y - TabVisualHeight - Margin * 2f);
            scheduleTable = (PawnTable)Activator.CreateInstance(
                PawnTableDefOf.Restrict.workerClass, PawnTableDefOf.Restrict, getter, w, h);
            workTable = (PawnTable)Activator.CreateInstance(
                PawnTableDefOf.Work.workerClass, PawnTableDefOf.Work, getter, w, h);
        }
    }
}
