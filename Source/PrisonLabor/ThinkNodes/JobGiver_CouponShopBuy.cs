using RimPrison.CouponShop;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.PrisonLabor
{
    // ThinkNode that triggers prisoner shopping via PrisonerShoppingService.
    // Priority-based: staple food → premium food → drugs.
    public class JobGiver_CouponShopBuy : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (!pawn.IsPrisonerOfColony || !PrisonLaborUtility.IsLaborEnabled(pawn))
                return 0f;

            int maxDebt = RimPrisonMod.Settings.MaxDebt;
            var tracker = pawn.TryGetComp<CompWorkTracker>();
            if (tracker == null || tracker.earnedCoupons <= -maxDebt)
                return 0f;

            return 7f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.IsPrisonerOfColony || !PrisonLaborUtility.IsLaborEnabled(pawn))
                return null;

            int maxDebt = RimPrisonMod.Settings.MaxDebt;
            var tracker = pawn.TryGetComp<CompWorkTracker>();
            if (tracker == null || tracker.earnedCoupons <= -maxDebt)
                return null;

            return PrisonerShoppingService.TryGetShoppingJob(pawn);
        }
    }
}
