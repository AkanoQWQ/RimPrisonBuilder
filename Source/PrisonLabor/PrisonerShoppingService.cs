using System;
using System.Collections.Generic;
using RimPrison.CouponShop;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.PrisonLabor
{
    public static class PrisonerShoppingService
    {
        // Map-level cache: map.uniqueID → cached shop items
        private static readonly Dictionary<int, CachedShopData> mapCache = new Dictionary<int, CachedShopData>();
        private const int CacheRefreshIntervalTicks = 2000;

        // [TODO] Change it into a game level throttle
        // Per-pawn log throttle: pawn.thingID → last log tick
        private static readonly Dictionary<int, int> noFoodLogThrottle = new Dictionary<int, int>();
        private const int NoFoodLogIntervalTicks = 60000; // 1 day

        // baselineRatio : best nutrition/price among food
        // used in future AI
        private class CachedShopData
        {
            public int cachedTick;
            public List<ShopItem> items = new List<ShopItem>();
            public float baselineRatio;   
            public bool hasAnyFood;
        }

        public struct ShopItem
        {
            public Building_CouponShop shop;
            public ThingDef itemDef;
            public int price;
            public float nutrition; // 0 for non-food
            public FoodPreferability preferability;
            public bool isDrug;
        }

        public static Job TryGetShoppingJob(Pawn pawn)
        {
            if (!pawn.IsPrisonerOfColony || !PrisonLaborUtility.IsLaborEnabled(pawn))
                return null;
            if (pawn.Map == null) return null;

            var data = RefreshCache(pawn.Map);

            float dailyCost = CalculateDailyMealCost(pawn, data);
            float invNutrition = GetInventoryNutrition(pawn);
            float twoDayNeed = GetDailyNutritionNeed(pawn) * 2f;
            int balance = GetBalance(pawn);
            int maxDebt = RimPrisonMod.Settings.MaxDebt;

            var thoughts = pawn.TryGetComp<CompPrisonPawn>();

            // 0. No food at all (no shops, or no food items in any shop). Must be
            //    before data.items.Count==0 so we catch the "no shops on map" case.
            if (!data.hasAnyFood)
            {
                ThrottleNoFoodLog(pawn);
                thoughts?.RecordThought("RimPrison.ThoughtNoFood".Translate());
                return null;
            }
            // Complain if income can't cover food cost
            float smoothedIncome = thoughts?.smoothedDailyIncome ?? 0f;
            if (smoothedIncome > 0f && smoothedIncome < dailyCost * 0.7f)
                thoughts?.RecordThought("RimPrison.ThoughtIncomeTooLow".Translate());

            // 1. Basic food: backpack nutrition < 2-day need
            if (invNutrition < twoDayNeed)
            {
                var staple = FindBestStapleFood(data, pawn);
                // Firstly try not to run into debt if have any food
                // Only run into debt if don't have any food
                if (staple.HasValue && staple.Value.price <= balance)
                {
                    return MakeShoppingJob(pawn, staple.Value.shop);
                }
                else if (staple.HasValue && staple.Value.price <= balance + maxDebt && invNutrition <= 0f)
                {
                    return MakeShoppingJob(pawn, staple.Value.shop);
                }
                
                return null;
            }

            // 2. Premium food: nutrition met, money > dailyCost × 3
            if (balance > dailyCost * 3)
            {
                var premium = FindBestPremiumFood(data, pawn);
                if (premium.HasValue && premium.Value.price <= balance)
                {
                    thoughts?.RecordThought("RimPrison.ThoughtPremiumFood".Translate(premium.Value.itemDef.label));
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(DefOfs.RP_ThoughtDefOf.RPR_BoughtPremiumFood);
                    return MakeShoppingJob(pawn, premium.Value.shop);
                }
            }

            // 3. Drugs: money > dailyCost × 5
            // Don't buy if already hoarding — they can't consume them fast enough.
            if (balance > dailyCost * 5 && CountDrugsInInventory(pawn) < 5)
            {
                var drug = FindRandomAllowedDrug(data, pawn);
                // Of cource not run into debt in this case!
                if (drug.HasValue && drug.Value.price <= balance)
                {
                    thoughts?.RecordThought("RimPrison.ThoughtBuyDrug".Translate(drug.Value.itemDef.label));
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(DefOfs.RP_ThoughtDefOf.RPR_BoughtDrug);
                    return MakeShoppingJob(pawn, drug.Value.shop);
                }
            }

            return null;
        }

        // --- Cache ---
        // This should be a hot logic in this game
        // So optimization here shuold be very important

        private static CachedShopData RefreshCache(Map map)
        {
            int id = map.uniqueID;
            if (mapCache.TryGetValue(id, out var data)
                && Find.TickManager.TicksGame - data.cachedTick < CacheRefreshIntervalTicks)
                return data;

            data = new CachedShopData { cachedTick = Find.TickManager.TicksGame };
            var shops = map.listerBuildings.AllBuildingsColonistOfClass<Building_CouponShop>();

            foreach (var shop in shops)
            {
                if (!shop.Spawned || !shop.HasStock || shop.PricePerItem <= 0)
                    continue;

                var comp = shop.GetComp<CompCouponShop>();
                var itemDef = comp?.storedItemDef ?? comp?.Filter?.AnyAllowedDef;
                if (itemDef == null) continue;

                var si = new ShopItem
                {
                    shop = shop,
                    itemDef = itemDef,
                    price = shop.PricePerItem,
                    isDrug = itemDef.IsDrug,
                };

                if (itemDef.IsNutritionGivingIngestible && itemDef.ingestible != null)
                {
                    si.nutrition = itemDef.ingestible.CachedNutrition;
                    si.preferability = itemDef.ingestible.preferability;
                }

                data.items.Add(si);
            }

            // Calculate baseline: highest nutrition/price among foods
            data.baselineRatio = 0f;
            data.hasAnyFood = false;
            foreach (var si in data.items)
            {
                if (si.nutrition > 0f && si.price > 0)
                {
                    data.hasAnyFood = true;
                    float ratio = si.nutrition / si.price;
                    if (ratio > data.baselineRatio)
                        data.baselineRatio = ratio;
                }
            }

            mapCache[id] = data;
            return data;
        }

        // --- Nutrition helpers ---

        private static float GetDailyNutritionNeed(Pawn pawn)
        {
            if (pawn.needs?.food == null) return 1.6f;
            return pawn.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * 60000f;
        }

        private static float GetInventoryNutrition(Pawn pawn)
        {
            float total = 0f;
            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing.def.IsNutritionGivingIngestible && pawn.WillEat(thing.def))
                    total += thing.def.ingestible.CachedNutrition * thing.stackCount;
            }
            return total;
        }

        private static int CountDrugsInInventory(Pawn pawn)
        {
            int count = 0;
            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing.def.IsDrug)
                    count += thing.stackCount;
            }
            return count;
        }

        private static float CalculateDailyMealCost(Pawn pawn, CachedShopData data)
        {
            if (data.baselineRatio <= 0f) return float.MaxValue;
            return GetDailyNutritionNeed(pawn) / data.baselineRatio;
        }

        // Public wrapper for external use (DailyAllowance thoughts).
        // Returns float.MaxValue if no shops or no food on the map.
        public static float GetDailyMealCost(Pawn pawn)
        {
            if (pawn.Map == null) return float.MaxValue;
            var data = RefreshCache(pawn.Map);
            if (data.baselineRatio <= 0f) return float.MaxValue;
            return GetDailyNutritionNeed(pawn) / data.baselineRatio;
        }

        private static int GetBalance(Pawn pawn)
        {
            return pawn.TryGetComp<CompWorkTracker>()?.earnedCoupons ?? 0;
        }

        // --- Item selection ---

        private static ShopItem? FindBestStapleFood(CachedShopData data, Pawn pawn)
        {
            ShopItem? best = null;
            float bestRatio = 0f;
            foreach (var si in data.items)
            {
                if (si.nutrition <= 0f) continue;
                if (!pawn.WillEat(si.itemDef)) continue;
                float ratio = si.nutrition / si.price;
                if (ratio > bestRatio)
                {
                    bestRatio = ratio;
                    best = si;
                }
            }
            return best;
        }

        private static ShopItem? FindBestPremiumFood(CachedShopData data, Pawn pawn)
        {
            // Scan from highest preferability down, return first affordable option
            // Actually should choose here... anyway!
            for (int pref = (int)FoodPreferability.MealLavish;
                 pref >= (int)FoodPreferability.MealFine; pref--)
            {
                foreach (var si in data.items)
                {
                    if (si.nutrition <= 0f) continue;
                    if ((int)si.preferability != pref) continue;
                    if (!pawn.WillEat(si.itemDef)) continue;
                    return si;
                }
            }
            return null;
        }

        private static ShopItem? FindRandomAllowedDrug(CachedShopData data, Pawn pawn)
        {
            var allowed = new List<ShopItem>();
            var policy = pawn.drugs?.CurrentPolicy;
            foreach (var si in data.items)
            {
                if (!si.isDrug) continue;
                if (policy != null)
                {
                    var entry = policy[si.itemDef];
                    if (entry == null || !entry.allowedForJoy)
                        continue;
                }
                if (pawn.IsTeetotaler() && si.itemDef.IsNonMedicalDrug)
                    continue;
                allowed.Add(si);
            }
            return allowed.Count > 0 ? allowed.RandomElement() : (ShopItem?)null;
        }

        // --- Logging ---

        private static void ThrottleNoFoodLog(Pawn pawn)
        {
            int tick = Find.TickManager.TicksGame;
            if (noFoodLogThrottle.TryGetValue(pawn.thingIDNumber, out int last)
                && tick - last < NoFoodLogIntervalTicks)
                return;
            noFoodLogThrottle[pawn.thingIDNumber] = tick;

            pawn.Map?.GetComponent<GameComponent_ActivityLog>()?.Log(pawn,
                "RimPrison.LogNoFood".Translate());
        }

        // --- Job creation ---

        private static Job MakeShoppingJob(Pawn pawn, Building_CouponShop shop)
        {
            // Pre-check: shop may have been destroyed/deconstructed since cache refresh,
            // stock may be depleted, or another prisoner may have reserved it.
            if (shop == null || !shop.HasStock) return null;
            if (!pawn.CanReserve(shop)) return null;
            return JobMaker.MakeJob(DefOfs.RP_JobDefOf.RimPrison_BuyFromCouponShop, shop);
        }
    }
}
