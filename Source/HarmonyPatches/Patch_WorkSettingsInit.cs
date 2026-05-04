using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.Core;

namespace RimPrison.HarmonyPatches
{
    // When a prisoner's "Allow labor" mode changes
    // ensure their work settings are initialized.
    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
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
        }
    }
}
