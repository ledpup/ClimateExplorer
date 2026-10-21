# US customary units toggle

- **Date:** 2026-10-21
- **Status:** Proposed
- **Author:** Claude
- **Scope:** `ClimateExplorer.Core/Enums.cs` (+ new `UnitConversion`), `ClimateExplorer.Core/Calculators/AnomalyCalculator.cs`,
  `ClimateExplorer.Web.Client` (`Routes`, `Layout/NavMenu`, `Services/Chart`, `UiLogic`, `UiModel`,
  `Services/Trends`, `Services/RecentObservations`, `Components/Location`, `Components/ClimateRecords`,
  `Pages/Locations`, `Services/Exporter`), `ClimateExplorer.Web/Program.cs`, `ClimateExplorer.UnitTests`
- **Branch context:** `development`

## Goal

Let a reader switch the site between metric and **US customary** units with a toggle in the nav
bar. In US customary mode, temperatures show in °F, rainfall and sea level in inches, and areas in
square miles. Every visit starts in metric.

## Decisions already made

- **US customary, not imperial.** For the units the site shows today (°F, inches, square miles) the
  two systems are identical; the name matters only if volume or mass units are ever added (US
  gallon 3.785 L vs imperial 4.546 L; short ton vs long ton).
- **Nothing is persisted.** No cookie and no localStorage. The setting lives in memory for the
  page load: it survives in-app navigation and resets to metric on reload, in a new tab, or when
  someone opens a shared link.
- **Toggle in `NavMenu`.**
- **No UnitsNet.** The site needs five unit mappings and two kinds of formula (affine for absolute
  temperatures, scale-only for everything else). UnitsNet would add a large assembly to the
  WebAssembly download and still couldn't tell which values are differences, which is the part
  that's easy to get wrong (see [Absolute values vs differences](#absolute-values-vs-differences)).
- **Metric everywhere except the display edge.** The WebApi, data files, `DataSetDefinition`s,
  chart URL state and caches stay metric. Conversion happens in the client, as close to display
  as each feature allows.
- **Areas convert** (km² → mi²) as well as temperature and length.
- **CSV export follows the toggle.** Its headers already state the unit.
- **Descriptions don't change.** Preset and dataset descriptions keep their metric wording,
  including thresholds such as "35°C or above".

## Which units convert

| `UnitOfMeasure` (source) | US customary display unit | Formula | Used by |
|---|---|---|---|
| `DegreesCelsius` | `DegreesFahrenheit` (°F) | absolute: `c × 1.8 + 32`; difference: `c × 1.8` | station TMax/TMin/TMean |
| `DegreesCelsiusAnomaly` | `DegreesFahrenheitAnomaly` (°F anomaly) | always a difference: `c × 1.8` | NOAAGlobalTemp, SST anomalies |
| `Millimetres` | `Inches` (in) | `mm ÷ 25.4` | precipitation, mean sea level |
| `SqKm` | `SqMiles` (mi²) | `km² ÷ 2.589988110336` | Greenland melt area |
| `MillionSqKm` | `MillionSqMiles` (million mi²) | `km² ÷ 2.589988110336` | sea ice extent, ozone hole area |

All factors are exact by definition (1 in = 25.4 mm; 1 mi² = 2.589988110336 km²).

**Unchanged in both modes:** ppm, ppb, W/m², MJ/m², DU, pH, ENSO index, Sn, ODGI, atmospheric
transmission, Mt, m w.e. These are either dimensionless or scientific units that US agencies also
publish in SI (EPA reports CO₂ in metric tonnes, for example). Converting them would make the site
disagree with its own sources.

## Design

### 1. Display-only units in `UnitOfMeasure`

Add `DegreesFahrenheit`, `DegreesFahrenheitAnomaly`, `Inches`, `SqMiles` and `MillionSqMiles` to
`Enums.UnitOfMeasure` and give each a case in `UnitOfMeasureLabelShort`, `UnitOfMeasureLabel` and
`UnitOfMeasureRounding`. No `DataSetDefinition` ever uses them.

