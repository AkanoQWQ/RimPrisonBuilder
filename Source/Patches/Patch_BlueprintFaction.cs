using HarmonyLib;
using RimWorld;
using Verse;
using RimPrisonBuilder.PrisonLabor;

namespace RimPrisonBuilder.Patches
{
    // Reassign blueprint result to player faction when built by a prisoner.
    [HarmonyPatch(typeof(Blueprint), "TryReplaceWithSolidThing")]
    public static class Patch_BlueprintFaction
    {
        static void Postfix(Pawn workerPawn, Thing createdThing, bool __result)
        {
            if (!__result || createdThing == null || !workerPawn.IsLaborEnabled())
                return;

            if (createdThing.def.CanHaveFaction)
            {
                createdThing.SetFactionDirect(Faction.OfPlayer);
            }
        }
    }
}
