using UnityEngine;
using Verse;
using RimWorld;
using RimPrisonBuilder.PrisonLabor;

namespace RimPrisonBuilder.UI
{
    // [UNREVIEWED] Haven't reviewed carefully
    public class Dialog_GrantCoupons : Window
    {
        private Pawn pawn;
        private string inputBuffer = "0";
        private bool focused;

        public Dialog_GrantCoupons(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseX = true;
            doCloseButton = false;
            forcePause = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            optionalTitle = "RimPrisonBuilder.GrantCouponsTitle".Translate(pawn.LabelShortCap);
        }

        public override Vector2 InitialSize => new Vector2(320f, 240f);

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y + 8f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f),
                "RimPrisonBuilder.GrantCouponsDesc".Translate(RimPrisonBuilderMod.Settings.WorkCouponName));
            y += 35f;

            // Number input
            Rect inputRect = new Rect(inRect.x, y, inRect.width - 20f, 30f);
            GUI.SetNextControlName("CouponInput");
            string text = Widgets.TextField(inputRect, inputBuffer);

            // Only allow digits
            if (IsDigitsOnly(text) && text.Length < 8)
                inputBuffer = text;

            if (!focused)
            {
                Verse.UI.FocusControl("CouponInput", this);
                focused = true;
            }

            y += 45f;

            // Confirm button
            Rect btnRect = new Rect(inRect.x + 40f, y, inRect.width - 80f, 35f);
            if (Widgets.ButtonText(btnRect, "RimPrisonBuilder.ConfirmGrant".Translate())
                || (Event.current.type == EventType.KeyDown
                    && Event.current.keyCode == KeyCode.Return))
            {
                if (int.TryParse(inputBuffer, out int amount) && amount > 0)
                {
                    var comp = pawn.TryGetComp<CompWorkTracker>();
                    if (comp != null)
                    {
                        comp.earnedCoupons += amount;
                    }
                    Close();
                }
            }
        }

        private static bool IsDigitsOnly(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            for (int i = 0; i < s.Length; i++)
            {
                if (!char.IsDigit(s[i])) return false;
            }
            return true;
        }
    }
}
