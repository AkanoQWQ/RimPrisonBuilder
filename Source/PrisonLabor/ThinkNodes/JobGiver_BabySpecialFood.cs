using RimPrison.API;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.PrisonLabor.ThinkNodes
{
    // Gives baby special food Jobs from registered IRimPrisonBabySpecialFoodProvider extensions.
    // Injected into the prisoner labor ThinkTree BEFORE JobGiver_Work, so hungry babies
    // try to find special food (e.g. foraging filth) before attempting regular work.
    public class JobGiver_BabySpecialFood : ThinkNode
    {
        public override float GetPriority(Pawn pawn)
        {
            if (pawn?.needs?.food == null
                || !pawn.DevelopmentalStage.Baby()
                || pawn.MapHeld == null)
                return 0f;

            var category = pawn.needs.food.CurCategory;
            if (category >= HungerCategory.Starving) return 19.5f;
            if (category >= HungerCategory.UrgentlyHungry) return 14.5f;
            if (category >= HungerCategory.Hungry) return 9.5f;
            return 0f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (RimPrisonExtensionApi.TryResolveBabySpecialFoodJob(pawn, pawn.MapHeld, out Job job))
                return new ThinkResult(job, this);
            return ThinkResult.NoJob;
        }
    }
}
