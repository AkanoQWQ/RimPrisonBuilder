using System.Collections.Generic;
using RimPrisonBuilder.CouponShop;
using RimPrisonBuilder.DefOfs;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimPrisonBuilder.PrisonLabor
{
    public class WorkGiver_CouponShopStore : WorkGiver_Scanner
    {
        // Tell the work system we care about haulable items on the ground.
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.HaulableAlways);
        // Pawn just needs to touch the item to pick it up. No interaction cell needed.
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;
        // Pawns can path through dangerous areas to reach the item (safe for non-player pawns).
        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var shops = GetShopsWithSpace(pawn.Map);
            if (shops.Count == 0)
            {
                yield break;
            }

            // [OPTIMIZE]
            var allowedDefs = new HashSet<ThingDef>();
            foreach (var shop in shops)
            {
                var filter = shop.CouponComp?.Filter;
                if (filter == null) continue;
                foreach (var def in filter.AllowedThingDefs)
                {
                    allowedDefs.Add(def);
                }
            }

            if (allowedDefs.Count == 0)
            {
                yield break;
            }

            foreach (var thing in pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling())
            {
                if (thing.IsForbidden(pawn))
                {
                    continue;
                }
                if (pawn.carryTracker.AvailableStackSpace(thing.def) == 0)
                {
                    continue;
                }
                if (allowedDefs.Contains(thing.def))
                {
                    yield return thing;
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t))
            {
                return false;
            }
            return FindShopFor(pawn, t) != null;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t))
            {
                return null;
            }

            var shop = FindShopFor(pawn, t);
            if (shop == null)
            {
                return null;
            }

            var comp = shop.CouponComp;
            int shopSpace = comp != null ? comp.Capacity - comp.stockCount : 1;
            int canCarry = pawn.carryTracker.AvailableStackSpace(t.def);
            if (canCarry <= 0)
            {
                return null;
            }
            Job job = JobMaker.MakeJob(RP_JobDefOf.RimPrisonBuilder_TakeToCouponShop, t, shop);
            job.count = Mathf.Min(shopSpace, canCarry, t.stackCount);
            job.haulOpportunisticDuplicates = false;
            job.haulMode = HaulMode.ToCellNonStorage;
            return job;
        }

        private static List<Building_CouponShop> GetShopsWithSpace(Map map)
        {
            var result = new List<Building_CouponShop>();
            var candidates = map.listerBuildings.AllBuildingsColonistOfClass<Building_CouponShop>();
            foreach (var shop in candidates)
            {
                if (shop.HasSpace)
                {
                    result.Add(shop);
                }
            }
            return result;
        }

        private static Building_CouponShop FindShopFor(Pawn pawn, Thing item)
        {
            var shops = GetShopsWithSpace(pawn.Map);
            Building_CouponShop best = null;
            float bestDist = 0f;
            // Find the closest shop now
            foreach (var shop in shops)
            {
                if (!shop.Accepts(item) || !pawn.CanReserve(shop))
                {
                    continue;
                }
                float dist = shop.Position.DistanceToSquared(item.Position);
                if (best == null || dist < bestDist)
                {
                    best = shop;
                    bestDist = dist;
                }
            }
            return best;
        }
    }
}
