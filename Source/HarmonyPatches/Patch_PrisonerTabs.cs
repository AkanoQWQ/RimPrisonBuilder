using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.Core;

namespace RimPrison.HarmonyPatches
{
    // Add labor-enabled prisoners to the vanilla Schedule and Work tabs.
    [HarmonyPatch(typeof(MainTabWindow_PawnTable), "get_Pawns")]
    public static class Patch_PawnTable_Pawns
    {
        static void Postfix(ref IEnumerable<Pawn> __result)
        {
            var map = Find.CurrentMap;
            if (map == null)
                return;

            __result = __result.Concat(
                map.mapPawns.PrisonersOfColony.Where(p => p.IsLaborEnabled()));
        }
    }
}
