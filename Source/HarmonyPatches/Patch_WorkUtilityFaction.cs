using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using RimPrison.Core;

namespace RimPrison.HarmonyPatches
{
    // Same faction replacement as Patch_WorkGiverFaction, but for static
    // utility methods where pawn is Ldarg_0 instead of Ldarg_1.
    [HarmonyPatch]
    public static class Patch_WorkUtilityFaction
    {
        private static readonly MethodInfo s_getWorkFaction =
            typeof(PrisonLaborUtility).GetMethod(nameof(PrisonLaborUtility.GetWorkFaction));

        private static readonly MethodInfo s_getFaction =
            AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Faction));

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(RepairUtility), nameof(RepairUtility.PawnCanRepairEver));
            yield return AccessTools.Method(typeof(RepairUtility), nameof(RepairUtility.PawnCanRepairNow));
            yield return AccessTools.Method(typeof(HaulAIUtility), nameof(HaulAIUtility.HaulToStorageJob));
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (i > 0
                    && codes[i - 1].opcode == OpCodes.Ldarg_0
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