This keeps most of the change away from downstream code. Once a value has been converted, it
carries its display unit. Code that already formats via `UnitOfMeasureLabelShort(dataSet.MeasurementDefinition.UnitOfMeasure)`,
such as the chart tooltips, `ChartView`'s series titles, `ClimateStripe` and `Top100`, produces "°F"
without being touched.

Rounding for the new units:

| Unit | Decimal places | Why |
|---|---|---|
| `DegreesFahrenheit`, `DegreesFahrenheitAnomaly` | 1 | same as °C; a °F is smaller, so it's slightly more precise |
| `Inches` | 2 | US convention (daily rain is reported to 0.01 in); mm uses 0 |
| `SqMiles` | 0 | same as km² |
| `MillionSqMiles` | 2 | sea ice ~4.5 million mi²; 1 dp would lose precision km² keeps |

### 2. `UnitConversion` (Core, new file)

```csharp
public enum UnitSystem { Metric, UsCustomary }

public enum ValueKind { Absolute, Difference, NotAQuantity }

public static class UnitConversion
{
    public static UnitOfMeasure DisplayUnit(UnitOfMeasure unit, UnitSystem system);
    public static double Convert(double value, UnitOfMeasure unit, UnitSystem system, ValueKind kind);
    public static double? Convert(double? value, UnitOfMeasure unit, UnitSystem system, ValueKind kind);
}
```

- `DisplayUnit` maps per the table above. In metric, or for units that don't convert, it returns
  `unit` unchanged.
- `Convert` applies the affine formula only for `DegreesCelsius` with `ValueKind.Absolute`.
  Every other case is scale-only. `NotAQuantity` and metric return the value unchanged.
- Pure, static and in Core, so it's unit-tested directly.

### 3. Setting and re-render (`UnitPreference`)

Add a scoped `UnitPreference` service in `ClimateExplorer.Web.Client/UiLogic`, following the
pattern of `DateLabels`:

```csharp
public sealed class UnitPreference
{
    public UnitSystem Current { get; private set; } = UnitSystem.Metric;
    public event Action? Changed;
    public void Set(UnitSystem system);
}
```

Register it with `AddScoped<UnitPreference>()` in both `ClimateExplorer.Web.Client/Program.cs` and
`ClimateExplorer.Web/Program.cs`. Unlike `DateLabels` it needs no request-derived factory and no
`[PersistentState]`: the server prerender is always metric, and so is the first interactive
render, so nothing flickers.

Propagation: `Routes` subscribes to `Changed` and wraps the `Router` in
`<CascadingValue Value="@unitPreference.Current">`. Components that format values declare
`[CascadingParameter] public UnitSystem Units { get; set; }`. A change re-renders consumers and
re-runs their `OnParametersSet`, so there are no per-component subscriptions to dispose.
`Routes` is the single `InteractiveAuto` root, so the value survives every in-app navigation.

Watch for components that skip `OnParametersSet` work by manual change detection. They must also
compare `Units`, otherwise they won't refresh. Known case: `LocationDashboard.OnParametersSetAsync`
returns early when `Location.Id` is unchanged (`LocationDashboard.razor.cs:139`).

### 4. Toggle UI

In `NavMenu`, add a two-state control in `BarEnd` before the CO₂ tile showing **°C** / **°F**, with
`aria-pressed` on the active state and an accessible name of "Units: metric" / "Units: US
customary". On mobile, put the same control in the hamburger dropdown, since `BarEnd` is crowded at
≤767px. Build it from `ClimateButton`; text stays at or above 0.75rem.

**Hide the toggle until every area below is converted** (step 8 in the implementation order). A
half-converted site, where the chart shows °F and the dashboard shows °C, is worse than no toggle.

## Absolute values vs differences

This is the main correctness risk. Adding 32 to a difference is wrong: a +1.0°C anomaly is +1.8°F,
not +33.8°F. Every value has to be classified.

