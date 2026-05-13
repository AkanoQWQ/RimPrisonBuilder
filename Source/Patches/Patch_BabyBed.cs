using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.Patches
{
    // Add ForPrisoners toggle to baby beds (cribs/bassinets).
    // Baby beds can only be ForPrisoners inside already-existing prison cells.
    // Auto-sync: baby beds built in prison cells are auto-marked ForPrisoners.
    [HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.GetGizmos))]
    internal static class Patch_BabyBed_Gizmo
    {
        private static readonly FieldInfo forOwnerTypeField =
            AccessTools.Field(typeof(Building_Bed), "forOwnerType");
        private static readonly MethodInfo removeAllOwners =
            AccessTools.Method(typeof(Building_Bed), "RemoveAllOwners");
        private static readonly MethodInfo notifyColorChanged =
            AccessTools.Method(typeof(Building_Bed), "Notify_ColorChanged");

        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_Bed __instance)
        {
            foreach (var g in __result)
                yield return g;

            if (__instance.Faction != Faction.OfPlayer) yield break;
            if (!__instance.ForHumanBabies) yield break;
            if (!__instance.def.building.bed_humanlike) yield break;

            bool isPrisoner = (BedOwnerType)forOwnerTypeField.GetValue(__instance) == BedOwnerType.Prisoner;

            var toggle = new Command_Toggle
            {
                defaultLabel = "CommandBedSetForPrisonersLabel".Translate(),
                defaultDesc = "CommandBedSetForPrisonersDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ForPrisoners"),
                isActive = () => (BedOwnerType)forOwnerTypeField.GetValue(__instance) == BedOwnerType.Prisoner,
                toggleAction = () =>
                {
                    bool current = (BedOwnerType)forOwnerTypeField.GetValue(__instance) == BedOwnerType.Prisoner;
                    if (current)
                    {
                        forOwnerTypeField.SetValue(__instance, BedOwnerType.Colonist);
                        removeAllOwners.Invoke(__instance, new object[] { false });
                    }
                    else
                    {
                        forOwnerTypeField.SetValue(__instance, BedOwnerType.Prisoner);
                        removeAllOwners.Invoke(__instance, new object[] { false });
                    }
                    notifyColorChanged.Invoke(__instance, null);
                },
                hotKey = KeyBindingDefOf.Misc3
            };

            // Only allow turning on inside existing prison cells
            if (!isPrisoner)
            {
                var room = __instance.GetRoom();
                if (room == null || !room.IsPrisonCell)
                    toggle.Disable("RimPrison.BabyBedMustBeInPrisonCell".Translate());
            }

            yield return toggle;
        }
    }

    // Auto-mark baby beds in prison rooms as ForPrisoners on spawn.
    [HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.SpawnSetup))]
    internal static class Patch_BabyBed_AutoSync
    {
        private static readonly FieldInfo forOwnerTypeField =
            AccessTools.Field(typeof(Building_Bed), "forOwnerType");
        private static readonly MethodInfo notifyColorChanged =
            AccessTools.Method(typeof(Building_Bed), "Notify_ColorChanged");

        static void Postfix(Building_Bed __instance)
        {
            if (!__instance.ForHumanBabies) return;
            if (__instance.Faction != Faction.OfPlayer) return;
            if ((BedOwnerType)forOwnerTypeField.GetValue(__instance) == BedOwnerType.Prisoner) return;
            var room = __instance.GetRoom();
            if (room == null || !room.IsPrisonCell) return;

            forOwnerTypeField.SetValue(__instance, BedOwnerType.Prisoner);
            notifyColorChanged.Invoke(__instance, null);
        }
    }
}
