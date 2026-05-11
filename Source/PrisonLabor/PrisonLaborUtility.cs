using RimPrisonBuilder.DefOfs;
using RimWorld;
using Verse;

namespace RimPrisonBuilder.PrisonLabor
{
    public static class PrisonLaborUtility
    {
        public static bool IsLaborEnabled(this Pawn pawn)
        {
            if (!pawn.IsPrisonerOfColony)
                return false;

            if (pawn.guest == null)
                return false;

            return pawn.guest.IsInteractionEnabled(RP_DefOf.RimPrisonBuilder_AllowLabor);
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
