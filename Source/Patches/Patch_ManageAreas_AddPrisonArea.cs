using HarmonyLib;
using RimPrison.PrisonArea;
using RimPrison.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.Patches
{
    // [UNREVIEWED]
    // Add "Create prison area" button when Area_Prison doesn't exist.
    // When Area_Prison exists, show a custom row with expand/clear buttons
    // (like vanilla Home area, Area_Prison is not editable through standard dialog).
    [HarmonyPatch(typeof(Dialog_ManageAreas), nameof(Dialog_ManageAreas.DoWindowContents))]
    internal static class Patch_ManageAreas_AddPrisonArea
    {
        static void Postfix(Dialog_ManageAreas __instance, Rect inRect, Map ___map)
        {
            var area = ___map.areaManager.Get<Area_Prison>();

            float y = inRect.yMax - 35f;
            if (area == null)
            {
                Rect btnRect = new Rect(inRect.x, y, inRect.width, 30f);
                if (RPR_UiStyle.DrawColoredButton(btnRect, "RimPrison.CreatePrisonArea".Translate()))
                {
                    Area_Prison.CreateNew(___map);
                }
            }
            else
            {
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
}
