# Recent Observations: Trend expanded tab

- **Date:** 2026-07-16
- **Status:** Proposed
- **Author:** Claude
- **Scope:** `ClimateExplorer.Core/Stats` (new trend-window abstraction), `ClimateExplorer.Web.Client/Services/RecentObservations/RecentObservationsCalculator.cs`, `ClimateExplorer.Web.Client/UiModel/RecentObservations/*`, `ClimateExplorer.Web.Client/Components/RecentObservations/*`, `ClimateExplorer.UnitTests`
- **Builds on:** [Linear Regression Utility](2026-07-11-01-linear-regression.md), [Recent Observations: Variation expanded tab](2026-07-08-02-recent-observations-variation-tab-plan.md)
- **Branch context:** `issues/file-rework`

## Goal

Add a **Trend** expanded-tab to Recent Observation tiles, appearing after
**Variation**, showing whether the tile's metric is trending up, down, or not
significantly changing over the long term — using the existing
`LinearRegressionCalculator`.

## Investigation summary

### Where tabs are defined and rendered

`RecentObservationsCalculator.BuildExpandedTabs` (`RecentObservationsCalculator.cs:896-926`)
builds the ordered tab list for a tile: one `RecentObservationRecordsTabViewModel`
per record group (`Period records`/`Day records`, or `Records` for daily tiles),
then a `RecentObservationVariationTabViewModel` appended if
`BuildVariationMetrics` produces at least one row. `RecentObservationTile.razor`
(`RecentObservationTile.razor:30-44`) switches on the tab's runtime type
(`is RecentObservationRecordsTabViewModel` / `is RecentObservationVariationTabViewModel`)
to pick which child component renders it. Tab selection itself
(`RecentObservationTileExpansionState`) is entirely key-based (`MetricGroupKey`)
and needs no changes.

**Integration point that is easy to miss:** `RecentObservationTileViewModel.StripComparisons`
(`RecentObservationTileViewModel.cs:105-114`) also switches on tab runtime type,
to blank out comparison content when a tile falls below the completeness
threshold. It has an explicit case for records tabs and variation tabs, with a
`_ => tab` fallthrough. Without a matching case for the new trend tab type,
a below-threshold tile would keep showing full trend numbers — silently
breaking the "respects completeness threshold" rule the Variation tab already
follows. A `RecentObservationTrendTabViewModel` case must be added here.

### Where comparable historical values come from (and completeness)

`RecentObservationsCalculator.GetHistoricalDistributions` builds one
`HistoricalValues` per metric per tile, already filtered to like-for-like
comparable periods:

- Daily tiles (`GetHistoricalDailyDateDistributions`, line 557): one value per
  prior year on the *same calendar date*.
- Period tiles (`GetHistoricalDailyRangeDistributions`, line 589): one
  aggregated value per prior *equivalent period* (same month/date range in an
  earlier year), only included if that year's period met the existing 90%
  (`MinimumHistoricalCoverage`) day-coverage bar (`group.RequiredDays`).

Both paths produce `HistoricalPeriodValue(double? Value, short? Year, DateOnly? OccurredOn)`
records (line 1773), one per comparable year, already deduplicated by year and
already respecting `ComparisonEndMode` (excluding the observed year itself).
`HistoricalValues.FiniteValues`/`ComparablePeriodCount` (lines 1744-1748) is the
same shape the Variation tab already reduces to a mean/standard-deviation over.
**This is exactly the (X = year, Y = comparable value) input the trend needs —
no new period-building or completeness code is required**, only a new
consumer of `HistoricalValues.PeriodValues`.

### Where Variation's pattern already shows the shape to copy

The Variation tab (`2026-07-08-02-recent-observations-variation-tab-plan.md`)
established the pattern this plan reuses directly:

