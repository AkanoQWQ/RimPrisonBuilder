using System.Collections.Generic;
using RimPrison.DefOfs;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.PrisonLabor
{
    // [UNREVIEWED] Sorry I have stayed up late...QWQ
    public class ChoiceLetter_Ransom : ChoiceLetter
    {
        public Pawn prisoner;
        public int ransomAmount;

        private bool responded;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (responded || prisoner == null || prisoner.Destroyed)
                {
                    yield return Option_Close;
                    yield break;
                }

                // Accept — release prisoner
                string acceptLabel = "RimPrison.RansomAccept".Translate(
                    prisoner.LabelShortCap, ransomAmount.ToString());
                var acceptOpt = new DiaOption(acceptLabel);
                acceptOpt.action = delegate
                {
                    responded = true;
                    GameComponent_Ransom.ClearOffered(prisoner);
                    var tracker = prisoner.TryGetComp<CompWorkTracker>();
                    if (tracker != null)
                        tracker.earnedCoupons -= ransomAmount;
                    // Release: mark as released, try to walk out, fallback to despawn.
                    // Door access may block pathing from cells — despawn if no exit path.
                    prisoner.guest.Released = true;
                    prisoner.guest.SetNoInteraction();
                    if (prisoner.Spawned && prisoner.jobs != null
                        && RCellFinder.TryFindBestExitSpot(prisoner, out var spot))
                    {
                        prisoner.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                        var job = JobMaker.MakeJob(JobDefOf.Goto, spot);
                        job.exitMapOnArrival = true;
                        prisoner.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                    else if (prisoner.Spawned)
                    {
                        prisoner.DeSpawn(DestroyMode.Vanish);
                    }
                    prisoner.Map?.GetComponent<GameComponent_ActivityLog>()?.Log(prisoner,
                        "RimPrison.LogRansomAccepted".Translate(ransomAmount.ToString()));
                    Find.LetterStack.RemoveLetter(this);
                };
                acceptOpt.resolveTree = true;
                yield return acceptOpt;

                // Reject — apply despair
                string rejectLabel = "RimPrison.RansomReject".Translate(prisoner.LabelShortCap);
                var rejectOpt = new DiaOption(rejectLabel);
                rejectOpt.action = delegate
                {
                    responded = true;
                    GameComponent_Ransom.ClearOffered(prisoner);
                    ApplyDespair();
                    Find.LetterStack.RemoveLetter(this);
                };
                rejectOpt.resolveTree = true;
                yield return rejectOpt;
            }
        }

        private void ApplyDespair()
        {
            if (prisoner == null || prisoner.Destroyed) return;
            var hediff = HediffMaker.MakeHediff(RP_HediffDefOf.RPR_Despair, prisoner);
            prisoner.health.AddHediff(hediff);
            prisoner.Map?.GetComponent<GameComponent_ActivityLog>()?.Log(prisoner,
                "RimPrison.LogRansomRejected".Translate());
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref prisoner, "prisoner");
            Scribe_Values.Look(ref ransomAmount, "ransomAmount", 0);
            Scribe_Values.Look(ref responded, "responded", false);
        }
    }
}
