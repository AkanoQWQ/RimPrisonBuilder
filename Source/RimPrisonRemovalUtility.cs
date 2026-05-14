using System.Collections.Generic;
using RimPrison.CouponShop;
using RimPrison.DefOfs;
using RimPrison.PrisonArea;
using RimPrison.PrisonLabor;
using RimWorld;
using Verse;

namespace RimPrison
{
    // [UNREVIEWED] Just simple review. Have not considered a lot.
    // Maybe better solution in the future.
    public static class RimPrisonRemovalUtility
    {
        public static void RemoveModFromAllMaps()
        {
            CleanGameComponents();

            foreach (var map in Find.Maps)
                RemoveFromMap(map);

            // World pawns (caravans, etc.) may have RimPrison_AllowLabor enabled.
            // Pawn_GuestTracker saves interaction modes by defName, which fails
            // after mod removal if the def no longer exists.
            CleanWorldPawns();

            CleanArchive();

            Messages.Message("RimPrison.RemovalComplete".Translate(),
                MessageTypeDefOf.NeutralEvent, false);
        }

        static void CleanGameComponents()
        {
            if (Current.Game?.components == null) return;
            Current.Game.components.RemoveAll(c =>
                c.GetType().Namespace.StartsWith("RimPrison"));
        }

        static void RemoveFromMap(Map map)
        {
            // Remove all RimPrison MapComponents (prevents abstract class fallback errors)
            map.components.RemoveAll(c =>
                c.GetType().Namespace.StartsWith("RimPrison"));

            // Destroy all coupon shops (items drop on ground)
            var shops = new List<Building>(
                map.listerBuildings.AllBuildingsColonistOfClass<Building_CouponShop>());
            foreach (var shop in shops)
                shop.Destroy(DestroyMode.Vanish);

            // Delete prison area
            map.areaManager.Get<Area_Prison>()?.Delete();

            // Reset AllowLabor on ALL map pawns (prisoners, slaves, anyone)
            // and remove despair hediffs
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                CleanPawn(pawn);
                // Interrupt any active job so custom JobDefs aren't serialized
                pawn.jobs?.StopAll();
            }
        }

        static void CleanWorldPawns()
        {
            // Alive + mothballed (caravans, off-map pawns)
            var alives = Find.WorldPawns?.AllPawnsAlive;
            if (alives != null)
            {
                foreach (var pawn in alives)
                    CleanPawn(pawn);
            }

            // Dead world pawns are also Deep-serialized
            var deads = Find.WorldPawns?.AllPawnsDead;
            if (deads != null)
            {
                foreach (var pawn in deads)
                    CleanPawn(pawn);
            }
        }

        static void CleanPawn(Pawn pawn)
        {
            pawn.jobs?.StopAll();

            if (pawn.guest?.IsInteractionEnabled(RP_DefOf.RimPrison_AllowLabor) == true)
                pawn.guest.ToggleNonExclusiveInteraction(RP_DefOf.RimPrison_AllowLabor, false);

            if (pawn.health?.hediffSet != null)
            {
                var despair = pawn.health.hediffSet.GetFirstHediffOfDef(RP_HediffDefOf.RPR_Despair);
                if (despair != null) pawn.health.RemoveHediff(despair);

                var harsh = pawn.health.hediffSet.GetFirstHediffOfDef(RP_HediffDefOf.RPR_RegimeHarsh);
                if (harsh != null) pawn.health.RemoveHediff(harsh);

                var deter = pawn.health.hediffSet.GetFirstHediffOfDef(RP_HediffDefOf.RPR_RegimeDeterrence);
                if (deter != null) pawn.health.RemoveHediff(deter);

                var equal = pawn.health.hediffSet.GetFirstHediffOfDef(RP_HediffDefOf.RPR_RegimeEquality);
                if (equal != null) pawn.health.RemoveHediff(equal);
            }

            // Remove memory thoughts added by prisoner shopping / allowance
            pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(
                RP_ThoughtDefOf.RPR_BoughtPremiumFood);
            pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(
                RP_ThoughtDefOf.RPR_BoughtDrug);
            pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(
                RP_ThoughtDefOf.RPR_AllowanceGood);
            pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(
                RP_ThoughtDefOf.RPR_FeeExploitation);
        }

        static void CleanArchive()
        {
            if (Find.Archive == null) return;
            var toRemove = new List<IArchivable>();
            foreach (var a in Find.Archive.ArchivablesListForReading)
            {
                if (a is ChoiceLetter_Ransom)
                    toRemove.Add(a);
            }
            foreach (var letter in toRemove)
                Find.Archive.Remove(letter);
        }
    }
}
