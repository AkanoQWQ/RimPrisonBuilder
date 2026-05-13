using HarmonyLib;
using RimPrison.PrisonArea;
using Verse;

namespace RimPrison.Patches
{
    // Auto-create Area_Prison on map generation like vanilla Area_Home.
    // No manual "create" step needed — expand/clear designators work immediately.
    [HarmonyPatch(typeof(AreaManager), nameof(AreaManager.AddStartingAreas))]
    internal static class Patch_AreaManager_AddStartingAreas
    {
        static void Postfix(AreaManager __instance)
        {
            __instance.AllAreas.Add(new Area_Prison(__instance));
        }
    }
}
