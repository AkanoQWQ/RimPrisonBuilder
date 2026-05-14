using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace RimPrison.API
{
    // [UNREVIEWED] Have not reviewed every single detail
    // WARNING: API port is NOT STABLE recently!
    // ============================================================
    // Enums — ported from old RimPrisonReset namespace
    // ============================================================

    public enum RimPrisonApiRuleDecision
    {
        Unhandled = 0,
        Allow = 1,
        Deny = 2
    }

    public enum PrisonCultureRegime
    {
        Harsh = 0,
        Deterrence = 1,
        Equality = 2
    }

    public enum PrisonTimeAssignment
    {
        Sleep = 0,
        Recreation = 1,
        Labor = 2,
        Anything = 3
    }

    public enum PrisonScheduleAxisKind
    {
        Main = 0,
        Child = 1,
        Baby = 2
    }

    // ============================================================
    // Value types (structs)
    // ============================================================

    public readonly struct RimPrisonApiRuleResult
    {
        public static readonly RimPrisonApiRuleResult Unhandled = new(RimPrisonApiRuleDecision.Unhandled);
        public static readonly RimPrisonApiRuleResult Allow = new(RimPrisonApiRuleDecision.Allow);
        public static readonly RimPrisonApiRuleResult Deny = new(RimPrisonApiRuleDecision.Deny);

        public RimPrisonApiRuleResult(RimPrisonApiRuleDecision decision, float value = 0f, string reason = null)
        {
            Decision = decision;
            Value = value;
            Reason = reason;
        }

        public RimPrisonApiRuleDecision Decision { get; }
        public float Value { get; }
        public string Reason { get; }
        public bool Handled => Decision != RimPrisonApiRuleDecision.Unhandled;
    }

    public struct PrisonSuppressionSnapshot
    {
        public float suppression;
        public int prisonerCount;
        public int adultCount;
        public int childCount;
        public int babyCount;
        public float averageMood;
        public float averageHealth;
        public float guardFactor;
        public float turretFactor;
        public float prisonerFactor;
        public float moodFactor;
        public float healthFactor;
        public float regimeModifier;
        public float difficultyModifier;
        public int guardCount;
        public int turretCount;
        public PrisonSuppressionThresholds thresholds;
    }

    public struct PrisonSuppressionThresholds
    {
        public float mentalBreakThreshold;
        public float prisonBreakThreshold;
    }

    public readonly struct RimPrisonCultureSnapshot
    {
        public RimPrisonCultureSnapshot(PrisonCultureRegime regime, bool cultureEffectsEnabled,
            bool wardenSystemEnabled, bool hasPrisonMeme, bool hasWardenPrecept)
        {
            Regime = regime;
            CultureEffectsEnabled = cultureEffectsEnabled;
            WardenSystemEnabled = wardenSystemEnabled;
            HasPrisonMeme = hasPrisonMeme;
            HasWardenPrecept = hasWardenPrecept;
        }

        public PrisonCultureRegime Regime { get; }
        public bool CultureEffectsEnabled { get; }
        public bool WardenSystemEnabled { get; }
        public bool HasPrisonMeme { get; }
        public bool HasWardenPrecept { get; }
    }

    public readonly struct RimPrisonStateSnapshot
    {
        public RimPrisonStateSnapshot(bool hasComponent, PrisonTimeAssignment currentAssignment,
            PrisonSuppressionSnapshot suppression, bool wardenSystemEnabled)
        {
            HasComponent = hasComponent;
            CurrentAssignment = currentAssignment;
            Suppression = suppression;
            WardenSystemEnabled = wardenSystemEnabled;
        }

        public bool HasComponent { get; }
        public PrisonTimeAssignment CurrentAssignment { get; }
        public PrisonSuppressionSnapshot Suppression { get; }
        public bool WardenSystemEnabled { get; }
    }

    // ============================================================
    // Reference types
    // ============================================================

    public sealed class RimPrisonBabySpecialFoodOption
    {
        public RimPrisonBabySpecialFoodOption(string id, string label, string description = null)
        {
            Id = id;
            Label = label;
            Description = description;
        }

        public string Id { get; }
        public string Label { get; }
        public string Description { get; }
    }

    // Set by main mod during labor job search; read by extensions that bypass
    // work type age restrictions (e.g. young prisoner self-reliance).
    public sealed class LaborSearchContext
    {
        public Pawn Pawn;
        public IReadOnlyList<WorkTypeDef> AllowedWorkTypesIgnoringAgeBand;
    }

    // ============================================================
    // Extension Interfaces (9)
    // ============================================================

    public interface IRimPrisonWorkEligibilityRule
    {
        RimPrisonApiRuleResult CanWork(Pawn pawn, WorkTypeDef workType);
    }

    public interface IRimPrisonWorkEfficiencyRule
    {
        RimPrisonApiRuleResult GetEfficiency(Pawn pawn, WorkTypeDef workType);
    }

    public interface IRimPrisonLaborJobProvider
    {
        bool TryGiveLaborJob(Pawn pawn, out Job job);
    }

    public interface IRimPrisonBabyFoodRule
    {
        RimPrisonApiRuleResult CanUseAsBabyFood(Pawn baby, ThingDef food);
    }

    public interface IRimPrisonBabySpecialFoodProvider
    {
        RimPrisonBabySpecialFoodOption Option { get; }

        // [TODO] TryGiveJob second parameter: needs a component that tracks
        // which baby special food options are enabled per-map.
        // Currently using Map as placeholder.
        bool TryGiveJob(Pawn baby, Map map, out Job job);
    }

    public interface IRimPrisonFoodEffectRule
    {
        void NotifyFoodConsumed(Pawn pawn, ThingDef food);
    }

    public interface IRimPrisonMoodRule
    {
        RimPrisonApiRuleResult GetMoodOffset(Pawn pawn);
    }

    public interface IRimPrisonPreceptInterpreter
    {
        RimPrisonApiRuleResult Interpret(Map map, PreceptDef precept);
    }

    public interface IRimPrisonUiExtension
    {
        string Label { get; }
        // [TODO] Add DoTabContent(Rect, Pawn) when UI extension system is built
    }

    // ============================================================
    // Extension Registration Center
    // ============================================================

    public static class RimPrisonExtensionApi
    {
        private static readonly Dictionary<string, IRimPrisonWorkEligibilityRule> WorkEligibilityRuleById =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRimPrisonWorkEfficiencyRule> WorkEfficiencyRuleById =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRimPrisonLaborJobProvider> LaborJobProviderById =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRimPrisonBabyFoodRule> BabyFoodRuleById =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRimPrisonBabySpecialFoodProvider> BabySpecialFoodProviderById =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRimPrisonFoodEffectRule> FoodEffectRuleById =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRimPrisonMoodRule> MoodRuleById =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRimPrisonPreceptInterpreter> PreceptInterpreterById =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRimPrisonUiExtension> UiExtensionById =
            new(StringComparer.Ordinal);
        private static readonly ConditionalWeakTable<Job, AnnotatedLaborWorkTypeHolder> AnnotatedLaborWorkTypeByJob =
            new();

        // [NOTE] Set by labor search loop before enumerating WorkGivers.
        // Extensions read this to bypass age-based work restrictions for young prisoners.
        public static LaborSearchContext CurrentLaborSearchContext { get; internal set; }

        public static IReadOnlyDictionary<string, IRimPrisonWorkEligibilityRule> WorkEligibilityRules =>
            WorkEligibilityRuleById;
        public static IReadOnlyDictionary<string, IRimPrisonWorkEfficiencyRule> WorkEfficiencyRules =>
            WorkEfficiencyRuleById;
        public static IReadOnlyDictionary<string, IRimPrisonLaborJobProvider> LaborJobProviders =>
            LaborJobProviderById;
        public static IReadOnlyDictionary<string, IRimPrisonBabyFoodRule> BabyFoodRules =>
            BabyFoodRuleById;
        public static IReadOnlyDictionary<string, IRimPrisonBabySpecialFoodProvider> BabySpecialFoodProviders =>
            BabySpecialFoodProviderById;
        public static IReadOnlyDictionary<string, IRimPrisonFoodEffectRule> FoodEffectRules =>
            FoodEffectRuleById;
        public static IReadOnlyDictionary<string, IRimPrisonMoodRule> MoodRules =>
            MoodRuleById;
        public static IReadOnlyDictionary<string, IRimPrisonPreceptInterpreter> PreceptInterpreters =>
            PreceptInterpreterById;
        public static IReadOnlyDictionary<string, IRimPrisonUiExtension> UiExtensions =>
            UiExtensionById;

        // --- Register/Unregister ---

        public static bool RegisterWorkEligibilityRule(string id, IRimPrisonWorkEligibilityRule rule)
            => Register(id, rule, WorkEligibilityRuleById);
        public static bool UnregisterWorkEligibilityRule(string id)
            => Unregister(id, WorkEligibilityRuleById);

        public static bool RegisterWorkEfficiencyRule(string id, IRimPrisonWorkEfficiencyRule rule)
            => Register(id, rule, WorkEfficiencyRuleById);
        public static bool UnregisterWorkEfficiencyRule(string id)
            => Unregister(id, WorkEfficiencyRuleById);

        public static bool RegisterLaborJobProvider(string id, IRimPrisonLaborJobProvider provider)
            => Register(id, provider, LaborJobProviderById);
        public static bool UnregisterLaborJobProvider(string id)
            => Unregister(id, LaborJobProviderById);

        public static bool RegisterBabyFoodRule(string id, IRimPrisonBabyFoodRule rule)
            => Register(id, rule, BabyFoodRuleById);
        public static bool UnregisterBabyFoodRule(string id)
            => Unregister(id, BabyFoodRuleById);

        public static bool RegisterBabySpecialFoodProvider(string id, IRimPrisonBabySpecialFoodProvider provider)
        {
            if (provider?.Option == null
                || string.IsNullOrWhiteSpace(provider.Option.Id)
                || !string.Equals(id, provider.Option.Id, StringComparison.Ordinal))
            {
                return false;
            }
            return Register(id, provider, BabySpecialFoodProviderById);
        }
        public static bool UnregisterBabySpecialFoodProvider(string id)
            => Unregister(id, BabySpecialFoodProviderById);

        public static bool RegisterFoodEffectRule(string id, IRimPrisonFoodEffectRule rule)
            => Register(id, rule, FoodEffectRuleById);
        public static bool UnregisterFoodEffectRule(string id)
            => Unregister(id, FoodEffectRuleById);

        public static bool RegisterMoodRule(string id, IRimPrisonMoodRule rule)
            => Register(id, rule, MoodRuleById);
        public static bool UnregisterMoodRule(string id)
            => Unregister(id, MoodRuleById);

        public static bool RegisterPreceptInterpreter(string id, IRimPrisonPreceptInterpreter interpreter)
            => Register(id, interpreter, PreceptInterpreterById);
        public static bool UnregisterPreceptInterpreter(string id)
            => Unregister(id, PreceptInterpreterById);

        public static bool RegisterUiExtension(string id, IRimPrisonUiExtension extension)
            => Register(id, extension, UiExtensionById);
        public static bool UnregisterUiExtension(string id)
            => Unregister(id, UiExtensionById);

        // --- Decision resolvers ---

        public static bool EvaluateWorkEligibility(Pawn pawn, WorkTypeDef workType, bool fallbackAllowed)
        {
            return ResolveBooleanRules(
                WorkEligibilityRuleById.Values.Select(rule => rule.CanWork(pawn, workType)),
                fallbackAllowed);
        }

        public static float ResolveWorkEfficiencyFactor(Pawn pawn, WorkTypeDef workType, float fallbackFactor = 1f)
        {
            var factor = fallbackFactor;
            foreach (var rule in WorkEfficiencyRuleById.Values)
            {
                var result = rule.GetEfficiency(pawn, workType);
                if (result.Handled)
                    factor *= Math.Max(0f, result.Value);
            }
            return factor;
        }

        public static bool TryResolveLaborJob(Pawn pawn, out Job job)
        {
            foreach (var provider in LaborJobProviderById.Values)
            {
                try
                {
                    if (provider.TryGiveLaborJob(pawn, out job) && job != null)
                        return true;
                }
                catch (Exception ex)
                {
                    Log.Warning($"RimPrison.API: labor job provider failed for {pawn?.LabelShortCap ?? "unknown"}: {ex.Message}");
                }
            }
            job = null;
            return false;
        }

        // --- LaborJob annotation ---

        public static bool AnnotateLaborJobWorkType(Job job, string workTypeDefName)
        {
            if (job == null || string.IsNullOrWhiteSpace(workTypeDefName))
                return false;
            AnnotatedLaborWorkTypeByJob.Remove(job);
            AnnotatedLaborWorkTypeByJob.Add(job, new AnnotatedLaborWorkTypeHolder(workTypeDefName));
            return true;
        }

        public static string ConsumeAnnotatedLaborJobWorkType(Job job)
        {
            if (job == null || !AnnotatedLaborWorkTypeByJob.TryGetValue(job, out var holder))
                return null;
            AnnotatedLaborWorkTypeByJob.Remove(job);
            return holder.WorkTypeDefName;
        }

        public static string PeekAnnotatedLaborJobWorkType(Job job)
        {
            if (job == null || !AnnotatedLaborWorkTypeByJob.TryGetValue(job, out var holder))
                return null;
            return holder.WorkTypeDefName;
        }

        // --- Food resolvers ---

        public static bool EvaluateBabyFood(Pawn baby, ThingDef food, bool fallbackAllowed)
        {
            return ResolveBooleanRules(
                BabyFoodRuleById.Values.Select(rule => rule.CanUseAsBabyFood(baby, food)),
                fallbackAllowed);
        }

        public static IReadOnlyList<RimPrisonBabySpecialFoodOption> GetBabySpecialFoodOptions()
        {
            var result = new List<RimPrisonBabySpecialFoodOption>();
            foreach (var provider in BabySpecialFoodProviderById.Values)
            {
                var option = provider.Option;
                if (option != null && !string.IsNullOrWhiteSpace(option.Id))
                    result.Add(option);
            }
            return result;
        }

        // [TODO] TryResolveBabySpecialFoodJob — needs a per-map component that tracks
        // which baby special food options are enabled. Currently passes Map as placeholder.
        public static bool TryResolveBabySpecialFoodJob(Pawn baby, Map map, out Job job)
        {
            foreach (var provider in BabySpecialFoodProviderById.Values)
            {
                try
                {
                    if (provider.TryGiveJob(baby, map, out job) && job != null)
                        return true;
                }
                catch (Exception ex)
                {
                    Log.Warning($"RimPrison.API: baby special food provider failed for {baby?.LabelShortCap ?? "unknown"}: {ex.Message}");
                }
            }
            job = null;
            return false;
        }

        public static void NotifyFoodConsumed(Pawn pawn, ThingDef food)
        {
            foreach (var rule in FoodEffectRuleById.Values)
                rule.NotifyFoodConsumed(pawn, food);
        }

        // --- Mood / Precept resolvers ---

        public static float ResolveMoodOffset(Pawn pawn, float fallbackOffset = 0f)
        {
            var offset = fallbackOffset;
            foreach (var rule in MoodRuleById.Values)
            {
                var result = rule.GetMoodOffset(pawn);
                if (result.Handled)
                    offset += result.Value;
            }
            return offset;
        }

        public static bool EvaluatePrecept(Map map, PreceptDef precept, bool fallbackAllowed)
        {
            return ResolveBooleanRules(
                PreceptInterpreterById.Values.Select(rule => rule.Interpret(map, precept)),
                fallbackAllowed);
        }

        // --- Internal ---

        internal static void ClearAllRulesForTesting()
        {
            WorkEligibilityRuleById.Clear();
            WorkEfficiencyRuleById.Clear();
            LaborJobProviderById.Clear();
            BabyFoodRuleById.Clear();
            BabySpecialFoodProviderById.Clear();
            FoodEffectRuleById.Clear();
            MoodRuleById.Clear();
            PreceptInterpreterById.Clear();
            UiExtensionById.Clear();
        }

        private static bool Register<T>(string id, T rule, IDictionary<string, T> registry) where T : class
        {
            if (string.IsNullOrWhiteSpace(id) || rule == null || registry.ContainsKey(id))
                return false;
            registry[id] = rule;
            return true;
        }

        private static bool Unregister<T>(string id, IDictionary<string, T> registry)
        {
            return !string.IsNullOrWhiteSpace(id) && registry.Remove(id);
        }

        private static bool ResolveBooleanRules(IEnumerable<RimPrisonApiRuleResult> results, bool fallbackAllowed)
        {
            var anyAllow = false;
            foreach (var result in results)
            {
                switch (result.Decision)
                {
                    case RimPrisonApiRuleDecision.Deny:
                        return false;
                    case RimPrisonApiRuleDecision.Allow:
                        anyAllow = true;
                        break;
                }
            }
            return anyAllow || fallbackAllowed;
        }

        private sealed class AnnotatedLaborWorkTypeHolder
        {
            public AnnotatedLaborWorkTypeHolder(string workTypeDefName)
            {
                WorkTypeDefName = workTypeDefName;
            }
            public string WorkTypeDefName { get; }
        }
    }

    // ============================================================
    // Sub-API: Work
    // ============================================================

    public static class RimPrisonWorkApi
    {
        // WIRED — reads group.workPriorities (priority > 0 = allowed)
        public static bool IsWorkTypeAllowed(Pawn pawn, WorkTypeDef workType)
        {
            if (pawn?.MapHeld == null || workType == null) return false;
            var mgr = pawn.MapHeld.GetComponent<PrisonLabor.PrisonerGroupManager>();
            var group = mgr?.GetGroupFor(pawn);
            return group?.workPriorities?[workType] > 0;
        }

        // [TODO] Need internal port — per-map work type age band config storage
        public static bool SetDefaultWorkAgeAllowed(Map map, WorkTypeDef workType, int ageBand, bool allowed)
        {
            return false;
        }

        public static bool RegisterWorkEligibilityRule(string id, IRimPrisonWorkEligibilityRule rule)
            => RimPrisonExtensionApi.RegisterWorkEligibilityRule(id, rule);

        public static bool RegisterWorkEfficiencyRule(string id, IRimPrisonWorkEfficiencyRule rule)
            => RimPrisonExtensionApi.RegisterWorkEfficiencyRule(id, rule);

        public static bool RegisterLaborJobProvider(string id, IRimPrisonLaborJobProvider provider)
            => RimPrisonExtensionApi.RegisterLaborJobProvider(id, provider);

        public static bool AnnotateLaborJobWorkType(Job job, string workTypeDefName)
            => RimPrisonExtensionApi.AnnotateLaborJobWorkType(job, workTypeDefName);

        public static string ConsumeAnnotatedLaborJobWorkType(Job job)
            => RimPrisonExtensionApi.ConsumeAnnotatedLaborJobWorkType(job);

        public static string PeekAnnotatedLaborJobWorkType(Job job)
            => RimPrisonExtensionApi.PeekAnnotatedLaborJobWorkType(job);

        // WIRED — returns all WorkTypeDefs with priority > 0 for the pawn's group
        public static IReadOnlyList<WorkTypeDef> GetConfiguredWorkTypesIgnoringAgeBand(Pawn pawn)
        {
            var result = new List<WorkTypeDef>();
            if (pawn?.MapHeld == null) return result;
            var mgr = pawn.MapHeld.GetComponent<PrisonLabor.PrisonerGroupManager>();
            var group = mgr?.GetGroupFor(pawn);
            if (group?.workPriorities == null) return result;
            foreach (var wt in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (group.workPriorities[wt] > 0)
                    result.Add(wt);
            }
            return result;
        }

        public static float ResolveWorkEfficiencyFactor(Pawn pawn, WorkTypeDef workType, float fallbackFactor = 1f)
            => RimPrisonExtensionApi.ResolveWorkEfficiencyFactor(pawn, workType, fallbackFactor);
    }

    // ============================================================
    // Sub-API: Food
    // ============================================================

    public static class RimPrisonFoodApi
    {
        // WIRED — checks group.foodRestriction, same logic as IsFoodAllowed
        public static bool IsBabyFoodAllowed(Pawn baby, ThingDef food)
        {
            return IsFoodAllowed(baby, food);
        }

        // WIRED — checks group foodRestriction (FoodPolicy), falls back to vanilla
        public static bool IsFoodAllowed(Pawn pawn, ThingDef food)
        {
            if (pawn?.MapHeld == null || food == null) return true;
            var mgr = pawn.MapHeld.GetComponent<PrisonLabor.PrisonerGroupManager>();
            var group = mgr?.GetGroupFor(pawn);
            if (group?.foodRestriction != null)
                return group.foodRestriction.Allows(food);
            return pawn.foodRestriction?.CurrentFoodPolicy?.Allows(food) ?? true;
        }

        public static bool RegisterBabyFoodRule(string id, IRimPrisonBabyFoodRule rule)
            => RimPrisonExtensionApi.RegisterBabyFoodRule(id, rule);

        public static bool RegisterBabySpecialFoodProvider(string id, IRimPrisonBabySpecialFoodProvider provider)
            => RimPrisonExtensionApi.RegisterBabySpecialFoodProvider(id, provider);

        public static IReadOnlyList<RimPrisonBabySpecialFoodOption> GetBabySpecialFoodOptions()
            => RimPrisonExtensionApi.GetBabySpecialFoodOptions();

        public static bool TryResolveBabySpecialFoodJob(Pawn baby, Map map, out Job job)
            => RimPrisonExtensionApi.TryResolveBabySpecialFoodJob(baby, map, out job);

        public static bool RegisterFoodEffectRule(string id, IRimPrisonFoodEffectRule rule)
            => RimPrisonExtensionApi.RegisterFoodEffectRule(id, rule);

        public static void NotifyFoodConsumed(Pawn pawn, ThingDef food)
            => RimPrisonExtensionApi.NotifyFoodConsumed(pawn, food);
    }

    // ============================================================
    // Sub-API: Culture
    // ============================================================

    public static class RimPrisonCultureApi
    {
        // [TODO] regime / warden system state per map — partially wired (HasPrisonMeme works)
        public static RimPrisonCultureSnapshot GetCultureSnapshot(Map map)
        {
            var meme = DefDatabase<MemeDef>.GetNamedSilentFail("RPR_PrisonMeme");
            return new RimPrisonCultureSnapshot(
                PrisonCultureRegime.Deterrence,
                false,
                false,
                Faction.OfPlayer?.ideos?.PrimaryIdeo?.HasMeme(meme) == true,
                false);
        }

        // WIRED — checks PrimaryIdeo + runs extension interpreters as fallback
        public static bool PlayerIdeologyHasPrecept(PreceptDef precept)
        {
            return precept != null
                && RimPrisonExtensionApi.EvaluatePrecept(
                    Find.CurrentMap,
                    precept,
                    Faction.OfPlayer?.ideos?.PrimaryIdeo?.HasPrecept(precept) == true);
        }

        public static bool RegisterPreceptInterpreter(string id, IRimPrisonPreceptInterpreter interpreter)
            => RimPrisonExtensionApi.RegisterPreceptInterpreter(id, interpreter);
    }

    // ============================================================
    // Sub-API: Finance — WIRED to CompWorkTracker
    // ============================================================

    public static class RimPrisonFinanceApi
    {
        // GetBalance and GetDebtBalance return null-negative value
        // (Only for forward compatibility)
        // GetEffectiveBalance return real balance
        // Only suggest using GetEffectiveBalance in the future
        public static float GetBalance(Pawn pawn)
        {
            if (pawn == null) return 0f;
            return Math.Max(pawn.GetComp<PrisonLabor.CompWorkTracker>()?.earnedCoupons ?? 0, 0);
        }

        public static float GetDebtBalance(Pawn pawn)
        {
            if (pawn == null) return 0f;
            return Math.Max(-(pawn.GetComp<PrisonLabor.CompWorkTracker>()?.earnedCoupons ?? 0), 0);
        }

        public static float GetEffectiveBalance(Pawn pawn)
        {
            if (pawn == null) return 0f;
            return pawn.GetComp<PrisonLabor.CompWorkTracker>()?.earnedCoupons ?? 0;
        }

        // WIRED — return final cost
        // ... AI failed to write such simple function
        public static float ChargeBalanceOrAddDebt(Pawn pawn, float amount)
        {
            if (pawn == null) return 0f;
            var tracker = pawn.GetComp<PrisonLabor.CompWorkTracker>();
            if (tracker == null) return 0f;
            var oldBalance = tracker.earnedCoupons;
            tracker.earnedCoupons = (int)(tracker.earnedCoupons - amount);
            tracker.earnedCoupons = Math.Max(tracker.earnedCoupons,-RimPrisonMod.Settings.MaxDebt);
            return tracker.earnedCoupons - oldBalance;
        }

        public static void AddBalance(Pawn pawn, float amount)
        {
            if (pawn == null || amount <= 0f) return;
            var tracker = pawn.GetComp<PrisonLabor.CompWorkTracker>();
            if (tracker != null)
                tracker.earnedCoupons += (int)Math.Floor(amount);
        }

        public static string GetCurrencyName()
        {
            return RimPrisonMod.Settings.WorkCouponName;
        }
    }

    // ============================================================
    // Sub-API: State — PARTIALLY WIRED
    // ============================================================

    public static class RimPrisonStateApi
    {
        // [TODO] WTF AI implement an INDEPENDENT CACULATOR HERE
        // ?????????? OF COURSE NOT OK! CHANGE IN THE FUTURE!
        // WIRED — fills suppression snapshot from GameComponent_Suppression + map data
        public static RimPrisonStateSnapshot GetStateSnapshot(Map map)
        {
            if (map == null)
                return default;

            var comp = map.GetComponent<PrisonLabor.GameComponent_Suppression>();
            var hasComponent = comp != null;

            var prisoners = map.mapPawns.PrisonersOfColony;
            int adultCount = 0, childCount = 0, babyCount = 0;
            float totalMood = 0f, totalHealth = 0f;
            foreach (var p in prisoners)
            {
                if (p.DevelopmentalStage.Baby()) babyCount++;
                else if (p.DevelopmentalStage.Child()) childCount++;
                else adultCount++;
                totalMood += p.needs?.mood?.CurLevelPercentage ?? 0.5f;
                totalHealth += p.health?.summaryHealth?.SummaryHealthPercent ?? 1f;
            }
            int prisonerCount = prisoners.Count;
            float avgMood = prisonerCount > 0 ? totalMood / prisonerCount : 0.5f;
            float avgHealth = prisonerCount > 0 ? totalHealth / prisonerCount : 1f;
            int turretCount = PrisonLabor.SuppressionCalculator.CountTurretsInPrisonArea(map);
            int guardCount = 0; // [TODO] warden system not implemented
            int colonistCount = map.mapPawns.FreeColonistsSpawnedCount;
            float difficultyValue = Find.Storyteller?.difficulty?.threatScale ?? 1f;

            var regime = PrisonLabor.SuppressionCalculator.CurrentRegime;
            float effectivePrisoners = PrisonLabor.SuppressionCalculator.CalculateEffectivePrisonerCount(map);
            float guardFactor = (guardCount * 2f + colonistCount) / Math.Max(1f, effectivePrisoners) * 5f;
            float turretFactor = Math.Min(turretCount * 2f, 20f);
            float prisonerFactor = Math.Min(effectivePrisoners * 1.5f, 25f);
            float moodFactor = (avgMood * avgMood * 12f - 3f) * 1.25f;
            float healthFactor = (0.5f - avgHealth) * 8f;
            float regimeModifier = regime switch
            {
                PrisonLabor.SuppressionCalculator.Regime.Harsh => 10f,
                PrisonLabor.SuppressionCalculator.Regime.Deterrence => 3f,
                PrisonLabor.SuppressionCalculator.Regime.Equality => -5f,
                _ => 0f
            };
            float difficultyModifier = (1f - difficultyValue) * 8f;

            var thresholds = new PrisonSuppressionThresholds
            {
                mentalBreakThreshold = regime switch
                {
                    PrisonLabor.SuppressionCalculator.Regime.Harsh => 55f,
                    PrisonLabor.SuppressionCalculator.Regime.Equality => 40f,
                    _ => 50f
                },
                prisonBreakThreshold = regime switch
                {
                    PrisonLabor.SuppressionCalculator.Regime.Harsh => 35f,
                    PrisonLabor.SuppressionCalculator.Regime.Equality => 20f,
                    _ => 30f
                }
            };

            var snapshot = new PrisonSuppressionSnapshot
            {
                suppression = comp?.colonySuppression ?? 50f,
                prisonerCount = prisonerCount,
                adultCount = adultCount,
                childCount = childCount,
                babyCount = babyCount,
                averageMood = avgMood,
                averageHealth = avgHealth,
                guardFactor = guardFactor,
                turretFactor = turretFactor,
                prisonerFactor = prisonerFactor,
                moodFactor = moodFactor,
                healthFactor = healthFactor,
                regimeModifier = regimeModifier,
                difficultyModifier = difficultyModifier,
                guardCount = guardCount,
                turretCount = turretCount,
                thresholds = thresholds
            };

            // [NOTE] GetCurrentAssignmentForPawn requires a specific pawn.
            // Snapshot-level assignment is set to Anything as a safe default.
            return new RimPrisonStateSnapshot(
                hasComponent,
                PrisonTimeAssignment.Anything,
                snapshot,
                false); // [TODO] warden system not implemented
        }

        // WIRED — reads pawn.timetable, maps TimeAssignmentDef to PrisonTimeAssignment
        public static PrisonTimeAssignment GetCurrentAssignmentForPawn(Pawn pawn)
        {
            if (pawn?.timetable?.CurrentAssignment == null) return PrisonTimeAssignment.Anything;
            return TimeAssignmentDefToEnum(pawn.timetable.CurrentAssignment);
        }

        // WIRED — returns axis based on developmental stage
        public static PrisonScheduleAxisKind GetScheduleAxisForPawn(Pawn pawn)
        {
            if (pawn == null) return PrisonScheduleAxisKind.Main;
            if (pawn.DevelopmentalStage.Baby()) return PrisonScheduleAxisKind.Baby;
            if (pawn.DevelopmentalStage.Child()) return PrisonScheduleAxisKind.Child;
            return PrisonScheduleAxisKind.Main;
        }

        // WIRED — reads pawn.timetable.times (24 entries), maps to int 0-3
        public static IReadOnlyList<int> GetScheduleForPawn(Pawn pawn)
        {
            var result = new List<int>(24);
            if (pawn?.timetable?.times == null)
            {
                for (int i = 0; i < 24; i++) result.Add(3); // default Anything
                return result;
            }
            for (int h = 0; h < 24; h++)
                result.Add(TimeAssignmentDefToInt(pawn.timetable.times[h]));
            return result;
        }

        private static PrisonTimeAssignment TimeAssignmentDefToEnum(TimeAssignmentDef def)
        {
            if (def == TimeAssignmentDefOf.Sleep) return PrisonTimeAssignment.Sleep;
            if (def == TimeAssignmentDefOf.Work) return PrisonTimeAssignment.Labor;
            if (def == TimeAssignmentDefOf.Joy) return PrisonTimeAssignment.Recreation;
            return PrisonTimeAssignment.Anything;
        }

        private static int TimeAssignmentDefToInt(TimeAssignmentDef def)
        {
            if (def == TimeAssignmentDefOf.Sleep) return 0;
            if (def == TimeAssignmentDefOf.Work) return 2;
            if (def == TimeAssignmentDefOf.Joy) return 1;
            return 3;
        }

        // WIRED — reads Area_Prison
        public static bool IsPrisonAreaCell(Map map, IntVec3 cell)
        {
            if (map == null) return false;
            return map.areaManager.Get<PrisonArea.Area_Prison>()?[cell] ?? false;
        }

        // WIRED — reads bed.ForPrisoners on baby-capable beds
        public static bool IsPrisonBabyBed(Building_Bed bed)
        {
            return bed != null && bed.ForPrisoners;
        }

        // [TODO] Need internal port — warden system not implemented
        public static bool IsWardenSystemEnabled(Map map)
        {
            return false;
        }

        /*
            Should be set in extension!
            public static bool IsBabyForageFilthFoodPoisoningEnabled(Map map)
        */

        // WIRED — forwards to GameComponent_ActivityLog
        public static void LogEvent(Map map, string entry)
        {
            if (map == null || string.IsNullOrWhiteSpace(entry)) return;
            map.GetComponent<PrisonLabor.GameComponent_ActivityLog>()?.Log("API", entry);
        }
    }

    // ============================================================
    // Top-level RimPrisonApi facade
    // ============================================================

    public static class RimPrisonApi
    {
        public const string ApiVersion = "2.0";

        private static readonly HashSet<string> SupportedFeatures = new(StringComparer.OrdinalIgnoreCase)
        {
            "Identity",
            "WorkEligibilityRules",
            "WorkEfficiencyRules",
            "LaborJobProviders",
            "BabyFoodRules",
            "BabySpecialFoodProviders",
            "FoodEffectRules",
            "MoodRules",
            "CultureSnapshots",
            "CulturePreceptInterpreters",
            "UiExtensionSlots",
            "Finance",
            "StateSnapshots",
            "ExtensionRegistration"
        };

        public static bool Supports(string feature)
            => !string.IsNullOrWhiteSpace(feature) && SupportedFeatures.Contains(feature);

        // WIRED
        public static bool IsManagedPrisoner(Pawn pawn)
            => pawn != null && pawn.IsPrisonerOfColony && PrisonLabor.PrisonLaborUtility.IsLaborEnabled(pawn);

        // WIRED
        public static bool IsManagedPrisonBaby(Pawn pawn)
            => pawn != null && pawn.IsPrisonerOfColony && pawn.DevelopmentalStage.Baby();

        // WIRED
        public static bool IsManagedYoungPawn(Pawn pawn)
            => pawn != null && pawn.IsPrisonerOfColony && !pawn.DevelopmentalStage.Adult();

        // WIRED
        public static bool IsPrisonAreaCell(Map map, IntVec3 cell)
            => RimPrisonStateApi.IsPrisonAreaCell(map, cell);

        // WIRED — pawn.PositionHeld inside Area_Prison
        public static bool IsInsidePrisonArea(Pawn pawn)
        {
            if (pawn?.MapHeld == null || !pawn.Spawned) return false;
            return IsPrisonAreaCell(pawn.MapHeld, pawn.PositionHeld);
        }

        // WIRED — pawn currently executing a work job
        public static bool IsDoingWorkJob(Pawn pawn)
            => pawn?.CurJob?.workGiverDef?.workType != null;

        // WIRED
        public static bool IsPrisonBabyBed(Building_Bed bed)
            => RimPrisonStateApi.IsPrisonBabyBed(bed);

        // WIRED
        public static float GetBalance(Pawn pawn)
            => RimPrisonFinanceApi.GetBalance(pawn);

        // WIRED
        public static float GetDebtBalance(Pawn pawn)
            => RimPrisonFinanceApi.GetDebtBalance(pawn);

        // WIRED
        public static float GetEffectiveBalance(Pawn pawn)
            => RimPrisonFinanceApi.GetEffectiveBalance(pawn);

        // WIRED
        public static float ChargeBalanceOrAddDebt(Pawn pawn, float amount)
            => RimPrisonFinanceApi.ChargeBalanceOrAddDebt(pawn, amount);

        // WIRED
        public static void AddBalance(Pawn pawn, float amount)
            => RimPrisonFinanceApi.AddBalance(pawn, amount);

        // WIRED
        public static string GetCurrencyName()
            => RimPrisonFinanceApi.GetCurrencyName();
    }
}
