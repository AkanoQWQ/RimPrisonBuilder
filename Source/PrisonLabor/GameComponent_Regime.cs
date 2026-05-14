using RimPrison.DefOfs;
using Verse;

namespace RimPrison.PrisonLabor
{
    // Per-map regime state. On regime change, removes old hediff from all prisoners
    // and applies the new one. Synced on pawn spawn.
    public class GameComponent_Regime : MapComponent
    {
        public SuppressionCalculator.Regime CurrentRegime
        {
            get => SuppressionCalculator.CurrentRegime;
            set
            {
                if (SuppressionCalculator.CurrentRegime == value) return;
                SuppressionCalculator.CurrentRegime = value;
                ApplyToAllPrisoners();
            }
        }

        public GameComponent_Regime(Map map) : base(map) { }

        public void ApplyToAllPrisoners()
        {
            if (map?.mapPawns == null) return;
            foreach (var p in map.mapPawns.PrisonersOfColony)
                SyncRegimeHediff(p);
        }

        public void SyncRegimeHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;
            var hediffs = pawn.health.hediffSet;
            var harsh = hediffs.GetFirstHediffOfDef(RP_HediffDefOf.RPR_RegimeHarsh);
            var deter = hediffs.GetFirstHediffOfDef(RP_HediffDefOf.RPR_RegimeDeterrence);
            var equal = hediffs.GetFirstHediffOfDef(RP_HediffDefOf.RPR_RegimeEquality);

            // Remove all existing regime hediffs
            if (harsh != null) pawn.health.RemoveHediff(harsh);
            if (deter != null) pawn.health.RemoveHediff(deter);
            if (equal != null) pawn.health.RemoveHediff(equal);

            // Apply current regime
            HediffDef target = CurrentRegime switch
            {
                SuppressionCalculator.Regime.Harsh => RP_HediffDefOf.RPR_RegimeHarsh,
                SuppressionCalculator.Regime.Deterrence => RP_HediffDefOf.RPR_RegimeDeterrence,
                SuppressionCalculator.Regime.Equality => RP_HediffDefOf.RPR_RegimeEquality,
                _ => null
            };
            if (target != null)
                pawn.health.AddHediff(target);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            int stored = (int)SuppressionCalculator.CurrentRegime;
            Scribe_Values.Look(ref stored, "currentRegime", (int)SuppressionCalculator.Regime.Deterrence);
            SuppressionCalculator.CurrentRegime = (SuppressionCalculator.Regime)stored;
        }
    }
}
