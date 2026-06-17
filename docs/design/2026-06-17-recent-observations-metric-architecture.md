# Recent Observations: evolving from period-centric to period + metric

- **Date:** 2026-06-17
- **Status:** Implemented 2026-06-17 (steps 1–5 + dead-code removal; step 6 deferred — see addendum)
- **Author:** Patrick Lea (with Claude Code review)
- **Scope:** `RecentObservationsService` and the Recent Observation tile pipeline
- **Branch context:** `issues/recent-ghcnd`

## Why this document exists

The Recent Observation tiles currently support a fixed, complete set of period
types (daily, last week, monthly, seasonal, current-year / year-to-date). No new
period types are planned. The next phase of work adds **more statistics within
each period**, for example:

> average max temp, average min temp, mean temp, highest/lowest daily maximum,
> highest/lowest daily minimum, historical equivalents, rankings, percentiles,
> record detection, additional precipitation metrics.

This is a different growth axis than the one the code was built for. The current
design treats each period as carrying **one** ranked metric. Expanding to ~10
ranked metrics under that model means editing 6+ parallel methods per metric.
This document records the decision to pivot the data model from
**period-centric** to **period + pluggable metric**, and the concrete plan to do
it as one focused refactor *before* the statistics expansion begins.

## Current architecture (as of this date)

Flow, per data domain (temperature, precipitation):

1. **Build daily records** — `BuildDailyTemperature` / `BuildDailyPrecipitation`.
2. **Generate periods** — `BuildTemperaturePeriods` / `BuildPrecipitationPeriods`
   emit a flat `List<PeriodObservation>`.
3. **Compute historical distribution** — for the period's single primary metric,
   over equivalent historical periods.
4. **Build a tile** — `BuildTemperatureTile` / `BuildPrecipitationTile`.

The central contract:

```csharp
private sealed record PeriodObservation(
    string Title,
    string ComparisonLabel,
    string ComparisonLabelPlural,
    DateOnly StartDate,
    DateOnly EndDate,
    double PrimaryValue,          // the ONE ranked metric
    double? SupportingValueOne,   // display-only (avg max temp)
    double? SupportingValueTwo,   // display-only (avg min temp)
    ObservationCompleteness Completeness,
    PeriodKind Kind,
    PeriodComparisonMode ComparisonMode,
    int? PeriodOffset,
    string? Note);
```

`RecentObservationComparison.Rank(value, historicalValues)` ranks **only**
`PrimaryValue`. The supporting values are shown but never ranked — that gap is
the exact shape of all the future work.

### What is already good (and stays)

- **Period generation** (days/weeks/months/seasons/year, including the dynamic
  generators and `MeteorologicalSeasonCalculator`). Complete, clean, and unrelated
  to the metric axis. **Do not generalise further.**
- **`RecentObservationComparison.Rank`** — already metric-agnostic. It takes a
  current value plus a distribution and returns rank / percentile / record flags.
  It does not need to change to support more metrics; it just needs to be *called
  once per metric*.
- **Completeness model, season calculator, view models, tile razor.** Fine as-is.

## The two blocking problems

### Problem 1 — single ranked value per period

`PeriodObservation` carries one `PrimaryValue`. Every new ranked statistic today
requires extending this record, touching every `Create*Period` / `Add*RangePeriod`
call site, adding a parallel historical-extraction path, and editing both tile
builders. Cost is multiplicative in (metrics × periods).

### Problem 2 — historical max/min is discarded (must fix first)

`CombineTemperatureRecords` averages historical daily max + min into a single
`mean` value and **throws max and min away**:

```csharp
.Select(x => new DataRecord(x.Year, x.Month, x.Day, (x.Value!.Value + minByKey[x.Key!]) / 2d))
```

Because the historical series collapses to mean before it reaches the comparison
stage, you **cannot** rank "average max temp", "highest daily maximum", etc.
historically — the data simply isn't there. This is a prerequisite for any
max/min-based statistic and must be fixed before the metric model can pay off.

## Decision

Invert the model:

- A **period** becomes "a date range + completeness + labels" — no embedded
  metric values.
- A **metric** is a small, code-defined descriptor applied to *every* period.
- The historical extractor returns a **distribution per metric**, computed in a
  **single pass** over each period's equivalent-year grouping.
- The two tile builders collapse into **one** loop over a period's metrics.

This keeps the expensive, correct parts (period generation, ranking math)
untouched and concentrates change in the data routing between them.

## Target shapes (sketch)

These are illustrative, not final signatures — match surrounding style on
implementation.

### Metric descriptor

```csharp
internal enum MetricAggregation
{
    Mean,   // average of daily values over the period
    Sum,    // total over the period (rainfall)
    Max,    // highest daily value in the period
    Min,    // lowest daily value in the period
}

/// Defines one ranked statistic and how to compute it from daily values.
internal sealed record Metric(
    string Key,                              // stable id, e.g. "temp.avg-max"
    string Label,                            // tile label, e.g. "Average max temp"
    Func<DailyObservation, double?> Select,  // pull one field from a daily record
    MetricAggregation Aggregation,           // how the period rolls daily -> one value
    Func<double, string> Format,             // "21.4°C" / "12.3mm"
    ComparisonVocabulary Vocabulary);        // warmest/coolest vs wettest/driest, etc.
```

