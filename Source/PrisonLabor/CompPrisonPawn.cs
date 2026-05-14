using System.Collections.Generic;
using Verse;

namespace RimPrison.PrisonLabor
{
    // Per-pawn "soft state" — thoughts, mood decisions, future escape intent etc.
    // Thoughts are not Scribed; they evaporate on save/load.
    public class CompPrisonPawn : ThingComp
    {
        private const int ThoughtThrottleTicks = 6000;
        private const int MaxThoughts = 10;

        [Unsaved] public List<string> thoughts = new List<string>();
        [Unsaved] private int lastThoughtTick = -1;

        public CompProperties_PrisonPawn Props => (CompProperties_PrisonPawn)props;

        public void RecordThought(string thought)
        {
            if (string.IsNullOrWhiteSpace(thought)) return;

            int now = Find.TickManager.TicksGame;
            if (lastThoughtTick >= 0 && now - lastThoughtTick < ThoughtThrottleTicks)
                return;
            lastThoughtTick = now;

            if (thoughts.Count >= MaxThoughts)
                thoughts.RemoveAt(0);
            thoughts.Add(thought);
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
