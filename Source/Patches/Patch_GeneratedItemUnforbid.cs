using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // When a labor-enabled prisoner harvests plants or mines, vanilla
    // auto-forbids the resulting items (actor.Faction != Faction.OfPlayer).
    // Our FactionInjection only covers WorkGiver scanning, not JobDriver
    // execution. Instead of transpiling into compiler-generated lambdas,
    // we undo the forbid immediately after it happens

    // Plant harvest: SetForbidden(true) then Notify_PlantHarvested is called.
    [HarmonyPatch(typeof(QuestManager), "Notify_PlantHarvested")]
    public static class Patch_PlantHarvestUnforbid
    {
        static void Postfix(Pawn worker, Thing harvested)
        {
            if (worker != null && harvested != null && worker.IsLaborEnabled())
                harvested.SetForbidden(false, warnOnFail: false);
        }
    }

    // Mining: Items are forbidden by Mineable.TrySpawnYield (pawn=null path)
    // and again by the tickAction inside JobDriver_Mine. We add a finishAction
    // to the mining Toil that un-forbids everything at the mined cell.
    [HarmonyPatch(typeof(JobDriver_Mine), "MakeNewToils")]
    public static class Patch_MineUnforbid
    {
        static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_Mine __instance)
        {
            var toils = __result.ToList();

            if (toils.Count > 0)
            {
                toils[toils.Count - 1].AddFinishAction(() =>
                {
                    var pawn = __instance.pawn;
                    if (pawn == null || !pawn.IsLaborEnabled())
                        return;

                    var mineTarget = __instance.job?.GetTarget(TargetIndex.A).Thing;
                    if (mineTarget == null || !mineTarget.Destroyed)
                        return;

                    var things = mineTarget.Position.GetThingList(pawn.Map);
                    for (int i = 0; i < things.Count; i++)
                        things[i].SetForbidden(false, warnOnFail: false);
                });
            }

            return toils;
        }
    }
}
