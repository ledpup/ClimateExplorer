# Recent Observations: Variation expanded tab

- **Date:** 2026-07-08
- **Status:** Proposed
- **Author:** Codex
- **Scope:** `RecentObservationsCalculator`, Recent Observation expanded tab view models, `RecentObservationTile` rendering/CSS, related unit tests
- **Builds on:** [recent observations metric architecture](2026-06-17-01-recent-observations-metric-architecture.md), [expandable tiles with metric groups](2026-06-17-02-recent-observations-expandable-tiles.md), and [day records date display](2026-07-08-01-day-records-date-display-plan.md)
- **Branch context:** `development`

## Goal

Add a **Variation** expanded-tab button to non-day Recent Observation tiles for
both temperature and precipitation.

The tab explains how far the current period's aggregate value sits from
equivalent historical periods:

- Historical range: historical minimum to historical maximum.
- Typical variation: standard deviation of equivalent historical period values.
- Variation score: `(current value - historical average) / standard deviation`.

## Current model and rendering flow

`RecentObservationsPanel` passes each `RecentObservationTileViewModel` into
`RecentObservationTile` with a UI-only `RecentObservationTileExpansionState`.
The tile is expandable when `Tile.MetricGroups.Count > 0`.

Expanded tab buttons are generated from `Tile.MetricGroups`. Selection is stored
as `MetricGroupKey?` in `RecentObservationTileExpansionState`, and
`EnsureSelection` defaults to the first available group.

`RecentObservationTile` currently assumes every selected group contains
`RecentObservationRecordsViewModel` rows. That model is record/rank-shaped:
current value, optional occurrence date, rank text, record badge, record high,
record low, and comparison capability flags.

`RecentObservationsCalculator` builds each period, computes current metric values,
then calls `GetHistoricalDistributions` once per tile. That distribution is
already keyed by metric and contains the historical period values used by
`Period records` and `Day records`.

The relevant existing non-day groups are declared in `MetricDomain.Groups`:

- Temperature: `PeriodRecords` and `DayRecords`.
- Precipitation: `PeriodRecords` and `DayRecords`.

Daily tiles use `MetricDomain.DailyGroups`, which only contains `Day`.

## Recommended model approach

Do not solve this by only adding `Variation` to `MetricGroupKey` and reusing
`RecentObservationRecordsViewModel` as-is. The records model is already clear
and specific; forcing historical range, standard deviation, and z-score fields
into it would make the model read as "records, except when it is not records."

Use a dedicated variation row model plus a shared expanded-tab wrapper:

```csharp
public abstract record RecentObservationExpandedTabViewModel
{
    public MetricGroupKey Key { get; init; }
    public string Title { get; init; } = string.Empty;
}

public sealed record RecentObservationRecordsTabViewModel
    : RecentObservationExpandedTabViewModel
{
    public IReadOnlyList<RecentObservationRecordsViewModel> Metrics { get; init; } = [];
}

public sealed record RecentObservationVariationTabViewModel
    : RecentObservationExpandedTabViewModel
{
    public IReadOnlyList<RecentObservationVariationViewModel> Metrics { get; init; } = [];
}
```

Then change the tile from `MetricGroups` to an expanded-tab collection, or add
the new collection alongside `MetricGroups` as a short transition. Prefer the
rename if the implementation stays focused, because the UI will no longer be
rendering only metric record groups.

`RecentObservationVariationViewModel` should carry raw values and display text:

```csharp
public sealed record RecentObservationVariationViewModel
{
    public string Label { get; init; } = string.Empty;
    public double? HistoricalMinimum { get; init; }
    public double? HistoricalMaximum { get; init; }
    public double? TypicalVariation { get; init; }
    public double? VariationScore { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string? HistoricalRangeText { get; init; }
    public string? TypicalVariationText { get; init; }
    public string? VariationScoreText { get; init; }
    public string? UnavailableReason { get; init; }
    public int ComparablePeriodCount { get; init; }
}
```

Add `MetricGroupKey.Variation`, but use it only as the stable tab-selection key.
The type of the selected tab should decide whether records or variation markup is
rendered.

## Where to calculate variation

Calculate variation stats in `RecentObservationsCalculator`, directly after the
historical distributions have been built for a period.

`HistoricalValues` already has the inputs needed:

- finite equivalent historical values via `PeriodValues` / `Values`;
- historical min/max via `MinValue` and `MaxValue`;
- comparable count and unavailable reason;
- rank sample-size gates used by existing comparison text.

Add a small finite-values helper on `HistoricalValues` so variation calculation
does not accidentally include null or non-finite values. Use
`ClimateExplorer.Core.Stats.StandardDeviation`:

- `PopulationStandardDeviation(finiteHistoricalValues)` for typical variation;
- `StandardDeviationsFromMean(currentValue, finiteHistoricalValues)` for the
  variation score, or equivalent code using the same population standard
  deviation convention.

The current period value should come from `period.MetricValues[metric.Key]`,
matching `Period records`. For temperature, use the period aggregate metrics:

- average max temperature;
- average min temperature;
- mean temperature.

For precipitation, use the period precipitation amount.

Add variation metric labels explicitly rather than reusing record labels blindly:

- `Average max temp`
- `Average min temp`
- `Mean temperature`
- `Precipitation`