| Value | Kind |
|---|---|
| A reading, mean, median, min or max of readings; a record high/low; a period average | Absolute |
| Any anomaly (`SeriesValueOptions.Anomaly`, `DegreesCelsiusAnomaly`, `CalculatedAnomaly.AnomalyValue`, `Location.WarmingAnomaly`) | Difference |
| `DifferenceBetweenTwoSeries`, `AverageOfAnomaliesInRegion` | Difference |
| Trend slope / per-decade rate; standard deviation; range (max − min) | Difference |
| Heating score warming-anomaly ranges | Difference |
| Standard score (×), percentiles, counts, day-of-year, `IsPositive`/`IsNegative`/`DayOfYearIfFrost`/`Custom` transformations | NotAQuantity |

**Prefer converting inputs over outputs.** Where a feature computes its own statistics from raw
values, convert the raw values on the way in. Means, differences, standard deviations, smoothing
and regression slopes then come out correct in the display unit, because they're all linear in the
input. That covers the chart and recent observations. Classify output values one by one only
where the client receives already-computed numbers from the API (the `Location` model, climate
records).

Two edge cases where input conversion isn't enough:

- **`Negate` transformation** (chart): the server sends `−c`. The correct °F value is
  `−(1.8c + 32) = 1.8v − 32` for `v = −c`, so negate, convert, negate again.
- **`Sum` aggregation of an absolute temperature**: the affine offset is added once per summed
  value, and the client doesn't know how many there were. A sum of temperatures isn't meaningful
  anyway, so leave such a series in metric, labelled °C. In general, a value that can't be
  converted correctly stays metric with a metric label rather than getting a wrong °F one.

## Changes by area

### Chart (`ChartDataBuilder`, `ChartOptionsFactory`, `ChartSeriesDefinition`, `ChartablePage`)

- `ChartDataBuilder.BuildAsync` takes the `UnitSystem` (via `ChartState` or a parameter). Right
  after `RetrieveDataSets`, replace each `SourceDataSet` with a converted copy: new `BinnedRecord`s
  and a cloned `MeasurementDefinition` whose `UnitOfMeasure` is the display unit. **Never mutate**
  the fetched objects: `MeasurementDefinition`s on `ChartSeriesDefinition` come from the
  per-process dataset-definition cache in `DataService`. The temporal calculations, smoothing,
  trend fitting and tooltip anomaly bands that follow then all work in display units unchanged.
- `ValueKind` for a chart series is resolved from the `ChartSeriesDefinition`, in one small,
  tested helper:
  - `NotAQuantity` if `SeriesTransformation` is not `Identity`/`Negate`
  - `Difference` if `Value == Anomaly`, the unit is `DegreesCelsiusAnomaly`, or the derivation is
    `DifferenceBetweenTwoSeries`/`AverageOfAnomaliesInRegion`
  - leave in metric if the unit is `DegreesCelsius`, `Aggregation == Sum` and the kind is
    `Absolute`
  - otherwise `Absolute`
- Code that reads the unit from the series definition instead of the data set has to switch to the
  data set's (display) unit:
  - `ChartDataBuilder.ResolveTrendUnit` (`ChartDataBuilder.cs:318`)
  - `ChartOptionsFactory.CreateYAxes` (`ChartOptionsFactory.cs:116-123`), for both the axis label
    and `ChartLogic.GetYAxisId`, so °C and °F series never share an axis
  - `ChartSeriesDefinition.GetFriendlyDescription` (`ChartSeriesDefinition.cs:316`)
- `ChartablePage`: when `Units` changes, call `ApplyChartStateAsync(CurrentChartState, updateUrl: false)`.
  The URL is unaffected, which is correct, because units aren't part of chart state.
- Already correct once the data set carries the display unit: `ChartTooltipMetadataBuilder`
  (label and rounding from `dataSet.MeasurementDefinition`), `ChartView.razor.cs:505`.

### Location dashboard, info panels and climate stripe

