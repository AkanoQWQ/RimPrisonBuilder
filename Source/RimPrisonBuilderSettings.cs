using UnityEngine;
using Verse;

namespace RimPrisonBuilder
{
    public class RimPrisonBuilderSettings : ModSettings
    {
        public string WorkCouponName = "WorkCoupon";
        public float CouponsPerHour = 1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref WorkCouponName, "WorkCouponName", "WorkCoupon");
            Scribe_Values.Look(ref CouponsPerHour, "CouponsPerHour", 1f);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("RimPrisonBuilder.WorkCouponName".Translate());
            WorkCouponName = listing.TextEntry(WorkCouponName);

            listing.Gap(12f);

            listing.Label("RimPrisonBuilder.CouponsPerHour".Translate(CouponsPerHour.ToString("F1")));
            CouponsPerHour = listing.Slider(CouponsPerHour, 0.1f, 10f);

            listing.End();
        }
    }
}