The cleanest place is either a `VariationLabel` field on the internal `Metric`
record or a `MetricDomain.VariationMetrics` list whose entries include display
labels.

## Rendering changes

Update `RecentObservationTile` so the expanded tab toggle iterates the shared tab
collection. The button behavior, selected styling, accessible group name, and
`aria-pressed` pattern can remain unchanged.

Render records tabs with the existing markup. Render variation tabs with a
separate branch:

```razor
@switch (SelectedTab)
{
    case RecentObservationRecordsTabViewModel records:
        // existing records layout
        break;
    case RecentObservationVariationTabViewModel variation:
        // variation layout
        break;
}
```

Use the existing `.recent-observation-detail-metrics` and
`.recent-observation-detail-metric` grid. Temperature will naturally lay out as:

- row 1, column 1: Average max temp;
- row 1, column 2: Average min temp;
- row 2, column 1: Mean temperature.

For each variation row, render only the available lines:

- `Historical range: 20.1°C to 25.8°C`
- `Typical variation: ±1.1°C`
- `Variation score: +1.8×`

Display formatting rules:

- temperature values: one decimal place and `°C`;
- precipitation values: existing precipitation formatting and `mm`;
- variation score: one decimal place, explicit `+` for positive values, normal
  minus for negative values, and `×`;
- omit the variation score when standard deviation is null, zero, non-finite, or
  otherwise unavailable.

Keep any new C# helpers in `RecentObservationTile.razor.cs`.

## CSS changes

Prefer no new layout CSS. The existing expanded metric grid already provides the
one-column mobile and two-column tablet/fullscreen behavior needed for the
temperature example.

If the variation lines need a small visual adjustment, add narrowly scoped styles
that reuse the current typography:

- a muted label/value line using the existing
  `.recent-observation-detail-record` style, or
- a single `.recent-observation-variation-line` class with the same color, size,
  and weight as expanded record context.

Do not change tile colors, button colors, spacing scale, or the expanded visual
hierarchy.

## Edge cases

Insufficient history:
Use `HistoricalValues.UnavailableReason` where there are no comparable values.
When there are too few values for a meaningful variation display, keep the row
but show a concise unavailable/limited-history reason rather than blank content.
Follow the existing count language, such as `only 1 comparable period`, where it
fits.

Standard deviation zero/null:
Show historical range and typical variation when valid, but omit `Variation
score` if the score cannot be calculated. A zero standard deviation should never
produce `Infinity`, `NaN`, or `0×` by division.

Missing precipitation data:
No precipitation period is produced when there are no current precipitation
records. If an individual precipitation metric lacks current value or historical
values, omit unavailable numeric lines and show the same unavailable reason used
for comparisons.

Missing max/min/mean temperature:
Current temperature daily observations require paired max/min values, so missing
recent max/min reduces completeness. Historical max/min may be unavailable, in
which case the calculator falls back to mean-temperature history. In that case,
mean temperature variation can still be calculated, but average max/min variation
rows should show unavailable history rather than borrowing mean history.

Completeness threshold:
When `ApplyCompletenessThreshold` suppresses comparisons, variation content must
also be suppressed because it is entirely comparison-derived. Either remove the
Variation tab from the thresholded tile or keep it with an unavailable reason;
prefer the latter only if the UI would otherwise cause a jarring tab-count shift
while the user adjusts the threshold.

## Tests to add or update

Use the repo's `MethodName_StateUnderTest_ExpectedBehavior` naming convention for
new tests.

Service/calculator tests:

- Non-day precipitation tiles expose tabs in order:
  `PeriodRecords`, `DayRecords`, `Variation`.
- Daily precipitation and daily temperature tiles expose `Variation`.
- Precipitation variation contains historical range, typical variation, and a
  signed variation score formatted with `mm` and `×`.
- Temperature variation contains exactly average max, average min, and mean
  temperature rows in that order, each using `°C`.
- Negative variation scores include the minus sign; positive scores include `+`.
- A zero-standard-deviation historical distribution omits the variation score.
- Mean-temperature fallback history does not fabricate average max/min variation.

View-model/completeness tests:

- `ApplyCompletenessThreshold` suppresses variation comparison values below the
  threshold.
- Existing record/rank stripping tests still pass for records tabs after the tab
  wrapper refactor.
- `RecentObservationTileExpansionState` preserves a valid `Variation` selection
  and falls back when a selected tab key disappears.

Core stats tests:

- Existing `StandardDeviation` tests already cover population standard deviation,
  z-score sign, empty history, and zero variance. Add only if the implementation
  changes filtering or formatting behavior outside the calculator.

Rendering tests:

- Keep this to unit/component-level assertions if available. Per project notes,
  do not run the website, Playwright, Lighthouse, or browser tests.

## Staged implementation plan

1. Introduce the expanded-tab view-model shape and add
   `RecentObservationVariationViewModel`.
2. Update expansion state and tile rendering to select/render polymorphic tabs,
   preserving existing records output.
3. Add `MetricGroupKey.Variation` and define per-domain variation metric lists.
4. Build variation rows in `RecentObservationsCalculator` from existing
   historical distributions and `StandardDeviation`.
5. Wire non-day tiles to append a **Variation** tab after **Day records**.
6. Apply completeness-threshold suppression to variation content.
7. Add focused unit tests for tab presence/order, variation calculation,
   formatting, zero-variance handling, missing history, and day-tile exclusion.
