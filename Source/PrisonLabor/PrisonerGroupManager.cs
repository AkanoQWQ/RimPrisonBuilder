using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    // Inherited from MapComponent, it will auto-load with map
    public class PrisonerGroupManager : MapComponent
    {
        private static readonly AccessTools.FieldRef<Pawn_WorkSettings, DefMap<WorkTypeDef, int>>
            PrioritiesField = AccessTools.FieldRefAccess<Pawn_WorkSettings, DefMap<WorkTypeDef, int>>("priorities");

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
                GetLog()?.Log(pawn, "RimPrison.LogAssignedToGroup".Translate(group.name));
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

        // Apply group settings to a single pawn.
        // NOTE: The early return for missing workSettings/timetable was
        // preventing apparel/drug/food policies from being applied to
        // babies and children who lack work/timetable components.
        // Work and schedule guards are now per-section. Don't re-merge them.
        // [OPTIMIZE] O(blocks * members * workType)
        // I am not sure if it's OK to create with pawn.setting==null here
        public void ApplyGroupSettings(Pawn pawn, PrisonerGroup group)
        {
            if (group.workPriorities == null)
                group.InitDefaults();

            // Work priorities — create workSettings if missing
            pawn.workSettings ??= new Pawn_WorkSettings(pawn);
            foreach (var wt in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(wt))
                    pawn.workSettings.SetPriority(wt, group.GetPriority(wt));
            }

            // Schedule — create timetable if missing
            pawn.timetable ??= new Pawn_TimetableTracker(pawn);
            for (int h = 0; h < 24; h++)
                pawn.timetable.SetAssignment(h, group.GetAssignment(h));

            // Apparel / drug / food policies — create trackers if needed
            if (group.apparelPolicy != null)
            {
                pawn.outfits ??= new Pawn_OutfitTracker(pawn);
                pawn.outfits.CurrentApparelPolicy = group.apparelPolicy;
            }
            if (group.drugPolicy != null)
            {
                pawn.drugs ??= new Pawn_DrugPolicyTracker(pawn);
                pawn.drugs.CurrentPolicy = group.drugPolicy;
            }
            if (group.foodRestriction != null)
            {
                pawn.foodRestriction ??= new Pawn_FoodRestrictionTracker(pawn);
                pawn.foodRestriction.CurrentFoodPolicy = group.foodRestriction;
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
                pawn.workSettings ??= new Pawn_WorkSettings(pawn);
                PrioritiesField(pawn.workSettings) ??= new DefMap<WorkTypeDef, int>();
                if (!pawn.WorkTypeIsDisabled(wt))
                {
                    pawn.workSettings.SetPriority(wt, priority);
                }
                else
                {
                    blockedPawnNames.Add(pawn.LabelShortCap);
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

        private GameComponent_ActivityLog GetLog() => map?.GetComponent<GameComponent_ActivityLog>();

        public void SyncOutfitPolicy(PrisonerGroup group)
        {
            Map map = this.map;
            if (map == null) return;
            for (int i = group.pawnThingIds.Count - 1; i >= 0; i--)
            {
                Pawn pawn = FindPawnById(map, group.pawnThingIds[i]);
                if (pawn != null && group.apparelPolicy != null)
                {
                    pawn.outfits ??= new Pawn_OutfitTracker(pawn);
                    pawn.outfits.CurrentApparelPolicy = group.apparelPolicy;
                }
            }
            GetLog()?.Log(group.name, "RimPrison.LogOutfitChanged".Translate());
        }

        public void SyncDrugPolicy(PrisonerGroup group)
        {
            Map map = this.map;
            if (map == null) return;
            for (int i = group.pawnThingIds.Count - 1; i >= 0; i--)
            {
                Pawn pawn = FindPawnById(map, group.pawnThingIds[i]);
                if (pawn != null && group.drugPolicy != null)
                {
                    pawn.drugs ??= new Pawn_DrugPolicyTracker(pawn);
                    pawn.drugs.CurrentPolicy = group.drugPolicy;
                }
            }
        }

        public void SyncFoodRestriction(PrisonerGroup group)
        {
            Map map = this.map;
            if (map == null) return;
            for (int i = group.pawnThingIds.Count - 1; i >= 0; i--)
            {
                Pawn pawn = FindPawnById(map, group.pawnThingIds[i]);
                if (pawn != null && group.foodRestriction != null)
                {
                    pawn.foodRestriction ??= new Pawn_FoodRestrictionTracker(pawn);
                    pawn.foodRestriction.CurrentFoodPolicy = group.foodRestriction;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
        }
    }
}
