using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimPrisonBuilder.PrisonLabor
{
    // [NOTE] I don't think AI wrote elegant code for PrisonerGroup and .Manager
    // Maybe reconstruct in the future, only for code cleanliness obsession
    public class PrisonerGroup : IExposable, IRenameable
    {
        public string name;
        public List<int> pawnThingIds = new List<int>();
        public DefMap<WorkTypeDef, int> workPriorities;
        public List<TimeAssignmentDef> times;
        public ApparelPolicy apparelPolicy;
        public DrugPolicy drugPolicy;
        public FoodPolicy foodRestriction;

        // IRenameable
        public string RenamableLabel { get => name; set => name = value; }
        public string BaseLabel => name;
        public string InspectLabel => name;

        public PrisonerGroup() { name = ""; }
        public PrisonerGroup(string name)
        {
            this.name = name;
            InitDefaults();
        }

        public void InitDefaults()
        {
            workPriorities = new DefMap<WorkTypeDef, int>();
            workPriorities.SetAll(0);
            times = new List<TimeAssignmentDef>(24);
            for (int i = 0; i < 24; i++)
            {
                TimeAssignmentDef def;
                if (i <= 5 || i >= 22)
                    def = TimeAssignmentDefOf.Sleep;
                else if (i <= 8 || (i >= 12 && i <= 18))
                    def = TimeAssignmentDefOf.Work;
                else
                    def = TimeAssignmentDefOf.Anything;
                times.Add(def);
            }
        }

        // Four defensive functions
        public int GetPriority(WorkTypeDef w)
        {
            if (workPriorities == null)
                InitDefaults();
            return workPriorities[w];
        }
        public void SetPriority(WorkTypeDef w, int priority)
        {
            if (workPriorities == null)
                InitDefaults();
            workPriorities[w] = priority;
        }
        public TimeAssignmentDef GetAssignment(int hour)
        {
            if (times == null)
                InitDefaults();
            return times[hour];
        }
        public void SetAssignment(int hour, TimeAssignmentDef ta)
        {
            if (times == null)
                InitDefaults();
            times[hour] = ta;
        }

        // Some simple contianer functions
        public bool Contains(Pawn pawn)
        {
            return pawnThingIds.Contains(pawn.thingIDNumber);
        }
        public void AddPawn(Pawn pawn)
        {
            if (!pawnThingIds.Contains(pawn.thingIDNumber))
            {
                pawnThingIds.Add(pawn.thingIDNumber);
            }
        }
        public void RemovePawn(Pawn pawn)
        {
            pawnThingIds.Remove(pawn.thingIDNumber);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", "");
            Scribe_Collections.Look(ref pawnThingIds, "pawnThingIds", LookMode.Value);
            Scribe_Deep.Look(ref workPriorities, "workPriorities");
            Scribe_Collections.Look(ref times, "times", LookMode.Def);
            Scribe_References.Look(ref apparelPolicy, "apparelPolicy");
            Scribe_References.Look(ref drugPolicy, "drugPolicy");
            Scribe_References.Look(ref foodRestriction, "foodRestriction");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (workPriorities == null)
                    workPriorities = new DefMap<WorkTypeDef, int>();
                if (times == null)
                    InitDefaults();
            }
        }
    }
}
