using System.Collections.Generic;
using Verse;

namespace RimPrison.PrisonLabor
{
    // Per-pawn "soft state" — thoughts, mood decisions, future escape intent etc.
    // Thoughts are not Scribed; they evaporate on save/load.
    public class CompPrisonPawn : ThingComp
    {
        private const int ThoughtThrottleTicks = 3000;
        private const int MaxThoughts = 10;
        private const float IncomeSmoothAlpha = 0.7f;

        [Unsaved] public List<string> thoughts = new List<string>();
        [Unsaved] private int lastThoughtTick = -1;

        // First-order low-pass filter on daily income
        // Val = Val * Alpha + today * (1-Alpha)
        public float smoothedDailyIncome;
        [Unsaved] private int lastDayBalance;
        [Unsaved] private bool balanceInitialized;

        public CompProperties_PrisonPawn Props => (CompProperties_PrisonPawn)props;

        public void RecordThought(string thought)
        {
            if (string.IsNullOrWhiteSpace(thought)) return;

            int now = Find.TickManager.TicksGame;
            if (lastThoughtTick >= 0 && now - lastThoughtTick < ThoughtThrottleTicks)
                return;
            lastThoughtTick = now;

            // Dedup: remove then re-add so it moves to the front (newest slot).
            thoughts.Remove(thought);
            if (thoughts.Count >= MaxThoughts)
                thoughts.RemoveAt(0);
            thoughts.Add(thought);
        }

        // Called once per day (at midnight) by GameComponent_DailyAllowance.
        public void SettleDailyIncome()
        {
            var tracker = ((Pawn)parent).TryGetComp<CompWorkTracker>();
            if (tracker == null) return;
            int currentBalance = tracker.earnedCoupons;

            if (!balanceInitialized)
            {
                lastDayBalance = currentBalance;
                balanceInitialized = true;
                return;
            }

            float todayIncome = currentBalance - lastDayBalance;
            lastDayBalance = currentBalance;

            if (smoothedDailyIncome <= 0f)
                smoothedDailyIncome = todayIncome;
            else
                smoothedDailyIncome = smoothedDailyIncome * IncomeSmoothAlpha + todayIncome * (1f - IncomeSmoothAlpha);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref smoothedDailyIncome, "smoothedDailyIncome");
        }
    }

    public class CompProperties_PrisonPawn : CompProperties
    {
        public CompProperties_PrisonPawn()
        {
            compClass = typeof(CompPrisonPawn);
        }
    }
}
