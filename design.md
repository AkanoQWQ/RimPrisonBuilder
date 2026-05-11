# RimPrisonBuilder — Design Document

## Introduction

RimPrisonBuilder is a RimWorld mod focused on **prisoner rehabilitation**. Unlike vanilla RimWorld — where prisoners sit idle in cells until recruited, sold, or **harvested** — our mod asks: *can we make them better people?*

The core fantasy is simple: prisoners join the colony broken (0% reform), and through consistent care — decent food, recreation, meaningful labor — they gradually reform. At 100% reform, they earn the choice to stay as a colonist or leave as a free person. The road is slow (roughly one in-game year), but the payoff is real: reformed prisoners are **improved** — they gain skills, shed negative traits, earn positive ones, and work more efficiently.

This is not a slavery mod. We compete on rehabilitation depth, not exploitation breadth. Every mechanic should make the player feel like a warden running a humane prison, not a slaver extracting value. Paired with RimTalk, prisoners express their transformation in their own words.

---

## Reform Value System

### Overview

Each prisoner has a **Reform Value** (0–100), representing their progress toward becoming a better person.

- **0%** — A fresh prisoner, unchanged since arrest.
- **50%** — Halfway. Basic reform achieved. High-reform content unlocks (art therapy, advanced courses).
- **100%** — Fully reformed. Eligible for parole or voluntary joining.

The curve is **fast-then-slow**: reaching 50% takes ~10 days of good treatment; the remaining 50% takes ~50 days (~60 days total, or one in-game year). This gives players early positive feedback (visible progress within a quadrum) while keeping full reform a long-term investment.

### Reform Change Rate

Every **6 in-game hours** (1500 ticks), the system computes a **daily reform change rate** (units: % per day) and applies it as `rate × 0.25` to the Reform Value (since 6h = 1/4 of a day).

The daily rate is the sum of all active bonuses and penalties, clamped by a soft cap:

| Cap | Value | Note |
|-----|-------|------|
| Positive soft cap | **+1.0%/day** | The ×5 multiplier for <50% applies AFTER the cap |
| Negative soft cap | **−5.0%/day** | Hard floor, no multiplier |

### The ×5 Early-Reform Multiplier

When Reform Value is **below 50%**, the final positive daily rate is multiplied by **×5**. This multiplier ignores the 1%/day cap — the cap is applied to the base sum first, then the multiplier applies.

**Example**: base positive sum = 1.2% → clamped to 1.0% → ×5 = **5.0%/day** when <50%.

Without the multiplier: 1.0%/day → 50% takes 50 days (too slow, no early payoff).
With the multiplier: 5.0%/day → 50% takes 10 days (visible progress, good feedback).

### When Reform Change Is Computed

Reform change is computed every **6 game hours** (1500 ticks at 1x speed, ~25 real-time seconds). The 6-hour window is sampled continuously — every tick, the system records whether the prisoner's needs are in "good" or "bad" ranges. At the end of each 6-hour period, percentages are computed and the daily rate is calculated.

---

## Conditions (Daily Rate Components)

### Food (sampled over past 6 hours)

| Condition | Threshold | Rate | Rationale |
|-----------|-----------|------|-----------|
| Hunger < 0.15 for >50% of the period | Starving half the time | **−3.0%/day** | Severe neglect is the strongest negative signal |
| Saturation > 0.3 for >80% of the period | Well-fed most of the time | **+0.2%/day** | Being fed is the bare minimum — low bonus by design |

Threshold note: 0.15 hunger means the pawn is deeply hungry (malnutrition risk). 0.25 is "just starting to feel hungry" — we intentionally use the lower 0.15 threshold because RimWorld AI occasionally forgets to eat, and prisoners wake up hungry. This gives a tolerance buffer.

### Recreation (sampled over past 6 hours)

| Condition | Threshold | Rate | Rationale |
|-----------|-----------|------|-----------|
| Recreation < 0.2 for >60% of the period (reform < 50%) | Bored, low reform | **−0.1%/day** | Low-reform prisoners aren't expected to have luxury — mild penalty |
| Recreation < 0.2 for >60% of the period (reform ≥ 50%) | Bored, high reform | **−0.5%/day** | High-reform prisoners should expect decent conditions — harsher penalty |
| Recreation > 0.7 for >60% of the period | Well-entertained | **+0.5%/day** | Providing good recreation is a meaningful positive signal |

Design intent: recreation expectations rise with reform. A fresh prisoner tolerates boredom; a halfway-reformed prisoner doesn't.

### Work (sampled over past 24 hours; adults only)

Work conditions **only apply to adult prisoners**. Children are exempt from work-based reform changes.

