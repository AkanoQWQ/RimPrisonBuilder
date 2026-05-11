using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimPrisonBuilder.PrisonLabor
{
    public class CompProperties_ReformTracker : CompProperties
    {
        public CompProperties_ReformTracker()
        {
            compClass = typeof(CompReformTracker);
        }
    }

    public class CompReformTracker : ThingComp
    {
        private const int PeriodTicks = 7500; // 3 in-game hours
        private const int TicksPerHour = 2500;
        private const int PeriodCount = 8; // 8 x 3h = 24h rolling window

        // --- Persistent state ---
        public float reformValue;
        public float currentDailyRate;

        // Breakdown for UI tooltip (populated each period)
        private float brPositiveFood;
        private float brPositiveRec;
        private float brPositiveWork;
        private float brPositiveCapped;
        private float brNegativeFood;
        private float brNegativeRec;
        private float brNegativeWorkIdle;
        private float brNegativeWorkOverwork;
        private float brMultiplier;
        private float brWorkHours;
        private bool brIsJuvenile;

        private int periodTickCounter;
        private int periodIndex;
        private int periodsCompleted;

        // 24h rolling buffers (8 periods x 3h each)
        private List<int> foodStarvingBuf;
        private List<int> foodFedBuf;
        private List<int> foodTotalBuf;
        private List<int> recBoredBuf;
        private List<int> recEntertainedBuf;
        private List<int> recTotalBuf;
        private List<int> workBuf;

        private Pawn Pawn => (Pawn)parent;

        private void EnsureBuffers()
        {
            if (workBuf != null)
                return;
            foodStarvingBuf = NewPeriodList();
            foodFedBuf = NewPeriodList();
            foodTotalBuf = NewPeriodList();
            recBoredBuf = NewPeriodList();
            recEntertainedBuf = NewPeriodList();
            recTotalBuf = NewPeriodList();
            workBuf = NewPeriodList();
        }

        private static List<int> NewPeriodList()
        {
            var list = new List<int>(PeriodCount);
            for (int i = 0; i < PeriodCount; i++)
                list.Add(0);
            return list;
        }

        public override void CompTick()
        {
            Pawn pawn = Pawn;
            if (!pawn.IsPrisonerOfColony)
                return;

            EnsureBuffers();

            periodTickCounter++;

            // Sample food
            Need_Food food = pawn.needs?.food;
            if (food != null)
            {
                foodTotalBuf[periodIndex]++;
                float level = food.CurLevelPercentage;
                if (level < 0.15f)
                    foodStarvingBuf[periodIndex]++;
                if (level > 0.3f)
                    foodFedBuf[periodIndex]++;
            }

            // Sample recreation
            Need_Joy joy = pawn.needs?.joy;
            if (joy != null)
            {
                recTotalBuf[periodIndex]++;
                float level = joy.CurLevelPercentage;
                if (level < 0.2f)
                    recBoredBuf[periodIndex]++;
                if (level > 0.7f)
                    recEntertainedBuf[periodIndex]++;
            }

            // Check 3h boundary
            if (periodTickCounter >= PeriodTicks)
            {
                ComputeReformRate();
                ResetPeriod();
            }
        }

        public void Notify_WorkTick()
        {
            EnsureBuffers();
            workBuf[periodIndex]++;
        }

        private void ComputeReformRate()
        {
            float positiveRate = 0f;
            float negativeRate = 0f;

            // Reset breakdown
            brPositiveFood = 0f;
            brPositiveRec = 0f;
            brPositiveWork = 0f;
            brPositiveCapped = 0f;
            brNegativeFood = 0f;
            brNegativeRec = 0f;
            brNegativeWorkIdle = 0f;
            brNegativeWorkOverwork = 0f;
            brMultiplier = 1f;
            brWorkHours = 0f;
            brIsJuvenile = Pawn.DevelopmentalStage.Juvenile();

            // --- Sum across 24h buffer ---
            int totalFoodStarving = 0, totalFoodFed = 0, totalFoodTicks = 0;
            int totalRecBored = 0, totalRecEntertained = 0, totalRecTicks = 0;
            int totalWorkTicks = 0;
            for (int i = 0; i < PeriodCount; i++)
            {
                totalFoodStarving += foodStarvingBuf[i];
                totalFoodFed += foodFedBuf[i];
                totalFoodTicks += foodTotalBuf[i];
                totalRecBored += recBoredBuf[i];
                totalRecEntertained += recEntertainedBuf[i];
                totalRecTicks += recTotalBuf[i];
                totalWorkTicks += workBuf[i];
            }

            // --- Food conditions ---
            if (totalFoodTicks > 0)
            {
                float starvingPct = (float)totalFoodStarving / totalFoodTicks;
                float fedPct = (float)totalFoodFed / totalFoodTicks;

                if (starvingPct > 0.5f)
                {
                    negativeRate += 3.0f;
                    brNegativeFood = 3.0f;
                }
                if (fedPct > 0.8f)
                {
                    positiveRate += 0.2f;
                    brPositiveFood = 0.2f;
                }
            }

            // --- Recreation conditions ---
            if (totalRecTicks > 0)
            {
                float boredPct = (float)totalRecBored / totalRecTicks;
                float entertainedPct = (float)totalRecEntertained / totalRecTicks;

                if (boredPct > 0.6f)
                {
                    if (reformValue < 50f)
                    {
                        negativeRate += 0.1f;
                        brNegativeRec = 0.1f;
                    }
                    else
                    {
                        negativeRate += 0.5f;
                        brNegativeRec = 0.5f;
                    }
                }
                if (entertainedPct > 0.6f)
                {
                    positiveRate += 0.5f;
                    brPositiveRec = 0.5f;
                }
            }

            // --- Work conditions (adults only) ---
            if (!brIsJuvenile)
            {
                brWorkHours = (float)totalWorkTicks / TicksPerHour;

                if (brWorkHours < 2f)
                {
                    negativeRate += 0.5f;
                    brNegativeWorkIdle = 0.5f;
                }
                else if (brWorkHours >= 2f && brWorkHours <= 10f)
                {
                    positiveRate += 0.5f;
                    brPositiveWork = 0.5f;
                }

                if (brWorkHours > 12f)
                {
                    if (reformValue < 50f)
                    {
                        negativeRate += 0.2f;
                        brNegativeWorkOverwork = 0.2f;
                    }
                    else
                    {
                        negativeRate += 1.0f;
                        brNegativeWorkOverwork = 1.0f;
                    }
                }
            }

            // --- Apply soft caps ---
            if (positiveRate > 1.0f)
                positiveRate = 1.0f;
            brPositiveCapped = positiveRate;
            if (negativeRate > 5.0f)
                negativeRate = 5.0f;

            // --- Apply early-reform multiplier (after cap) ---
            if (reformValue < 50f)
            {
                positiveRate *= 5f;
                brMultiplier = 5f;
            }

            // --- Apply to reform value (3h = 1/8 day) ---
            float dailyRate = positiveRate - negativeRate;
            reformValue += dailyRate * 0.125f;
            reformValue = Mathf.Clamp(reformValue, 0f, 100f);
            currentDailyRate = dailyRate;
            periodsCompleted++;
        }

        public string GetRateBreakdown()
        {
            float totalNeg = brNegativeFood + brNegativeRec + brNegativeWorkIdle + brNegativeWorkOverwork;
            if (periodsCompleted == 0)
                return "RimPrisonBuilder.RateBreakdownIdle".Translate();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // Header: work hours (adults) or juvenile skip
            if (brIsJuvenile)
                sb.AppendLine("RimPrisonBuilder.RateJuvenileSkip".Translate());
            else
                sb.AppendLine("RimPrisonBuilder.RateWorkHours".Translate(brWorkHours.ToString("F1")));

            // Positive section
            sb.AppendLine();
            sb.AppendLine("RimPrisonBuilder.RatePositive".Translate() + ":");
            if (brPositiveFood > 0f)
                sb.AppendLine("  " + "RimPrisonBuilder.RateSrcFood".Translate() + " +" + brPositiveFood.ToString("F1") + "/" + "RimPrisonBuilder.RateUnit".Translate());
            if (brPositiveRec > 0f)
                sb.AppendLine("  " + "RimPrisonBuilder.RateSrcRec".Translate() + " +" + brPositiveRec.ToString("F1") + "/" + "RimPrisonBuilder.RateUnit".Translate());
            if (brPositiveWork > 0f)
                sb.AppendLine("  " + "RimPrisonBuilder.RateSrcWork".Translate() + " +" + brPositiveWork.ToString("F1") + "/" + "RimPrisonBuilder.RateUnit".Translate());
            if (brPositiveCapped == 0f)
                sb.AppendLine("  (" + "None".Translate() + ")");

            // Cap & multiplier info
            if (brPositiveCapped > 0f)
            {
                if (brMultiplier > 1f)
                    sb.AppendLine("RimPrisonBuilder.RateCapMult".Translate(brPositiveCapped.ToString("F1"), brMultiplier.ToString("F0"), (brPositiveCapped * brMultiplier).ToString("F1")));
                else
                    sb.AppendLine("RimPrisonBuilder.RateCapOnly".Translate(brPositiveCapped.ToString("F1")));
            }

            // Negative section
            sb.AppendLine();
            sb.AppendLine("RimPrisonBuilder.RateNegative".Translate() + ":");
            if (brNegativeFood > 0f)
                sb.AppendLine("  " + "RimPrisonBuilder.RateSrcFood".Translate() + " -" + brNegativeFood.ToString("F1") + "/" + "RimPrisonBuilder.RateUnit".Translate());
            if (brNegativeRec > 0f)
                sb.AppendLine("  " + "RimPrisonBuilder.RateSrcRec".Translate() + " -" + brNegativeRec.ToString("F1") + "/" + "RimPrisonBuilder.RateUnit".Translate());
            if (brNegativeWorkIdle > 0f)
                sb.AppendLine("  " + "RimPrisonBuilder.RateSrcWorkIdle".Translate() + " -" + brNegativeWorkIdle.ToString("F1") + "/" + "RimPrisonBuilder.RateUnit".Translate());
            if (brNegativeWorkOverwork > 0f)
                sb.AppendLine("  " + "RimPrisonBuilder.RateSrcWorkOverwork".Translate() + " -" + brNegativeWorkOverwork.ToString("F1") + "/" + "RimPrisonBuilder.RateUnit".Translate());
            if (totalNeg == 0f)
                sb.AppendLine("  (" + "None".Translate() + ")");

            sb.AppendLine();
            sb.AppendLine("RimPrisonBuilder.RateDaily".Translate(currentDailyRate.ToString("F1")));

            return sb.ToString().TrimEnd();
        }

        private void ResetPeriod()
        {
            periodTickCounter = 0;
            periodIndex = (periodIndex + 1) % PeriodCount;
            foodStarvingBuf[periodIndex] = 0;
            foodFedBuf[periodIndex] = 0;
            foodTotalBuf[periodIndex] = 0;
            recBoredBuf[periodIndex] = 0;
            recEntertainedBuf[periodIndex] = 0;
            recTotalBuf[periodIndex] = 0;
            workBuf[periodIndex] = 0;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref reformValue, "reformValue");
            Scribe_Values.Look(ref currentDailyRate, "currentDailyRate");
            Scribe_Values.Look(ref periodTickCounter, "periodTickCounter");
            Scribe_Values.Look(ref periodIndex, "periodIndex");
            Scribe_Values.Look(ref periodsCompleted, "periodsCompleted");
            Scribe_Collections.Look(ref foodStarvingBuf, "foodStarvingBuf", LookMode.Value);
            Scribe_Collections.Look(ref foodFedBuf, "foodFedBuf", LookMode.Value);
            Scribe_Collections.Look(ref foodTotalBuf, "foodTotalBuf", LookMode.Value);
            Scribe_Collections.Look(ref recBoredBuf, "recBoredBuf", LookMode.Value);
            Scribe_Collections.Look(ref recEntertainedBuf, "recEntertainedBuf", LookMode.Value);
            Scribe_Collections.Look(ref recTotalBuf, "recTotalBuf", LookMode.Value);
            Scribe_Collections.Look(ref workBuf, "workBuf", LookMode.Value);
        }
    }
}
