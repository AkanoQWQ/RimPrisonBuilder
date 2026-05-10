using Verse;
using Verse.AI;

namespace RimPrison.PrisonLabor.ThinkNodes
{
    // ThinkTree condition node
    // For prisoner, only activated when "Allow labor"
    public class ThinkNode_ConditionalPrisonerLabor : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return pawn.IsLaborEnabled();
        }
    }
}
