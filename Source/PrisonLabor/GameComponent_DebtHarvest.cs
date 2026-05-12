using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    // Runs periodic debt harvest checks on all managed prisoners.
    public class GameComponent_DebtHarvest : GameComponent
    {
        private int lastCheckTick = -1;
        private const int CheckIntervalTicks = 60000; // check once per day

        public GameComponent_DebtHarvest(Game game) { }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            int threshold = RimPrisonMod.Settings.DebtHarvestThreshold;
            if (threshold <= 0) return;

            int tick = Find.TickManager.TicksAbs;
            if (tick - lastCheckTick < CheckIntervalTicks) return;
            lastCheckTick = tick;

            foreach (Pawn pawn in PawnsFinder.AllMaps_PrisonersOfColony)
            {
                var tracker = pawn.TryGetComp<CompWorkTracker>();
                if (tracker == null) continue;

                PrisonDebtHarvestService.TryProcessDebtHarvest(pawn, tracker);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastCheckTick, "lastCheckTick", -1);
        }
    }
}
