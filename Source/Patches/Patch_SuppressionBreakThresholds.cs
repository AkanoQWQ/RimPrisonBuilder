using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // Modify prison break MTB and mental break eligibility based on suppression.
    [HarmonyPatch(typeof(PrisonBreakUtility), nameof(PrisonBreakUtility.InitiatePrisonBreakMtbDays))]
    internal static class Patch_SuppressionPrisonBreak
    {
        static void Postfix(Pawn pawn, ref float __result)
        {
            if (__result <= 0f) return;
            if (!pawn.IsPrisonerOfColony || !pawn.IsLaborEnabled()) return;

            var comp = pawn.Map?.GetComponent<GameComponent_Suppression>();
            if (comp == null) return;

            float mult = SuppressionCalculator.GetBreakMtbMultiplier(
                comp.GetSuppression(pawn), SuppressionCalculator.CurrentRegime);
            __result *= mult;
        }
    }

    [HarmonyPatch(typeof(MentalBreaker), "CanHaveMentalBreak")]
    internal static class Patch_SuppressionMentalBreak
    {
        static void Postfix(MentalBreaker __instance, Pawn ___pawn, ref bool __result)
        {
            if (!__result) return;
            if (___pawn == null) return;
            if (!___pawn.IsPrisonerOfColony || !___pawn.IsLaborEnabled()) return;

            var comp = ___pawn.Map?.GetComponent<GameComponent_Suppression>();
            if (comp == null) return;

            if (!comp.AllowsMentalBreak(___pawn))
                __result = false;
        }
    }
}