- `LocationDashboard`: `Location.WarmingAnomaly` is a difference, `RecordHigh.Value` is absolute,
  and the precipitation anomaly is a difference. Extend the early-return check to include `Units`.
- `AnomalyCalculator.ValueAsString` (`AnomalyCalculator.cs:39`) hard-codes `"°C"`/`"mm"` and is
  called with `UnitOfMeasure.DegreesCelsius` for what is actually an anomaly. Replace it with a
  formatter that takes the display unit and treats the value as a difference. Callers:
  `LocationDashboard`, `WarmingAnomalyInfo`, `PrecipitationAnomalyInfo`.
- `WarmingAnomalyInfo.razor` / `PrecipitationAnomalyInfo.razor`: the period averages are absolute
  and currently formatted inline with literal units.
- `HeatingScoreInfo.razor`: convert the anomaly ranges table and the location's anomaly (all
  differences). The score itself is unitless.
- `ClimateStripe` / `ClimateStripeInfo` / `YearlyValuesHelper`: `LocationMean` is absolute and
  `Relative` is a difference. Colours are computed from the relative min/max, so the stripe looks
  the same in both modes.

### Climate records, Top 100 and export

- `ClimateRecords.razor.cs` / `Top100.razor.cs`: convert when building `ClimateRecordViewModel`s
  (value and average are absolute, anomaly is a difference). The view model then carries the
  display unit and the existing `UnitOfMeasureLabelShort` calls work. Again, build new view models;
  don't modify the API response objects, because `DataService` may hold them in its cache.
- `Exporter.ExportClimateRecords` exports the view models and labels its headers with the unit, so
  it follows the toggle automatically, as decided. Check that `FormatCsvValue` keeps enough
  decimals for inches.

### Locations page

- `Locations.razor.cs:169-198`: `FormatAnomaly` (difference) and `FormatRecordHigh` (absolute)
  hard-code `"0.0"` and `"°C"`. Route both through the shared formatter.
- Sorting uses the raw values and every conversion is increasing, so sort order is unchanged.

### Recent observations

- Convert the daily series values at the entry to `RecentObservationsCalculator`, not in
  `RecentObservationsDataProvider`: the provider caches its data sets for the page's lifetime, and
  converting there would leave cached data in whatever unit was active when it loaded. Averages,
  anomalies, variation (std dev), standard scores and the trend tab then come out in display units.
- `Metric` currently carries a hard-coded unit string (`"°C"`/`"mm"` in
  `RecentObservationsCalculator.MetricDefinitions.cs`) and a static `Format` delegate
  (`FormatTemperature`/`FormatPrecipitation` in `RecentObservationsCalculator.Formatting.cs`).
  Change `Metric` to carry the source `UnitOfMeasure`, and format with its display unit for the
  current `UnitSystem`. CO₂ metrics are unaffected (ppm).
- `IRecentObservationsService`/the calculator take the `UnitSystem` as an input, and the panel
  re-runs the calculation when `Units` changes. The data provider's cached data is reused, so no
  refetch is needed.

### Trends

