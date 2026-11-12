# Heating score: trend vs warming anomaly

- **Date:** 2026-11-12
- **Status:** Decided 2026-11-12: option E (trend since 1961). See
  [Since-1961 trend and heating score](2026-11-12-03-since-1961-trend-and-heating-score-plan.md)
- **Author:** Patrick (with Claude)
- **Scope:** `ClimateExplorer.Core` (`Location.SetHeatingScores`, `HeatingScoreRow`, a fixed-period trend),
  `ClimateExplorer.WebApi` (`LocationEndpoints` location cache build), `ClimateExplorer.Web.Client`
  (`HeatingScoreInfo`, `ApiMeasures`)
- **Branch context:** written on `development`

## Question

Should the heating score be ranked on a location's temperature **trend** rather than its **warming
anomaly**? The anomaly depends heavily on how long the record is; a trend looks like a more standard
number and a better signal of global heating. The dashboard already shows a trend: the last 30 years
if that's significant, otherwise the full record.

## How it works today

- **Warming anomaly** (`AnomalyCalculator`): the mean of the last 30 complete years minus the mean of
  the first half of the complete years. Needs at least 60 complete years.
- **Heating score** (`Location.SetHeatingScores`): locations with an anomaly ≥ 0 are split into deciles
  and scored 9 (top 10%) to 0. A negative anomaly is used as the score itself, rounded (so -1, -2, ...).
  The bands go into `HeatingScoreRow` and the info page shows them.
- **Trends** (`TemperatureTrendCalculator`): an OLS slope over the full record (30+ complete years) and
  over the last 30 complete years (60+ years), each with an F-test significance flag (p < 0.05).
- **Dashboard trend** (`LocationTrendFieldBuilder`): the last-30 trend if it's significant; otherwise
  the full-record trend if *that* is significant; otherwise nothing. The full-record trend isn't shown
  when it's not significant either, so the dashboard shows nothing for 83 locations.

All three are calculated from the same yearly series (`DataSubstitute.StandardTemperatureDataMatches()`).

## What the data says

Taken from the production `/location` endpoint on 2026-11-12: 2,014 locations, 1,750 with an
anomaly. All 1,750 also have both trends, since the 60-year minimum is the same. Scripts are in the
session scratchpad, not the repo.

### Each measure against record length

| Measure | Mean | SD | Spearman ρ with record length |
|---|---|---|---|
| Warming anomaly | 1.24 °C | 0.52 | **+0.21** |
| Full-record trend | 0.21 °C/decade | 0.10 | **−0.61** |
| Last-30-years trend | 0.40 °C/decade | 0.22 | **+0.01** |

Medians by record length:

| Record length (years) | n | Anomaly (°C) | Full trend (°C/dec) | Last-30 trend (°C/dec) | Last-30 significant |
|---|---|---|---|---|---|
| 60–79 | 440 | 1.19 | 0.294 | 0.383 | 83% |
| 80–99 | 494 | 1.28 | 0.240 | 0.368 | 75% |
| 100–119 | 279 | 1.07 | 0.152 | 0.314 | 79% |
| 120–149 | 361 | 1.38 | 0.155 | 0.378 | 82% |
| 150+ | 176 | 1.74 | 0.112 | 0.453 | 92% |

What this shows:

1. **The concern about the anomaly is real but modest.** Its link to record length is weak overall
   (ρ 0.21) and mostly comes from the 176 records of 150+ years. The cause is structural: the anomaly
   roughly equals the warming rate times the gap between the midpoints of its two periods, and that gap
   grows with record length. Divided by the gap, the anomaly works out at a median 0.20 °C/decade, with
   ρ −0.58 against length. That's the same pattern as the full-record trend.
2. **The full-record trend is worse than the anomaly**, and biased the other way. Warming sped up after
   about 1970, so a long record dilutes the recent rate with flat 19th- and early 20th-century years.
   Ranked on this, every 150-year station would score low because its record is long. That's the same
   flaw as now, but stronger and reversed.
