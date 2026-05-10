using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using RimPrison.Core;

namespace RimPrison
{
    // [UNREVIEWED] main table UI, not carefully reviewed yet
    public class MainTabWindow_Prisoners : MainTabWindow
    {
        private PawnTable scheduleTable;
        private PawnTable workTable;
        private bool sizeInitialized;

        private const float TimeSelectorHeight = 65f;
        private const float TableGap = 0f;
        private const float CouponColumnWidth = 80f;
        private const float CouponColumnGap = 8f;

        protected override float Margin => 6f;

        // [OPTIMIZE] PawnList may have unnecessary cost?
        private List<Pawn> PawnList =>
            Find.CurrentMap.mapPawns.PrisonersOfColony
                .Where(p => p.IsLaborEnabled()).ToList();

        public override Vector2 RequestedTabSize
        {
            get
            {
                if (scheduleTable == null || workTable == null)
                    return Vector2.zero;

                float minW = Mathf.Max(PawnTableDefOf.Restrict.minWidth,
                    PawnTableDefOf.Work.minWidth);
                float w = Mathf.Max(scheduleTable.Size.x + CouponColumnWidth, workTable.Size.x, minW);
                float h = Mathf.Max(scheduleTable.Size.y, 100f)
                    + Mathf.Max(workTable.Size.y, 100f)
                    + TableGap;
                return new Vector2(w + Margin * 2f, h + Margin * 2f);
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            RebuildTables();
            SetInitialSizeAndPosition();
        }

        public override void Notify_ResolutionChanged()
        {
            base.Notify_ResolutionChanged();
            RebuildTables();
            SetInitialSizeAndPosition();
        }

        public override void DoWindowContents(Rect rect)
        {
            if (scheduleTable == null)
            {
                RebuildTables();
                SetInitialSizeAndPosition();
            }

            // Draw schedule table first, then overlay TimeSelector on top (vanilla pattern)
            // [TODO] maybe optimize here, refresh every frame?
            scheduleTable.SetDirty();
            scheduleTable.PawnTableOnGUI(new Vector2(rect.x, rect.y));
            TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid(
                new Rect(rect.x, rect.y, 191f, TimeSelectorHeight));

            // Coupon column: right of schedule table
            DrawCouponColumn(rect);

            // Bottom: work priorities
            float workY = rect.y + scheduleTable.Size.y + TableGap;
            workTable.SetDirty();
            workTable.PawnTableOnGUI(new Vector2(rect.x, workY));

            // Tables only have correct Size after the first PawnTableOnGUI call.
            if (!sizeInitialized && scheduleTable.Size.x > 0f && workTable.Size.x > 0f)
            {
                sizeInitialized = true;
                SetInitialSizeAndPosition();
            }
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

        private void RebuildTables()
        {
            sizeInitialized = false;
            Func<IEnumerable<Pawn>> getter = () => PawnList;
            int w = UI.screenWidth - (int)(Margin * 2f);
            int h = (UI.screenHeight - 35) / 2;
            scheduleTable = (PawnTable)Activator.CreateInstance(
                PawnTableDefOf.Restrict.workerClass, PawnTableDefOf.Restrict, getter, w, h);
            workTable = (PawnTable)Activator.CreateInstance(
                PawnTableDefOf.Work.workerClass, PawnTableDefOf.Work, getter, w, h);
        }
    }
}