| Condition | Threshold | Rate | Rationale |
|-----------|-----------|------|-----------|
| Work time < 2h | Idle | **−0.5%/day** | Prisoners should contribute — idleness is punished |
| Work time 2h–10h | Productive | **+0.5%/day** | Labor is the core of reform — the positive sweet spot |
| Work time > 12h (reform < 50%) | Overworked, low reform | **−0.2%/day** | Low-reform prisoners tolerate extra work — mild penalty |
| Work time > 12h (reform ≥ 50%) | Overworked, high reform | **−1.0%/day** | Don't overwork your reformed prisoners — strong penalty |

Design intent: the ideal is "productive but not exploited." 2–10 hours mirrors a healthy workday. Overwork is punished more harshly for high-reform prisoners — they've earned dignity. The work window is 24h instead of 6h because work happens in scheduled blocks, and a single missed work session shouldn't tank the rate.

### Work Fairness — TODO

Currently, work conditions apply regardless of the prisoner's physical capability. A prisoner with a broken spine or severe illness who cannot work will be punished for "idleness" through no fault of their own (or the player's).

**Planned fix**: before applying work conditions, check `pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)`. If the prisoner is incapable of manipulation (injured, sick, catatonic), skip all work conditions for that period.

### Mood — TODO

Mood is deliberately absent from the current formula. RimWorld's mood system is volatile — a single corpse, a psychic drone, or a random insult can crater mood through no strategic fault of the player. Introducing mood-based reform changes would create frustration: players would see their carefully-managed reform progress wiped out by events they can't control.

**We will add mood conditions in a future update**, but the values must be small (±0.1% to ±0.3%/day) and only trigger on extreme, sustained states (e.g., mood < 0.1 for >80% of 6h → −0.2%/day). The goal is to acknowledge mood's role in rehabilitation without making it the dominant factor.

### Children — TODO

Currently, the reform system is designed for adult prisoners. RimWorld's Biotech DLC introduces children and child growth mechanics. We want to eventually integrate child prisoners into the reform system:

- Children should NOT have work-based reform conditions (already the case).
- Food and recreation conditions still apply (basic humanitarian treatment).
- Reform might grant children growth moments or trait opportunities rather than parole.
- This is a future integration — scope TBD.

---

## Summary of Daily Rate

### Positive rate (daily)

| Source | Raw Value |
|--------|-----------|
| Food: saturation > 0.3 for >80% of 6h | +0.2% |
| Recreation: > 0.7 for >60% of 6h | +0.5% |
| Work: 2h–10h in 24h (adults only) | +0.5% |
| **Base sum** | **+1.2%** |
| **After positive soft cap** | **+1.0%/day** |
| **After ×5 (<50% reform)** | **+5.0%/day** |
| **After ×1 (≥50% reform)** | **+1.0%/day** |

### Negative rate (daily)

| Source | Raw Value |
|--------|-----------|
| Food: hunger < 0.15 for >50% of 6h | −3.0% |
| Recreation: < 0.2 for >60% of 6h (<50% reform) | −0.1% |
| Recreation: < 0.2 for >60% of 6h (≥50% reform) | −0.5% |
| Work: < 2h in 24h (adults only) | −0.5% |
| Work: > 12h in 24h (<50% reform, adults only) | −0.2% |
| Work: > 12h in 24h (≥50% reform, adults only) | −1.0% |
| **Worst-case sum (<50%)** | **−3.6%/day** |
| **Worst-case sum (≥50%)** | **−5.0%/day** |

Both are within or at the −5%/day soft cap.

### Ideal timeline

| Phase | Reform Range | Max Daily Rate | Days Required |
|-------|-------------|----------------|---------------|
| Early reform | 0% → 50% | +5.0%/day | 10 days |
| Late reform | 50% → 100% | +1.0%/day | 50 days |
| **Total** | 0% → 100% | — | **~60 days (1 year)** |

---

## Implementation Notes

- Reform tracking data is stored in a **per-pawn ThingComp**, not a MapComponent. CompTick provides per-pawn callbacks, eliminating any iteration overhead.
- Sampling runs every tick: a few float comparisons and counter increments per pawn. At 100 prisoners × 60 ticks/sec, this is negligible (RimWorld's pathfinding is orders of magnitude more expensive).
- Rate computation runs every 1500 ticks (6h): simple percentage math. Even with 100 prisoners, this fires 4 times per day per pawn, totaling 400 simple calculations per in-game day.
- Rolling windows: 6h food/recreation data requires 1 sample period of counters. 24h work data requires storing 4 sample periods (4 × 6h = 24h) and summing across them.
