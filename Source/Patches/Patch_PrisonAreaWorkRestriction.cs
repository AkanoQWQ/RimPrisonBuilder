using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using RimPrison.PrisonArea;

namespace RimPrison.Patches
{
    // [OPTIMIZE] Cache prison area! EZ opt
    // [TODO] Disable specific jobs in the future
    // When RestrictColonistWorkInPrisonArea is enabled, block colonists from
    // taking jobs on things inside prison areas.
    //
    // Patches HasJobOnThing(bool) on all WorkGiver_Scanner subclasses.
    // The base returns bool, and every vanilla override also returns bool
    // (there are zero `new`-hiding Job-returning variants).
    // JobOnThing(Job) is always called through HasJobOnThing, so this
    // single patch covers all job-creation paths.
    [HarmonyPatch]
    public static class Patch_PrisonAreaWorkRestriction
    {
        // Only execute when starting game. Almost no cost
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var type in typeof(WorkGiver_Scanner).Assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || !type.IsSubclassOf(typeof(WorkGiver_Scanner)))
                    continue;

                var method = type.GetMethod("HasJobOnThing",
                    BindingFlags.Public | BindingFlags.Instance);
                if (method != null && method.ReturnType == typeof(bool))
                    yield return method;
            }
        }

        static void Postfix(Pawn pawn, Thing t, ref bool __result)
        {
            if (!__result) return;
            if (!RimPrisonMod.Settings.RestrictColonistWorkInPrisonArea) return;
            if (pawn == null || t == null) return;
            if (!pawn.IsColonist) return;

            var area = pawn.Map?.areaManager?.AllAreas?
                .Find(a => a is Area_Prison) as Area_Prison;
            if (area == null) return;
            if (!area[t.Position]) return;

            __result = false;
        }
    }
}
