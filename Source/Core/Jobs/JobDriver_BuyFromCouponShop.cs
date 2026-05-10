using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.Core
{
    public class JobDriver_BuyFromCouponShop : JobDriver
    {
        private const TargetIndex ShopInd = TargetIndex.A;

        private Building_CouponShop Shop => (Building_CouponShop)job.GetTarget(ShopInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // All vanilla job use stackCount=-1 here, which means reserve the whole building
            return pawn.Reserve(Shop, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Some FailOn condition
            this.FailOnDespawnedOrNull(ShopInd);
            this.FailOn(() => !Shop.HasStock);
            this.FailOn(() =>
            {
                var tracker = pawn.TryGetComp<CompWorkTracker>();
                return tracker == null || tracker.earnedCoupons < Shop.PricePerItem;
            });

            // Goto shop,buy item,brief wait
            yield return Toils_Goto.GotoCell(ShopInd, PathEndMode.InteractionCell);
            yield return BuyItem();
            yield return Toils_General.Wait(30);
        }

        private Toil BuyItem()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                var comp = Shop.CouponComp;
                var tracker = pawn.TryGetComp<CompWorkTracker>();
                // These guards mirror the FailOn conditions above and should never trigger.
                // Kept as a defensive safety net.
                if (comp == null || tracker == null || comp.stockCount <= 0
                    || tracker.earnedCoupons < comp.pricePerItem)
                {
                    return;
                }

                ThingDef itemDef = comp.storedItemDef ?? comp.Filter?.AnyAllowedDef;
                if (itemDef == null)
                {
                    return;
                }

                // Simply buying logic
                Thing item = ThingMaker.MakeThing(itemDef);
                item.stackCount = 1;
                bool addingSuccess = pawn.inventory.innerContainer.TryAdd(item);
                // Add success condition to fix bug made by AI. This is why you need code review!
                if (addingSuccess)
                {
                    tracker.earnedCoupons -= comp.pricePerItem;
                    comp.stockCount--;
                    if (comp.stockCount == 0)
                    {
                        comp.storedItemDef = null;
                    }
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
