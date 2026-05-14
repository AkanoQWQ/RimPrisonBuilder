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
