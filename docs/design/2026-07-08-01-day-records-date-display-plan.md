# Recent Observations: Day records date display

- **Date:** 2026-07-08
- **Status:** Implemented 2026-07-08 (see addendum)
- **Author:** Codex
- **Scope:** `RecentObservationsCalculator`, Recent Observation expanded metric view models, `RecentObservationTile` rendering/CSS, related unit tests
- **Builds on:** [recent observations metric architecture](2026-06-17-01-recent-observations-metric-architecture.md) and [expandable tiles with metric groups](2026-06-17-02-recent-observations-expandable-tiles.md)
- **Branch context:** `development`

## Goal

Show the actual occurrence date beside values in the expanded **Day records** group:

```text
Highest daily maximum
42.6°C · 12 Jan
3rd highest of 102
Record high: 44.0°C · 4 Jan 2020
Record low: 31.2°C · 8 Feb 1972
```

Apply this to every metric in the `Day records` group: the four temperature daily
extremes and the precipitation daily extreme. Keep the existing Period records
group, ranking logic, tile ordering, summary historical range, and responsive
two-column expanded layout unchanged.

## Current findings

The source data already has full daily dates. `DataRecord.Date` flows into
`DailyObservation.Date`, and both recent and historical series are merged by date
before calculation.

The date is lost later in two places:

- `PeriodObservation.MetricValues` stores only `metric key -> double`, so a
  range metric such as "highest daily maximum" keeps the selected value but not
  the day in the current period that produced it.
- `HistoricalPeriodValue` stores only `Value` and `Year`, so record references can
  only render `44.0°C (2020)` even when the record came from a specific daily
  observation inside the comparable period.

The expanded tile UI then renders:

- current metric line: `CurrentValue` plus either a badge or `RankText`
- historical references: `Record high: Value (Year)` / `Record low: Value (Year)`

So this should be fixed in the calculation/model layer first, then rendered by
the `Day records` group only.

## Proposed model changes

Introduce an internal calculated metric value that carries the selected occurrence
date when an aggregation selects a single day:

```csharp
private sealed record MetricObservationValue(double Value, DateOnly? OccurredOn);
```

Change `PeriodObservation.MetricValues` from `IReadOnlyDictionary<string, double>`
to `IReadOnlyDictionary<string, MetricObservationValue>`.

Update metric aggregation so:

- `MetricAggregation.Max` returns the maximum value and the date of the daily
  record that supplied it.
- `MetricAggregation.Min` returns the minimum value and the date of the daily
  record that supplied it.
- `MetricAggregation.Mean` and `MetricAggregation.Sum` keep `OccurredOn = null`
  because they describe the whole period, not one selected day.

Extend historical values similarly:

```csharp
private sealed record HistoricalPeriodValue(double? Value, short? Year, DateOnly? OccurredOn);
```

For daily-date comparisons, `OccurredOn` is the record date. For range
comparisons, it is the day inside that equivalent historical period that supplied
the metric's max/min value.

Tie handling should stay deterministic and close to current behavior:

- If several days in a period share the same selected max/min, use the earliest
  date in that period.
- If several historical periods tie for a record high/low, keep the current
  earliest-year behavior, then use earliest occurrence date within that year.

## Proposed view-model changes

Add date fields rather than deriving new display text from the existing year-only
property:

```csharp
public sealed record RecentObservationRecordsViewModel
{
    public DateOnly? CurrentValueDate { get; init; }
}

public sealed record RecentObservationMetricRecordViewModel
{
    public DateOnly? Date { get; init; }
}
```

Keep `RecentObservationMetricRecordViewModel.Year` for this narrow change so the
Period records group and any existing tests can continue to render exactly as
they do today. Populate both `Year` and `Date` from `HistoricalPeriodValue` where
the date is known; the UI chooses which field to show based on the selected group.

## Rendering plan

In the expanded metrics UI, branch only when the selected group key is
`MetricGroupKey.DayRecords`.

For Day records current values:

- Render the existing bold/prominent `CurrentValue`.
- If `CurrentValueDate` is present, append ` · d MMM` on the same line in muted
  supporting text.
