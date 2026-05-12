using HarmonyLib;
using RimPrison.PrisonLabor;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.Patches
{
    // Colonists should not feed labor-enabled prisoners – prisoners buy their own food.
    // Block both HasJobOnThing and JobOnThing for warden-feeding work givers.
    internal static class Patch_DisableColonistFeeding_Feed
    {
        [HarmonyPatch(typeof(WorkGiver_Warden_Feed), nameof(WorkGiver_Warden_Feed.JobOnThing))]
        [HarmonyPrefix]
        static bool Prefix_Feed(Pawn pawn, Thing t, bool forced, ref Job __result)
        {
            if (t is Pawn prisoner && prisoner.IsPrisonerOfColony && prisoner.IsLaborEnabled())
            {
                __result = null;
                return false;
            }
            return true;
        }
    }

    internal static class Patch_DisableColonistFeeding_DeliverFood
    {
        [HarmonyPatch(typeof(WorkGiver_Warden_DeliverFood), nameof(WorkGiver_Warden_DeliverFood.JobOnThing))]
        [HarmonyPrefix]
        static bool Prefix_Deliver(Pawn pawn, Thing t, bool forced, ref Job __result)
        {
            if (t is Pawn prisoner && prisoner.IsPrisonerOfColony && prisoner.IsLaborEnabled())
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}
