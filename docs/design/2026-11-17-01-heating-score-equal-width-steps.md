# Heating score: equal-width steps instead of deciles

- **Date:** 2026-11-17
- **Status:** Proposed (investigation only; no code changed)
- **Author:** Patrick (with Claude)
- **Scope:** `Location.SetHeatingScores` in `ClimateExplorer.Core`; its unit tests; the wording in
  `HeatingScoreInfo`. The table, API and map need no change.
- **Builds on:** [Since-1961 trend and heating score](2026-11-12-03-since-1961-trend-and-heating-score-plan.md)
- **Branch context:** `ccr-3856ba89-osln77`

## Question

The score is now deciles of the since-1961 trend, so a score of 9 covers 0.425 to 1.04 °C/decade, a
range of about 0.6 °C, while score 3 covers about 0.02. Would it look better to find the highest and
lowest trend, divide the difference by 10, and put each location in the step its trend falls in?

## What the data shows

Computed from the cached locations (`cache-longterm`): 3,953 scored locations, 7 of them negative
(which score −1 and stay out of the ranking). Rates are °C per decade.

- Range of the non-negative rates: 0.012 to 1.040. Median 0.283.
- The ten fastest are 0.79, 0.80, 0.80, 0.83, 0.85, 0.86, 0.87, 0.91, 0.98, 1.04. One or two outliers
  stretch the top of the range.

Equal-width steps from 0 to the maximum (step 0.104):

| Score | Range | Locations now (deciles) | Locations, equal width |
|---|---|---|---|
| 9 | 0.936 – 1.040 | ~395 | 2 |
| 8 | 0.832 – 0.936 | ~395 | 4 |
| 7 | 0.728 – 0.832 | ~395 | 8 |
| 6 | 0.624 – 0.728 | ~395 | 13 |
| 5 | 0.520 – 0.624 | ~395 | 41 |
| 4 | 0.416 – 0.520 | ~395 | 397 |
| 3 | 0.312 – 0.416 | ~395 | 1,103 |
| 2 | 0.208 – 0.312 | ~395 | 1,524 |
| 1 | 0.104 – 0.208 | ~395 | 763 |
| 0 | 0.000 – 0.104 | ~395 | 91 |

(Anchoring the bottom at the lowest positive rate, 0.012, instead of 0 changes the counts by less than
50 per step. Including the negatives in the range, as the request literally says, widens the step to 0.132
and wastes steps 0 and 1 on 7 stations, so keep −1 separate.)

## Assessment

- **It fixes the stated problem.** A step is a fixed number of degrees, so a 9 now means the same thing
  everywhere on the scale, and the legend rows are all the same width.
- **The cost is the visual, not the counts.** About 90% of stations land in scores 1 to 4, and scores 7 to 9
  hold 14 stations between them. On the map that is a sea of one or two colours with a few red dots, and
  scores 7 to 9 are too rare to filter on usefully in the Locations page. The extremes are decided by the
  two or three most extreme stations: the next dataset refresh that adds a 1.2 °C/decade station would
  shift every boundary and rescore the whole map.
- **The score stops being relative.** Deciles answer "how does this place rank?". Equal steps answer "how fast
  is it warming?", which `HeatingScoreInfo` already shows as a rate. The score becomes a rounded rate and
  adds little beyond it.
- **The stored table works as is.** `HeatingScoreRow` already holds min and max per score, so the info
  page table needs no change. A step with no locations would have no row; keep the empty rows so the
  legend still shows ten steps.

## Options

| Option | Top of range | Behaviour |
|---|---|---|
| A. Equal width, true max (as requested) | 1.040 | Above. Unstable at the top. |
| B. Equal width, clipped at a percentile | 99th percentile, 0.559 | Counts per score from 9 to 0: 79, 174, 425, 604, 740, 878, 587, 341, 102, 16. The top step then holds the fastest 1% as "9 or more". Most even visual, stable against outliers. |
| C. Keep deciles | n/a | No change. |

Recommendation: **B**, or A if you want the plain version. B keeps the equal-width property (each step is
a fixed number of degrees) and gives the map a usable spread, at the price of one rule to explain:
"scores 0 to 8 are 0.056 °C wide, and 9 is everything faster than that". The percentile (99% vs 99.5%) is a
single constant. Either way, the information page needs a sentence saying the steps are equal widths,
not equal numbers of stations.

## Implementation sketch

In `Location.SetHeatingScores`, after `ranked` is built, replace the `tenPercent`/`Skip`/`Take` loop with:

- `low = 0` (or the smallest ranked rate), `high` = the largest ranked rate (A) or its 99th percentile (B).
- `step = (high - low) / 10`.
- `score = (short)Math.Min(9, (int)((slope - low) / step))`, group by score, and emit a row per score
  using the group's min and max rate (or the step's bounds; the bounds are tidier for a legend).
- Guard `step == 0` (all rates equal, or one location) by scoring everything 0.

Negative scoring and the eligibility rules are untouched.

Tests: `SetHeatingScores_TwentyPositiveRates_TwoLocationsPerScore` and `..._ReturnsBandsPerDecade` assume
deciles and need rewriting; add tests for an outlier (top score holds one station), the clipped top (B),
an empty middle step, and a single location.

## Summary

Equal-width steps make each score cover the same number of degrees, which fixes the very wide 9, but on the
real data it leaves 90% of stations in scores 1 to 4 and 14 stations in 7 to 9, and lets two outliers
set every boundary. Clipping the top at the 99th percentile keeps the fixed-width idea and gives a
usable spread. The change is confined to one method and its tests.

### Change size

- **Projects:** `ClimateExplorer.Core`, `ClimateExplorer.Core.UnitTests`, `ClimateExplorer.Web.Client`
  (info text only). Unchanged: `WebApi`, `WebApiClient`, map and Locations page.

| Area | Added | Changed | Removed |
|---|---|---|---|
| `Location.cs` (`SetHeatingScores`) | none | `~40` | none |
| `HeatingScoreTests.cs` | none | `~50` | none |
| `HeatingScoreInfo` text | none | `~5` | none |
| **Total** | 0 files | 3 files, `~95` lines | 0 files |

Risk: low. The cached table and locations regenerate on the next cache build, so existing cached scores
stay deciles until then.
