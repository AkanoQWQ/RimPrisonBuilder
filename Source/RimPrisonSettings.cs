using UnityEngine;
using Verse;

namespace RimPrison
{
    public class RimPrisonSettings : ModSettings
    {
        public string WorkCouponName = "WorkCoupon";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref WorkCouponName, "WorkCouponName", "WorkCoupon");
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("RimPrison.WorkCouponName".Translate());
            WorkCouponName = listing.TextEntry(WorkCouponName);

            listing.End();
        }
    }
}
