# Location temperature trends on the dashboard

- **Date:** 2026-10-18
- **Status:** Implemented 2026-10-18 (see addendum). Build and unit tests are clean; the UI is unverified and the cache hasn't been regenerated yet. (Revisions before implementation, 2026-10-18: a compact `TrendSummary` replaces storing `PolynomialRegressionResult`, because of download size; then `PValue` and `YearCount` were dropped from it; then the trend minimum was lowered from 60 to 30 years for charts and the dashboard, with the last-30 and early-period windows from 60 years inclusive; then `LastYear` moved from `TrendSummary` onto `Location.LastYear`)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Core/Model/Location.cs`, `ClimateExplorer.Core/Stats/PolynomialRegressionCalculator.cs` (constant-series significance fix), new `ClimateExplorer.Core/Model/TrendSummary.cs`,
  new `ClimateExplorer.Core/Stats/TrendDefaults.cs`,
  new `ClimateExplorer.Core/Calculators/TemperatureTrendCalculator.cs`, `ClimateExplorer.WebApi/LocationEndpoints.cs`,
  `ClimateExplorer.Web.Client/Services/Trends/TrendFormatting.cs`,
  `ClimateExplorer.Web.Client/Services/Trends/ChartSeriesTrendCalculator.cs` (minimum lowered to 30),
  `ClimateExplorer.Web.Client/Services/RecentObservations/Calculator/RecentObservationsCalculator.Trend.cs` (message wording only),
  new `ClimateExplorer.Web.Client/Services/Trends/LocationTrendFieldBuilder.cs`,
  `ClimateExplorer.Web.Client/Components/Location/LocationDashboard.razor*`, new and updated unit tests
- **Builds on:** [Polynomial regression for chart trends](2026-08-14-01-polynomial-regression-chart-trends.md),
  [Linear regression utility](2026-07-11-01-linear-regression.md),
  ["About trends" button and full-statistics modal](2026-07-20-01-about-trends-button-plan.md)
- **Branch context:** `development`

## Goal

Pre-calculate two linear temperature trends for every location (full record and last 30 years) while the
API builds its cached location list. Store a **compact summary** of each on `Location`, and show one as an
`OverviewField` on `LocationDashboard`. The next stage's modal **recalculates** the full statistics
client-side from the yearly series the dashboard already downloads for the climate stripe.

This plan also lowers the chart trend module's minimum from 60 to 30 complete years, so the chart and
the dashboard agree on when a trend exists. Recent Observations keeps its 60-year minimum.

**Out of scope:** precipitation, the modal itself (next stage), `Locations.razor`, the warming
anomaly / heating score minimum (stays 60).

## Investigation

### Download size rules out storing `PolynomialRegressionResult`

The first draft stored `PolynomialRegressionResult` on `Location`. Measured baseline: the cached
`Locations` file (`cache-longterm`) is **778 KB for 2,014 locations** (~390 B each). 1,750 of them have a
warming anomaly, i.e. 60+ complete years. A degree-1 `PolynomialRegressionResult` serialises to ~1 KB, so
two per location would have added **~3.5–4 MB, about 5× the current payload**. `Index.razor.cs:472` and
`Locations.razor.cs:107` both fetch that list in full. Rejected.

### No existing class fits the compact case

The dashboard field and its tooltip need, per trend: **year range, rate, significance**. (The first
revision also carried the number of years and the p-value. Both were dropped to save download size; the
modal shows them from its own recalculation.)

| Candidate | Has | Missing |
| --- | --- | --- |
| `RateOfChangeEstimate` (Core/Stats/Model) | X, rate, SE, CI | significance, year range, count |
| `RegressionSignificance` (Core/Stats/Model) | p-value, `IsSlopeSignificant`, slope SE/CI, t, F, DF | the rate itself (only via `t × SE`), year range, count; and it's ~250 B |
| `RegressionInputSummary` (Core/Stats/Model) | count, min/max X | rate, significance |
| `CalculatedAnomaly` (Core/Calculators) | year ranges, counts | not a trend at all |
| `RecentObservationTrendViewModel`, `ChartSeriesTrendWindowResult` | trend display data | in `Web.Client`, so not visible from `Core.Model`; the latter also wraps a full `PolynomialRegressionResult` |

