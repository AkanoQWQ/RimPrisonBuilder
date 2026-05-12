using RimPrison.DefOfs;
using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    public class ThoughtWorker_Despair : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p?.health?.hediffSet?.HasHediff(RP_HediffDefOf.RPR_Despair) ?? false)
                return ThoughtState.ActiveDefault;
            return ThoughtState.Inactive;
        }
    }
}
