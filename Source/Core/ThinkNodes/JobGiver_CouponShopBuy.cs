using RimPrison.DefOfs;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.Core
{
    public class JobGiver_CouponShopBuy : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            if (!pawn.IsPrisonerOfColony || !PrisonLaborUtility.IsLaborEnabled(pawn))
            {
                return 0f;
            }
            var tracker = pawn.TryGetComp<CompWorkTracker>();
            if (tracker == null || tracker.earnedCoupons <= 0)
            {
                return 0f;
            }
            return 7f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.IsPrisonerOfColony || !PrisonLaborUtility.IsLaborEnabled(pawn))
            {
                return null;
            }

            var tracker = pawn.TryGetComp<CompWorkTracker>();
            if (tracker == null || tracker.earnedCoupons <= 0)
            {
                return null;
            }

            var map = pawn.Map;
            var shops = map.listerBuildings.AllBuildingsColonistOfClass<Building_CouponShop>();
            Building_CouponShop bestShop = null;
            // Current logic: Use the closest available shop
            float bestDist = 0f;
            foreach (var shop in shops)
            {
                // defensive protection
                if (!shop.Spawned || shop.Map != map || shop.PricePerItem <= 0)
                {
                    continue;
                }
                if (!shop.HasStock)
                {
                    continue;
                }
                // Can't afford QWQ
                if (tracker.earnedCoupons < shop.PricePerItem)
                {
                    continue;
                }
                if (!pawn.CanReserve(shop))
                {
                    continue;
                }
                if (!pawn.CanReach((LocalTargetInfo)shop, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    continue;
                }
                // Choose the closest shop for now
                float dist = shop.Position.DistanceToSquared(pawn.Position);
                if (bestShop == null || dist < bestDist)
                {
                    bestShop = shop;
                    bestDist = dist;
                }
            }

            if (bestShop == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(RP_JobDefOf.RimPrison_BuyFromCouponShop, bestShop);
            return job;
        }
    }
}
