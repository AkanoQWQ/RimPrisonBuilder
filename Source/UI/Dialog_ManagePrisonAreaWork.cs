using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.UI
{
    // [UNREVIEWED] Typical unreviewed UI!
    public class Dialog_ManagePrisonAreaWork : Window
    {
        private Vector2 scrollPos;
        private HashSet<string> disabled;

        private static List<WorkTypeDef> allWorkTypes;
        private const int Cols = 4;  // per row

        public Dialog_ManagePrisonAreaWork()
        {
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = false;
            draggable = true;
            absorbInputAroundWindow = true;
            layer = WindowLayer.Dialog;
            optionalTitle = "RimPrison.ManagePrisonAreaWork".Translate();

            disabled = RimPrisonMod.Settings.DisabledWorkInPrisonArea;

            if (allWorkTypes == null)
            {
                allWorkTypes = new List<WorkTypeDef>();
                foreach (var wt in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    allWorkTypes.Add(wt);
                allWorkTypes.SortBy(wt => wt.labelShort);
            }
        }

        public override Vector2 InitialSize => new Vector2(700f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            float rowH = 26f;
            int rows = (allWorkTypes.Count + Cols - 1) / Cols;
            float totalH = rows * rowH;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, totalH);
            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);

            float colW = (viewRect.width - 8f) / Cols;

            for (int i = 0; i < allWorkTypes.Count; i++)
            {
                var wt = allWorkTypes[i];
                int col = i % Cols;
                int row = i / Cols;

                Rect cell = new Rect(col * colW, row * rowH, colW, rowH);

                if ((col % 2 == 0 && row % 2 == 0) || (col % 2 == 1 && row % 2 == 1))
                    Widgets.DrawLightHighlight(cell);

                bool blocked = disabled.Contains(wt.defName);
                Rect checkRect = new Rect(cell.x + 2f, cell.y + 4f, 18f, 18f);
                Widgets.Checkbox(checkRect.position, ref blocked, 18f);
                Rect labelRect = new Rect(cell.x + 22f, cell.y, cell.width - 24f, rowH);
                Widgets.Label(labelRect, wt.labelShort.CapitalizeFirst());

                if (blocked != disabled.Contains(wt.defName))
                {
                    if (blocked)
                        disabled.Add(wt.defName);
                    else
                        disabled.Remove(wt.defName);
                    RimPrisonMod.Settings.Write();
                }
            }

            Widgets.EndScrollView();
        }
    }
}
