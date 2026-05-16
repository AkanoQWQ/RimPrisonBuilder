using System;
using RimWorld;
using UnityEngine;
using Verse;
using RimPrison.PrisonArea;

namespace RimPrison.PrisonLabor
{
    public readonly struct SuppressionBreakdown
    {
        public readonly float suppression;
        public readonly float guardFactor;
        public readonly float turretFactor;
        public readonly float prisonerFactor;
        public readonly float moodFactor;
        public readonly float healthFactor;
        public readonly float regimeMod;
        public readonly float difficultyMod;

        public SuppressionBreakdown(float suppression, float guardFactor, float turretFactor,
            float prisonerFactor, float moodFactor, float healthFactor,
            float regimeMod, float difficultyMod)
        {
            this.suppression = suppression;
            this.guardFactor = guardFactor;
            this.turretFactor = turretFactor;
            this.prisonerFactor = prisonerFactor;
            this.moodFactor = moodFactor;
            this.healthFactor = healthFactor;
            this.regimeMod = regimeMod;
            this.difficultyMod = difficultyMod;
        }
    }

    public static class SuppressionCalculator
    {
        private const float MaxEffectivePrisoners = 20f;

        public enum Regime { Standard, Harsh, Deterrence, Equality }
        public static Regime CurrentRegime = Regime.Standard;

        // Calculate, babies count less than adult prisoner
        public static float CalculateEffectivePrisonerCount(Map map)
        {
            if (map == null) return 0f;
            float count = 0f;
            var prisoners = map.mapPawns.PrisonersOfColony;
            foreach (var p in prisoners)
            {
                if (p.DevelopmentalStage.Baby()) count += 0.1f;
                else if (p.DevelopmentalStage.Child()) count += 0.25f;
                else count += 1f;
            }
            return Math.Min(count, MaxEffectivePrisoners);
        }

        public static int CountTurretsInPrisonArea(Map map)
        {
            if (map == null) return 0;
            var area = map.areaManager.Get<Area_Prison>();
            if (area == null) return 0;
            int count = 0;
            var turrets = map.listerBuildings.AllBuildingsColonistOfClass<Building_Turret>();
            foreach (var t in turrets)
            {
                if (area[t.Position])
                    count++;
            }
            return count;
        }

        public static SuppressionBreakdown CalculateSuppression(float effectivePrisoners, int guardCount,
            int colonistCount, int turretCount, float avgMood, float avgHealth,
            Regime regime, float difficultyValue)
        {
            if (effectivePrisoners <= 0f)
                return new SuppressionBreakdown(50f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            // k = prisoners / (colonists + guards×2). More prisoners per caretaker → lower suppression.
            float k = effectivePrisoners / Math.Max(colonistCount + guardCount * 2f, 1f);
            float guardFactor;
            if (k > 1f)
                guardFactor = -Math.Min((float)Math.Pow(2f, k - 1f) - 1f, 15f);
            else if (k < 1f)
                guardFactor = (float)Math.Pow(4f, 2f - k) - 4f;
            else
                guardFactor = 0f;
            float turretFactor = Math.Min(turretCount * 2f, 20f);
            float prisonerFactor = -Math.Min(effectivePrisoners, 15f);
            float moodFactor = (avgMood * avgMood * 12f - 3f) * 1.25f;
            float healthFactor = (0.5f - avgHealth) * 8f;
            float regimeMod = regime switch
            {
                Regime.Harsh => 10f,
                Regime.Deterrence => 3f,
                Regime.Equality => -5f,
                _ => 0f
            };
            float difficultyMod = (1f - difficultyValue) * 8f;

            float suppression = Mathf.Clamp(
                50f + guardFactor + turretFactor + prisonerFactor
                + moodFactor + healthFactor + regimeMod + difficultyMod,
                0f, 100f);

            return new SuppressionBreakdown(suppression, guardFactor, turretFactor,
                prisonerFactor, moodFactor, healthFactor, regimeMod, difficultyMod);
        }

        public static bool AllowsPrisonBreak(float suppression, Regime regime)
        {
            float threshold = regime switch
            {
                Regime.Harsh => 35f,
                Regime.Equality => 20f,
                _ => 30f
            };
            return suppression < threshold;
        }

        public static bool AllowsMentalBreak(float suppression, Regime regime)
        {
            float threshold = regime switch
            {
                Regime.Harsh => 55f,
                Regime.Equality => 40f,
                _ => 50f
            };
            return suppression < threshold;
        }

        // MTB - Mean Time Between
        // Multiplier for prison break MTB — higher suppression = longer between breaks
        public static float GetBreakMtbMultiplier(float suppression, Regime regime)
        {
            float threshold = regime switch
            {
                Regime.Harsh => 35f,
                Regime.Equality => 20f,
                _ => 30f
            };
            if (suppression >= threshold)
                return 999f; // effectively disabled
            float ratio = suppression / threshold;
            return 1f + ratio * 9f; // 1x at 0 suppression, up to ~10x near threshold
        }
    }
}
