using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrisonBuilder.PrisonLabor.ThinkNodes
{
    // ThinkTree condition node
    // For prisoner, only activated when "Allow labor"
    public class ThinkNode_ConditionalPrisonerLabor : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (!pawn.IsLaborEnabled())
                return false;

            // Prisoners don't have outfit/drug trackers by default.
            // Initialize them here so vanilla JobGiver_OptimizeApparel
            // and JobGiver_SatisfyChemicalNeed can work.
            if (pawn.outfits == null)
                pawn.outfits = new Pawn_OutfitTracker(pawn);
            if (pawn.drugs == null)
                pawn.drugs = new Pawn_DrugPolicyTracker(pawn);

            return true;
        }
    }
}
