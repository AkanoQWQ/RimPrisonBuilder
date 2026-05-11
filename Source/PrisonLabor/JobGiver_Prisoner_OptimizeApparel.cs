using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.PrisonLabor
{
    // [UNREVIEWED]
    // Custom OptimizeApparel for prisoners. Vanilla TryGiveJob has a
    // pawn.Faction != Faction.OfPlayer guard that rejects non-colonists.
    // We override it with prisoner-specific logic that respects the outfit policy.
    // To be honest I dont think it's necessary to customize a JobGiver but vanilla doesn't work
    public class JobGiver_Prisoner_OptimizeApparel : JobGiver_OptimizeApparel
    {
        private static readonly FieldInfo f_neededWarmth =
            typeof(JobGiver_OptimizeApparel).GetField("neededWarmth",
                BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly List<float> wornScores = new List<float>();

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.IsPrisonerOfColony)
                return null;
            if (!pawn.IsLaborEnabled())
                return null;
            if (pawn.outfits == null)
                return null;
            if (pawn.IsQuestLodger())
                return null;
            if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                return null;

            ApparelPolicy currentOutfit = pawn.outfits.CurrentApparelPolicy;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;

            // Strip non-policy clothes
            for (int i = wornApparel.Count - 1; i >= 0; i--)
            {
                Apparel ap = wornApparel[i];
                if (!currentOutfit.filter.Allows(ap)
                    && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(ap)
                    && !pawn.apparel.IsLocked(ap))
                {
                    SetNextOptimizeTick(pawn);
                    Job job = JobMaker.MakeJob(JobDefOf.RemoveApparel, ap);
                    job.haulDroppedApparel = true;
                    return job;
                }
            }

            // Scan for better apparel to wear
            List<Thing> candidates = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            if (candidates.Count == 0)
            {
                SetNextOptimizeTick(pawn);
                return null;
            }

            f_neededWarmth?.SetValue(null,
                PawnApparelGenerator.CalculateNeededWarmth(pawn, pawn.Map.Tile,
                    GenLocalDate.Twelfth(pawn)));

            wornScores.Clear();
            for (int i = 0; i < wornApparel.Count; i++)
                wornScores.Add(ApparelScoreRaw(pawn, wornApparel[i]));

            Thing bestThing = null;
            float bestScore = 0f;
            for (int j = 0; j < candidates.Count; j++)
            {
                Apparel candidate = (Apparel)candidates[j];
                if (!currentOutfit.filter.Allows(candidate))
                    continue;
                if (!candidate.IsInAnyStorage())
                    continue;
                if (candidate.IsForbidden(pawn))
                    continue;
                if (candidate.IsBurning())
                    continue;
                if (candidate.def.apparel.gender != Gender.None
                    && candidate.def.apparel.gender != pawn.gender)
                    continue;
                if (CompBiocodable.IsBiocoded(candidate)
                    && !CompBiocodable.IsBiocodedFor(candidate, pawn))
                    continue;
                if (!ApparelUtility.HasPartsToWear(pawn, candidate.def))
                    continue;
                if (!candidate.def.apparel.developmentalStageFilter.Has(pawn.DevelopmentalStage))
                    continue;

                float score = ApparelScoreGain(pawn, candidate, wornScores);
                if (score < 0.05f || score < bestScore)
                    continue;
                if (!pawn.CanReserveAndReach(candidate, PathEndMode.OnCell,
                    pawn.NormalMaxDanger()))
                    continue;

                bestThing = candidate;
                bestScore = score;
            }

            if (bestThing == null)
            {
                SetNextOptimizeTick(pawn);
                return null;
            }
            return JobMaker.MakeJob(JobDefOf.Wear, bestThing);
        }

        private static void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick =
                Find.TickManager.TicksGame + Rand.Range(6000, 9000);
        }
    }
}
