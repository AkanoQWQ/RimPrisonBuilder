using System.Collections.Generic;
using RimPrison.DefOfs;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimPrison.CouponShop
{
    public class JobDriver_TakeToCouponShop : JobDriver
    {
        private const TargetIndex ItemInd = TargetIndex.A;
        private const TargetIndex ShopInd = TargetIndex.B;

        private Thing Item => job.GetTarget(ItemInd).Thing;
        private Building_CouponShop Shop => (Building_CouponShop)job.GetTarget(ShopInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Item, job, 1, job.count, null, errorOnFailed))
            {
                return false;
            }
            if (!pawn.Reserve(Shop, job, 1, -1, null, errorOnFailed))
            {
                // stackCount=-1 to reserve whole building here
                return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(ItemInd);
            this.FailOnDespawnedOrNull(ShopInd);
            this.FailOn(() => !Shop.Accepts(Item));
            this.FailOn(() => !Shop.HasSpace);
            this.FailOn(() => pawn.carryTracker.AvailableStackSpace(Item.def) == 0);

            // Goto item and pickup item
            // IMPORTANT: Use custom StartCarry to fix 10-job bug here
            // This avoid FailOnSomeonePhysicallyInteracting of Toils_Haul.StartCarryThing
            // Which leads to infinite job-loop
            // [TODO] Completely fix 10-job bug (Still appears sometimes)
            yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch);
            yield return StartCarry(ItemInd);
            // Goto shop interaction cell
            yield return Toils_Goto.GotoCell(ShopInd, PathEndMode.InteractionCell);
            // Deposit item into shop
            yield return DepositItem();
        }

        private static Toil StartCarry(TargetIndex index)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing thing = curJob.GetTarget(index).Thing;
                int count = Mathf.Min(curJob.count, actor.carryTracker.AvailableStackSpace(thing.def));
                count = Mathf.Min(count, thing.stackCount);
                if (count <= 0)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                Thing taken = thing.SplitOff(count);
                if (taken == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                int added = actor.carryTracker.innerContainer.TryAdd(taken, count);
                if (added <= 0)
                {
                    if (taken != thing)
                    {
                        taken.Destroy();
                    }
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                // O(Count of all items in storage area) complexity
                // Around 0.1 ms for 20-pawns colony
                actor.MapHeld.resourceCounter.UpdateResourceCounts();
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private Toil DepositItem()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null)
                {
                    return;
                }
                var comp = Shop.CouponComp;
                if (comp == null)
                {
                    return;
                }
                int count = carried.stackCount;
                pawn.carryTracker.innerContainer.Remove(carried);
                carried.Destroy();
                // carried.def is still accessible after Destroy() in current RimWorld,
                // but if a future version changes Destroy() to null fields, this will NRE.
                if (comp.stockCount == 0)
                {
                    comp.storedItemDef = carried.def;
                }
                comp.stockCount += count;
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