3. **The last-30-years trend is the only one that doesn't depend on record length** (ρ 0.01). For most
   stations it covers roughly the same calendar years (most windows start in 1992–1996), so it is the
   standardised number. But it's noisy (next section).
4. **The measures don't agree much.** Rank correlations: anomaly vs last-30 0.42, anomaly vs full 0.54,
   full vs last-30 0.30.

### The last-30-years trend is noisy

An OLS slope over 30 years has a standard error of about σ × 0.21 °C/decade, where σ is the
station's year-to-year standard deviation. That's roughly 0.5 °C at a maritime station and 1–1.5 °C at a
continental one, so the error is about **0.1 °C/decade at the coast and 0.2–0.3 inland**. The middle
score bands for a last-30 decile score would each be only about 0.05–0.06 °C/decade wide:

```
Last-30 deciles (°C/decade): 0.16 0.23 0.28 0.32 0.37 0.43 0.49 0.57 0.67
```

So a station's score would carry about ±2 bands of noise near the coast and ±4 inland. The spread
between stations (SD 0.22) isn't much bigger than that noise, so a large part of any ranking would be
noise. Other signs of it:

- **20% of last-30 trends aren't significant** (337 of 1,750), and some non-significant slopes are as
  high as 0.58 °C/decade.
- **Ends have the most leverage.** One final year 1 °C warmer than usual moves a 30-year slope by about
  0.065 °C/decade, a whole band. Scores would change every year as the window slides.
- **The scores would reshuffle a lot:**

  | Score change, anomaly → last-30 | 0 | 1 | 2 | 3 | 4 | 5+ |
  |---|---|---|---|---|---|---|
  | Locations (n = 1,714) | 248 | 427 | 339 | 292 | 173 | 235 |

  The biggest movers are continental stations. Saratov, Kyakhta, Bismarck, Tatarsk, Tomsk and Hay River
  go from 9 to 0: anomaly above 2.1 °C, full-record trend 0.17–0.30 °C/decade, last-30 trend only
  0.02–0.18. These places warmed sharply around the late 1980s and have been flat or noisy since. A 0
  would wrongly tell readers they're among the least-heated places in the dataset. Mugla, Tasiilaq,
  Amundsen–Scott and Sydney Airport go from 0 to 7 or 8 on strong recent slopes.

### The dashboard's mixed rule doesn't work for ranking

The rule picks the last-30 trend for 1,413 locations, the full record for 518, and nothing for 83. Those
two trends measure different things: their means are 0.40 and 0.21 °C/decade. Ranking them together
would push every location on the fallback towards a low score just because its 30-year trend wasn't
significant. That's fine for a single number on the dashboard with a caption, but not for comparing
locations.

### Gaps stretch "the last 30 years"

`TakeLast(30)` counts *complete* years, and a year is only complete when every day is present
(`RequiredBinDataProportion = 1.0`). So 1,243 of the 1,750 "last 30 years" windows cover more than 30
calendar years, 403 cover more than 35, and 129 cover more than 45 (Baia Mare covers 118). This affects
the anomaly and the last-30 trend alike, so neither is quite the same calendar period for every station.

## How climate science handles this

This is a well-discussed topic, though not as "anomaly vs trend" for a ranking. Each part of it is
discussed:

- **Fixed reference periods are how anomalies are standardised.** The WMO keeps **1961–1990** as a
  fixed reference period for long-term climate change assessment. It recommends it for tracking
  anomalies "relative to a fixed and common reference period", while the rolling 30-year normals
  (1991–2020 now) are for operational use. Anomalies against different baselines aren't comparable.
  Copernicus's example: January 2021 was +0.24 °C against 1991–2020 but +0.43 °C against 1981–2010.
  The site's anomaly uses a different baseline for every station (the first half of its record), and
  that, not anomalies as such, is why it isn't standardised.
