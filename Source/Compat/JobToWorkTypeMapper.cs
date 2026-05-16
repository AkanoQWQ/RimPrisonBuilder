using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace RimPrison.Compat
{
    // Maps JobDef.defName → workType.defName for mods that create work jobs
    // outside JobGiver_Work (e.g. comp-driven job assignment). When a job's
    // workGiverDef is null, Patch_WorkTickTracker checks this map to find
    // the correct workType for wage calculation.
    [StaticConstructorOnStartup]
    public static class JobToWorkTypeMapper
    {
        public static readonly Dictionary<string, string> Map = new();

        static JobToWorkTypeMapper()
        {
            TryAdd("RK_Job_HamsterWheel", "RK_WorkType_GeneratePower",
                "NewRatkin.WorkGiver_HamsterWheel");

            // Add more mod-specific mappings here as needed.
            // First param: JobDef.defName, second: WorkTypeDef.defName,
            // third: any type name from the mod (used to verify it's loaded).
        }

        private static void TryAdd(string jobDefName, string workTypeName, string checkTypeName)
        {
            if (AccessTools.TypeByName(checkTypeName) != null)
            {
                Map[jobDefName] = workTypeName;
                Log.Message($"[RimPrison] Compat: mapped {jobDefName} → {workTypeName}");
            }
        }
    }
}
