using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // Allows prisoners with labor enabled to pass JobGiver_Work.PawnCanUseWorkGiver.
    [HarmonyPatch(typeof(JobGiver_Work), "PawnCanUseWorkGiver")]
    public static class Patch_PawnCanUseWorkGiver
    {
        public static bool Prefix(Pawn pawn, WorkGiver giver, ref bool __result)
        {
            // WorkGiver already allows non-colonists
            if (giver.def.nonColonistsCanDo)
                return true;
            // Not a labor-enabled prisoner
            if (!pawn.IsLaborEnabled())
                return true;

            // Original checks without the IsColonist filter
            // Not sure whether this is a good implementation
            // Could also be done via IL weaving
            // [UPDATE] Adapt if the original implementation changes
            if (pawn.WorkTagIsDisabled(giver.def.workTags))
            {
                __result = false;
                return false;
            }
            if (giver.def.workType != null && pawn.WorkTypeIsDisabled(giver.def.workType))
            {
                __result = false;
                return false;
            }
            if (giver.ShouldSkip(pawn))
            {
                __result = false;
                return false;
            }
            if (giver.MissingRequiredCapacity(pawn) != null)
            {
                __result = false;
                return false;
            }
            if (pawn.RaceProps.IsMechanoid && !giver.def.canBeDoneByMechs)
            {
                __result = false;
                return false;
            }

            // Childcare WorkGivers (PlayWithBaby et al.) have a vanilla race:
            // HasJobOnThing→BabyPlayGiver.CanDo→true, but JobOnThing re-rolls
            // InRandomOrder and TryGiveJob→null, producing a log error.
            // Let vanilla PawnCanUseWorkGiver block these for now; prisoner
            // childcare will use dedicated JobGivers, not the vanilla WorkGivers.
            // SO ONLY MOM COULD FEED CHILDREN
            // Maybe it's a problem!
            if (giver.def.workType == WorkTypeDefOf.Childcare)
                return true;

            // All other checks passed so prisoner can use this WorkGiver
            __result = true;
            return false;
        }
    }
}
