using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
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

        static void Postfix(Pawn_CarryTracker __instance, Pawn ___pawn, Thing resultingThing)
        {
            if (resultingThing == null)
                return;
            if (___pawn != null && ___pawn.IsLaborEnabled())
                resultingThing.SetForbidden(false, warnOnFail: false);
        }
    }
}
