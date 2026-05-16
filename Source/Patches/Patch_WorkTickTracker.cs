using HarmonyLib;
using RimPrison.PrisonLabor;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimPrison.Patches
{
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.DriverTick))]
    public static class Patch_WorkTickTracker
    {
        private static void Postfix(JobDriver __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null) return;
            if (!PrisonLaborUtility.IsLaborEnabled(pawn)) return;

            var workGiverDef = pawn.CurJob?.workGiverDef;

            // Fallback: some mods create work jobs outside JobGiver_Work,
            // leaving workGiverDef null. Check the compat map.
            if (workGiverDef == null)
            {
                string jobDefName = pawn.CurJob?.def.defName;
                if (jobDefName == null || !Compat.JobToWorkTypeMapper.Map.TryGetValue(jobDefName, out var mapped))
                    return;
                var comp = pawn.TryGetComp<CompWorkTracker>();
                if (comp == null) return;
                comp.Notify_WorkTick(mapped);
                return;
            }

            // Normal path: job from JobGiver_Work has workGiverDef set.
            {
                var comp = pawn.TryGetComp<CompWorkTracker>();
                if (comp == null) return;
                string wtName = workGiverDef.workType?.defName;
                comp.Notify_WorkTick(wtName);
            }
        }
    }
}
