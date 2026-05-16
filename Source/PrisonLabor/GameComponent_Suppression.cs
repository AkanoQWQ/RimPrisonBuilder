using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    public class GameComponent_Suppression : MapComponent
    {
        private const int RecalcIntervalTicks = 2000;

        // Per-pawn suppression values, keyed by thingIDNumber
        public Dictionary<int, float> suppressionByPawn = new Dictionary<int, float>();
        public float colonySuppression; // average across all managed prisoners
        private int lastRecalcTick = -1;

        public GameComponent_Suppression(Map map) : base(map) { }

        public float GetSuppression(Pawn pawn)
        {
            if (pawn == null) return 50f;
            RecalcIfStale();
            return suppressionByPawn.TryGetValue(pawn.thingIDNumber, out float v) ? v : 50f;
        }

        public bool AllowsPrisonBreak(Pawn pawn)
        {
            return SuppressionCalculator.AllowsPrisonBreak(
                GetSuppression(pawn), SuppressionCalculator.CurrentRegime);
        }

        public bool AllowsMentalBreak(Pawn pawn)
        {
            return SuppressionCalculator.AllowsMentalBreak(
                GetSuppression(pawn), SuppressionCalculator.CurrentRegime);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            RecalcIfStale();
        }

        private void RecalcIfStale()
        {
            int tick = Find.TickManager.TicksGame;
            if (lastRecalcTick >= 0 && tick - lastRecalcTick < RecalcIntervalTicks)
                return;
            lastRecalcTick = tick;
            RecalculateAll();
        }

        private void RecalculateAll()
        {
            var prisoners = map.mapPawns.PrisonersOfColony;
            float effectiveCount = SuppressionCalculator.CalculateEffectivePrisonerCount(map);
            int turrets = SuppressionCalculator.CountTurretsInPrisonArea(map);
            int colonistCount = map.mapPawns.FreeColonistsSpawnedCount;
            // [TODO] NO LOGIC — guard count not implemented yet
            int guardCount = 0;
            float difficultyValue = Find.Storyteller?.difficulty?.threatScale ?? 1f;
            var regime = SuppressionCalculator.CurrentRegime;

            // Clean up dead/released pawns
            var liveIds = new HashSet<int>();
            var deadIds = new List<int>();
            foreach (var pawn in prisoners)
            {
                if (pawn.IsLaborEnabled())
                    liveIds.Add(pawn.thingIDNumber);
            }
            foreach (var id in suppressionByPawn.Keys)
            {
                if (!liveIds.Contains(id))
                    deadIds.Add(id);
            }
            foreach (var id in deadIds)
                suppressionByPawn.Remove(id);

            float totalSuppression = 0f;
            int managedCount = 0;

            foreach (var pawn in prisoners)
            {
                if (!pawn.IsLaborEnabled()) continue;
                managedCount++;

                float mood = pawn.needs?.mood?.CurLevelPercentage ?? 0.5f;
                float health = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f;

                var bd = SuppressionCalculator.CalculateSuppression(
                    effectiveCount, guardCount, colonistCount, turrets,
                    mood, health, regime, difficultyValue);

                suppressionByPawn[pawn.thingIDNumber] = bd.suppression;
                totalSuppression += bd.suppression;
            }

            colonySuppression = managedCount > 0
                ? totalSuppression / managedCount
                : 50f;
        }

        // [UNREVIEWED] Have not reviewed ExposeData here
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref colonySuppression, "colonySuppression", 50f);
            Scribe_Values.Look(ref lastRecalcTick, "lastRecalcTick", -1);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Convert dictionary to lists for serialization
                var ids = new List<int>();
                var vals = new List<float>();
                foreach (var kv in suppressionByPawn)
                {
                    ids.Add(kv.Key);
                    vals.Add(kv.Value);
                }
                Scribe_Collections.Look(ref ids, "suppIds", LookMode.Value);
                Scribe_Collections.Look(ref vals, "suppVals", LookMode.Value);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var ids = new List<int>();
                var vals = new List<float>();
                Scribe_Collections.Look(ref ids, "suppIds", LookMode.Value);
                Scribe_Collections.Look(ref vals, "suppVals", LookMode.Value);
                ids ??= new List<int>();
                vals ??= new List<float>();
                suppressionByPawn.Clear();
                for (int i = 0; i < Math.Min(ids.Count, vals.Count); i++)
                    suppressionByPawn[ids[i]] = vals[i];
            }
        }
    }
}
