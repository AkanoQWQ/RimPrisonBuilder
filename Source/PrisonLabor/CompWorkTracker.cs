using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrisonBuilder.PrisonLabor
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

        public CompProperties_WorkTracker Props => (CompProperties_WorkTracker)props;

        private int TicksPerCoupon
        {
            get
            {
                float rate = RimPrisonBuilderMod.Settings.CouponsPerHour;
                if (rate <= 0f) rate = 1f;
                int tpc = Mathf.RoundToInt(GenDate.TicksPerHour / rate);
                return tpc < 1 ? 1 : tpc;
            }
        }

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