- **For "how much has it warmed", IPCC AR6 uses differences between period means rather than trends.**
  The headline is 1850–1900 to 2011–2020 (+1.09 °C). AR5 had used a linear trend over 1880–2012, and
  AR6 still lists trends over 1880–2020, 1960–2020 and 1980–2020 next to the differences. A linear fit
  doesn't suit total warming because the warming hasn't been linear. Carbon Brief puts it at about
  0.18 °C/decade over 1970–2008 and about 0.30 over 2009–2023. That's the full-record trend's problem
  above.
- **Trend windows need to be long, and the noise properly allowed for.** Santer et al. (2000) is the
  standard reference on trend significance with autocorrelated data. Year-to-year values are correlated,
  so the effective sample size is smaller than the number of years and the true uncertainty is wider
  than a plain F-test says. Our `IsSignificant` flag overstates significance a little for that reason.
- **Noise is much larger at a single station than in a global series.** Hawkins & Sutton (2012) on
  "time of emergence" divide the local warming signal by local year-to-year variability. The ratio is
  high in the tropics and low at continental mid and high latitudes, so the same warming shows up
  later there. That's why a 30-year trend ranks Siberia and the Canadian prairies so unreliably.

Ranking stations by decile with a 0–9 score is your own invention, but the inputs are standard.
Researchers either use a common fixed window (a trend over a fixed period, or the difference between
fixed periods) or flag that the stations aren't comparable.

## Options

| Option | Same for every station? | Noise | Coverage | Notes |
|---|---|---|---|---|
| **A. Current anomaly** (last 30 vs first half) | No: baseline varies | Low | 1,750 | Weak length bias; strongest for 150+ years |
| **B. Full-record trend** | No: window varies | Low | 1,950 | Strong length bias, reversed. Reject |
| **C. Last-30 trend** | Mostly | **High** | 1,750 | Unbiased, but a large share of the ranking is noise; scores jump every year |
| **D. Dashboard mixed rule** | No | Mixed | 1,931 | Mixes two different quantities. Reject for ranking |
| **E. Fixed-period trend**: OLS slope from 1961 to the last year | Yes | Low (about σ × 0.07 °C/decade over 65 years, a third of C) | ≤ 1,757* | A rate, like C, without C's noise; nearly linear over this period |
| **F. Fixed-baseline anomaly**: last 30 years minus the 1961–1990 mean | Yes | Low | ≤ 1,757* | The WMO convention; a temperature, not a rate |

\* Locations whose first complete year is 1961 or earlier and whose last year is 2020 or later. Gaps
inside the window will bring this down. Each option needs a coverage rule, e.g. at least 20 complete
years in 1961–1990 and in the last 30 years for F, or 45 complete years with data in both the first and
last decade for E.

E and F both remove the record-length problem with no noise penalty. They also fix most of the gap
stretching, because the window is set in calendar years rather than counted from complete years. Both
lose the old records' longer view, but that view is exactly what makes the current score
non-standard. The full history stays visible on the chart and in the warming anomaly field.

## Recommendation

Use **option E, a trend over a fixed period, 1961 to the present**:

- It keeps what's good about the idea: a rate of heating, the same kind of number as the dashboard
  trend, and independent of record length.
- It's about three times less noisy than the last-30 trend, so the bands mean something and scores don't
  swing every year.
- 1961 is the start of the WMO reference period, which is easy to justify on the info page. It's also
  close to when warming took off, so a straight line fits the period reasonably well.
- It can be calculated in `LocationEndpoints` from the yearly series that the anomaly and trends already
  use.

F works just as well. Choose it if you'd rather the score stay in °C, so the existing info page wording
mostly carries over and the anomaly field and score bands use the same units.

Don't use the last-30 trend (C) or the dashboard rule (D) for the score. The dashboard can keep showing
D. If so, the info page should say the score uses a different window from the dashboard's trend field.

### Things to settle

