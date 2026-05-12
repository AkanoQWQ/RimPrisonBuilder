using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrison.UI
{
    // [UNREVIEWED]
    public class Dialog_SetWage : Window
    {
        private WorkTypeDef workType;
        private string buffer;

        public override Vector2 InitialSize => new Vector2(340f, 190f);

        public Dialog_SetWage(WorkTypeDef workType)
        {
            this.workType = workType;
            float current = RimPrisonMod.Settings.GetWorkTypeWage(workType.defName);
            buffer = current.ToString("F1");
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 10f, inRect.width, 30f),
                "RimPrison.SetWageTitle".Translate(workType.labelShort.CapitalizeFirst()));
            Text.Font = GameFont.Small;

            float y = 56f;
            Widgets.Label(new Rect(0f, y, inRect.width - 80f, 30f),
                "RimPrison.WageBaseRate".Translate());
            buffer = Widgets.TextField(new Rect(inRect.width - 80f, y, 70f, 30f), buffer);

            y += 44f;
            if (RPR_UiStyle.DrawColoredButton(new Rect(inRect.width / 2f - 80f, y, 160f, 36f),
                "RimPrison.ConfirmWage".Translate()))
            {
                if (float.TryParse(buffer, out float val) && val >= 0f && val <= 100f)
                {
                    RimPrisonMod.Settings.SetWorkTypeWage(workType.defName, val);
                    RimPrisonMod.Settings.Write();
                    Close();
                }
            }
        }
    }
}
