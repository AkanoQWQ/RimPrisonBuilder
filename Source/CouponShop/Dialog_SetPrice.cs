using RimPrison.UI;
using UnityEngine;
using Verse;

namespace RimPrison.CouponShop
{
    // [UNREVIEWED] This is the UI of SetPrice of couponshop
    // Have't reviewed it now.
    public class Dialog_SetPrice : Window
    {
        private Building_CouponShop shop;
        private string priceBuffer;

        public override Vector2 InitialSize => new Vector2(300f, 150f);

        public Dialog_SetPrice(Building_CouponShop shop)
        {
            this.shop = shop;
            priceBuffer = shop.PricePerItem.ToString();
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 10f;
            float w = inRect.width - 20f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(10f, y, w, 30f),
                "RimPrison.SetPriceDialogTitle".Translate(RimPrisonMod.Settings.WorkCouponName));
            y += 38f;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(10f, y, 120f, 28f),
                "RimPrison.PricePerItem".Translate());
            priceBuffer = Widgets.TextField(new Rect(130f, y, 80f, 28f), priceBuffer);
            y += 36f;

            if (RPR_UiStyle.DrawColoredButton(new Rect(10f, y, 120f, 32f), "RimPrison.ConfirmPrice".Translate()))
            {
                if (int.TryParse(priceBuffer, out int price) && price >= 0)
                {
                    shop.CouponComp.pricePerItem = price;
                    Close();
                }
            }

            if (RPR_UiStyle.DrawColoredButton(new Rect(140f, y, 120f, 32f), "CancelButton".Translate()))
            {
                Close();
            }
        }
    }
}
