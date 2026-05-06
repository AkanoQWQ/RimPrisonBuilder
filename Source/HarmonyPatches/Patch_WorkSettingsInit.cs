using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.Core;

namespace RimPrison.HarmonyPatches
{
    // When the "Allow labor" checkbox is toggled, ensure workSettings are initialized.
    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.ToggleNonExclusiveInteraction))]
    public static class Patch_InteractionModeChanged
    {
        public static void Postfix(Pawn_GuestTracker __instance, Pawn ___pawn)
        {
            if (___pawn == null || !___pawn.IsPrisonerOfColony)
                return;
            if (!___pawn.IsLaborEnabled())
                return;

            var workSettings = ___pawn.workSettings;
            if (workSettings != null && !workSettings.EverWork)
            {
                workSettings.EnableAndInitialize();
            }

            // These fields are only created via Scribe_Deep.Look during save/load,
            // so freshly spawned non-colonist pawns may have null values here.
            if (___pawn.timetable == null)
            {
                ___pawn.timetable = new Pawn_TimetableTracker(___pawn);
            }
            if (___pawn.playerSettings == null)
            {
                ___pawn.playerSettings = new Pawn_PlayerSettings(___pawn);
            }
        }
    }
}
