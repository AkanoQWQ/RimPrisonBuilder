using HarmonyLib;
using RimWorld;
using Verse;
using RimPrisonBuilder.PrisonLabor;

namespace RimPrisonBuilder.Patches
{
    // Labor-enabled prisoners should respect allowed area restrictions.
    [HarmonyPatch(typeof(ForbidUtility), "IsForbidden", typeof(IntVec3), typeof(Pawn))]
    public static class Patch_RespectAreaRestrictions
    {
        static void Postfix(IntVec3 c, Pawn pawn, ref bool __result)
        {
            if (!__result && pawn.IsLaborEnabled() && !c.InAllowedArea(pawn))
                __result = true;
        }
    }
}
