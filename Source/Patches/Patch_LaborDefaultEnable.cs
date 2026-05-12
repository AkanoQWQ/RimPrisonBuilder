using HarmonyLib;
using RimPrison.DefOfs;
using RimWorld;
using Verse;

namespace RimPrison.Patches
{
    // Auto-enable AllowLabor interaction mode when a pawn becomes a colony prisoner.
    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
    internal static class Patch_LaborDefaultEnable
    {
        static void Postfix(Pawn_GuestTracker __instance, GuestStatus guestStatus, Pawn ___pawn)
        {
            if (guestStatus != GuestStatus.Prisoner) return;
            if (___pawn == null || !___pawn.IsPrisonerOfColony) return;
            if (RP_DefOf.RimPrison_AllowLabor == null) return;

            if (!__instance.IsInteractionEnabled(RP_DefOf.RimPrison_AllowLabor))
                __instance.ToggleNonExclusiveInteraction(RP_DefOf.RimPrison_AllowLabor, enabled: true);
        }
    }
}
