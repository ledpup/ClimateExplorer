# Why `WgmsGlacierMassBalanceSourceFileTransformer`'s numbers differ from WGMS's reference-glacier figures

- **Date:** 2026-08-27 (updated same day — see "Update" note below)
- **Author:** Patrick Lea (with Claude)
- **Related:** [docs/design/2026-08-26-01-wgms-glacier-mass-balance-dataset-plan.md](../design/2026-08-26-01-wgms-glacier-mass-balance-dataset-plan.md) (the plan that shipped the original transformer, and whose addendum records the 2026-08-28 rewrite below); `ClimateExplorer.UnitTests/GlacierFixtures/mb_ref.csv` (the WGMS reference figures this investigation was triggered by); `ClimateExplorer.UnitTests/WgmsReferenceGlacierMassBalanceTests.cs` (tests backing this write-up)

## Update (same day)

The first version of this doc treated the >10-year "Benchmark" glacier list as
something to be corrected toward WGMS's >30-year "Reference" threshold, and
described the calculation-method difference as something the current
transformer "needs" to fix. Neither framing is right:

- **The >10-year Benchmark rule is a deliberate choice**, not an oversight —
  it's already known that WGMS's own reference glaciers require >30 years;
  using a wider, looser set is intentional. Step 1/2 below is kept as
  background context (it was already investigated and the fixtures/tests
  exist), not as a recommendation to narrow the list.
- **WGMS's raw/two-stage-regional method isn't "more correct" than the
  transformer's own-mean-anomaly method** — they're different metrics
  answering different questions, not a right answer and a wrong one. Step 3
  below is now framed as "how much do the two metrics differ, and why,"
  not "how do we make ours match theirs."

## Summary

`WgmsGlacierMassBalanceSourceFileTransformer`'s output and WGMS's own
published reference-glacier regional average (`GlacierFixtures/mb_ref.csv`)
are **two different metrics computed two different ways**, not the same
metric with a bug in one of them:

1. **Different glacier list.** The transformer uses WGMS's own "Benchmark"
   category (>10 years of records, ≤1-year gap in the dataset's own most
   recent decade) — deliberately wider than WGMS's "Reference" category
   (>30 years), which is what `mb_ref.csv` is built from.
