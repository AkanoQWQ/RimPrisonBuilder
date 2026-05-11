using RimWorld;
using Verse;

namespace RimPrisonBuilder.PrisonLabor
{
    // Grants daily coupon allowance to all prisoners at midnight (hour 0).
    // Uses day-boundary detection so it fires at most once per in-game day.
    // [OPTIMIZE] check every tick, not elegant
    public class GameComponent_DailyAllowance : GameComponent
    {
        private int lastGrantDay = -1;

        public GameComponent_DailyAllowance(Game game) { }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            int allowance = RimPrisonBuilderMod.Settings.DailyAllowance;
            if (allowance <= 0) return;

            int currentDay = Find.TickManager.TicksGame / GenDate.TicksPerDay;
            if (currentDay <= lastGrantDay) return;
            lastGrantDay = currentDay;

            foreach (Pawn pawn in PawnsFinder.AllMaps_PrisonersOfColony)
            {
                var comp = pawn.TryGetComp<CompWorkTracker>();
                if (comp != null)
                {
                    comp.earnedCoupons += allowance;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastGrantDay, "lastGrantDay", -1);
        }
    }
}
