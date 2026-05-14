using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // Mirrors old mod's PrisonIdentityUnlock patches.
    // For all labor-enabled prisoners, unconditionally removes vanilla work restrictions.
    // Work filtering is handled by group workPriorities + extension IRimPrisonWorkEligibilityRule.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTypeIsDisabled))]
    public static class Patch_WorkTypeIsDisabled_Unlock
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (__instance.IsLaborEnabled())
                __result = false;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTagIsDisabled))]
    public static class Patch_WorkTagIsDisabled_Unlock
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (__instance.IsLaborEnabled())
                __result = false;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetDisabledWorkTypes))]
    public static class Patch_GetDisabledWorkTypes_Unlock
    {
        public static void Postfix(Pawn __instance, ref List<WorkTypeDef> __result)
        {
            if (__instance.IsLaborEnabled())
                __result = new List<WorkTypeDef>();
        }
    }

    [HarmonyPatch(typeof(Pawn), "get_CombinedDisabledWorkTags")]
    public static class Patch_CombinedDisabledWorkTags_Unlock
    {
        public static void Postfix(Pawn __instance, ref WorkTags __result)
        {
            if (__instance.IsLaborEnabled())
                __result = WorkTags.None;
        }
    }
}
