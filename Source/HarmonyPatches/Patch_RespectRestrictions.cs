using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.Core;

namespace RimPrison.HarmonyPatches
{
    // Labor-enabled prisoners should respect forbidden markers and area
    // restrictions just like colonists do.
    [HarmonyPatch(typeof(ForbidUtility), "CaresAboutForbidden")]
    public static class Patch_RespectRestrictions
    {
        static void Postfix(Pawn pawn, ref bool __result)
        {
            if (!__result && pawn.IsLaborEnabled())
                __result = true;
        }
    }
}
