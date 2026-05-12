using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using RimPrison.PrisonArea;

namespace RimPrison.Patches
{
    // Add "Create prison area" button to the area management dialog.
    // Only shown when no Area_Prison exists yet on this map.
    [HarmonyPatch(typeof(Dialog_ManageAreas), nameof(Dialog_ManageAreas.DoWindowContents))]
    internal static class Patch_ManageAreas_AddPrisonArea
    {
        static void Postfix(Dialog_ManageAreas __instance, Rect inRect, Map ___map)
        {
            if (___map.areaManager.Get<Area_Prison>() != null) return;

            float y = inRect.yMax - 35f;
            Rect btnRect = new Rect(inRect.x, y, inRect.width, 30f);
            if (Widgets.ButtonText(btnRect, "RimPrison.CreatePrisonArea".Translate()))
            {
                Area_Prison.CreateNew(___map);
            }
        }
    }
}
