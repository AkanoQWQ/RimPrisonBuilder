using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.PrisonLabor.ThinkNodes
{
    // ThinkTree condition node
    // For prisoner, only activated when "Allow labor"
    public class ThinkNode_ConditionalPrisonerLabor : ThinkNode_Conditional
    {
        private static readonly AccessTools.FieldRef<Pawn_WorkSettings, DefMap<WorkTypeDef, int>>
            PrioritiesField = AccessTools.FieldRefAccess<Pawn_WorkSettings, DefMap<WorkTypeDef, int>>("priorities");

        protected override bool Satisfied(Pawn pawn)
        {
            if (!pawn.IsLaborEnabled())
                return false;

            if (pawn.outfits == null)
                pawn.outfits = new Pawn_OutfitTracker(pawn);
            if (pawn.drugs == null)
                pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
            if (pawn.workSettings == null)
                pawn.workSettings = new Pawn_WorkSettings(pawn);
            PrioritiesField(pawn.workSettings) ??= new DefMap<WorkTypeDef, int>();

            return true;
        }
    }
}
