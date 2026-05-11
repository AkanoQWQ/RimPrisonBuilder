using UnityEngine;
using Verse;

namespace RimPrison.UI
{
    // [UNREVIEWED] Haven't reviewed carefully
    public static class RPR_UiStyle
    {
        public static readonly Color PanelBg = new Color(0.12f, 0.125f, 0.13f, 0.62f);
        public static readonly Color SubPanelBg = new Color(0.18f, 0.185f, 0.19f, 0.38f);
        public static readonly Color MutedPanelBg = new Color(1f, 1f, 1f, 0.035f);
        public static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.075f);
        public static readonly Color TextMuted = new Color(0.72f, 0.74f, 0.76f, 1f);
        public static readonly Color SelectionColor = new Color(0.28f, 0.38f, 0.46f, 0.58f);

        public static void DrawPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, PanelBg);
            DrawSoftBorder(rect);
        }

        public static void DrawSubPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, SubPanelBg);
            DrawSoftBorder(rect);
        }

        public static void DrawSectionTitle(Rect rect, string label)
        {
            var oldFont = Text.Font;
            Text.Font = GameFont.Small;
            Widgets.Label(rect, label);
            Text.Font = oldFont;
        }

        public static void DrawMutedLabel(Rect rect, string label)
        {
            var oldColor = GUI.color;
            GUI.color = TextMuted;
            Widgets.Label(rect, label);
            GUI.color = oldColor;
        }

        public static void DrawSoftBorder(Rect rect)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), BorderColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BorderColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 1f, rect.height), BorderColor);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), BorderColor);
        }
    }
}
