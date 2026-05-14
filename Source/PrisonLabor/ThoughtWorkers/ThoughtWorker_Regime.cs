using RimPrison.DefOfs;
using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    public class ThoughtWorker_RegimeHarsh : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return p?.health?.hediffSet?.HasHediff(RP_HediffDefOf.RPR_RegimeHarsh) == true
                ? ThoughtState.ActiveDefault
                : ThoughtState.Inactive;
        }
    }

    public class ThoughtWorker_RegimeDeterrence : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return p?.health?.hediffSet?.HasHediff(RP_HediffDefOf.RPR_RegimeDeterrence) == true
                ? ThoughtState.ActiveDefault
                : ThoughtState.Inactive;
        }
    }

    public class ThoughtWorker_RegimeEquality : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return p?.health?.hediffSet?.HasHediff(RP_HediffDefOf.RPR_RegimeEquality) == true
                ? ThoughtState.ActiveDefault
                : ThoughtState.Inactive;
        }
    }
}
