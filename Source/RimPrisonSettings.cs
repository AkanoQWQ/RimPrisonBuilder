using UnityEngine;
using Verse;

namespace RimPrison
{
    public class RimPrisonSettings : ModSettings
    {
        public string WorkCouponName = "WorkCoupon";
        public float CouponsPerHour = 1f;
        public int DailyAllowance = 0;
        public bool DoorAccessEnabled;
        public bool RestrictColonistWorkInPrisonArea;
        public int MaxDebt = 200;
        public string MaxDebtBuffer = "200";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref WorkCouponName, "WorkCouponName", "WorkCoupon");
            Scribe_Values.Look(ref CouponsPerHour, "CouponsPerHour", 1f);
            Scribe_Values.Look(ref DailyAllowance, "DailyAllowance", 0);
            Scribe_Values.Look(ref DoorAccessEnabled, "DoorAccessEnabled");
            Scribe_Values.Look(ref RestrictColonistWorkInPrisonArea, "RestrictColonistWorkInPrisonArea");
            Scribe_Values.Look(ref MaxDebt, "MaxDebt", 200);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("RimPrison.WorkCouponName".Translate());
            WorkCouponName = listing.TextEntry(WorkCouponName);

            listing.Gap(12f);

            listing.Label("RimPrison.CouponsPerHour".Translate(CouponsPerHour.ToString("F1")));
            CouponsPerHour = listing.Slider(CouponsPerHour, 0.1f, 10f);

            listing.Gap(12f);

            listing.Label("RimPrison.DailyAllowance".Translate());
            string buf = DailyAllowance.ToString();
            string result = listing.TextEntry(buf);
            if (int.TryParse(result, out int val) && val >= 0 && val <= 9999)
                DailyAllowance = val;

            listing.Gap(12f);

            listing.CheckboxLabeled("RimPrison.DoorAccessEnabled".Translate(), ref DoorAccessEnabled);

            listing.Gap(12f);

            listing.CheckboxLabeled("RimPrison.RestrictColonistWorkInPrisonArea".Translate(), ref RestrictColonistWorkInPrisonArea);

            listing.Gap(12f);

            listing.Label("RimPrison.MaxDebt".Translate());
            MaxDebtBuffer = listing.TextEntry(MaxDebtBuffer);
            if (int.TryParse(MaxDebtBuffer, out int d) && d >= 0 && d <= 99999)
                MaxDebt = d;

            listing.End();
        }
    }
}
