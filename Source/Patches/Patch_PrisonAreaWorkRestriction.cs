using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using RimPrison.PrisonArea;

namespace RimPrison.Patches
{
    // Cache prison area! EZ opt (DONE)
    // Block specific work types for colonists and colony mechs inside the prison area.
    // Configured via Dialog_ManagePrisonAreaWork (a per-work-type checklist).
    // Patches HasJobOnThing(bool) on all WorkGiver_Scanner subclasses.
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

        static void Postfix(WorkGiver_Scanner __instance, Pawn pawn, Thing t, ref bool __result)
        {
            if (!__result) return;
            if (pawn == null || t == null) return;
            // Include ColonyMech here!
            if (!pawn.IsColonist && !pawn.IsColonyMech) return;

            var disabled = RimPrisonMod.Settings.DisabledWorkInPrisonArea;
            if (disabled.Count == 0) return;

            WorkTypeDef workType = __instance.def.workType;
            if (workType == null) return;
            if (!disabled.Contains(workType.defName)) return;

            var area = CachedPrisonArea(pawn.Map);
            if (area == null) return;
            if (!area[t.Position]) return;

            __result = false;
        }

        // Throttled cache: HasJobOnThing fires hundreds of times per frame.
        // Refresh every 5000 ticks (~83s at 1x speed) per map.
        private static Area_Prison cachedArea;
        private static Map cachedMap;
        private static int cacheRefreshTick;

        static Area_Prison CachedPrisonArea(Map map)
        {
            if (map == null) return null;
            int now = Find.TickManager.TicksGame;
            if (map != cachedMap || now >= cacheRefreshTick)
            {
                cachedMap = map;
                cachedArea = map.areaManager.Get<Area_Prison>();
                cacheRefreshTick = now + 5000;
            }
            return cachedArea;
        }
    }
}
