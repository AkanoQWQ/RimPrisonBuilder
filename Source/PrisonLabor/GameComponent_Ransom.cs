using System.Collections.Generic;
using RimPrison.DefOfs;
using RimWorld;
using Verse;

namespace RimPrison.PrisonLabor
{
    // AI:
    // Checks daily if any prisoner has enough balance for ransom.
    // Sends a ChoiceLetter for each eligible prisoner.
    // Uses a HashSet to track which prisoners have already received a letter,
    // so each prisoner only gets one offer until their balance changes.
    // Akano:
    // I am not really sure if it's good to make it an independent GameComponent
    // Maybe better implementation in the future!
    public class GameComponent_Ransom : GameComponent
    {
        private int lastCheckDay = -1;
        private HashSet<int> offeredPrisoners = new HashSet<int>();

        public GameComponent_Ransom(Game game) { }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            int ransomAmount = RimPrisonMod.Settings.RansomAmount;
            if (ransomAmount <= 0) return;

            int currentDay = Find.TickManager.TicksGame / GenDate.TicksPerDay;
            if (currentDay <= lastCheckDay) return;
            lastCheckDay = currentDay;

            foreach (Pawn pawn in PawnsFinder.AllMaps_PrisonersOfColony)
            {
                if (offeredPrisoners.Contains(pawn.thingIDNumber)) continue;

                var comp = pawn.TryGetComp<CompWorkTracker>();
                if (comp == null) continue;

                if (comp.earnedCoupons >= ransomAmount)
                {
                    offeredPrisoners.Add(pawn.thingIDNumber);

                    var letter = (ChoiceLetter_Ransom)LetterMaker.MakeLetter(
                        RP_LetterDefOf.RPR_RansomLetter);
                    letter.Label = "RimPrison.RansomLetterLabel".Translate(
                        pawn.LabelShortCap);
                    letter.Text = "RimPrison.RansomLetterText".Translate(
                        pawn.LabelShortCap, ransomAmount.ToString(),
                        comp.earnedCoupons.ToString(),
                        RimPrisonMod.Settings.WorkCouponName);
                    letter.prisoner = pawn;
                    letter.ransomAmount = ransomAmount;
                    letter.StartTimeout(60000); // 1 day to respond
                    Find.LetterStack.ReceiveLetter(letter);
                }
            }
        }

        public static void ClearOffered(Pawn pawn)
        {
            // Called when ransom is accepted or rejected so the pawn can be re-offered later
            var instance = Current.Game?.GetComponent<GameComponent_Ransom>();
            instance?.offeredPrisoners.Remove(pawn.thingIDNumber);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastCheckDay, "ransomLastCheckDay", -1);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var ids = new List<int>(offeredPrisoners);
                Scribe_Collections.Look(ref ids, "ransomOfferedIds", LookMode.Value);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var ids = new List<int>();
                Scribe_Collections.Look(ref ids, "ransomOfferedIds", LookMode.Value);
                offeredPrisoners = new HashSet<int>(ids);
            }
        }
    }
}