A unified daily record so one `Select` shape works across domains:

```csharp
internal sealed record DailyObservation(
    DateOnly Date,
    double? Max,
    double? Min,
    double? Mean,
    double? Rainfall);
```

Metric catalogues are static, code-defined lists (no DI / reflection):

```csharp
internal static class TemperatureMetrics
{
    public static readonly Metric MeanTemp  = new("temp.mean", "Mean temperature", d => d.Mean, MetricAggregation.Mean, FormatTemp, Warm);
    public static readonly Metric AvgMax    = new("temp.avg-max", "Average max temp", d => d.Max, MetricAggregation.Mean, FormatTemp, Warm);
    public static readonly Metric AvgMin    = new("temp.avg-min", "Average min temp", d => d.Min, MetricAggregation.Mean, FormatTemp, Warm);
    public static readonly Metric HighestMax= new("temp.highest-max", "Highest daily max", d => d.Max, MetricAggregation.Max, FormatTemp, Warm);
    // ... lowest max, highest min, lowest min, etc.

    public static readonly IReadOnlyList<Metric> All = [MeanTemp, AvgMax, AvgMin, HighestMax, /* ... */];
}
```

Adding a statistic later = **one line in this list**.

### Period carries computed metric values, not named fields

```csharp
private sealed record PeriodObservation(
    PeriodLabels Labels,                 // Title + comparison labels (see below)
    DateOnly StartDate,
    DateOnly EndDate,
    ObservationCompleteness Completeness,
    PeriodKind Kind,
    PeriodComparisonMode ComparisonMode,
    int? PeriodOffset,
    string? Note,
    IReadOnlyDictionary<string, double> MetricValues);  // keyed by Metric.Key
```

`MetricValues` is produced by applying each domain metric's `Aggregation` over the
period's daily records — one place, replacing the bespoke `Average(Mean/Max/Min)`
/ `Sum(Rainfall)` logic scattered across the `Add*RangePeriod` methods.

### Historical extractor returns a distribution per metric, one pass

```csharp
// Group equivalent historical years ONCE, compute every metric inside the group.
private static IReadOnlyDictionary<string, HistoricalValues>
    GetHistoricalDistributions(
        IReadOnlyList<DailyObservation> historical,
        PeriodObservation period,
        IReadOnlyList<Metric> metrics);
```

This simultaneously removes the temp/precip historical duplication **and** the
per-metric re-scan performance concern: the `GroupBy` over equivalent years runs
once per period instead of once per (period × metric).

### One tile builder

```csharp
private static RecentObservationTileViewModel BuildTile(
    PeriodObservation period,
    IReadOnlyDictionary<string, HistoricalValues> distributions,
    IReadOnlyList<Metric> metrics)
{
    foreach (var metric in metrics)
    {
        var current = period.MetricValues[metric.Key];
        var ranking = RecentObservationComparison.Rank(current, distributions[metric.Key].Values);
        // emit a stat row (+ optional headline/record/percentile) using metric.Vocabulary
    }
}
```

Temperature vs precipitation becomes **data** (which metric list, which
vocabulary), not parallel code.

### Label consolidation (opportunistic)

Fold `CreatePeriodTitle` / `CreateComparisonLabel` / `CreateComparisonLabelPlural`
/ `CreateHistoricalContextLabel` into a single `PeriodLabels` record computed once
when the period is built. Reduces label drift as metrics multiply.

## Implementation plan (ordered)

Do these as **one focused refactor**, behaviour-preserving, then expand statistics
on top.

1. **Fix Problem 2 first.** Carry historical max/min through instead of collapsing
   to mean in `CombineTemperatureRecords`. Introduce the unified `DailyObservation`
   carrier (Max/Min/Mean/Rainfall) for both current and historical series.
   *Checkpoint: existing tiles still render identically; tests green.*

2. **Introduce `Metric` + `MetricAggregation` + domain metric catalogues** that
   reproduce exactly today's outputs: temp = {mean (primary), avg-max, avg-min};
   precip = {rainfall sum}. No new statistics yet.

3. **Change `PeriodObservation` to `MetricValues`** and have period generation
   populate it via the metric `Aggregation`. Collapse
   `BuildTemperaturePeriods`/`BuildPrecipitationPeriods` and the `Add*RangePeriod`
   pair into single generic versions.

4. **Single-pass historical extractor** returning a distribution per metric.
   Collapse the temp/precip historical methods into one path.

5. **Unify the tile builders** into one metric-driven loop. Keep the view model
   (`RecentObservationTileViewModel`) and razor unchanged — they already render a
   `List<RecentObservationStatViewModel>`.

