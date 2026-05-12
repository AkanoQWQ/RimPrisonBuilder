using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.API
{
    // THIS IS NOT OK for current extension
    // [TODO] Rewrite it!
    // BTW why AI write so many unreviewed here?
    // ======== Extension Interfaces ========

    // [UNREVIEWED]
    public interface IExtension_WorkEligibility
    {
        bool CanPawnDoWorkType(Pawn pawn, WorkTypeDef workType);
    }

    // [UNREVIEWED]
    public interface IExtension_WorkEfficiency
    {
        float GetWorkEfficiencyMultiplier(Pawn pawn, WorkTypeDef workType);
    }

    // [UNREVIEWED]
    public interface IExtension_LaborJob
    {
        Job TryGetLaborJob(Pawn pawn);
    }

    // [UNREVIEWED]
    public interface IExtension_BabyFood
    {
        bool CanBabyEat(Pawn baby, ThingDef foodDef);
    }

    // [UNREVIEWED]
    public interface IExtension_BabySpecialFood
    {
        string Id { get; }
        string Label { get; }
        string Description { get; }
    }

    // [UNREVIEWED]
    public interface IExtension_FoodEffect
    {
        float GetFoodNutritionMultiplier(Pawn pawn, Thing food);
    }

    // [UNREVIEWED]
    public interface IExtension_Mood
    {
        void ApplyMoodThoughts(Pawn pawn);
    }

    // [UNREVIEWED]
    public interface IExtension_Precept
    {
        void OverridePreceptSettings(ref float value, string preceptId);
    }

    // ======== Central API Manager ========

    // [UNREVIEWED]
    public static class RimPrisonApi
    {
        private static readonly List<IExtension_WorkEligibility> workEligExtensions = new List<IExtension_WorkEligibility>();
        private static readonly List<IExtension_WorkEfficiency> workEffExtensions = new List<IExtension_WorkEfficiency>();
        private static readonly List<IExtension_LaborJob> laborJobExtensions = new List<IExtension_LaborJob>();
        private static readonly List<IExtension_BabyFood> babyFoodExtensions = new List<IExtension_BabyFood>();
        private static readonly List<IExtension_BabySpecialFood> babySpecialFoodExtensions = new List<IExtension_BabySpecialFood>();
        private static readonly List<IExtension_FoodEffect> foodEffectExtensions = new List<IExtension_FoodEffect>();
        private static readonly List<IExtension_Mood> moodExtensions = new List<IExtension_Mood>();
        private static readonly List<IExtension_Precept> preceptExtensions = new List<IExtension_Precept>();

        public static void RegisterWorkEligibility(IExtension_WorkEligibility ext) => workEligExtensions.Add(ext);
        public static void UnregisterWorkEligibility(IExtension_WorkEligibility ext) => workEligExtensions.Remove(ext);
        public static void RegisterWorkEfficiency(IExtension_WorkEfficiency ext) => workEffExtensions.Add(ext);
        public static void UnregisterWorkEfficiency(IExtension_WorkEfficiency ext) => workEffExtensions.Remove(ext);
        public static void RegisterLaborJob(IExtension_LaborJob ext) => laborJobExtensions.Add(ext);
        public static void UnregisterLaborJob(IExtension_LaborJob ext) => laborJobExtensions.Remove(ext);
        public static void RegisterBabyFood(IExtension_BabyFood ext) => babyFoodExtensions.Add(ext);
        public static void UnregisterBabyFood(IExtension_BabyFood ext) => babyFoodExtensions.Remove(ext);
        public static void RegisterBabySpecialFood(IExtension_BabySpecialFood ext) => babySpecialFoodExtensions.Add(ext);
        public static void UnregisterBabySpecialFood(IExtension_BabySpecialFood ext) => babySpecialFoodExtensions.Remove(ext);
        public static void RegisterFoodEffect(IExtension_FoodEffect ext) => foodEffectExtensions.Add(ext);
        public static void UnregisterFoodEffect(IExtension_FoodEffect ext) => foodEffectExtensions.Remove(ext);
        public static void RegisterMood(IExtension_Mood ext) => moodExtensions.Add(ext);
        public static void UnregisterMood(IExtension_Mood ext) => moodExtensions.Remove(ext);
        public static void RegisterPrecept(IExtension_Precept ext) => preceptExtensions.Add(ext);
        public static void UnregisterPrecept(IExtension_Precept ext) => preceptExtensions.Remove(ext);

        // Internal accessors for sub-API classes
        internal static List<IExtension_WorkEligibility> GetWorkEligExtensions() => workEligExtensions;
        internal static List<IExtension_WorkEfficiency> GetWorkEffExtensions() => workEffExtensions;
        internal static List<IExtension_LaborJob> GetLaborJobExtensions() => laborJobExtensions;
        internal static List<IExtension_BabyFood> GetBabyFoodExtensions() => babyFoodExtensions;
        internal static List<IExtension_BabySpecialFood> GetBabySpecialFoodExtensions() => babySpecialFoodExtensions;
        internal static List<IExtension_FoodEffect> GetFoodEffectExtensions() => foodEffectExtensions;
        internal static List<IExtension_Mood> GetMoodExtensions() => moodExtensions;
        internal static List<IExtension_Precept> GetPreceptExtensions() => preceptExtensions;

        // Feature query
        public static bool Supports(string feature)
        {
            return feature switch
            {
                "WorkEligibility" => workEligExtensions.Count > 0,
                "WorkEfficiency" => workEffExtensions.Count > 0,
                "LaborJob" => laborJobExtensions.Count > 0,
                "BabyFood" => babyFoodExtensions.Count > 0,
                "BabySpecialFood" => babySpecialFoodExtensions.Count > 0,
                "FoodEffect" => foodEffectExtensions.Count > 0,
                "Mood" => moodExtensions.Count > 0,
                "Precept" => preceptExtensions.Count > 0,
                _ => false
            };
        }

        // Static query methods used by other mods
        public static bool IsManagedPrisoner(Pawn pawn)
        {
            return pawn != null && pawn.IsPrisonerOfColony && PrisonLabor.PrisonLaborUtility.IsLaborEnabled(pawn);
        }

        public static float GetBalance(Pawn pawn)
        {
            // [TODO] NO LOGIC — CompPrisonerPolicy not implemented yet
            return 0f;
        }

        public static string GetCurrencyName()
        {
            return RimPrisonMod.Settings.WorkCouponName;
        }
    }

    // ======== Sub-API Classes ========

    // [UNREVIEWED]
    public static class RimPrisonWorkApi
    {
        public static bool CanPawnDoWorkType(Pawn pawn, WorkTypeDef workType)
        {
            foreach (var ext in RimPrisonApi.GetWorkEligExtensions())
            {
                if (!ext.CanPawnDoWorkType(pawn, workType))
                    return false;
            }
            return true;
        }

        public static float GetWorkEfficiency(Pawn pawn, WorkTypeDef workType)
        {
            float mult = 1f;
            foreach (var ext in RimPrisonApi.GetWorkEffExtensions())
                mult *= ext.GetWorkEfficiencyMultiplier(pawn, workType);
            return mult;
        }

        public static Job TryGetLaborJob(Pawn pawn)
        {
            foreach (var ext in RimPrisonApi.GetLaborJobExtensions())
            {
                Job job = ext.TryGetLaborJob(pawn);
                if (job != null) return job;
            }
            return null;
        }
    }

    // [UNREVIEWED]
    public static class RimPrisonFoodApi
    {
        public static bool CanBabyEat(Pawn baby, ThingDef foodDef)
        {
            foreach (var ext in RimPrisonApi.GetBabyFoodExtensions())
            {
                if (!ext.CanBabyEat(baby, foodDef))
                    return false;
            }
            return true;
        }

        public static List<IExtension_BabySpecialFood> GetBabySpecialFoodOptions()
        {
            return RimPrisonApi.GetBabySpecialFoodExtensions();
        }

        public static float GetNutritionMultiplier(Pawn pawn, Thing food)
        {
            float mult = 1f;
            foreach (var ext in RimPrisonApi.GetFoodEffectExtensions())
                mult *= ext.GetFoodNutritionMultiplier(pawn, food);
            return mult;
        }
    }

    // [UNREVIEWED]
    public static class RimPrisonCultureApi
    {
        public static void ApplyMoodThoughts(Pawn pawn)
        {
            foreach (var ext in RimPrisonApi.GetMoodExtensions())
                ext.ApplyMoodThoughts(pawn);
        }

        public static void OverridePrecept(string preceptId, ref float value)
        {
            foreach (var ext in RimPrisonApi.GetPreceptExtensions())
                ext.OverridePreceptSettings(ref value, preceptId);
        }
    }

    // [UNREVIEWED]
    public static class RimPrisonFinanceApi
    {
        public static float GetBalance(Pawn pawn) => RimPrisonApi.GetBalance(pawn);
        public static string GetCurrencyName() => RimPrisonApi.GetCurrencyName();
    }

    // [UNREVIEWED]
    public static class RimPrisonStateApi
    {
        public static bool IsManagedPrisoner(Pawn pawn) => RimPrisonApi.IsManagedPrisoner(pawn);
        public static bool Supports(string feature) => RimPrisonApi.Supports(feature);
    }
}
