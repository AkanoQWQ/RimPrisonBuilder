using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using RimPrison.PrisonLabor;

namespace RimPrison.Patches
{
    // Draw the allowed area selector for labor-enabled prisoners.
    [HarmonyPatch(typeof(PawnColumnWorker_AllowedArea), "DoCell")]
    public static class Patch_AllowedAreaUI
    {
        static void Postfix(Rect rect, Pawn pawn, PawnTable table)
        {
            if (!pawn.IsLaborEnabled())
                return;

            if (pawn.playerSettings == null)
                return;

            AreaAllowedGUI.DoAllowedAreaSelectors(rect, pawn);
        }
    }
}
