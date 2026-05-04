using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using RimPrison.Core;

namespace RimPrison.HarmonyPatches
{
    // IL transpiler that replaces pawn.Faction checks in WorkGiver scanning paths
    // Replace original Thing.get_Faction()
    // with PrisonLaborUtility.GetWorkFaction(pawn)
    // which reports Faction.OfPlayer for prisoners
    // Inspired by PrisonLabor
    [HarmonyPatch]
    public static class Patch_WorkGiverFaction
    {
        // Cache the replacement method once
        private static readonly MethodInfo s_getWorkFaction =
            typeof(PrisonLaborUtility).GetMethod(nameof(PrisonLaborUtility.GetWorkFaction));
        private static readonly MethodInfo s_getFaction =
            AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Faction));

        // Use reflection to discover all WorkGiver_Scanner subclasses 
        // and find their overrides of four key methods 
        // where pawn.Faction is typically checked during work scanning.
        // Only methods declared directly on the subclass (not inherited)
        // are collected — base virtual methods don't contain Faction checks.
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var type in typeof(WorkGiver_Scanner).Assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || !type.IsSubclassOf(typeof(WorkGiver_Scanner)))
                    continue;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.Name == "PotentialWorkThingsGlobal" ||
                        method.Name == "HasJobOnThing" ||
                        method.Name == "ShouldSkip" ||
                        method.Name == "JobOnThing")
                    {
                        yield return method;
                    }
                }
            }

            // Construction-related methods that have their own Faction checks
            yield return AccessTools.Method(typeof(WorkGiver_ConstructFinishFrames), nameof(WorkGiver_ConstructFinishFrames.JobOnThing));
            yield return AccessTools.Method(typeof(WorkGiver_ConstructDeliverResourcesToFrames), nameof(WorkGiver_ConstructDeliverResourcesToFrames.JobOnThing));
            yield return AccessTools.Method(typeof(WorkGiver_ConstructDeliverResourcesToBlueprints), nameof(WorkGiver_ConstructDeliverResourcesToBlueprints.JobOnThing));
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (i > 0
                    && codes[i - 1].opcode == OpCodes.Ldarg_1
                    && codes[i].opcode == OpCodes.Callvirt
                    && codes[i].OperandIs(s_getFaction))
                {
                    yield return new CodeInstruction(OpCodes.Call, s_getWorkFaction);
                }
                else
                {
                    yield return codes[i];
                }
            }
        }
    }
}
