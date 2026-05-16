using HarmonyLib;
using RimPrison.PrisonLabor;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.Patches
{
    // Colonists should not feed labor-enabled prisoners that can walk — prisoners buy
    // their own food. Downed or immobile prisoners still get fed
    internal static class Patch_DisableColonistFeeding_Feed
    {
        [HarmonyPatch(typeof(WorkGiver_Warden_Feed), nameof(WorkGiver_Warden_Feed.JobOnThing))]
        [HarmonyPrefix]
        static bool Prefix_Feed(Pawn pawn, Thing t, bool forced, ref Job __result)
        {
            if (t is Pawn prisoner 
                && prisoner.IsPrisonerOfColony 
                && prisoner.IsLaborEnabled()
                && !prisoner.Downed
                && prisoner.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
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
            // Never deliver food to any prisoner — defeats the economic system.
            // And leads problem for hauling.
            if (t is Pawn prisoner && prisoner.IsPrisonerOfColony)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}
