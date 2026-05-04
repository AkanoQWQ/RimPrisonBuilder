using RimWorld;
using Verse;

namespace RimPrison.Core
{
    public static class PrisonLaborUtility
    {
        /// Check if prisoner labor is enabled for this pawn.
        public static bool IsLaborEnabled(this Pawn pawn)
        {
            if (!pawn.IsPrisonerOfColony)
                return false;

            if (pawn.guest == null)
                return false;

            return pawn.guest.IsInteractionEnabled(DefOfs.RP_DefOf.RimPrison_AllowLabor);
        }

        // Return Faction.OfPlayer for prisoners so WorkGiver scanning treats them as colonists.
        // Used by IL weaving, inspired by PrisonLabor
        public static Faction GetWorkFaction(Pawn pawn)
        {
            if (pawn == null)
                return null;

            return IsLaborEnabled(pawn) ? Faction.OfPlayer : pawn.Faction;
        }
    }
}
