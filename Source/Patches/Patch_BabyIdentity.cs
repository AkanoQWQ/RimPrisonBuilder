using HarmonyLib;
using RimWorld;
using Verse;

namespace RimPrison.Patches
{
    // When a prisoner gives birth, the baby inherits the mother's hostile faction.
    // Vanilla does NOT auto-prisonerize the newborn, so we set guest status here.
    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.TrySpawnHatchedOrBornPawn))]
    internal static class Patch_BabyIdentity
    {
        static void Postfix(Pawn pawn, Thing motherOrEgg, bool __result)
        {
            if (!__result) return;
            if (motherOrEgg is not Pawn mother) return;
            if (!mother.IsPrisonerOfColony) return;
            if (!pawn.RaceProps.Humanlike) return;
            if (!pawn.DevelopmentalStage.Baby()) return;

            pawn.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);

            pawn.guest?.ToggleNonExclusiveInteraction(
                DefOfs.RP_DefOf.RimPrison_AllowLabor, true);
        }
    }
}
