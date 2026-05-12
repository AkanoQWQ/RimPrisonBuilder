using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    // [UNREVIEWED] Just simple review, more review needed
    public static class PrisonDebtHarvestService
    {
        // 30-step harvest order: ears → organs → tail → toes → fingers → feet → palms → heart
        private static readonly string[] HarvestTokenOrder =
        {
            "LeftEar", "RightEar",
            "LeftKidney", "LeftLung",
            "Tail",
            "LeftBigToe", "LeftIndexToe", "LeftMiddleToe", "LeftRingToe", "LeftPinkyToe",
            "RightBigToe", "RightIndexToe", "RightMiddleToe", "RightRingToe", "RightPinkyToe",
            "LeftThumb", "LeftIndexFinger", "LeftMiddleFinger", "LeftRingFinger", "LeftPinky",
            "RightThumb", "RightIndexFinger", "RightMiddleFinger", "RightRingFinger", "RightPinky",
            "LeftFoot", "RightFoot",
            "LeftPalm", "RightPalm",
            "Heart"
        };

        private const float DebtReductionPerHarvest = 100f;
        private const float RewardPerHarvest = 150f;

        // [OPTIMIZE] Cache: pawn thingID → (notMissingParts snapshot, token→part map).
        // Entries for dead/destroyed pawns are cleaned up periodically.
        private static readonly Dictionary<int, CachedParts> partsCache = new Dictionary<int, CachedParts>();
        private static int lastPartsCleanupTick;
        private const int PartsCleanupIntervalTicks = 60000; // 1 day

        private class CachedParts
        {
            public List<BodyPartRecord> NotMissingParts;
            public int PartCount;
            public Dictionary<string, BodyPartRecord> TokenToPart = new Dictionary<string, BodyPartRecord>();
        }

        public static void TryProcessDebtHarvest(Pawn pawn, CompWorkTracker tracker)
        {
            if (pawn == null || pawn.Dead || tracker == null) return;
            if (!pawn.IsPrisonerOfColony || !pawn.Spawned) return;

            // [OPTIMIZE] Periodic cleanup — remove cache entries for dead/destroyed pawns
            int now = Find.TickManager.TicksGame;
            if (now - lastPartsCleanupTick > PartsCleanupIntervalTicks)
            {
                lastPartsCleanupTick = now;
                if (partsCache.Count > 0)
                {
                    var liveSet = new HashSet<int>();
                    foreach (var p in Find.WorldPawns.AllPawnsAliveOrDead)
                        liveSet.Add(p.thingIDNumber);
                    var deadIds = new List<int>();
                    foreach (var kv in partsCache)
                    {
                        if (!PawnExists(kv.Key, liveSet))
                            deadIds.Add(kv.Key);
                    }
                    foreach (var id in deadIds)
                        partsCache.Remove(id);
                }
            }

            int debt = -tracker.earnedCoupons; // positive when in debt
            int threshold = RimPrisonMod.Settings.DebtHarvestThreshold;
            if (threshold <= 0 || debt < threshold) return;

            int cooldownTicks = RimPrisonMod.Settings.DebtHarvestIntervalDays * GenDate.TicksPerDay;
            int ticksSince = Find.TickManager.TicksAbs - tracker.lastDebtHarvestTick;
            if (tracker.lastDebtHarvestTick > 0 && ticksSince < cooldownTicks) return;

            // Keep harvesting while debt >= threshold and parts remain
            int maxIter = 5; // safety cap — don't remove 5+ parts in one cycle
            for (int i = 0; i < maxIter && !pawn.Dead; i++)
            {
                debt = -tracker.earnedCoupons;
                if (debt < threshold) break;

                var part = ResolveNextPart(pawn);
                if (part == null) break;

                if (!ApplyMissingPart(pawn, part)) break;

                // Reduce debt and give reward
                tracker.earnedCoupons += (int)DebtReductionPerHarvest;
                tracker.earnedCoupons += (int)RewardPerHarvest;
                tracker.lastDebtHarvestTick = Find.TickManager.TicksAbs;

                // Spawn organ on ground if defined
                if (part.def.spawnThingOnRemoved != null)
                {
                    var drop = ThingMaker.MakeThing(part.def.spawnThingOnRemoved);
                    GenPlace.TryPlaceThing(drop, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                }

                pawn.Map?.GetComponent<GameComponent_ActivityLog>()?.Log(pawn,
                    "RimPrison.LogDebtHarvest".Translate(part.LabelCap,
                        ((int)DebtReductionPerHarvest).ToString()));
            }

            InvalidateCache(pawn);
        }

        private static BodyPartRecord ResolveNextPart(Pawn pawn)
        {
            var parts = GetNotMissingParts(pawn);
            var cache = GetCache(pawn);

            foreach (var token in HarvestTokenOrder)
            {
                if (cache.TokenToPart.TryGetValue(token, out var cached) && cached != null)
                {
                    if (parts.Contains(cached)) return cached;
                }
                var part = FindPartByToken(parts, token);
                cache.TokenToPart[token] = part;
                if (part != null) return part;
            }
            return null;
        }

        private static BodyPartRecord FindPartByToken(List<BodyPartRecord> parts, string token)
        {
            return token switch
            {
                "LeftEar" => FindSidedPart(parts, "Ear", left: true),
                "RightEar" => FindSidedPart(parts, "Ear", left: false),
                "LeftKidney" => FindSidedPart(parts, "Kidney", left: true),
                "LeftLung" => FindSidedPart(parts, BodyPartDefOf.Lung, left: true),
                "Tail" => FindPartByDefNameToken(parts, "Tail"),
                "LeftBigToe" or "LeftIndexToe" or "LeftMiddleToe" or "LeftRingToe" or "LeftPinkyToe"
                    => FindSidedPartByDefName(parts, "Toe", left: true),
                "RightBigToe" or "RightIndexToe" or "RightMiddleToe" or "RightRingToe" or "RightPinkyToe"
                    => FindSidedPartByDefName(parts, "Toe", left: false),
                "LeftThumb" or "LeftIndexFinger" or "LeftMiddleFinger" or "LeftRingFinger" or "LeftPinky"
                    => FindSidedPartByDefName(parts, "Finger", left: true),
                "RightThumb" or "RightIndexFinger" or "RightMiddleFinger" or "RightRingFinger" or "RightPinky"
                    => FindSidedPartByDefName(parts, "Finger", left: false),
                "LeftFoot" => FindSidedPart(parts, "Foot", left: true),
                "RightFoot" => FindSidedPart(parts, "Foot", left: false),
                "LeftPalm" => FindSidedPart(parts, BodyPartDefOf.Hand, left: true),
                "RightPalm" => FindSidedPart(parts, BodyPartDefOf.Hand, left: false),
                "Heart" => FindOrganByName(parts, "Heart"),
                _ => null
            };
        }

        private static bool ApplyMissingPart(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health == null || part == null) return false;
            if (!GetNotMissingParts(pawn).Contains(part)) return false;

            var hediff = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, part);
            hediff.Part = part;
            hediff.lastInjury = HediffDefOf.SurgicalCut;
            hediff.IsFresh = false;
            pawn.health.AddHediff(hediff, part);

            // Invalidate cache immediately since body part was removed
            InvalidateCache(pawn);
            return true;
        }

        private static List<BodyPartRecord> GetNotMissingParts(Pawn pawn)
        {
            var cache = GetCache(pawn);
            return cache.NotMissingParts;
        }

        private static CachedParts GetCache(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            int count = pawn.health?.hediffSet?.GetNotMissingParts()?.Count() ?? 0;

            if (!partsCache.TryGetValue(id, out var cache) || cache.PartCount != count)
            {
                var list = pawn.health?.hediffSet?.GetNotMissingParts()?.ToList()
                    ?? new List<BodyPartRecord>();
                cache = new CachedParts { NotMissingParts = list, PartCount = list.Count };
                partsCache[id] = cache;
            }
            return cache;
        }

        private static bool PawnExists(int thingID, HashSet<int> liveSet)
        {
            return liveSet.Contains(thingID);
        }

        private static void InvalidateCache(Pawn pawn)
        {
            partsCache.Remove(pawn.thingIDNumber);
        }

        // --- Part matching helpers ---

        private static BodyPartRecord FindSidedPart(List<BodyPartRecord> parts, string defName, bool left)
        {
            var def = DefDatabase<BodyPartDef>.GetNamedSilentFail(defName);
            return def == null ? null : FindSidedPart(parts, def, left);
        }

        private static BodyPartRecord FindSidedPartByDefName(List<BodyPartRecord> parts, string defName, bool left)
        {
            var def = DefDatabase<BodyPartDef>.GetNamedSilentFail(defName);
            return def == null ? null : parts
                .Where(p => p.def == def && (left ? IsLeft(p) : IsRight(p)))
                .FirstOrDefault();
        }

        private static BodyPartRecord FindSidedPart(List<BodyPartRecord> parts, BodyPartDef def, bool left)
        {
            return parts
                .Where(p => p.def == def && (left ? IsLeft(p) : IsRight(p)))
                .FirstOrDefault();
        }

        private static BodyPartRecord FindOrganByName(List<BodyPartRecord> parts, string defName)
        {
            var def = DefDatabase<BodyPartDef>.GetNamedSilentFail(defName);
            return def == null ? null : parts.FirstOrDefault(p => p.def == def);
        }

        private static BodyPartRecord FindPartByDefNameToken(List<BodyPartRecord> parts, string token)
        {
            return parts.FirstOrDefault(p =>
                p.def.defName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsLeft(BodyPartRecord part)
        {
            for (var cur = part; cur != null; cur = cur.parent)
                if (ContainsAny(cur.Label, "左", "left")
                    || ContainsAny(cur.customLabel, "左", "left")
                    || ContainsAny(cur.def?.label, "左", "left"))
                    return true;
            return false;
        }

        private static bool IsRight(BodyPartRecord part)
        {
            for (var cur = part; cur != null; cur = cur.parent)
                if (ContainsAny(cur.Label, "右", "right")
                    || ContainsAny(cur.customLabel, "右", "right")
                    || ContainsAny(cur.def?.label, "右", "right"))
                    return true;
            return false;
        }

        private static bool ContainsAny(string value, string a, string b)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
