using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.Core;

namespace RimPrison.HarmonyPatches
{
    // The vanilla getter forces Anything for all prisoners
    // Let labor-enabled prisoners use their actual timetable instead.
    [HarmonyPatch(typeof(Pawn_TimetableTracker), "get_CurrentAssignment")]
    public static class Patch_TimetableFix
    {
        static void Postfix(Pawn_TimetableTracker __instance, ref TimeAssignmentDef __result)
        {
            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn != null && pawn.IsLaborEnabled())
            {
                __result = __instance.times[GenLocalDate.HourOfDay(pawn)];
            }
        }
    }
}
