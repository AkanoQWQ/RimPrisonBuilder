using UnityEngine;
using Verse;

namespace RimPrison.UI
{
    public class Dialog_RimPrisonHelp : Window
    {
        private Vector2 scrollPos;

        public override Vector2 InitialSize => new Vector2(520f, 480f);

        public Dialog_RimPrisonHelp()
        {
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "RimPrison.HelpTitle".Translate());
            Text.Font = GameFont.Small;

            float viewH = 1200f;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, viewH);
            Widgets.BeginScrollView(new Rect(0f, 35f, inRect.width, inRect.height - 40f),
                ref scrollPos, viewRect);

            float y = 0f;
            string text = "RimPrison.HelpContent".Translate();
            float textH = Text.CalcHeight(text, viewRect.width);
            Widgets.Label(new Rect(0f, y, viewRect.width, textH), text);

            Widgets.EndScrollView();
        }
    }
}
