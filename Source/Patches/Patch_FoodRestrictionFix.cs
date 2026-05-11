using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // Vanilla GetCurrentRespectedRestriction only enforces food restrictions when
    // the pawn is Faction.OfPlayer OR the getter (feeder) is Faction.OfPlayer.
    // Prisoners eating on their own have getter==null, so restrictions are ignored.
    // This patch makes labor-enabled prisoners respect their assigned food restriction.
    [HarmonyPatch(typeof(Pawn_FoodRestrictionTracker), nameof(Pawn_FoodRestrictionTracker.GetCurrentRespectedRestriction))]
    internal static class Patch_FoodRestrictionFix
    {
        static void Postfix(Pawn_FoodRestrictionTracker __instance, ref FoodPolicy __result)
        {
            if (__result != null)
                return;

            var pawn = __instance.pawn;
            if (pawn != null && pawn.IsPrisonerOfColony && pawn.IsLaborEnabled())
            {
                __result = __instance.CurrentFoodPolicy;
            }
        }
    }
}
