using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.Core;

namespace RimPrison.HarmonyPatches
{
    // Prevent auto-forbid when a labor-enabled prisoner drops items.
    // Vanilla forbids items dropped by hostile factions as anti-theft.
    [HarmonyPatch]
    public static class Patch_AutoForbidFix
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var m in typeof(Pawn_CarryTracker).GetMethods())
            {
                if (m.Name == "TryDropCarriedThing")
                    yield return m;
            }
        }

        static void Postfix(Pawn_CarryTracker __instance, Thing resultingThing)
        {
            if (resultingThing == null)
                return;

            var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn != null && pawn.IsLaborEnabled())
                resultingThing.SetForbidden(false);
        }
    }
}