Each Core candidate covers only half the need. Adapting one would mean either sending fields we don't use
(`RegressionSignificance`) or bolting unrelated fields onto a stats type. **Decision: one small new record**,
`TrendSummary`, built from a `PolynomialRegressionResult` at calculation time.

### The stripe data is the same series the API uses

The modal can recalculate from the stripe's data because both sides build the same yearly series:

| | API: `LocationEndpoints.GetCachedLocations` | Client: `ClimateDataHelper.GetData` (stripe) |
| --- | --- | --- |
| Substitutes | `StandardTemperatureDataMatches()` | same (`LocationDashboard.OnAfterRenderAsync`) |
| Binning | ByYear, Mean ×3 | ByYear, Mean ×3 |
| Bin / bucket proportion | 1.0 / 1.0 | 1.0 / 1.0 |
| Cup size / proportion | `DefaultCupSize` 14 / `DefaultCupDataProportion` 0.7 | 14 / 0.7 |
| `SeriesSpecification.DataResolution` | **not set**, fixed by this plan (§3) | `MeasurementDefinition.DataResolution` |

The one difference, `DataResolution`, is an inconsistency in the API code, not an intended difference.
It makes no difference to the result today:
- It is only read by `SeriesProvider` (`SeriesProvider.cs:195-197`) to choose between measurement
  definitions that share a DataType/DataAdjustment at more than one resolution. That only happens for CO2
  (daily + monthly), never for location temperature datasets.
- Even in that case, null falls back to Monthly. `DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement`
  (line 87) applies the same Monthly default when it picks the definition the client then passes.

The API should still set it, so the two requests match field for field and keep matching if a location
dataset ever gains a second resolution.

`LocationDashboard` keeps the result as `temperatureAnomaly.DataSet`, whose `DataRecords` are
`BinnedRecord`s (`BinId` + nullable `Value`). That is the same shape `AnomalyCalculator` already accepts
on the client side. So the modal needs no extra download.

### Calculating

`GetCachedLocations` ([LocationEndpoints.cs:62-118](../../ClimateExplorer.WebApi/LocationEndpoints.cs#L62-L118))
already builds `series.DataPoints` for `AnomalyCalculator.CalculateAnomaly`. The trends use the same
series. Incomplete years come through with a null `Value` and are filtered out, as `AnomalyCalculator`
does. "Last 30 years" means the last 30 **complete** yearly values (`TakeLast(30)`), degree 1, alpha 0.05,
the same as `ChartSeriesTrendCalculator` and `TrendWindowCalculator`.

### Minimum years: where the 60-year rule applies today

Every trend minimum in the codebase traces back to one constant,
`AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly = 60`:

| Where | Used for | After this plan |
| --- | --- | --- |
| `AnomalyCalculator.CalculateAnomaly` | warming / precipitation anomaly, and so heating score | **60** (unchanged; `WarmingAnomalyInfo`/`PrecipitationAnomalyInfo` say "a minimum of 60 years") |
| `ChartTooltipMetadataBuilder` | chart hover tooltip's vs-Last30/vs-Avg/vs-Early anomaly figures | **60** (unchanged; it's anomaly figures, not a trend) |
| `Data.Ghcnm/Program.cs:63` | which GHCNm stations are imported at all | **60** (unchanged; data pipeline) |
| `RecentObservationsCalculator.Trend.cs:61-74` | Recent Observations trend tab | **60** (unchanged); the message wording is fixed, see below |
| `ChartSeriesTrendCalculator.MinimumYearsForTrend` | chart trend module (all four windows) | **30** |
| new `TemperatureTrendCalculator` | dashboard `OverviewField` | **30** |

**Why 30 is enough for charts and the dashboard.** 30 years is the WMO climatological standard-normal
period, the conventional length for describing climate rather than weather. Both surfaces also gate every
window on the F-test, so a short, noisy record simply comes out "not significant" rather than producing a
misleading headline. Recent Observations is different. It compares one current period against
history, and its trend tab sits beside per-year rankings that need a deep record, so 60 stays there.

**What lowering the chart minimum does to each chart window** at `n` complete years (30 ≤ `n` < 60):

| Window | Points at `n = 30` | Concern | Proposal |
| --- | --- | --- | --- |
| Full | 30 | none | keep |
| Last 30 (`TakeLast(min(30, n))`) | 30, **identical to Full** | at `n = 30` the dropdown would offer two identical options, and up to `n = 59` the two mostly overlap | **offer only when `n ≥ 60`**, the same rule as the dashboard |
| Recent decade | 10 | none new; already a 10-point fit at any `n` | keep |
| Early period (`n / 2`) | 15–29 | low statistical power on a 15–29 point fit | **offer only when `n ≥ 60`** |

**Why 60 for both sub-period windows.** This matches the warming anomaly rule. The warming anomaly compares
exactly these two sub-periods (the first-half average against the last-30 average), and it needs 60 years
for that comparison. At `n = 60` the early period is 30 points and the last-30 window is the other 30. So
every offered sub-period window has at least the 30-year minimum in its own right, except Recent decade,
which is deliberately 10 years.

Leaving windows out is safe. Everything downstream works from `ChartSeriesTrend.Windows` rather than
assuming four windows:
- the dropdown uses `SignificantWindows`
- `ChartTrendPanel` builds a tab per window
- `ChartSeriesTrendNotificationBuilder` iterates `Windows`
- `ResolveWindow` falls back by priority when a saved `TrendPeriod` (`Last30` or `FirstHalf`) isn't
  available

The `UnavailableReason` text has to change. It currently says the minimum "is used across the site (for
example the warming anomaly and the heating score)", which stops being true. The Recent Observations
message ("A minimum of 60 years is used across the site…") has the same problem, so its second sentence is
reworded. Its "Less than 60 complete years of data." prefix stays, because
`RecentObservationsServiceTests:2345` asserts on it.

