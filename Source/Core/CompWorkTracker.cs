using RimWorld;
using Verse;

namespace RimPrison.Core
{
    public class CompProperties_WorkTracker : CompProperties
    {
        public CompProperties_WorkTracker()
        {
            compClass = typeof(CompWorkTracker);
        }
    }

    public class CompWorkTracker : ThingComp
    {
        public int earnedCoupons;
        private int workTickCounter;

        private const int TicksPerCoupon = GenDate.TicksPerHour;

        public CompProperties_WorkTracker Props => (CompProperties_WorkTracker)props;

        public void Notify_WorkTick()
        {
            workTickCounter++;
            if (workTickCounter >= TicksPerCoupon)
            {
                workTickCounter -= TicksPerCoupon;
                earnedCoupons++;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref earnedCoupons, "earnedCoupons");
            Scribe_Values.Look(ref workTickCounter, "workTickCounter");
        }
    }
}
