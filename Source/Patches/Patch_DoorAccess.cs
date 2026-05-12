using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.DoorAccess;

namespace RimPrison.Patches
{
    // AI said:
    // Comp_DoorAccess is now injected via XML patch (Patch_DoorAccess.xml) at the Def level.
    // This ensures proper Comp lifecycle through vanilla InitializeComps().
    // All doors get the comp; DoorAccessEnabled setting only controls enforcement.
    // Override door access for prisoners. When allowPrisoners=true, grant access
    // (vanilla GenAI.MachinesLike blocks all prisoners). When false, block access.
    // Only active when DoorAccessEnabled setting is on.
    // INTENTIONAL: We unconditionally overwrite __result for prisoners. This bypasses
    // vanilla's faction-based door check (GenAI.MachinesLike) which blocks all prisoners.
    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.PawnCanOpen))]
    internal static class Patch_DoorAccess_CanOpen
    {
        static void Postfix(Building_Door __instance, Pawn p, ref bool __result)
        {
            if (!RimPrisonMod.Settings.DoorAccessEnabled) return;
            if (p == null) return;
            if (!p.IsPrisonerOfColony) return;

            // Linear search! But OK I think...
            var comp = __instance.GetComp<Comp_DoorAccess>();
            if (comp == null) return;
            if (comp.allowPrisoners)
                __result = true;
            else
                __result = false;
        }
    }

    // Add door access gizmo — always visible so users can configure before enabling.
    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.GetGizmos))]
    internal static class Patch_DoorAccess_Gizmo
    {
        static void Postfix(Building_Door __instance, ref IEnumerable<Gizmo> __result)
        {
            var comp = __instance.GetComp<Comp_DoorAccess>();
            if (comp == null) return;

            var list = new List<Gizmo>(__result) { new Gizmo_DoorAccess(comp) };
            __result = list;
        }
    }
}
