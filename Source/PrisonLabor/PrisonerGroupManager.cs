using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    // Inherited from MapComponent, it will auto-load with map
    public class PrisonerGroupManager : MapComponent
    {
        public List<PrisonerGroup> groups = new List<PrisonerGroup>();

        public PrisonerGroupManager(Map map) : base(map) { }

        public PrisonerGroup GetGroupFor(Pawn pawn)
        {
            // WTF AI made this? OK, fine but...
            int id = pawn.thingIDNumber;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].pawnThingIds.Contains(id))
                {
                    return groups[i];
                }
            }
            return null;
        }

        public void SetGroup(Pawn pawn, PrisonerGroup group)
        {
            RemoveFromAllGroups(pawn);
            if (group != null)
            {
                group.AddPawn(pawn);
                ApplyGroupSettings(pawn, group);
            }
        }

        public void RemoveFromAllGroups(Pawn pawn)
        {
            // ? Like my code when I was 12-years-old
            int id = pawn.thingIDNumber;
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].pawnThingIds.Remove(id);
            }
        }

        // Apply group work priorities and schedule to a single pawn.
        // [OPTIMIZE] O(blocks * members * workType)
        public void ApplyGroupSettings(Pawn pawn, PrisonerGroup group)
        {
            if (pawn.workSettings == null || pawn.timetable == null)
                return;
            if (group.workPriorities == null)
                group.InitDefaults();

            foreach (var wt in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(wt))
                    pawn.workSettings.SetPriority(wt, group.GetPriority(wt));
            }

            for (int h = 0; h < 24; h++)
            {
                pawn.timetable.SetAssignment(h, group.GetAssignment(h));
            }
        }

        // Sync a single work priority from group to all members.
        public void SyncWorkPriority(PrisonerGroup group, WorkTypeDef wt, int priority)
        {
            Map map = this.map;
            if (map == null) return;

            var blockedPawnNames = new List<string>();
            // Oh no iterator-removing in foreach, reasonable
            for (int i = group.pawnThingIds.Count - 1; i >= 0; i--)
            {
                Pawn pawn = FindPawnById(map, group.pawnThingIds[i]);
                if (pawn == null)
                {
                    group.pawnThingIds.RemoveAt(i); // Clean up stale ID
                    continue;
                }
                if (pawn.workSettings != null)
                {
                    if (!pawn.WorkTypeIsDisabled(wt))
                    {
                        pawn.workSettings.SetPriority(wt, priority);
                    }
                    else
                    {
                        blockedPawnNames.Add(pawn.LabelShortCap);
                    }
                }
            }
            if (blockedPawnNames.Count > 0)
            {
                Messages.Message(
                    "RimPrison.WorkPriorityBlocked".Translate(
                        blockedPawnNames.ToCommaList(useAnd: true), wt.labelShort),
                    MessageTypeDefOf.CautionInput, historical: false);
            }
        }

        // Apply group settings to ALL members of a group.
        public void SyncGroupToAllPawns(PrisonerGroup group)
        {
            Map map = this.map;
            if (map == null) return;

            for (int i = group.pawnThingIds.Count - 1; i >= 0; i--)
            {
                Pawn pawn = FindPawnById(map, group.pawnThingIds[i]);
                if (pawn != null)
                {
                    ApplyGroupSettings(pawn, group);
                }
                else
                {
                    // Pawn no longer exists, clean up
                    group.pawnThingIds.RemoveAt(i);
                }
            }
        }

        private static Pawn FindPawnById(Map map, int thingId)
        {
            foreach (var pawn in map.mapPawns.PrisonersOfColony)
            {
                if (pawn.thingIDNumber == thingId)
                    return pawn;
            }
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
        }
    }
}
