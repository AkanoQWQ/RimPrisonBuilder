using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.PrisonLabor
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
        public int lastDebtHarvestTick;
        private float partialCoupon;
        // workTickCounter removed — now uses fractional accumulation
        // so per-work-type wage multipliers apply precisely.

        public CompProperties_WorkTracker Props => (CompProperties_WorkTracker)props;

        public void Notify_WorkTick(string workTypeDefName)
        {
            float wage = RimPrisonMod.Settings.GetWorkTypeWage(workTypeDefName);
            float mult = RimPrisonMod.Settings.GlobalWageMultiplier * wage;
            if (mult <= 0f) mult = 0.1f;

            partialCoupon += mult / GenDate.TicksPerHour;
            if (partialCoupon >= 1f)
            {
                int add = Mathf.FloorToInt(partialCoupon);
                earnedCoupons += add;
                partialCoupon -= add;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;

            if (parent is Pawn pawn && pawn.IsPrisonerOfColony)
            {
                yield return new Gizmo_Balance(pawn, this);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref earnedCoupons, "earnedCoupons");
            Scribe_Values.Look(ref partialCoupon, "partialCoupon");
            Scribe_Values.Look(ref lastDebtHarvestTick, "lastDebtHarvestTick");
        }
    }
}
