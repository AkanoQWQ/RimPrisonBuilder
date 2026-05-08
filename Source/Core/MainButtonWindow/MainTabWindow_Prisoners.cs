using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using RimPrison.Core;

namespace RimPrison
{
    public class MainTabWindow_Prisoners : MainTabWindow
    {
        private PawnTable scheduleTable;
        private PawnTable workTable;
        private bool sizeInitialized;

        private const float TimeSelectorHeight = 65f;
        private const float TableGap = 0f;

        protected override float Margin => 6f;

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
                float w = Mathf.Max(scheduleTable.Size.x, workTable.Size.x, minW);
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
