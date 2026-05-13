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

            // Reset prisoner labor flag and remove despair hediffs
            foreach (var pawn in map.mapPawns.PrisonersOfColony)
            {
                if (pawn.guest.IsInteractionEnabled(RP_DefOf.RimPrison_AllowLabor))
                    pawn.guest.ToggleNonExclusiveInteraction(RP_DefOf.RimPrison_AllowLabor, false);

                var despair = pawn.health?.hediffSet?.GetFirstHediffOfDef(RP_HediffDefOf.RPR_Despair);
                if (despair != null)
                    pawn.health.RemoveHediff(despair);
            }
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
