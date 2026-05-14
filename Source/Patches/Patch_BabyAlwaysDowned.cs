using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // LifeStageDef.HumanlikeBaby has alwaysDowned=true.
    // These two patches neutralize it for laborenabled baby prisoners so they can move.

    // 1. MustKeepLyingDown locks the baby in place once they lie down.
    [HarmonyPatch(typeof(ThinkNode_ConditionalMustKeepLyingDown), "Satisfied")]
    internal static class Patch_BabyMustKeepLyingDown
    {
        // Only block MustKeepLyingDown when the baby is NOT asleep — sleeping babies
        // need the vanilla KeepLyingDown subtree to maintain their rest job.
        static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result && pawn?.IsLaborEnabled() == true && pawn.DevelopmentalStage.Baby()
                && pawn.jobs?.curDriver?.asleep != true)
                __result = false;
        }
    }

    // 2. ShouldBeDowned forces the baby into PawnHealthState.Down regardless of Moving.
    [HarmonyPatch(typeof(Pawn_HealthTracker), "ShouldBeDowned")]
    internal static class Patch_BabyShouldBeDowned
    {
        static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn, ref bool __result)
        {
            if (__result && ___pawn?.IsLaborEnabled() == true && ___pawn.DevelopmentalStage.Baby())
                __result = false;
        }
    }
}