- **Negative scores.** At present a negative anomaly rounded to the nearest degree becomes the score.
  Cooling trends are around −0.1 °C/decade, which would round to 0. One option: any negative slope
  scores −1, or a significant one does.
- **Locations without enough data.** Some will lose a score (fewer stations qualify for E than have an
  anomaly). The `Locations` page already has a "No heating score" filter.
- **Should the dashboard trend field also use the fixed period?** That's a separate question. Leaving it
  as it is means the score and the dashboard can disagree.
- **Run E on the real data first.** The production `/location` response only has the full and last-30
  slopes, so the reshuffle and coverage above for E and F are estimates. Before deciding, compute E (and
  F) for every location in a test run of the cache build and compare.

## Summary

The note compares the current anomaly score with trend-based alternatives on production data. The
anomaly's dependence on record length is real but mild. The full-record trend is more biased (the
other way). The last-30 trend doesn't depend on record length but is too noisy to rank stations on. The
dashboard's mixed rule ranks two different quantities together. The standard fix in climate science is
a fixed common period. The recommendation is a trend from 1961 to the present (E), or the anomaly
against 1961–1990 (F).

### Change size (option E)

- **Projects:** `ClimateExplorer.Core`, `ClimateExplorer.WebApi`, `ClimateExplorer.Web.Client`, and the
  Core and Web unit test projects.
- **Not changed:** `ClimateExplorer.WebApiClient` (the `HeatingScoreRow` shape changes, but the call
  doesn't), `ClimateExplorer.DataPipeline` (no dataset changes).

| Area | Added | Changed | Removed |
|---|---|---|---|
| Core: fixed-period slope in `TemperatureTrendCalculator`, new `Location` property, `SetHeatingScores` ranked on it, `HeatingScoreRow` bounds renamed | — | 4 files, ~60 | — |
| WebApi: set the property in `LocationEndpoints`, rounding in `CacheJsonOptions` | — | 2 files, ~10 | — |
| Web.Client: `HeatingScoreInfo` text and table in °C/decade, `ApiMeasures` | — | 3 files, ~40 | — |
| Tests: `HeatingScoreTests`, trend calculator tests, `ApiMeasuresTests` | — | 3 files, ~80 | — |
| **Total** | — | **12 files, ~190** | — |

The location cache and heating score table need rebuilding after deployment (`CachingTool`).

Risk: scores change a lot for some well-known locations, and anyone used to the old scores will notice.
The info page should say the method changed.

## Sources

- IPCC AR6 WGI Chapter 2 (warming as differences between period means; trend table):
  <https://www.ipcc.ch/report/ar6/wg1/downloads/report/IPCC_AR6_WGI_Chapter_02.pdf>
- IPCC AR6 Cross-Chapter Box 2.3, Figure 1 (AR5 trend metric vs AR6):
  <https://ipcc.ch/report/ar6/wg1/figures/chapter-2/ccbox-2-3-figure-1>
- Carbon Brief, AR6 vs AR5 headline warming:
  <https://www.carbonbrief.org/ipcc-how-the-ar6-wg1-summary-for-policymakers-compares-to-its-predecessor>
- Carbon Brief, recent acceleration (0.18 vs 0.30 °C/decade): <https://www.carbonbrief.org/?p=51002>
- WMO, updated 30-year reference period (1961–1990 kept for climate change assessment):
  <https://public.wmo.int/node/13886>
- WMO Climatological Normals: <https://community.wmo.int/node/36682>
- Copernicus, changing reference period and its effect on anomalies:
  <https://climate.copernicus.eu/new-decade-brings-reference-period-change-climate-data>
- Santer et al. (2000), statistical significance of trends with autocorrelation, JGR 105(D6):
  <https://www.arl.noaa.gov/documents/JournalPDFs/SanterEtal.JGR2000.pdf>
- Hawkins & Sutton (2012), time of emergence of climate signals, GRL:
  <https://centaur.reading.ac.uk/25489>