- `TrendFormatting` (`TrendFormatting.cs:15-28`) looks up decimal places and spacing by label text
  (`["°C"] = 2`). Re-key both tables by `UnitOfMeasure` (adding °F and in) and change
  `FormatPerDecade*` to take the unit rather than a label string. Otherwise °F and in would silently
  fall back to the 2-decimal, spaced default ("0.36 °F"). The trend-specific precision (2 dp for
  temperature, more than `UnitOfMeasureRounding`'s 1) is intentional and stays.
- `LocationTrendFieldBuilder`, `TemperatureTrendInfo` and `TemperatureTrendAboutPanel` each have
  `private const string Unit = "°C"`. Replace with the display unit. The slopes come from the
  `Location` model and are differences.

### Left in metric deliberately

- Explanatory prose with worked examples: `TrendsOverviewExplainer`, `About`, dataset descriptions.
  Converting a worked example ("0.45 ÷ 10 = 0.045°C per year") would mean rewriting the arithmetic.
- **Thresholds that define a calculation.** "Days ≥ 35°C", "frost ≤ 2.2°C", "rain days ≥ 1 mm"
  are evaluated server-side in metric through `CustomTransformation` expressions (`x >= 35`). The
  counts are correct in both modes, and the preset descriptions that mention these thresholds
  stay as written.
- User-typed custom transformation expressions are always in source (metric) units.

## Implementation order

1. Core: new enum members, labels/rounding, `UnitSystem`, `ValueKind`, `UnitConversion` + tests.
2. `UnitPreference`, DI registration in both projects, `CascadingValue` in `Routes`, toggle
   component in `NavMenu` (hidden).
3. Chart.
4. Trends formatting (`TrendFormatting` re-key, `LocationTrendFieldBuilder`, trend info panels).
5. Location dashboard, info panels, climate stripe, `AnomalyCalculator.ValueAsString` replacement.
6. Climate records, Top 100, export.
7. Locations page, recent observations.
8. Unhide the toggle.

Every step before 8 is safe to ship: with the toggle hidden, `Units` is always `Metric` and every
conversion is the identity.

## Tests

Following `MethodName_StateUnderTest_ExpectedBehavior`:

- `UnitConversion`: `Convert_AbsoluteCelsiusToUsCustomary_AppliesOffset` (0 → 32, 100 → 212, −40 →
  −40); `Convert_DifferenceCelsiusToUsCustomary_ScalesWithoutOffset` (1 → 1.8);
  `Convert_MillimetresToUsCustomary_ReturnsInches` (25.4 → 1); `Convert_Metric_ReturnsValueUnchanged`;
  `Convert_NotAQuantity_ReturnsValueUnchanged`; `DisplayUnit_PartsPerMillion_ReturnsSameUnit`.
- `Enums`: every `UnitOfMeasure` member has a short label, long label and rounding (one test
  enumerating all values, so a future unit can't be forgotten).
- Chart `ValueKind` resolution: one test per row of its rule list, including `Negate` and
  `Sum`-of-temperature.
- `ChartDataBuilder` (`ChartDataBuilderTests.cs` exists): a US customary build returns °F values and
  a `DegreesFahrenheit` data set, and doesn't mutate the input `MeasurementDefinition`.
- `TrendFormatting`: `FormatPerDecadeValue_DegreesFahrenheit_UsesTwoDecimalsWithoutSpace`,
  `FormatPerDecadeValue_Inches_UsesTwoDecimalsWithoutSpace`.
- Recent observations: an anomaly in US customary mode is formatted as a scaled difference, not an
  offset value.

## Risks

- **Offset applied to a difference** (or missing on an absolute value). The most likely bug, and it
  produces plausible-looking wrong numbers. Mitigations: the classification table above, the
  `ValueKind` argument being required rather than defaulted, and "convert inputs" wherever
  possible.
- **Mutating cached objects.** `DataService` caches dataset definitions and several API responses;
  `RecentObservationsDataProvider` caches data sets. Converting any of them in place would corrupt
  later renders after the toggle flips back. Always build new objects.
- **Stale components.** Any component that caches formatted strings and short-circuits
  `OnParametersSet` won't refresh on toggle. `LocationDashboard` is the known one; review others
  during each step.
- **Chart refetch on toggle.** `PostDataSet` isn't cached client-side, so flipping units re-requests
  every series. Acceptable for v1. If it's noticeably slow, keep the retrieved metric data sets
  between builds and re-run only the conversion and processing.
- **Threshold wording.** In US customary mode, a preset described as "days 35°C or above" sits next
  to °F values. This is accepted: descriptions don't change.

## Out of scope

- Remembering the choice (cookie/localStorage) or defaulting from `Accept-Language`. If wanted later,
  `DateLabels`/`DateLabelsFactory` already show how to derive a per-reader default during prerender
  without flicker.
- Converting server-side calculations, the API, or data files.
- Changing preset or dataset descriptions to mention US customary units.