- Render rank/status on the next line for this group so the value/date pair reads
  cleanly.

For Day records record references:

- Render `Record high: 44.0°C · 4 Jan 2020` when `Date` is present.
- Fall back to the existing `Record high: 44.0°C (2020)` shape if a date is
  missing.

Use explicit date helpers for the new display:

- Current selected daily extreme: `d MMM`
- Historical record high/low: `d MMM yyyy`

The existing `recent-observation-detail-metrics` grid can stay as-is. Add only
small supporting CSS classes, for example:

- `.recent-observation-detail-current-date`
- `.recent-observation-detail-rank.day-records`
- `.recent-observation-detail-record-value`
- `.recent-observation-detail-record-date`

These should wrap naturally inside each metric cell and preserve the current
mobile single-column / tablet-fullscreen two-column behavior.

## Implementation steps

1. Add `MetricObservationValue` and update `ComputeMetrics` to preserve
   occurrence dates for max/min aggregations.
2. Update `PeriodObservation.MetricValues` consumers to read `.Value` for
   existing ranking/formatting logic.
3. Extend `HistoricalPeriodValue` with `OccurredOn` and update both historical
   distribution paths to populate it.
4. Populate `CurrentValueDate` and record `Date` in `BuildMetric` /
   `BuildRecordReference`.
5. Update `RecentObservationTile` rendering/CSS for `MetricGroupKey.DayRecords`
   only. Keep Period records and the single-day `Day` group visually unchanged.
6. Update unit tests for current and historical dates in Day records.

If the tile component needs new formatting helpers, put new C# in a
`RecentObservationTile.razor.cs` code-behind. Moving the existing inline code
block can be done mechanically in the same change if needed, but should not alter
behavior.

## Test plan

Use `RecentObservationsServiceTests`; no app/browser/Playwright run is needed.

Add or update focused tests:

- Temperature range tile: `Highest daily maximum` exposes `CurrentValueDate`,
  `RecordHigh.Date`, and `RecordLow.Date`.
- Temperature range tile: `Lowest daily maximum`, `Highest daily minimum`, and
  `Lowest daily minimum` preserve the correct occurrence date for min/max
  aggregations.
- Precipitation range tile: `Highest daily precipitation` exposes current and
  historical occurrence dates.
- Tie case: current max/min ties choose the earliest date.
- Existing Period records assertions still pass with year-only display fields
  intact.

Suggested test names should follow the repository convention, for example:

- `GetTemperatureRecords_DayRecordsMetricsHaveOccurrences_ExposesCurrentAndHistoricalDates`
- `GetPrecipitationRecords_DayRecordsMetricHasOccurrence_ExposesCurrentAndHistoricalDates`
- `GetTemperatureRecords_DayRecordsMetricTies_UsesEarliestOccurrenceDate`

## Out of scope

- No changes to Period records display.
- No changes to collapsed tile historical range display.
- No changes to ranking, percentile, record-status, comparison-period selection,
  or tile ordering.
- No app run, Playwright, Lighthouse, or browser tests for this change.

## Addendum - implementation notes

Implemented as planned, with the following concrete choices:

- `MetricObservationValue` now carries the aggregated value and the occurrence
  date for max/min metrics. Mean and sum metrics keep no occurrence date.
- `HistoricalPeriodValue` now carries the record occurrence date, while retaining
  the existing `Year` field so year-only display paths continue to work.
- The tile UI branches only for `MetricGroupKey.DayRecords`. Current values show
  `value · d MMM`; historical record references show `value · d MMM yyyy` when a
  date is available, otherwise they fall back to the existing `value (year)`.
- New component C# helpers live in `RecentObservationTile.razor.cs`; existing
  inline component code was left in place to keep the change narrow.
- Added unit coverage for temperature Day records, precipitation Day records, and
  current-period tie handling.

Verification: `dotnet test ClimateExplorer.UnitTests/ClimateExplorer.UnitTests.csproj --no-restore`
passed with 257 tests.
