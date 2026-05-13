using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimPrison
{
    // [UNREVIEWED]
    public class RimPrisonSettings : ModSettings
    {
        public string WorkCouponName = "WorkCoupon";
        public float GlobalWageMultiplier = 1f;
        public int DailyAllowance = 0;
        public int DailyFee = 0;
        public int MaxDebt = 200;
        public int RansomAmount = 500;
        public int DebtHarvestThreshold = 150;
        public int DebtHarvestIntervalDays = 1;
        public bool DoorAccessEnabled;
        public bool RestrictColonistWorkInPrisonArea;

        // Per-work-type wage multiplier, keyed by workTypeDef.defName.
        // Default 1f — entries only stored when user changes from default.
        public Dictionary<string, float> WorkTypeWages = new Dictionary<string, float>();

        public float GetWorkTypeWage(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return 1f;
            return WorkTypeWages.TryGetValue(defName, out float w) ? w : 1f;
        }

        public void SetWorkTypeWage(string defName, float wage)
        {
            if (string.IsNullOrEmpty(defName)) return;
            WorkTypeWages[defName] = wage;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref WorkCouponName, "WorkCouponName", "WorkCoupon");
            Scribe_Values.Look(ref GlobalWageMultiplier, "CouponsPerHour", 1f); // keep old key
            Scribe_Values.Look(ref DailyAllowance, "DailyAllowance", 0);
            Scribe_Values.Look(ref DailyFee, "DailyFee", 0);
            Scribe_Values.Look(ref MaxDebt, "MaxDebt", 200);
            Scribe_Values.Look(ref RansomAmount, "RansomAmount", 500);
            Scribe_Values.Look(ref DebtHarvestThreshold, "DebtHarvestThreshold", 150);
            Scribe_Values.Look(ref DebtHarvestIntervalDays, "DebtHarvestIntervalDays", 1);
            Scribe_Values.Look(ref DoorAccessEnabled, "DoorAccessEnabled", false);
            Scribe_Values.Look(ref RestrictColonistWorkInPrisonArea, "RestrictColonistWorkInPrisonArea", false);

            // Dictionary → parallel lists for Scribe
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var wageKeys = new List<string>();
                var wageVals = new List<float>();
                foreach (var kv in WorkTypeWages)
                {
                    wageKeys.Add(kv.Key);
                    wageVals.Add(kv.Value);
                }
                Scribe_Collections.Look(ref wageKeys, "wageKeys", LookMode.Value);
                Scribe_Collections.Look(ref wageVals, "wageVals", LookMode.Value);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var wageKeys = new List<string>();
                var wageVals = new List<float>();
                Scribe_Collections.Look(ref wageKeys, "wageKeys", LookMode.Value);
                Scribe_Collections.Look(ref wageVals, "wageVals", LookMode.Value);
                // Scribe sets lists to null when nodes don't exist (old saves)
                if (wageKeys == null) wageKeys = new List<string>();
                if (wageVals == null) wageVals = new List<float>();
                WorkTypeWages.Clear();
                int n = System.Math.Min(wageKeys.Count, wageVals.Count);
                for (int i = 0; i < n; i++)
                    WorkTypeWages[wageKeys[i]] = wageVals[i];
            }
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("RimPrison.WorkCouponName".Translate());
            WorkCouponName = listing.TextEntry(WorkCouponName);

            listing.Gap(12f);

            listing.Label("RimPrison.GlobalWageMultiplier".Translate(GlobalWageMultiplier.ToString("F1")));
            GlobalWageMultiplier = listing.Slider(GlobalWageMultiplier, 0.1f, 10f);

            listing.Gap(12f);

            listing.Label("RimPrison.DailyAllowance".Translate());
            string buf = DailyAllowance.ToString();
            string result = listing.TextEntry(buf);
            if (int.TryParse(result, out int val) && val >= 0 && val <= 9999)
                DailyAllowance = val;

            listing.Gap(12f);

            listing.Label("RimPrison.DailyFee".Translate());
            string feeBuf = DailyFee.ToString();
            feeBuf = listing.TextEntry(feeBuf);
            if (int.TryParse(feeBuf, out int f) && f >= 0 && f <= 9999)
                DailyFee = f;

            listing.Gap(12f);

            listing.Label("RimPrison.MaxDebt".Translate());
            string debtBuf = MaxDebt.ToString();
            debtBuf = listing.TextEntry(debtBuf);
            if (int.TryParse(debtBuf, out int d) && d >= 0 && d <= 99999)
                MaxDebt = d;

            listing.Gap(12f);

            listing.Label("RimPrison.RansomAmount".Translate());
            string ransomBuf = RansomAmount.ToString();
            ransomBuf = listing.TextEntry(ransomBuf);
            if (int.TryParse(ransomBuf, out int r) && r >= 0 && r <= 99999)
                RansomAmount = r;

            listing.Gap(12f);

            listing.Label("RimPrison.DebtHarvestThreshold".Translate());
            string harvestBuf = DebtHarvestThreshold.ToString();
            harvestBuf = listing.TextEntry(harvestBuf);
            if (int.TryParse(harvestBuf, out int h) && h >= 0 && h <= 99999)
                DebtHarvestThreshold = h;

            listing.Gap(12f);

            listing.CheckboxLabeled("RimPrison.DoorAccessEnabled".Translate(), ref DoorAccessEnabled);

            listing.Gap(12f);

            listing.CheckboxLabeled("RimPrison.RestrictColonistWorkInPrisonArea".Translate(), ref RestrictColonistWorkInPrisonArea);

            listing.Gap(24f);
            if (listing.ButtonText("RimPrison.RemoveModButton".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RimPrison.RemoveModConfirm".Translate(),
                    () => RimPrisonRemovalUtility.RemoveModFromAllMaps(),
                    true,
                    "RimPrison.ConfirmRemove".Translate()));
            }

            listing.End();
        }
    }
}
