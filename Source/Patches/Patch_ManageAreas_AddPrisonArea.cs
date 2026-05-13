using HarmonyLib;
using RimPrison.PrisonArea;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.Patches
{
    // [UNREVIWED]
    // Show prison area row in Dialog_ManageAreas with expand/clear/rename/delete.
    // Area_Prison is auto-created by Patch_AreaManager_AddStartingAreas,
    // matching vanilla Home Area behavior (no manual "create" needed).
    // For existing saves, auto-created on first open of this dialog.
    [HarmonyPatch(typeof(Dialog_ManageAreas), nameof(Dialog_ManageAreas.DoWindowContents))]
    internal static class Patch_ManageAreas_AddPrisonArea
    {
        static void Postfix(Dialog_ManageAreas __instance, Rect inRect, Map ___map)
        {
            var area = ___map.areaManager.Get<Area_Prison>();
            if (area == null)
                area = Area_Prison.CreateNew(___map);

            float y = inRect.yMax - 35f;
            Rect rowRect = new Rect(inRect.x, y - 30f, inRect.width, 28f);
            if (Mouse.IsOver(rowRect))
            {
                area.MarkForDraw();
                GUI.color = area.Color;
                Widgets.DrawHighlight(rowRect);
                GUI.color = Color.white;
            }
            Widgets.BeginGroup(rowRect);
            WidgetRow widgetRow = new WidgetRow(0f, 0f);
            widgetRow.Icon(area.ColorTexture);
            widgetRow.Gap(4f);
            using (new TextBlock(TextAnchor.LowerLeft))
            {
                widgetRow.LabelEllipses(area.Label, 120f);
            }
            if (widgetRow.ButtonText("RimPrison.ExpandPrisonArea".Translate(), null,
                drawBackground: true, doMouseoverSound: true, active: true, 90f))
            {
                Find.MainTabsRoot.EscapeCurrentTab();
                foreach (var des in DesignationCategoryDefOf.Zone.AllResolvedDesignators)
                {
                    if (des is Designator_AreaPrisonExpand expand)
                    {
                        Find.DesignatorManager.Select(expand);
                    }
                }
                Find.WindowStack.TryRemove(__instance);
            }
            if (widgetRow.ButtonText("RimPrison.ClearPrisonArea".Translate(), null,
                drawBackground: true, doMouseoverSound: true, active: true, 90f))
            {
                Find.MainTabsRoot.EscapeCurrentTab();
                foreach (var des in DesignationCategoryDefOf.Zone.AllResolvedDesignators)
                {
                    if (des is Designator_AreaPrisonClear clear)
                    {
                        Find.DesignatorManager.Select(clear);
                    }
                }
                Find.WindowStack.TryRemove(__instance);
            }
            if (widgetRow.ButtonIcon(TexButton.Rename, null, GenUI.SubtleMouseoverColor))
            {
                Find.WindowStack.Add(new Dialog_RenameArea(area));
            }
            if (widgetRow.ButtonIcon(TexButton.Delete, null, GenUI.SubtleMouseoverColor))
            {
                area.Delete();
            }
            Widgets.EndGroup();
        }
    }
}
