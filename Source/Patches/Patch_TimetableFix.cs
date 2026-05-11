using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // The vanilla getter forces Anything for all prisoners
    // Let labor-enabled prisoners use their actual timetable instead.
    [HarmonyPatch(typeof(Pawn_TimetableTracker), "get_CurrentAssignment")]
    public static class Patch_TimetableFix
    {
        static void Postfix(Pawn_TimetableTracker __instance, Pawn ___pawn, ref TimeAssignmentDef __result)
        {
            if (___pawn != null && ___pawn.IsLaborEnabled())
            {
                __result = __instance.times[GenLocalDate.HourOfDay(___pawn)];
            }
        }
    }
}
