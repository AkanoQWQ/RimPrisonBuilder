using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    // Grants daily coupon allowance and deducts daily fee at midnight (hour 0).
    public class GameComponent_DailyAllowance : GameComponent
    {
        private int lastGrantDay = -1;

        public GameComponent_DailyAllowance(Game game) { }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            int allowance = RimPrisonMod.Settings.DailyAllowance;
            int fee = RimPrisonMod.Settings.DailyFee;
            if (allowance <= 0 && fee <= 0) return;

            int currentDay = Find.TickManager.TicksGame / GenDate.TicksPerDay;
            if (currentDay <= lastGrantDay) return;
            lastGrantDay = currentDay;

            foreach (Pawn pawn in PawnsFinder.AllMaps_PrisonersOfColony)
            {
                var comp = pawn.TryGetComp<CompWorkTracker>();
                if (comp == null) continue;
                comp.earnedCoupons += allowance;
                comp.earnedCoupons -= fee;

                pawn.TryGetComp<CompPrisonPawn>()?.SettleDailyIncome();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastGrantDay, "lastGrantDay", -1);
        }
    }
}