6. **Consolidate labels** into `PeriodLabels` (optional, do while in #3).

7. **Only now** add the new statistics — each is a one-line addition to a metric
   catalogue.

### Test strategy

`RecentObservationsServiceTests` (~1000 lines) is the safety net. Steps 1–6 must
be **output-identical**: run the suite green after each step. The refactor should
not change a single rendered tile until step 7 deliberately adds metrics.

## Explicitly out of scope (do NOT do)

- **Further generalising period generation.** Period types are fixed; abstracting
  for hypothetical new ones is pure cost.
- **Runtime/plugin/DI metric registration or reflection.** The metric set is
  closed and known — a static code list is the correct altitude.
- **Caching / extra parallelism beyond the single-pass grouping in step 4**, until
  profiling shows the already-batched (`Task.WhenAll`, shared-monthly-history)
  network fetches are the bottleneck.
- **A full rewrite.** Period generation and the ranking/percentile math are good
  and stay; this is targeted re-routing of data between them.

## Expected payoff

| Today | After refactor |
| --- | --- |
| New ranked metric = edit 6+ parallel methods | New metric = 1 line in a catalogue |
| Temp & precip logic duplicated ~90% | Single generic path; domain = data |
| Historical max/min unavailable (discarded) | Full daily max/min distributions available |
| Historical `GroupBy` re-run per period | Re-run once per period, all metrics in one pass |
| Only `PrimaryValue` rankable | Every metric rankable / percentile / record-checkable |

## Decision record

The architecture **should** evolve to period + metric before expanding the
statistics on each tile. The periods are done; the metrics are the growth axis,
and the current single-metric contract plus the lossy historical collapse are the
two things that would make the next ten statistics painful. Sequence the work as
steps 1–6 (behaviour-preserving) then step 7 (additive).

## Addendum — implementation notes (2026-06-17)

Implemented in `RecentObservationsService.cs`. All 160 unit tests
(48 `RecentObservations*`) stayed green and **output-identical** — no rendered
tile changed. Notes and corrections found while executing:

- **The monthly-history path was dead code, not an optimisation.** The original
  review called it "a genuine performance optimisation — keep it." That was
  wrong: `PeriodComparisonMode.MonthlySameMonth` was *referenced but never
  assigned* (both range builders hardcoded `DailyRange`, daily builders used
  `DailyDate`). So `GetTemperatureHistoricalMonthlyValues`,
  `GetPrecipitationHistoricalMonthlyValues`,
  `GetTemperatureHistoricalMonthlyRecords`, `ShouldUseSharedMonthlyHistory`
  (always false), the shared-monthly task (always null), and the
  `MonthlySameMonth` branch in `CreateHistoricalContextLabel` were all
  unreachable. **Deleted.** `PeriodComparisonMode` now has just `DailyDate` and
  `DailyRange`. This removed an entire dimension the plan assumed had to be
  generalised — net simplification.

- **Steps 1–5 landed as designed.** Unified `DailyObservation(Date, Max, Min,
  Mean, Rainfall)`; `Metric` + `MetricAggregation` (Mean/Sum/Max/Min) + static
  `TemperatureDomain` / `PrecipitationDomain` catalogues; `PeriodObservation`
  now carries `IReadOnlyDictionary<string, double> MetricValues`; one generic
  `BuildPeriods` / `AddRangePeriod`; one generic historical extractor
  (`GetHistoricalDistribution`) parameterised by the domain's primary metric;
  one generic `BuildTile`. The two `Build*Periods`, two `Add*RangePeriod`, two
  `Get*HistoricalDailyValues` and two `Build*Tile` pairs are now single methods.

- **Step 1 (max/min carry-through) is done structurally.** Historical temperature
  max/min now survive on `DailyObservation` instead of being averaged into mean
  and discarded. Only the *primary* metric (mean / rainfall) is ranked today, but
  the carrier and the `Metric.Select` indirection mean adding a max/min-based
  ranked statistic is now a catalogue entry, not a pipeline change.

- **Step 6 (PeriodLabels record) deferred — intentionally.** The four label
  methods (`CreatePeriodTitle`, `CreateComparisonLabel`,
  `CreateComparisonLabelPlural`, `CreateHistoricalContextLabel`) remain as pure
  functions called once at construction. Folding them into a `PeriodLabels`
  record is cosmetic, adds churn/risk, and the plan itself classed it as
  "opportunistic." Left for a future low-priority cleanup.

- **Single-pass-per-metric (the perf item in step 4) is latent, not yet active.**
  The extractor currently computes only the primary metric's distribution. When
  step 7 adds ranked supporting metrics, extend `GetHistoricalDistribution` to
  compute every metric inside the one `GroupBy` (return a dictionary keyed by
  metric) rather than re-grouping per metric. The shape is ready for it.

### Step 7 starting point (not done here)

Adding a new ranked statistic is now: (1) add a `Metric` to the relevant domain
catalogue; (2) if it needs ranking against history, have the extractor emit its
distribution in the shared grouping pass; (3) decide where it surfaces on the
tile (supporting stat row vs its own headline). No changes to period generation,
season logic, or the ranking/percentile math are required.
