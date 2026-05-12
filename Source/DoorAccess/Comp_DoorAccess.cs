using RimWorld;
using Verse;

namespace RimPrison.DoorAccess
{
    // What a simple component!
    // Simple per-door toggle: allow prisoners through this door.
    public class Comp_DoorAccess : ThingComp
    {
        public bool allowPrisoners;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref allowPrisoners, "allowPrisoners");
        }
    }

    public class CompProperties_DoorAccess : CompProperties
    {
        public CompProperties_DoorAccess()
        {
            compClass = typeof(Comp_DoorAccess);
        }
    }
}