### Display

`OverviewField` wraps itself in a `Multiline` `DelayedTooltip`, so `<br>` works in `Tooltip` (as
`RecordHighToolTip` already does). Without `OnClick`/`PopupContent` it renders as plain text, which is right
until the modal arrives.

## Design

### 1. Model — `TrendSummary` + `Location.TemperatureTrends`

New `ClimateExplorer.Core/Model/TrendSummary.cs`:

```csharp
/// A compact, display-ready summary of one linear trend, small enough to ship on every Location.
/// The full statistics are recalculated on demand from the underlying series - see TemperatureTrendCalculator.
public sealed record TrendSummary(
    int FirstYear,
    double Slope,       // units per year, rounded to 5 dp
    bool IsSignificant); // Significance.IsSlopeSignificant: overall-model F-test, p < 0.05
```

The name is generic rather than temperature-specific so precipitation can reuse it later. The p-value and
the number of complete years are deliberately left out. The tooltip doesn't need them, and the modal
gets both from its own recalculation (§6). The last year is the same for every window (all windows end
at the series' last complete year), so it lives once on `Location` as `LastYear`.

`Location`:

```csharp
public double? WarmingAnomaly { get; set; }
public short? HeatingScore { get; set; }

/// Linear trends of the location's yearly temperature series. Null below 30 complete years; the
/// full-record trend only for 30-59 years; full record then last 30 years from 60.
public List<TrendSummary>? TemperatureTrends { get; set; }

/// The last complete year of the series TemperatureTrends were fitted to - the end year of every
/// window. Null whenever TemperatureTrends is null.
public int? LastYear { get; set; }
public DataRecord? RecordHigh { get; set; }

[JsonIgnore]
public TrendSummary? FullPeriodTemperatureTrend => TemperatureTrends?.MinBy(x => x.FirstYear);

[JsonIgnore]
public TrendSummary? Last30YearTemperatureTrend =>
    TemperatureTrends is { Count: > 1 } ? TemperatureTrends.MaxBy(x => x.FirstYear) : null;
```

The accessors pick a window by `FirstYear`, not list position, so no window marker is needed. The full
record contains every point, so it always starts earliest. When both trends exist there are at least 60
complete years, so the last-30 window always starts strictly later (at `n = 60` it is points 31–60). The accessors follow the existing
computed-property style on `Location` (`FullTitle`, `DisplayTitle`).

**Size:** one `TrendSummary` is ~55 B of JSON, plus ~16 B for `"LastYear":2024` once per location. That
works out to roughly **+270 KB (~+35%)** on the 778 KB baseline, against +3.5–4 MB for the first draft.
Moving `LastYear` onto `Location` saves ~15 B per trend in exchange for one short property, so it is a
small net saving as well as removing the duplication. JSON property names repeat on every location and
compress well, so the gzip-compressed increase should be smaller. Measure after the first regeneration.

### 2. Core — `TrendDefaults` + `TemperatureTrendCalculator` (shared by API, chart and modal)

New `ClimateExplorer.Core/Stats/TrendDefaults.cs`. This is one neutral home for the thresholds, so the
chart (any series: temperature, CO2, glacier…) and the dashboard can't drift apart. It sits in Core so
both `TemperatureTrendCalculator` and the Web.Client `ChartSeriesTrendCalculator` can reference it:

```csharp
public static class TrendDefaults
{
    /// The WMO standard-normal length: below this, no trend is fitted at all.
    public const int MinimumYearsForTrend = 30;

    public const int RecentWindowYears = 30;

    /// The last-30-years and early-period windows are only offered from this many years - the same
    /// rule as the warming anomaly, which compares exactly those two sub-periods. At 60 years each
    /// sub-period has 30 points of its own, and the last 30 is genuinely different from the full period.
    public const int MinimumYearsForSubPeriodWindows = AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly; // 60
}
```

New `ClimateExplorer.Core/Calculators/TemperatureTrendCalculator.cs`, next to `AnomalyCalculator`. One
place defines the windows, so the dashboard summary and the modal's recalculation can't drift apart:

```csharp
public static class TemperatureTrendCalculator
{
    // Thresholds come from TrendDefaults (above).

    // Full-fidelity fits. Used by Summarise below, and by the modal next stage.
    public static TemperatureTrendFits? Fit(IReadOnlyList<DataPoint> points);

    // What goes on Location: sets TemperatureTrends and LastYear together (both null
    // when there's no trend), in the same static-mutator style as Location.SetHeatingScores.
    public static void SetTemperatureTrends(Location location, IEnumerable<ChartableDataPoint> dataPoints); // API's series.DataPoints
    public static List<TrendSummary>? Summarise(TemperatureTrendFits? fits);

    // Year/value extraction, mirroring AnomalyCalculator's overloads:
    // ((YearBinIdentifier)BinIdentifier.Parse(BinId)).Year, keep Value.HasValue && double.IsFinite.
    public static List<DataPoint> ToDataPoints(IEnumerable<ChartableDataPoint> dataPoints);
    public static List<DataPoint> ToDataPoints(IEnumerable<BinnedRecord> dataRecords);    // client's DataSet.DataRecords
}

public sealed record TemperatureTrendFits(
    PolynomialRegressionResult FullPeriod,
    PolynomialRegressionResult? Last30Years,
    IReadOnlyList<DataPoint> OrderedPoints);   // the modal slices window points for TrendStatSectionBuilder
```

`Fit` rules, with `n` = number of complete yearly values:

| `n` | Result |
| --- | --- |
| `n < 30` | `null` |
| `30 ≤ n < 60` | `FullPeriod` only |
| `n ≥ 60` | `FullPeriod` and `Last30Years` (`ordered.TakeLast(30)`) |

All fits use `PolynomialRegressionCalculator.Calculate(points, degree: 1)`.

`Summarise` maps each fit to a `TrendSummary`. The mapping sets `FirstYear` from `Input.MinimumX`, `Slope`
from `Curve.Slope` (rounded), and `IsSignificant` from `Significance.IsSlopeSignificant`.
`SetTemperatureTrends` sets `LastYear` from the full period's `Input.MaximumX` when the
summary list isn't null.

**Non-finite guard (still needed).** `FileBackedCache.Put` uses default `JsonSerializer` options, which
throw on NaN/Infinity. That `Put` runs after the parallel loop, outside the per-location `try`, so one bad
value would stop the whole locations cache from being written. `Slope` is now the only double in the
summary, so the infinite F-statistic of a perfect fit never reaches it. So the check is: if `Slope` isn't
finite, return `null` for the full window, or only `[full]` when it's the last-30 window. This keeps the
§1 accessor semantics intact.

A constant series is **not significant**. The F-test's null hypothesis is that the intercept-only (flat)
model fits as well as the regression. For a constant series the flat model already fits perfectly, so
there's no evidence against it, and F is strictly 0/0, i.e. undefined. Implementing this exposed a bug in
`PolynomialRegressionCalculator`: it gave any zero-residual fit F = ∞, p = 0, and so called a constant
series "significant" with slope 0. That bug was fixed (see the addendum).

### 3. API — `LocationEndpoints`

Straight after the `WarmingAnomaly` line:

```csharp
location.WarmingAnomaly = AnomalyCalculator.CalculateAnomaly(series.DataPoints)?.AnomalyValue;
TemperatureTrendCalculator.SetTemperatureTrends(location, series.DataPoints);
```

Wrap the trend line in its own small `try/catch` that logs the same way the outer catch does. Otherwise
an unexpected regression exception lands in the outer catch and the location silently loses `RecordHigh`
too, because that loop comes after.

Also set `DataResolution = dsdmd.MeasurementDefinition.DataResolution` on the existing
`SeriesSpecification` in the same method (alongside `DataAdjustment`, `DataSetDefinitionId`, `DataType`,
`LocationId`). Then the API's request matches `ClimateDataHelper.GetData`'s field for field (see the
Investigation table). This doesn't change any current output. It also affects the warming anomaly, which
is built from the same series, and that is equally unchanged today.

### 4. Chart — lower `ChartSeriesTrendCalculator`'s minimum to 30

- `MinimumYearsForTrend = TrendDefaults.MinimumYearsForTrend` (was
  `AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly`).
- `RecentWindowYears = TrendDefaults.RecentWindowYears`, so the value is shared and stays 30.
- The `Last30` and `FirstHalf` (early period) windows are added to `windows` only when
  `ordered.Count >= TrendDefaults.MinimumYearsForSubPeriodWindows`. Full and Recent decade are always
  fitted from 30 years.
- New `UnavailableReason` wording: `Only {n} complete years of data are plotted, and a trend needs at least
  30 - the standard 30-year climate period - so a trend reflects climate rather than year-to-year
  variability.`
- Update the class remarks and the `MinimumYearsForTrend` summary. Both currently say the minimum is
  "deliberately the same shape as the Recent Observations trend tab's" and the same 60-year floor as the
  warming anomaly.

`RecentObservationsCalculator.Trend.cs`: keep the `AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly`
gate and the "Less than 60 complete years of data." prefix. Reword only the second sentence to `Recent
Observations trends use the same 60-year minimum as the warming anomaly and heating score, so they're
comparable with them.`

### 5. Dashboard — one `OverviewField`

**`TrendFormatting`**: add `FormatPerDecadeValue(double slopePerYear, string unit)`. The existing
`FormatPerDecadeValue(PolynomialRegressionResult, double atX, string unit)` delegates to it with
`trend.Curve.Derivative(atX)`, so there stays one formatting path. It gives `+0.32°C /decade`.

Selection logic goes in a small, testable builder:

```csharp
// ClimateExplorer.Web.Client/Services/Trends/LocationTrendFieldBuilder.cs
public sealed record LocationTrendField(string Label, string Value, string Tooltip);

public static class LocationTrendFieldBuilder
{
    public static LocationTrendField? Build(Location location);
}
```

| Last 30 significant | Full significant | Field | Label | Value |
| --- | --- | --- | --- | --- |
| yes | any | shown | `Last 30 year trend` | last-30 rate per decade |
| no / absent | yes | shown | `Trend` | full rate per decade |
| no / absent | no | **hidden** | — | — |
| `TemperatureTrends == null` | | **hidden** | — | — |

Tooltip (multiline). It shows the period and significance of the displayed trend, plus the other trend
**only if it is significant**:

- Last 30 shown: `Statistically significant linear trend over the last 30 years of records (1995–2024): +0.32°C/decade.`
  then, if the full trend is significant, `<br>Full record (1910–2024): +0.14°C/decade.`
- Full shown: `Statistically significant linear trend over the full record (1910–2024): +0.14°C/decade.`

Each range is `{trend.FirstYear}–{location.LastYear}`. The builder returns `null` if
`LastYear` is missing, which only happens with hand-made data, since the two are always
set together.

The wording says "temperature", not "mean temperature", because `StandardTemperatureDataMatches()` falls
back to TempMax. "Last 30 years of records" is kept because, with gaps, the year range is longer than 30
calendar years (risk 3).

`LocationDashboard.razor.cs`: add `private LocationTrendField? TemperatureTrendField { get; set; }` and
set it in `OnParametersSetAsync` next to `WarmingAnomalyAsString`. The data is on `Location`, so there is
no fetch and no loading state.

`LocationDashboard.razor`: a **separate** `@if` after the warming-anomaly/heating-score block and before
Record high. It isn't nested because a location with 30–59 years has a trend but no warming anomaly.

```razor
@if (TemperatureTrendField is not null)
{
    <OverviewField Label="@TemperatureTrendField.Label" Tooltip="@TemperatureTrendField.Tooltip"><Value>@TemperatureTrendField.Value</Value></OverviewField>
}
```

No new CSS. `OverviewField`'s label (0.8rem) and value (1.25rem) are within the 0.75rem floor.

### 6. Next stage (modal) — how it will plug in

Not built now. These notes make sure this stage doesn't block the modal:

- `OnAfterRenderAsync` already holds `temperatureAnomaly.DataSet`. The modal component gets that `DataSet`
  (plus `Location`) through `PopupContent`, the way `WarmingAnomalyInfo` gets `CalculatedAnomaly` today.
- It calls `TemperatureTrendCalculator.Fit(TemperatureTrendCalculator.ToDataPoints(dataSet.DataRecords))`.
  That gives full `PolynomialRegressionResult`s plus the ordered points that `TrendStatSectionBuilder.Build`
  needs (window points and full-period points) for "About trends".
- Because `Fit` is the same code that produced the `TrendSummary`s, the modal's numbers match the field's,
  as long as the data is the same (see risk 2).

### 7. Populating

Deploy the API change, then run `ClimateExplorer.CachingTool`. It deletes the `cache-longterm` content
files and calls `GetLocations(permitCreateCache: true)`, which rebuilds the cache with `TemperatureTrends`.
Until then, the old cached file deserialises cleanly with `TemperatureTrends = null`, so the field is just
hidden.

## Implementation order

1. **Model:** `TrendSummary`, `Location.TemperatureTrends` and the accessors. Add the serialisation
   round-trip test.
2. **API:** `TrendDefaults`, `TemperatureTrendCalculator` and its tests, then the call (plus `try/catch`)
   in `LocationEndpoints`.
3. **Chart:** lower `ChartSeriesTrendCalculator`'s minimum, gate `Last30`, reword both
   unavailable-reason messages, and update the affected tests. This is independent of steps 1–2 and 4, so
   it can be its own commit.
4. **Dashboard:** the `TrendFormatting` overload, `LocationTrendFieldBuilder` and its tests, then
   `LocationDashboard.razor` / `.razor.cs`.
5. You run the CachingTool to populate.

## Tests

`ClimateExplorer.UnitTests`, names in `MethodName_StateUnderTest_ExpectedBehavior`:

- **`TemperatureTrendCalculatorTests`**
  - `Fit_29CompleteYears_ReturnsNull`
  - `Fit_30CompleteYears_ReturnsFullPeriodOnly`
  - `Fit_59CompleteYears_ReturnsFullPeriodOnly`
  - `Fit_60CompleteYears_ReturnsFullAndLast30`: last-30 `Input.Count == 30`, `MinimumX == lastYear - 29`
  - `Fit_UnorderedPoints_Last30IsMostRecent`
  - `ToDataPoints_NullValues_SkipsIncompleteYears`, covering both overloads
  - `Summarise_60CompleteYears_MapsFirstYearSlopeAndSignificance`: matches the `Fit` results
  - `SetTemperatureTrends_60CompleteYears_SetsTrendsAndLastYear`
  - `SetTemperatureTrends_29CompleteYears_LeavesTrendsAndLastYearNull`
  - `Summarise_ConstantSeries_IsNotSignificantAndSerialises`: slope 0, not significant, and nothing non-finite reaches the summary
- **`LocationTemperatureTrendsSerializationTests`**
  - `Serialize_TemperatureTrends_RoundTripsWithDefaultOptions` (the cache)
  - `Serialize_TemperatureTrends_RoundTripsWithWebOptions` (`DataService`)
  - `FullPeriodTemperatureTrend_TwoTrends_ReturnsEarliestFirstYear`
  - `Last30YearTemperatureTrend_OneTrend_ReturnsNull`
- **`LocationTrendFieldBuilderTests`**
  - `Build_Last30Significant_LabelsLast30YearTrend`
  - `Build_OnlyFullSignificant_LabelsTrend`
  - `Build_NeitherSignificant_ReturnsNull`
  - `Build_NullTrends_ReturnsNull`
  - `Build_Last30AndFullSignificant_TooltipIncludesFullPeriod`
  - `Build_FullNotSignificant_TooltipOmitsFullPeriod`
- **`ChartSeriesTrendCalculatorTests`** (existing). Most tests use the `MinimumYearsForTrend` constant or
  101-point series and are unaffected. These change:
  - `Calculate_ExactlyTheMinimumYears_FitsEveryWindow` currently expects 4 windows at the minimum. Rename
    it to `Calculate_ExactlyTheMinimumYears_FitsFullAndRecentDecadeOnly` and expect 2, with `Last30` and
    `FirstHalf` absent.
  - `Calculate_WindowsAreInDropdownDisplayOrder` uses `MinimumYearsForTrend` points and expects all 4 in
    order. Switch it to `TrendDefaults.MinimumYearsForSubPeriodWindows` points.
  - New: `Calculate_59Years_OmitsLast30AndEarlyPeriodWindows` and `Calculate_60Years_FitsEveryWindow`.
    At 60, the early period has 30 points.
  - New: `Calculate_SavedLast30PeriodBelow60Years_FallsBackToFull` and
    `Calculate_SavedEarlyPeriodBelow60Years_FallsBackToFull`
- **`ChartDataBuilderTests`** (existing).
  `BuildAsync_SwitchingToInsufficientDataAndBack_PreservesEachTrendsRequestedWindow` uses a **30-year**
  series as its "insufficient" location. That becomes sufficient, so shorten it to 20 years.
- **`ChartSeriesTrendNotificationBuilderTests`** and **`RecentObservationsServiceTests`**: no change
  expected. They use a 10-point series and the unchanged "Less than 60…" prefix.

Verify with `dotnet build` and the unit test suite. UI behaviour stays unverified until you run the site.

## Change size

- **Changed production files (7):**
  - `Location.cs`: ~10 lines
  - `LocationEndpoints.cs`: ~11 lines, including the `DataResolution` alignment
  - `TrendFormatting.cs`: ~8 lines
  - `ChartSeriesTrendCalculator.cs`: ~20 lines, including remarks
  - `RecentObservationsCalculator.Trend.cs`: ~2 lines
  - `LocationDashboard.razor`: ~4 lines
  - `LocationDashboard.razor.cs`: ~5 lines
- **New production files (4):**
  - `TrendDefaults.cs`: ~15 lines
  - `TrendSummary.cs`: ~10 lines
  - `TemperatureTrendCalculator.cs`: ~110 lines, including `TemperatureTrendFits`
  - `LocationTrendFieldBuilder.cs`: ~60 lines
- **Changed test files (2):**
  - `ChartSeriesTrendCalculatorTests.cs`: ~60 lines
  - `ChartDataBuilderTests.cs`: ~2 lines
- **New test files (3):** ~270 lines in total

## Risks and open questions

1. **Remaining payload increase (~+35% uncompressed).** This is already the minimal summary. If the
   measured figure is still too high, the remaining levers are:
   - Shorten JSON property names, though that cuts against the codebase's naming.
2. **Cache vs live data.** `TemperatureTrends` is a snapshot from the last CachingTool run, but the stripe
   (and so the modal) uses live data. If a new year has arrived since, the modal's recalculated numbers can
   differ slightly from the field, and in a borderline case so can the significance. This goes away on the
   next regeneration. The modal can say its figures are "based on data to {last year}".
3. **"Last 30 years" means 30 complete years, not 30 calendar years.** If there are gaps, the window spans
   more than 30 calendar years. This matches the chart and tile. The tooltip shows the actual year range.
4. **Chart behaviour changes for 30–59 year series.** Series that today show "no trend could be fitted"
   will start offering Full and Recent decade trends, where significant. Last 30 years and Early period
   appear from 60 years, as today.
5. **Saved chart URLs.** A shared chart saved with `TrendPeriod = Last30` or `FirstHalf` on a 30–59 year series falls back
   to Full via `ResolveWindow`, the same as when a saved window stops being significant. Nothing breaks,
   but the user sees a different window than the link's author did. Before this change those series showed
   no trend at all, so this is strictly an improvement.
6. **Recent Observations vs chart for the same location.** A 45-year location will show a trend on the
   chart and dashboard, but "Less than 60 complete years" on the Recent Observations trend tab. This is
   intentional, and the reworded message says why.

## Addendum — implementation notes

Implemented 2026-10-18 on `development`. `dotnet build` is clean (warnings are errors) and the unit
suite is green: **747 passed, 0 failed** (after the constant-series fix). Not verified: the dashboard UI (not run, per AGENTS.md) and the
regenerated locations cache (needs a CachingTool run).

**Shipped as planned:** `TrendSummary(FirstYear, Slope, IsSignificant)`;
`Location.TemperatureTrends` / `LastYear` and the two `[JsonIgnore]` accessors;
`TrendDefaults`; `TemperatureTrendCalculator` (`Fit`, `Summarise`, `SetTemperatureTrends`,
`ToDataPoints` ×2); the `LocationEndpoints` call with its own `try/catch`, plus the `DataResolution`
alignment; the chart minimum lowered to 30 with `Last30`/`FirstHalf` gated at 60; both reworded
unavailable-reason messages; the `TrendFormatting.FormatPerDecadeValue(double, string)` overload;
`LocationTrendFieldBuilder` and the `OverviewField`.

**Deviations:**
- **Two extra files, because StyleCop SA1402 allows one type per file.** `TemperatureTrendFits` lives in
  `Core/Calculators/TemperatureTrendFits.cs`, and `LocationTrendField` in
  `Web.Client/UiModel/Trends/LocationTrendField.cs`, next to the other trend UI models.
- **Tooltip rate text** uses `TrendFormatting`'s own output (`+0.32°C /decade`, with a space) rather than
  the `+0.32°C/decade` shown in §5's examples, so it matches every other trend surface.
- **Constant-series bug fixed in `PolynomialRegressionCalculator`.** Any zero-residual fit got F = ∞ and
  p = 0, so a constant series was reported as "significant" with slope 0. That is statistically wrong.
  The overall F-test asks whether the regression fits better than the intercept-only model. With no
  variance to explain, the flat model already fits perfectly, and F is 0/0. References:
  - [Statology, F-test of overall significance](https://www.statology.org/a-simple-guide-to-understanding-the-f-test-of-overall-significance-in-regression/)
  - [R-help, 2014](https://stat.ethz.ch/pipermail/r-help/2014-March/372154.html): "the tstat and the Fstat
    are both undefined (0/0); as are the p-values"
  - [R-help, 2010](https://stat.ethz.ch/pipermail/r-help/2010-January/223919.html): floating-point noise in
    R producing a bogus "significant" result for a constant response

  `CalculateSignificance` now reports F = 0, p = 1, not significant, whenever the regression sum of
  squares is 0. Those are finite stand-ins for "undefined, no evidence of a trend". A fit that explains
  some variance with zero residuals (a perfect line or curve) keeps F = ∞, p = 0, so the existing
  perfect-line tests are unchanged. New tests: `Calculate_ConstantSeries_IsNotSignificant` and
  `Calculate_ConstantSeriesAtHigherDegree_IsNotSignificant` (degrees 2 and 3). This affects every
  consumer (chart, Recent Observations, dashboard), but only for series with zero variance, which real
  records don't have.
- **Extra tests beyond the plan:**
  - `SetTemperatureTrends_IncompleteYearAtTheEnd_LastYearIsLastCompleteYear`
  - `Summarise_NullFits_ReturnsNull`
  - `Serialize_TemperatureTrends_OmitsComputedAccessors`
  - `FullPeriodTemperatureTrend_NoTrends_ReturnsNull`
  - `Build_OnlyFullTrendCalculatedAndSignificant_LabelsTrend`
  - `Build_MissingLastYear_ReturnsNull`
  - `Build_NegativeTrend_FormatsWithoutPlusSign`

**Build note.** The WebApi was running under IIS Express during implementation, which locks its `bin`
DLLs. Builds and tests were run into a separate `OutDir` under `ClimateExplorer.UnitTests/bin/`, which
was then deleted. The data-file tests resolve paths relative to the output depth, so the `OutDir` must be
absolute and at the normal `bin/<x>/net10.0` depth.

**Follow-ups:**
1. Run the CachingTool to populate `TemperatureTrends`, then measure the cached `Locations` file against
   the 778 KB baseline (estimate: ~+270 KB).
2. Check the dashboard field and tooltip in the browser, and check a 30–59 year location's chart trends.
3. Next stage: the trend modal (§6).
