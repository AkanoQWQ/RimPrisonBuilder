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

            // Filter job like "Rest/GetFood"
            // Of course you can't get paid for eating!
            var workGiverDef = pawn.CurJob?.workGiverDef;
            if (workGiverDef == null) return;

            var comp = pawn.TryGetComp<CompWorkTracker>();
            if (comp == null) return;

            string wtName = workGiverDef.workType?.defName;
            comp.Notify_WorkTick(wtName);
        }
    }
}
