using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // Laborenabled baby prisoners are forced to use adult Humanlike ThinkTrees.
    // HumanlikeBaby has no SubtreesByTag insertion points + KeepLyingDown.
    // HumanlikeBabyConstant has KeepLyingDown, overriding every 30 ticks.
    // [TODO] Replacing the entire ThinkTree is aggressive — HumanlikeBaby lacks subtreesByTag
    // so our RimPrison_PrisonerLabor subtree (insertTag="Humanlike_PostDuty") can't inject.
    // Fallback: keep the override but rely on a health-aware ShouldBeDowned (see
    // Patch_BabyAlwaysDowned.cs) to ensure injured babies are properly downed.
    // Long-term: consider a custom ThinkTreeDef (RimPrison_HumanlikeBabyLabor) that copies
    // HumanlikeBaby structure + our labor subtree, avoiding total replacement. No backward
    // compatibility concern (no serialization, no Def refs), so deferring is safe.
    [HarmonyPatch(typeof(Pawn_Thinker), "get_MainThinkTree")]
    internal static class Patch_BabyMainThinkTree_Override
    {
        static void Postfix(Pawn_Thinker __instance, ref ThinkTreeDef __result)
        {
            var pawn = __instance.pawn;
            if (pawn?.IsLaborEnabled() == true && pawn.DevelopmentalStage.Baby())
                __result = DefDatabase<ThinkTreeDef>.GetNamed("Humanlike");
        }
    }

    [HarmonyPatch(typeof(Pawn_Thinker), "get_ConstantThinkTree")]
    internal static class Patch_BabyConstantThinkTree_Override
    {
        static void Postfix(Pawn_Thinker __instance, ref ThinkTreeDef __result)
        {
            var pawn = __instance.pawn;
            if (pawn?.IsLaborEnabled() == true && pawn.DevelopmentalStage.Baby())
                __result = DefDatabase<ThinkTreeDef>.GetNamed("HumanlikeConstant");
        }
    }
}