- `MetricDomain.VariationMetrics` / `DailyVariationMetrics`
  (`RecentObservationsCalculator.cs:1607-1608`, `1627-1628`) already list
  exactly the metrics and labels a Trend row needs: temperature non-day
  `[AverageMaxTemperatureMetric, AverageMinTemperatureMetric, MeanTemperatureMetric]`
  → labels "Average max temp"/"Average min temp"/"Mean temperature"; day
  `[DailyMaxTemperatureMetric, DailyMinTemperatureMetric, DailyMeanTemperatureMetric]`
  → "Maximum"/"Minimum"/"Mean"; precipitation `[PrecipitationMetric]` /
  `[DailyPrecipitationMetric]` → "Precipitation". These are reused as-is for
  Trend rows (no new per-domain metric lists) — the labels the task's example
  wording uses are identical to `Metric.VariationLabel`.
- `BuildVariationMetrics`/`BuildVariationMetric` (lines 928-990) is the direct
  template for `BuildTrendMetrics`/`BuildTrendMetric`: same per-metric
  iteration, same `distribution?.FiniteValues`/`UnavailableReason` fallback
  shape.
- `RecentObservationVariations.razor` (and its `.razor.cs`) is the template for
  the new `RecentObservationTrend.razor`: a `<dl>` of `.recent-observation-detail-metric`
  rows, each with an optional unavailable-reason line, an "emphasis" headline
  (`.recent-observation-detail-headline` → `.recent-observation-detail-current`
  + `.recent-observation-detail-rank`), and plain `.recent-observation-detail-record`
  lines underneath. All of this CSS already exists, scoped `::deep` from
  `RecentObservationTile.razor.css:252-353`, and needs no additions.

### Where units are formatted

`FormatTemperature`/`FormatPrecipitation` (lines 1413-1421) format raw values
(1 dp / 0 dp) for display elsewhere on the tile; they are not reused as-is for
trend text because the task specifies different rounding for a *per-decade
slope* (2 dp for temperature, 0 dp for precipitation — matching the
in-flight, uncommitted rounding-to-0-dp change already present in
`FormatPrecipitation` and `PrecipitationAnomalyInfo.razor` on this branch, so
this plan's precipitation rounding is consistent with that change, not
introducing a new convention).

### The 60-year threshold already has a named precedent

`AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly = 60`
(`ClimateExplorer.Core/Calculators/AnomalyCalculator.cs:8`) is the exact
60-year gate the task references ("this has been a threshold applied
elsewhere... see heating score" — the warming anomaly, which feeds the heating
score, is what actually enforces it: `WarmingAnomalyInfo.razor:21`, "A minimum
of 60 years of data is required to calculate a warming anomaly."). Reuse this
constant directly rather than hard-coding a second `60` — same rationale, same
number, one source of truth.

`AnomalyCalculator` also already contains the closest existing precedent for
"first half of records" and "latest N years" windowing (lines 57-65): order by
year, take `count / 2` for the first half, take the most recent 30 by year for
the recent window. This plan's trend-window abstraction generalizes that
exact windowing logic — replacing "average of the window" with "regression
over the window" — rather than inventing new selection rules.

### `LinearRegressionCalculator` — reused, not modified

`ClimateExplorer.Core/Stats/LinearRegressionCalculator.cs` already returns a
`LinearRegressionResult` (`Input`, `Line`, `Fit`, `Significance`) that covers
every field the task's suggested `RecentObservationTrendResult` shape asks
for:

| Suggested field | Existing source |
|---|---|
| `Slope` (`± stderr`) | `Line.Slope`, `Significance.SlopeStandardError` |
| `PValue` | `Significance.PValue` |
| `IsSignificant` | `Significance.IsSlopeSignificant` |
| `RSquared` | `Fit.RSquared` |
| `Equation` | Derivable from `Line.Slope`/`Line.Intercept` (`RegressionLine.Predict`) — not stored as a separate string; not needed by any wording in this task |
| `Count` | `Input.Count` |
| `StartX`/`EndX` | `Input.MinimumX`/`Input.MaximumX` |

No new result type duplicates these fields. `LinearRegressionCalculator`
itself is not changed — per the task's constraint that it "should remain
abstract."

## Design: the reusable abstraction layer

The task asks for a layer that works for **both** Recent Observations and a
future chart-series trend/prediction preset, while keeping
`LinearRegressionCalculator` itself domain-agnostic. The natural split:

- **Layer 1 (unchanged):** `LinearRegressionCalculator` — pure OLS math over
  `DataPoint[]`. No knowledge of years, decades, temperature, or completeness.
- **Layer 2 (new, this plan):** a small chronological trend-window calculator
  in `ClimateExplorer.Core/Stats`, next to `LinearRegressionCalculator`. It
  knows about "a threshold of comparable points" and "a recent window" and
  "a first-half split" — the three views the task asks for — but nothing
  about temperature, precipitation, or years-vs-decades. This is reusable
  as-is by a future chart-series trend preset (which will also want a
  historical trend line and a recent-window trend line before calling
  `LinearRegressionCalculator.Predict` for the future-projection points).
- **Layer 3 (domain-specific, unchanged location):** `RecentObservationsCalculator`
  extracts `(year, comparable value)` points from each metric's existing
  `HistoricalValues`, calls Layer 2, then formats the three results into
  Recent-Observations-specific wording (labels, °C/mm, per-decade rounding,
  "no significant trend").

### New: `ClimateExplorer.Core/Stats/Model/TrendWindowSet.cs`

```csharp
namespace ClimateExplorer.Core.Stats.Model;

public sealed record TrendWindowSet(
    LinearRegressionResult HistoricalTrend,
    LinearRegressionResult RecentTrend,
    LinearRegressionResult FirstHalfTrend,
    int CompletePointCount);
```

All three trends are always present once a `TrendWindowSet` exists — the
30-year and first-half slices only need `Math.Min(30, count)` and `count / 2`
points respectively, both `>= 3` once the caller's minimum-points gate is
`>= 6`, so no per-slice nullability is needed. Insufficient overall data is
represented by `Calculate` returning `null`, not by null fields inside the
set.

### New: `ClimateExplorer.Core/Stats/TrendWindowCalculator.cs`

```csharp
namespace ClimateExplorer.Core.Stats;

using ClimateExplorer.Core.Stats.Model;

public static class TrendWindowCalculator
{
    public static TrendWindowSet? Calculate(
        IReadOnlyList<DataPoint> points,
        int minimumCompletePoints,
        int recentWindowSize,
        double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (minimumCompletePoints < 6)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumCompletePoints), "Must be at least 6 so a first-half split still has 3 points.");
        }

        if (points.Count < minimumCompletePoints)
        {
            return null;
        }

        var ordered = points.OrderBy(p => p.X).ToList();
        var recentCount = Math.Min(recentWindowSize, ordered.Count);
        var firstHalfCount = ordered.Count / 2;

        return new TrendWindowSet(
            LinearRegressionCalculator.Calculate(ordered, alpha),
            LinearRegressionCalculator.Calculate(ordered.TakeLast(recentCount), alpha),
            LinearRegressionCalculator.Calculate(ordered.Take(firstHalfCount), alpha),
            ordered.Count);
    }
}
```

Notes:

- Points are not assumed pre-sorted or pre-deduplicated by the caller — sorted
  defensively here — but Recent Observations' `HistoricalValues.PeriodValues`
  is already one value per year, so no dedup is needed on that call path.
- `alpha` defaults to 0.05, consistent with `LinearRegressionCalculator`.
- This type has zero knowledge of "year" or "decade" — `X`/`Y` are opaque
  doubles, same as `LinearRegressionCalculator`. Per-decade conversion is a
  display concern, done in Layer 3.

## Design: Recent-Observations-specific layer

### Extracting points from `HistoricalValues`

New private helper in `RecentObservationsCalculator`:

```csharp
private static List<DataPoint> BuildTrendPoints(HistoricalValues distribution)
{
    return distribution.PeriodValues
        .Where(x => x.Year.HasValue && x.Value.HasValue && double.IsFinite(x.Value.Value))
        .Select(x => new DataPoint(x.Year!.Value, x.Value!.Value))
        .ToList();
}
```

### `BuildTrendMetrics` / `BuildTrendMetric`

Mirrors `BuildVariationMetrics`/`BuildVariationMetric` (lines 928-990) almost
exactly: iterate `domain.VariationMetrics`/`DailyVariationMetrics` (reused,
not duplicated — see above), skip metrics the period has no current value
for, and for each remaining metric build a `RecentObservationTrendViewModel`:

```csharp
private static RecentObservationTrendViewModel BuildTrendMetric(Metric metric, HistoricalValues? distribution)
{
    var points = distribution is null ? [] : BuildTrendPoints(distribution);
    var trendSet = points.Count >= AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly
        ? TrendWindowCalculator.Calculate(points, AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly, RecentTrendWindowYears)
        : null;

    if (trendSet is null)
    {
        return new RecentObservationTrendViewModel
        {
            Label = metric.VariationLabel,
            Unit = metric.Unit,
            CompleteYearCount = points.Count,
            MinimumRequiredYears = AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly,
            UnavailableReason = $"Less than {AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly} complete years of data. "
                + $"A minimum of {AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly} years is used across the site "
                + "(for example the warming anomaly and heating score) so long-term trends aren't skewed by short records.",
        };
    }

    return new RecentObservationTrendViewModel
    {
        Label = metric.VariationLabel,
        Unit = metric.Unit,
        CompleteYearCount = trendSet.CompletePointCount,
        HeadlineText = FormatTrendPerDecade(trendSet.RecentTrend, metric.Unit),
        HeadlineCaption = "Latest 30-years",
        HistoricalTrendText = $"Historical trend: {FormatTrendPerDecade(trendSet.HistoricalTrend, metric.Unit)}",
        FirstHalfTrendText = $"First-half of records: {FormatTrendPerDecade(trendSet.FirstHalfTrend, metric.Unit)}",
    };
}
```

(`RecentTrendWindowYears = 30`, a new `private const int` alongside the
existing `LatestSevenDaysLength`.)

### Formatting: per-decade rounding and "no significant trend"

New private formatter, dispatched by unit (only two units exist: `"°C"` and
`"mm"`):

```csharp
private static string FormatTrendPerDecade(LinearRegressionResult trend, string unit)
{
    if (!trend.Significance.IsSlopeSignificant)
    {
        return "No significant trend";
    }

    var perDecade = trend.Line.Slope * 10;
    var sign = perDecade >= 0 ? "+" : string.Empty;
    return unit == "°C"
        ? $"{sign}{perDecade.ToString("0.00", CultureInfo.InvariantCulture)}°C per decade"
        : $"{sign}{perDecade.ToString("0", CultureInfo.InvariantCulture)}mm per decade";
}
```

This matches every wording example in the task: `+0.35°C per decade`,
`No significant trend`, `-0.25°C per decade`, `+30mm per decade`. The headline
case additionally needs the bare value without an inline "per decade" suffix
handled by the razor markup (see below), matching how Variation's
`StandardScoreValue`/`StandardScoreLabel` split a number from its caption —
`FormatTrendPerDecade` returns the number+unit text, "per decade"/"Latest
30-years" is a separate caption field so the markup can style them
differently, exactly like `StandardScoreValue`/`StandardScoreLabel` already do.

Revised headline field: `HeadlineText` holds `"+0.35°C"` (via a `bareValue: true`
overload/parameter that omits the trailing `" per decade"`), and a constant
literal `"per decade"` caption is rendered next to it, with `"Latest 30-years"`
rendered as a second line beneath — matching the task's example layout:

```text
Average max temp
+0.35°C  per decade
Latest 30-years
Historical trend: +0.20°C per decade
First-half of records: no significant trend
```

The exact split between "per decade" as an inline caption vs. folding it into
`HeadlineText` is a small implementation-time call — either reads correctly
against the existing `.recent-observation-detail-headline` markup; pick
whichever keeps `RecentObservationVariations.razor`'s two-span headline
pattern (`.recent-observation-detail-current` + `.recent-observation-detail-rank`)
unchanged.

### `MetricDomain` — no change needed

`domain.VariationMetrics`/`DailyVariationMetrics` are reused directly for
Trend, so `MetricDomain` gets no new fields. `AllMetrics` (line 1680) already
includes `VariationMetrics`/`DailyVariationMetrics` in its dedup pass, so no
change there either.

### `BuildExpandedTabs` — append Trend after Variation

```csharp
var trendMetrics = BuildTrendMetrics(period, domain, distributions);
if (trendMetrics.Count > 0)
{
    tabs.Add(new RecentObservationTrendTabViewModel
    {
        Key = MetricGroupKey.Trend,
        Title = "Trend",
        Metrics = trendMetrics,
    });
}
```

Placed immediately after the existing Variation-tab block (lines 914-923).
`BuildTrendMetrics` follows the same "skip metrics with no current period
value" rule as `BuildVariationMetrics` — a tile only gets a Trend tab if at
least one of its metrics has a current value, same gating Variation already
uses, so day tiles, precipitation-only tiles, etc. behave identically to how
Variation already decides whether to show.

## New/changed files

### `ClimateExplorer.Core`

- **New** `Stats/Model/TrendWindowSet.cs` — see above.
- **New** `Stats/TrendWindowCalculator.cs` — see above.

### `ClimateExplorer.Web.Client`

- **New** `UiModel/RecentObservations/RecentObservationTrendViewModel.cs`:

  ```csharp
  public sealed record RecentObservationTrendViewModel
  {
      public string Label { get; init; } = string.Empty;
      public string Unit { get; init; } = string.Empty;
      public int CompleteYearCount { get; init; }
      public int MinimumRequiredYears { get; init; }
      public string? HeadlineText { get; init; }
      public string? HeadlineCaption { get; init; }
      public string? HistoricalTrendText { get; init; }
      public string? FirstHalfTrendText { get; init; }
      public string? UnavailableReason { get; init; }
  }
  ```

- **New** `UiModel/RecentObservations/RecentObservationTrendTabViewModel.cs`:

  ```csharp
  public sealed record RecentObservationTrendTabViewModel : RecentObservationExpandedTabViewModel
  {
      public IReadOnlyList<RecentObservationTrendViewModel> Metrics { get; init; } = [];
  }
  ```

- **Changed** `UiModel/RecentObservations/MetricGroupKey.cs` — add `Trend`
  after `Variation`.

- **Changed** `UiModel/RecentObservations/RecentObservationTileViewModel.cs`
  (`StripComparisons`, lines 105-114) — add a case:

  ```csharp
  RecentObservationTrendTabViewModel trend => trend with { Metrics = StripTrendMetrics(trend.Metrics) },
  ```

  with a new `StripTrendMetrics` helper (mirrors `StripVariationMetrics`,
  lines 133-152) nulling `HeadlineText`/`HeadlineCaption`/`HistoricalTrendText`/
  `FirstHalfTrendText` and setting `UnavailableReason` to the same
  "Recent observations are below the completeness threshold." text Variation
  uses.

- **New** `Components/RecentObservations/Tile/RecentObservationTrend.razor` +
  `.razor.cs` — structurally identical to `RecentObservationVariations.razor`/
  `.razor.cs`: a `[Parameter] IReadOnlyList<RecentObservationTrendViewModel> Metrics`,
  rendered as a `.recent-observation-detail-metrics` `<dl>` with one
  `.recent-observation-detail-metric` per row, each either showing
  `UnavailableReason` or the headline + two record lines. No new CSS file.

- **Changed** `Components/RecentObservations/RecentObservationTile.razor`
  (lines 41-44) — add a branch:

  ```razor
  else if (SelectedTab is RecentObservationTrendTabViewModel trendTab)
  {
      <RecentObservationTrend Metrics="@trendTab.Metrics" />
  }
  ```

- **Changed** `Services/RecentObservations/RecentObservationsCalculator.cs`:
  - `BuildExpandedTabs` — append the Trend tab (see above).
  - New `BuildTrendMetrics`/`BuildTrendMetric`/`BuildTrendPoints`/`FormatTrendPerDecade`
    private methods (see above).
  - New `private const int RecentTrendWindowYears = 30;` alongside
    `LatestSevenDaysLength`.
  - No changes to period-building, completeness, or existing metric/domain
    definitions.

## Edge cases and how this design handles them

- **Less than 60 complete years of data.** `BuildTrendMetric` returns a row
  with `UnavailableReason` set and no headline/trend text; `RecentObservationTrend.razor`
  renders the reason instead of the headline, same branch Variation already
  uses for its `UnavailableReason`. Applied per metric (matching how Variation
  already handles partial data), so a tile where every metric shares the same
  underlying daily series reads as uniformly insufficient, matching the task's
  single "Less than 60 complete years..." example.
- **All invalid/incomplete comparable periods.** `BuildTrendPoints` filters to
  finite `Value`+`Year` pairs; an empty result falls into the same
  insufficient-data branch as "not enough years" (`points.Count` is `0 < 60`).
- **Missing precipitation series.** Unchanged — `CalculatePrecipitation`
  already short-circuits to an empty-message result before tiles are built
  when there are no precipitation records at all; per-tile, a precipitation
  metric with no historical points behaves like the "all invalid" case above.
- **Periods where completeness filtering removes too much data.** This is
  exactly the `< 60` gate — completeness filtering already happened upstream
  in `GetHistoricalDailyRangeDistributions`/`GetHistoricalDailyDateDistributions`;
  `BuildTrendMetric` only sees what survived it.
- **Negative trends.** `FormatTrendPerDecade` uses `perDecade >= 0 ? "+" : ""`
  — a negative slope already renders with its own `-` sign from
  `ToString("0.00"/"0")`, no extra handling needed (same pattern as the
  existing `FormatAnomaly`/`FormatStandardScore`).
- **Non-significant trends.** `Significance.IsSlopeSignificant` (α = 0.05,
  matching `LinearRegressionCalculator`'s default and `AnomalyCalculator`'s
  general precedent of not asserting significance it hasn't tested) gates the
  "No significant trend" text, applied independently per window (headline,
  historical, first-half can each independently say "no significant trend").
- **Day tiles.** `BuildTrendMetrics` is called with `domain.DailyVariationMetrics`
  when `period.Kind == PeriodKind.Daily` (mirroring `BuildVariationMetrics`'s
  existing selection at line 933), reusing the exact same
  same-calendar-date `HistoricalValues` the Day Variation tab already uses. No
  special-casing needed beyond what Variation already established.

## Formatting-rule cross-check against the task's examples

| Example | `FormatTrendPerDecade` output |
|---|---|
| `+0.35°C per decade` | slope×10 = 0.35, significant, `"+0.35°C per decade"` |
| `No significant trend` | `IsSlopeSignificant == false` |
| `-0.25°C per decade` | slope×10 = -0.25, `"-0.25°C per decade"` |
| `+30mm per decade` | slope×10 = 30, 0 dp, `"+30mm per decade"` |

## Tests

### New: `ClimateExplorer.UnitTests/TrendWindowCalculatorTests.cs`

Following `LinearRegressionCalculatorTests.cs`'s naming convention
(`MethodName_StateUnderTest_ExpectedBehavior`):

- `Calculate_FewerThanMinimumPoints_ReturnsNull`.
- `Calculate_ExactlyMinimumPoints_ReturnsNonNullResult`.
- `Calculate_UnsortedInput_SortsBeforeWindowing` — points supplied out of `X`
  order still produce the same result as pre-sorted input.
- `Calculate_RecentWindowLargerThanAvailablePoints_ClampsToAvailableCount`.
- `Calculate_SixtyPointPerfectLine_AllThreeWindowsReportTheSameSlope`.
- `Calculate_FirstHalfDiffersFromRecentWindow_ReportsDifferentSlopesPerWindow`
  — a dataset with a slope change partway through produces a different
  `FirstHalfTrend.Line.Slope` from `RecentTrend.Line.Slope`.
- `Calculate_MinimumCompletePointsBelowSix_Throws`.

### New assertions in `ClimateExplorer.UnitTests/RecentObservationsServiceTests.cs`

Following the file's existing Variation-tab test naming
(`ExpandedTilesExposePeriodDayRecordsAndVariationTabs`,
`GetTemperatureRecords_VariationTabs_ExposeExpectedTemperatureMetrics`, etc.):

- `ExpandedTilesExposeTrendTabAfterVariation` — non-day tile's
  `AvailableExpandedTabs` keys are `[PeriodRecords, DayRecords, Variation, Trend]`
  when enough history exists.
- `DailyTilesExposeTrendTabAfterVariation` — daily tile's tabs are
  `[Day, Variation, Trend]` with title `"Trend"`.
- `GetTemperatureRecords_TrendTab_WithSixtyYearsOfHistory_ExposesAverageMaxMinMeanRows`
  — labels in order `["Average max temp", "Average min temp", "Mean temperature"]`.
- `GetTemperatureRecords_TrendTab_FewerThanSixtyComparableYears_ShowsInsufficientDataReason`.
- `GetPrecipitationRecords_TrendTab_RoundsToWholeMillimetresPerDecade`.
- `GetTemperatureRecords_TrendTab_RoundsToTwoDecimalPlacesPerDecade`.
- `GetTemperatureRecords_TrendTab_NonSignificantSlope_ShowsNoSignificantTrendText`.
- `GetTemperatureRecords_TrendTab_NegativeSlope_ShowsMinusSign`.
- `GetTemperatureRecords_DailyTrendTab_UsesDailyMaximumMinimumMeanLabels`
  — day tiles produce `["Maximum", "Minimum", "Mean"]` rows from
  same-calendar-date history.
- `ApplyCompletenessThreshold_TrendTab_StripsNumbersAndSetsThresholdUnavailableReason`
  — locks in the `StripComparisons`/`StripTrendMetrics` fix described above.

### Existing tests that must keep passing unchanged

- `RecentObservationTileExpansionStateTests.cs` — no changes needed; selection
  is key-based and already generic.
- All existing Variation/records tab tests in `RecentObservationsServiceTests.cs` —
  tab order assertions for tiles *without* enough history for a Trend tab
  should be checked for any hard-coded `AvailableExpandedTabs.Count` equality
  that would now need a Trend tab appended in their fixtures; where a test's
  synthetic history already exceeds 60 comparable years (several already do,
  to test Variation/records with realistic sample sizes), its tab-order
  assertion will need updating to include `Trend` — this is expected
  fallout, not a regression, and should be fixed in the same change rather
  than avoided by keeping test fixtures under 60 years artificially.

## Acceptance criteria

- Recent Observation expanded tiles show a **Trend** tab after **Variation**.
- Trend appears for tile types where at least one applicable metric has both
  a current value and enough comparable history (day and non-day, temperature
  and precipitation).
- Trend values are computed from the tile's own `HistoricalValues.PeriodValues`
  — the same like-for-like comparable periods Variation and the records tabs
  already use — via the new `TrendWindowCalculator`, not a new period-building
  path.
- A tile with fewer than `AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly`
  (60) comparable years shows an explanatory insufficient-data message instead
  of numbers.
- Latest-30-years, historical (all-data), and first-half trends are all shown
  when data is sufficient, each independently reporting "no significant
  trend" when applicable.
- Temperature trends round to 2 decimal places per decade; precipitation
  trends round to 0 decimal places per decade.
- `RecentObservationTileViewModel.ApplyCompletenessThreshold` suppresses Trend
  content the same way it already suppresses Variation content.
- Existing Recent Observations tabs, tile visibility rules, and all existing
  unit tests still pass (with the expected tab-order fixture updates noted
  above).
- New unit tests cover `TrendWindowCalculator` directly and the
  Trend-tab-building logic in `RecentObservationsCalculator`.
- No period-building, completeness, or unrelated formatting code is changed.

## Out of scope (future work, enabled but not built here)

- The chart-series preset described in the task ("current temperatures and
  predicted future temperatures using the regression algorithm") is not
  implemented by this plan. `TrendWindowCalculator`/`LinearRegressionCalculator`
  are positioned so that work can reuse them directly: a historical trend via
  `TrendWindowCalculator` (or a direct `LinearRegressionCalculator.Calculate`
  call, if chart-series only needs one window rather than three) plus future
  points via `LinearRegressionCalculator.Predict`. Sizing that preset's own UI
  and series-generation code is left to a dedicated plan when that work
  starts.