2. **Different calculation method.** The transformer expresses each
   glacier's annual balance as a deviation from *that glacier's own
   all-time mean*, then flat-averages those deviations across every
   qualifying glacier. WGMS's regional average is the **raw** (non-anomaly)
   annual balance, averaged within each GTN-G region first and then
   averaged across regions (one value per region, so densely-instrumented
   regions like the Alps don't dominate).

Even restricting the input to *exactly* WGMS's 61 official reference
glaciers (removing difference #1), the transformer's own-mean-anomaly/flat-average
method (difference #2) still produces a series that differs from
`mb_ref.csv` by a mean of **0.53 m w.e.** (median 0.54, max 0.89) across 74
comparable years — see the full table in "Step 3" below. This is the
dominant source of the numeric gap between the two series, but it's a
*definitional* difference (what quantity is being averaged, and how), not
evidence that either approach is wrong.

## Background

`WgmsGlacierMassBalanceSourceFileTransformer` (`ClimateExplorer.Data.Downloading/Transformers/`)
currently:

1. Filters `mass_balance.csv` to glaciers WGMS calls **"Benchmark"** glaciers:
   more than 10 years of records, with at most a 1-year gap in the dataset's
   own most recent 10 calendar years.
2. For each qualifying glacier, expresses every year's `annual_balance` as a
   deviation from **that glacier's own all-time mean**.
3. Averages those anomalies, flatly, across all qualifying glaciers, per year
   (dropping years with fewer than 5 contributing glaciers).

`GlacierFixtures/mb_ref.csv` is WGMS's own published reference-glacier
regional average (from <https://wgms.ch/global-glacier-state/>), with columns
`Year, MB_REF_count, REF_regionAVG (mm w.e.), REF_regionAVG_cum-rel-1970`. The
cumulative column is confirmed to be a running sum of `REF_regionAVG`
anchored at 0 in 1970 — i.e. `REF_regionAVG` is WGMS's real, load-bearing
annual figure, not a derived display artifact.

## Step 1/2 — Reference glaciers vs. the transformer's Benchmark filter (background only)

WGMS's reference-glacier network (<https://wgms.ch/products_ref_glaciers/>,
<https://wgms.ch/global-glacier-state/>) is defined as **"more than 30
[continued/ongoing] years of glaciological mass-balance measurements"** — a
different, named WGMS category from the transformer's 10-year "Benchmark"
rule. The products page lists 61 named reference glaciers across 10 regions
(Alaska, Western North America, Arctic Canada, Iceland, Svalbard & Jan Mayen,
Scandinavia, Central Europe, Caucasus, Central Asia, Southern Andes).

Applying the transformer's exact rule shape (`years.Count > N`, `≤1 year gap
in the dataset's own most recent decade`) with `N = 30` against the real
2026-02-10 WGMS release (downloaded fresh for this investigation — see
"Data used" below) finds **82 glaciers**, not 61:

- **All 61 official reference glaciers match by name except one:** `LEVIY
  AKTRU` (Russian Altai) has 43 years of records overall, but a 2013–2018
  reporting gap means it fails the "≤1 year gap in the most recent decade"
  clause — even though it has reported every year since 2019.
- **22 extra glaciers pass the numeric rule but aren't on WGMS's official
  list** (e.g. `TAKU`, `RHONE`, `KONGSVEGEN`, `HANSEBREEN`, the Urumqi
  E/W sub-branches). WGMS's own page notes reference-glacier status also
  requires being "primarily climate-driven... without major influences from
  avalanches, calving, or surge dynamics" — criteria a years-and-gap rule
  can't see. WGMS's list is curated/maintained, not mechanically re-derived
  from the raw data every release.

This is now locked down by
`MoreThan30YearRule_AppliedToRealWgmsData_AlmostReproducesOfficialReferenceGlacierList`
in the new test file, purely as a factual record of where a 30-year numeric
rule would land relative to WGMS's curated list — **not** a proposal to
change the transformer's existing 10-year Benchmark rule, which was a
deliberate choice.

## Step 3 — How much does the calculation method alone account for, given the *same* glacier list?

To isolate "how much of the gap is the list, vs. the method," the test
`TransformAsync_RealDataRestrictedToOfficialReferenceGlaciers_StillDivergesFromWgmsRegionalAverage`
feeds `WgmsGlacierMassBalanceSourceFileTransformer` — completely unmodified —
only the real rows for WGMS's own 61 official reference glaciers (every one
trivially passes its existing ">10 years" filter, since all have 30+) and
compares its output to `mb_ref.csv`'s `REF_regionAVG` for every year both
sides have data for:

| Year | n (current) | Current method (m w.e.) | n (WGMS) | WGMS `mb_ref.csv` (m w.e.) | Diff (current − WGMS) |
|---|---|---|---|---|---|
| 1950 | 5 | +0.638 | 5 | −1.141 | +0.503 |
| 1951 | — (n<5, dropped) | — | 4 | −0.344 | — |
| 1952 | — (n<5, dropped) | — | 4 | −0.561 | — |
| 1953 | 8 | +0.075 | 8 | −0.561 | +0.636 |
| 1954 | 7 | +0.155 | 7 | −0.420 | +0.575 |
| 1955 | 8 | +0.845 | 8 | +0.372 | +0.473 |
| 1956 | 9 | +0.455 | 9 | −0.160 | +0.615 |
| 1957 | 12 | +0.495 | 12 | −0.094 | +0.589 |
| 1958 | 12 | −0.030 | 12 | −0.868 | +0.838 |
| 1959 | 13 | −0.026 | 13 | −0.468 | +0.442 |
| 1960 | 15 | +0.124 | 15 | −0.577 | +0.701 |
| 1961 | 16 | +0.257 | 16 | −0.437 | +0.694 |
| 1962 | 20 | +0.325 | 20 | −0.203 | +0.528 |
| 1963 | 23 | +0.053 | 23 | −0.352 | +0.405 |
| 1964 | 23 | +0.411 | 23 | +0.319 | +0.092 |
| 1965 | 25 | +0.835 | 25 | +0.159 | +0.676 |
| 1966 | 28 | +0.331 | 28 | −0.225 | +0.556 |
| 1967 | 30 | +0.593 | 30 | −0.118 | +0.711 |
| 1968 | 33 | +0.567 | 33 | −0.070 | +0.637 |
| 1969 | 34 | +0.107 | 34 | −0.488 | +0.595 |
| 1970 | 35 | +0.114 | 35 | −0.287 | +0.401 |
| 1971 | 35 | +0.269 | 35 | −0.231 | +0.500 |
| 1972 | 35 | +0.328 | 35 | −0.279 | +0.607 |
| 1973 | 35 | +0.298 | 35 | −0.177 | +0.475 |
| 1974 | 35 | +0.443 | 35 | −0.187 | +0.630 |
| 1975 | 36 | +0.485 | 36 | −0.225 | +0.710 |
| 1976 | 38 | +0.381 | 38 | −0.182 | +0.563 |
| 1977 | 39 | +0.453 | 39 | −0.256 | +0.709 |
| 1978 | 39 | +0.411 | 39 | −0.187 | +0.598 |
| 1979 | 39 | +0.125 | 39 | −0.417 | +0.542 |
| 1980 | 40 | +0.346 | 40 | −0.123 | +0.469 |
| 1981 | 39 | +0.417 | 39 | −0.190 | +0.607 |
| 1982 | 41 | +0.050 | 41 | −0.487 | +0.537 |
| 1983 | 42 | +0.417 | 42 | +0.128 | +0.289 |
| 1984 | 45 | +0.393 | 45 | −0.259 | +0.652 |
| 1985 | 45 | +0.066 | 45 | −0.307 | +0.373 |
| 1986 | 47 | +0.011 | 47 | −0.481 | +0.492 |
| 1987 | 46 | +0.445 | 46 | +0.096 | +0.349 |
| 1988 | 47 | +0.066 | 47 | −0.074 | +0.140 |
| 1989 | 51 | +0.481 | 51 | −0.228 | +0.709 |
| 1990 | 54 | +0.185 | 54 | −0.484 | +0.669 |
| 1991 | 55 | −0.002 | 55 | −0.503 | +0.501 |
| 1992 | 60 | +0.323 | 60 | −0.116 | +0.439 |
| 1993 | 61 | +0.499 | 61 | −0.132 | +0.631 |
| 1994 | 60 | +0.098 | 60 | −0.531 | +0.629 |
| 1995 | 59 | +0.314 | 59 | −0.459 | +0.773 |
| 1996 | 59 | +0.168 | 59 | −0.473 | +0.641 |
| 1997 | 59 | +0.043 | 59 | −0.640 | +0.683 |
| 1998 | 59 | −0.369 | 59 | −0.722 | +0.353 |
| 1999 | 57 | +0.192 | 57 | −0.698 | +0.890 |
| 2000 | 57 | +0.286 | 57 | −0.359 | +0.645 |
| 2001 | 57 | +0.127 | 57 | −0.270 | +0.397 |
| 2002 | 57 | −0.027 | 57 | −0.428 | +0.401 |
| 2003 | 57 | −0.659 | 57 | −0.524 | −0.135 |
| 2004 | 55 | −0.245 | 55 | −0.731 | +0.486 |
| 2005 | 58 | −0.318 | 58 | −0.816 | +0.498 |
| 2006 | 58 | −0.603 | 58 | −0.714 | +0.111 |
| 2007 | 57 | −0.029 | 57 | −0.539 | +0.510 |
| 2008 | 58 | +0.132 | 58 | −0.375 | +0.507 |
| 2009 | 58 | −0.114 | 58 | −0.452 | +0.338 |
| 2010 | 58 | −0.292 | 58 | −0.873 | +0.581 |
| 2011 | 59 | −0.280 | 59 | −0.747 | +0.467 |
| 2012 | 60 | −0.038 | 60 | −0.722 | +0.684 |
| 2013 | 56 | −0.117 | 56 | −0.716 | +0.599 |
| 2014 | 56 | −0.111 | 56 | −0.709 | +0.598 |
| 2015 | 60 | −0.255 | 60 | −0.805 | +0.550 |
| 2016 | 60 | −0.214 | 60 | −0.987 | +0.773 |
| 2017 | 60 | −0.179 | 60 | −0.666 | +0.487 |
| 2018 | 60 | −0.424 | 60 | −0.937 | +0.513 |
| 2019 | 61 | −0.476 | 61 | −0.993 | +0.517 |
| 2020 | 61 | −0.030 | 61 | −0.883 | +0.853 |
| 2021 | 61 | −0.263 | 61 | −0.676 | +0.413 |
| 2022 | 61 | −0.843 | 61 | −1.014 | +0.171 |
| 2023 | 61 | −0.974 | 61 | −1.218 | +0.244 |
| 2024 | 61 | −0.776 | 60 | −1.039 | +0.263 |
| 2025 | 60 | −0.744 | 59 | −1.091 | +0.347 |

(all values m w.e.; "current" = this exact 61-glacier input run through the
unmodified transformer; two years, 1951 and 1952, have only 4 contributing
glaciers for this list and are dropped by the transformer's own
`MinimumContributingGlaciers = 5` rule, so there's nothing to compare there.)

**Stats across the 74 comparable years:** mean absolute difference **0.526 m
w.e.**, median **0.54**, max **0.89** (1999). Same sign in 33/74 years;
`|diff| > 0.1` in 73/74 years.

**Note the near-universal direction:** the current method reads *higher*
(less negative, or positive where WGMS is negative) than `mb_ref.csv` in 73
of the 74 comparable years. That's a mechanical, structural consequence of
own-mean anomaly normalization, not a sign of a bug: every one of these
glaciers has a long-run *negative* mean annual balance (they've mostly been
losing mass across their multi-decade records), so each glacier's own
long-term mean is already negative. Expressing a year as a deviation *from
that negative mean* systematically pulls early- and mid-record years toward
(and often above) zero, while WGMS's raw figure keeps the absolute negative
magnitude throughout. The two series are answering different questions: "is
this year unusually bad *for this glacier, relative to its own history*"
(the transformer's anomaly index) vs. "what was the actual physical mass
balance this year" (WGMS's raw regional average).

## Why the two metrics differ mechanically

1. **Anomaly-from-own-mean vs. raw annual balance.** The transformer
   expresses each glacier's value as a deviation from its own multi-decade
   mean before averaging — a deliberate, documented design choice (see the
   linked plan doc) that produces an equal-weighted, trend-normalized-per-glacier
   index. WGMS's regional figure is a mean of the **raw** annual balance, so
   it carries each glacier's absolute magnitude (and the shared long-term
   downward trend) directly. `mb_ref.csv`'s own cumulative column (a running
   sum of `REF_regionAVG`, confirmed above) only makes physical sense as a
   cumulative *mass balance* because the yearly figure being summed is a raw
   balance, not an anomaly-from-self — that's true of WGMS's chosen metric,
   not evidence that the transformer's different metric is broken.

2. **Flat glacier average vs. two-stage regional average.** WGMS's own
   methodology page states global/regional values are "calculated using only
   one single value (averaged) for each region with glaciers, to avoid a bias
   to well-observed regions." The transformer instead flat-averages every
   qualifying glacier together. Of the 61 official reference glaciers, 17 are
   in Central Europe (the Alps) alone — almost 28% of the set — so a flat
   average weights the Alps roughly 6x more heavily than WGMS's
   one-value-per-region approach does. Whether that's desirable depends on
   what the index is for: WGMS's regional balancing suppresses any single
   region's local extremes; the transformer's flat average instead lets every
   individual glacier's record count equally, wherever it is.

As a separate data point (not a recommendation), a prototype implementing
WGMS's exact method — raw balance, two-stage regional averaging — was built
in the test suite purely to confirm the mechanism, not to propose replacing
anything:

| Method (same 61-glacier list throughout) | Mean abs diff vs. `mb_ref.csv` | Max abs diff |
|---|---|---|
| Transformer's own-mean-anomaly, flat average (current, unmodified) | 526 mm | 890 mm |
| Raw annual balance, flat average | 150 mm | 737 mm |
| Raw annual balance, **two-stage** regional average (WGMS's actual method) | 13 mm | 214 mm |

This confirms *both* the anomaly-vs-raw choice and the flat-vs-regional
averaging choice each contribute materially to the numeric gap — they're
compounding, not just one dominant factor — but the table exists to explain
*why* the two published series carry different numbers, not to argue one
average is more correct than the other.

## Verification

Three new tests in `ClimateExplorer.UnitTests/WgmsReferenceGlacierMassBalanceTests.cs`,
backed by real (not synthetic) WGMS "Fluctuations of Glaciers" 2026-02-10 data
trimmed into `ClimateExplorer.UnitTests/GlacierFixtures/`:

- `mass_balance_all_glaciers.csv` — every glacier's real `glacier_id,
  glacier_name, country, year, annual_balance` rows (551 glaciers, 8,944
  rows) from the live release, trimmed to the columns needed.
- `glacier_regions.csv` — `glacier_id → gtng_region` (WGMS's own GTN-G
  first-order region classification, from `glacier.csv`) for the same 551
  glaciers.
- `mb_ref.csv` — already present (WGMS's published reference-glacier
  regional average, the ground truth this investigation compares against).

Tests:

1. `MoreThan30YearRule_AppliedToRealWgmsData_AlmostReproducesOfficialReferenceGlacierList` —
   step 1/2's finding (1 false negative, 22 false positives vs. the official
   61), locked in as a regression test/reference — not a proposal to change
   the transformer's existing 10-year rule.
2. `TransformAsync_RealDataRestrictedToOfficialReferenceGlaciers_StillDivergesFromWgmsRegionalAverage` —
   runs the **actual, unmodified** `WgmsGlacierMassBalanceSourceFileTransformer`
   against real rows for exactly the 61 official reference glaciers, and
   asserts its output still diverges from `mb_ref.csv` by >0.1 m w.e. across
   8 representative years (a subset of the full table above) — quantifies how
   much of the gap survives once the glacier-list difference is removed.
3. `RawTwoStageRegionalAverage_OfOfficialReferenceGlaciers_ReproducesWgmsPublishedIndex` —
   a prototype of WGMS's actual methodology (raw balance, two-stage regional
   averaging), **not wired into production code**, asserting it reproduces
   `mb_ref.csv` within ±0.05 m w.e. — confirms the mechanism identified above,
   doesn't change anything.

`dotnet build` clean; full `ClimateExplorer.UnitTests` suite: 561/561 passed
(558 pre-existing + 3 new).

## Data used

Downloaded fresh for this investigation (not checked into the repo in full —
39 MB): `https://wgms.ch/downloads/DOI-WGMS-FoG-2026-02-10.zip`, the same
release already wired into `DataSetDefinitionsBuilder.cs`'s `DataDownloadUrl`.
`data/mass_balance.csv` and `data/glacier.csv` (for `gtng_region`) were read
directly; trimmed copies of the rows/columns actually needed now live in
`ClimateExplorer.UnitTests/GlacierFixtures/` as described above.

## What this doesn't decide (as of 2026-08-27 — superseded, see below)

At the time this investigation was written up, no production code had been
changed — it only explained and quantified where the numbers differed and
why, without recommending a fix. **That changed the next day; see the
"Calculation method replaced" section below.**

## Calculation method replaced (2026-08-28)

Having the numbers explained wasn't the end state wanted: the
anomaly-from-own-mean approach was judged more complicated than necessary
and harder to read as "what is mass balance actually doing over time" than
just averaging raw values — a judgement about which metric is more useful,
not a correction of an error. `WgmsGlacierMassBalanceSourceFileTransformer`
was rewritten accordingly:

- **Raw annual balance only, no anomaly, ever.** The per-glacier
  own-mean-deviation step is gone entirely.
- **The averaging stage is now a parameter**, `WgmsAveragingStage`:
  `OneStage` (flat mean of every qualifying glacier's raw balance) or
  `TwoStage` (mean within each glacier's GTN-G region first, then mean of
  region means — WGMS's own approach, reading `glacier.csv`'s `gtng_region`).
- **The glacier-list rule is now a parameter too**, `WgmsGlacierFilter`:
  `Benchmark` (>10 years) or `Reference` (>30 years) — the same two
  categories discussed in Step 1/2 above, now both selectable on the same
  transformer instead of only the 10-year rule being hard-coded.

This doesn't retract the "different metrics, not a bug" framing above — WGMS's
method still isn't presented as objectively "more correct." It was chosen
because a simple, direct, physically-meaningful raw average was judged the
better fit for this dataset, and because it happens to make an
apples-to-apples comparison against `mb_ref.csv` possible where the anomaly
version couldn't.

**Verification, using the actual (new) transformer against real data**
(`WgmsReferenceGlacierMassBalanceTests.cs`):

| Configuration | vs. `mb_ref.csv` (mean abs diff) |
|---|---|
| `Reference` + `TwoStage` (WGMS's own list-and-method shape, mechanically applied) | **0.134 m w.e.** |
| `Reference` + flat/one-stage (list fixed, averaging still flat) | 0.150 m w.e. (see the original Step 3 table above, name-restricted version) |
| Old anomaly/flat-average method (name-restricted to the 61 official glaciers) | 0.526 m w.e. |

`Reference`+`TwoStage` doesn't hit the ~0.013 m w.e. figure from the earlier
name-matched prototype in this doc's original "Why the two metrics differ"
section, because it uses the transformer's own mechanical 30-year filter
(82 glaciers: 1 official glacier missed, 22 extra included — see Step 1/2)
rather than WGMS's exact 61-glacier curated list by name. 0.134 m w.e. mean
(max 0.508, in the thin 1985 sample) is still a large improvement over the
old method's 0.526 m w.e., and confirms the remaining gap is now
overwhelmingly about *which glaciers* rather than *how the average is
computed*.

**Shipped choice: `Benchmark` + `TwoStage`**, not `Reference` — the wider,
138-glacier list, but with WGMS's own region-aware averaging. A direct
comparison test
(`TransformAsync_BenchmarkVsReferenceFilter_BothTwoStage_OnRealData_ProduceSimilarValues`)
confirms `Benchmark`+`TwoStage` stays close to `Reference`+`TwoStage`: 0.035
m w.e. mean absolute difference across the record — far smaller than either
filter's gap to `mb_ref.csv`, and far smaller than the old method's gap.
Using the broader Benchmark list costs very little once two-stage regional
averaging is in place.

Full details, code, and file list: see the "Addendum — calculation method
replaced" section in
[docs/design/2026-08-26-01-wgms-glacier-mass-balance-dataset-plan.md](../design/2026-08-26-01-wgms-glacier-mass-balance-dataset-plan.md).
