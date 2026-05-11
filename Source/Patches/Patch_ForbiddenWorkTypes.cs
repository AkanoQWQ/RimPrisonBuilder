using HarmonyLib;
using RimWorld;
using Verse;

namespace RimPrisonBuilder.Patches
{
    // Prisoners should never do Warden or Hunting work.
    [HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.SetPriority))]
    static class Patch_ForbiddenWorkTypes
    {
        static bool Prefix(Pawn_WorkSettings __instance, WorkTypeDef w, int priority, Pawn ___pawn)
        {
            if (priority > 0 && ___pawn.IsPrisonerOfColony
                && (w.defName == "Warden" || w.defName == "Hunting"))
            {
                return false;
            }
            return true;
        }
    }
}
