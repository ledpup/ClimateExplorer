# US customary units toggle

- **Date:** 2026-10-21 (revised 2026-10-24)
- **Status:** Implemented 2026-10-24 (see addendum)
- **Author:** Claude
- **Scope:** new `ClimateExplorer.Web.Client/Units/` folder; display code in `ClimateExplorer.Web.Client`
  (`Routes`, `Layout/NavMenu`, `Components/Chart`, `UiLogic`, `UiModel`, `Services/Trends`,
  `Services/RecentObservations/Calculator`, `Components/Location`, `Components/ClimateRecords`,
  `Pages/Locations`, `Services/Exporter.cs`); `ClimateExplorer.Web/Program.cs` (DI registration);
  `ClimateExplorer.UnitTests`. **No changes to `ClimateExplorer.Core`, the WebApi or data files.**
- **Branch context:** `development`

## Goal

Let a reader switch the site between metric and **US customary** units with a switch in the nav
bar. In US customary mode, temperatures show in °F, rainfall and sea level in inches, and areas in
square miles.

The change must not put any calculation at risk. Every number the site computes today stays
computed exactly as it is, in metric. A single display layer converts finished numbers on their way
to the screen, and that layer is the only code that knows US customary units exist.

## Decisions

- **US customary, not imperial.** For the units the site shows (°F, inches, square miles) the two
  systems are identical; the name only matters if volume or mass is ever added.
- **Remembered in `localStorage`**, through the `Blazored.LocalStorage` service already used by
  `InfoPanelDismissalService`. No cookie. See [Persistence and flicker](#persistence-and-flicker)
  for what this costs.
- **Core and the WebApi don't change.** `Enums.UnitOfMeasure` keeps its current members. Display
  units (°F, in, mi²) are a UI concept and live only in `ClimateExplorer.Web.Client/Units/`.
- **Calculations never see converted values.** Conversion is one-way and happens after all
  calculation. Converted numbers go to Razor markup, Chart.js and CSV, and nowhere else.
- **No UnitsNet.** Five mappings and one formula shape (scale, plus an offset for absolute
  temperatures). UnitsNet would add a large assembly to the WebAssembly download and still couldn't
  tell which values are differences, which is the part that goes wrong.
- **Areas convert** (km² → mi²) as well as temperature and length.
- **CSV exports follow the switch.** Their headers already state the unit.
- **Descriptions and explanatory prose don't change.** Preset and dataset descriptions keep their
  metric wording, including thresholds such as "35°C or above".

## Which units convert

| Core `UnitOfMeasure` | US customary display | Scale | Offset (absolute values only) | Used by |
|---|---|---|---|---|
| `DegreesCelsius` | °F | 1.8 | +32 | station TMax/TMin/TMean |
| `DegreesCelsiusAnomaly` | °F | 1.8 | none, ever | NOAAGlobalTemp, SST anomalies |
| `Millimetres` | in | 1 ÷ 25.4 | none | precipitation, mean sea level |
| `SqKm` | mi² | 1 ÷ 2.589988110336 | none | Greenland melt area |
| `MillionSqKm` | million mi² | 1 ÷ 2.589988110336 | none | sea ice extent, ozone hole area |

All factors are exact by definition (1 in = 25.4 mm; 1 mi² = 2.589988110336 km²).

**This is an allow-list.** Every other unit (ppm, ppb, W/m², MJ/m², DU, pH, ENSO index, Sn, ODGI,
atmospheric transmission, Mt, m w.e.) shows unchanged in both modes, and so will any unit added in
future until someone adds a row. These are dimensionless or scientific units that US agencies also
publish in SI.

**Only one row has an offset.** That narrows the main correctness risk to a single question,
asked only of `DegreesCelsius` values: is this number a temperature, or a difference between
temperatures? For every other unit the answer doesn't affect the result.

## The display layer

```
 Core / WebApi / client calculators       unchanged, metric
        │  double + Core UnitOfMeasure
        ▼
 Boundary code (one place per feature)    tags each number: Measure.Level(...) or Measure.Change(...)
        │  Measure
        ▼
 IUnitFormatter                           the only code that converts or writes a unit label
        │  string / double in display units
        ▼
 Razor, Chart.js, CSV
```

### Types (`ClimateExplorer.Web.Client/Units/`)

```csharp
public enum UnitSystem { Metric, UsCustomary }

/// What a number means, which decides whether the °C → °F offset applies.
public enum MeasureKind
{
    /// A temperature, rainfall total, extent: a reading, mean, median, min, max, record or
    /// period average of readings.
    Level,

    /// A difference between levels: an anomaly, trend slope, standard deviation, range, or
    /// "above/below average" amount.
    Change,
}

/// A finished, metric number on its way to the screen. There is no constructor without a kind,
/// so every call site states what the number means.
public readonly struct Measure
{
    public static Measure Level(double value, UnitOfMeasure unit);
    public static Measure Change(double value, UnitOfMeasure unit);

    public double Value { get; }
    public UnitOfMeasure Unit { get; }
    public MeasureKind Kind { get; }
}

public interface IUnitFormatter
{
    UnitSystem System { get; }

    /// "68.2°F". Rounds once, after converting. Signed adds "+" to positive values.
    string Format(Measure measure, bool signed = false, int? decimals = null);

    /// The number in display units, for Chart.js data and CSV cells.
    double ToDisplay(Measure measure);

    /// "°F", "in" or the unchanged metric label.
    string ShortLabel(UnitOfMeasure unit);
    string Label(UnitOfMeasure unit);
    int Decimals(UnitOfMeasure unit);
}

public sealed class UnitFormatter : IUnitFormatter
{
    public static readonly UnitFormatter Metric;
    public static readonly UnitFormatter UsCustomary;
    public static UnitFormatter For(UnitSystem system);
}
```

- The conversion table above is a private dictionary inside `UnitFormatter`. Nothing else holds a
  factor.
- **The metric formatter does no arithmetic.** It returns `Value` as-is and takes labels and
  rounding from the existing `Enums.UnitOfMeasureLabelShort`, `UnitOfMeasureLabel` and
  `UnitOfMeasureRounding`. Metric output is identical to today's, by construction rather than by
  multiplying by 1 and adding 0.
- `Format` never accepts a pre-rounded value or a string. Rounding a metric value and then
  converting it compounds the rounding error, so boundary code passes the raw `double`.
- US customary rounding: °F 1 dp (as °C); inches 2 dp (daily rain is reported to 0.01 in; mm uses
  0); mi² 0 dp (as km²); million mi² 2 dp (sea ice ≈ 4.5 million mi²; 1 dp would lose precision km²
  keeps). Feature-specific precision that exists today, such as 2 dp for trend slopes, stays and
  applies to the converted value.

### Setting, persistence and re-render

`UnitPreference` (scoped, `ClimateExplorer.Web.Client/Units/`), registered in both
`ClimateExplorer.Web.Client/Program.cs` and `ClimateExplorer.Web/Program.cs`:

```csharp
public sealed class UnitPreference
{
    public IUnitFormatter Formatter { get; private set; } = UnitFormatter.Metric;
    public event Action? Changed;

    public Task LoadAsync();                 // reads localStorage; no-op during prerender
    public Task SetAsync(UnitSystem system); // updates Formatter, writes localStorage, raises Changed
}
```

`Routes` subscribes to `Changed`, calls `LoadAsync` in `OnAfterRenderAsync(firstRender)`, and wraps
the `Router` in `<CascadingValue Value="@unitPreference.Formatter">`. Components that show numbers
declare `[CascadingParameter] public IUnitFormatter Units { get; set; }`. Because the cascaded value
is the formatter itself, and a change swaps one immutable instance for the other, consumers
re-render and re-run `OnParametersSet` without subscribing or disposing anything, and a component
can't format with a stale system.

Components that skip `OnParametersSet` work by comparing parameters must also compare `Units`.
Known case: `LocationDashboard.OnParametersSetAsync` returns early when `Location.Id` is unchanged
(`LocationDashboard.razor.cs:139`).

### Persistence and flicker

`localStorage` can only be read from the browser, so the server prerender is always metric. For a
reader who chose US customary, a **full page load** (first visit, reload, opening a link) renders
metric values and then swaps them to °F once the page is interactive. In-app navigation doesn't
prerender, so it doesn't flicker.

What does and doesn't flicker:

- **Charts don't.** Chart.js only draws after the page is interactive. `ChartView` waits for
  `UnitPreference.LoadAsync` before its first draw, so the first chart a US reader sees is in °F.
- **Prerendered text does:** location dashboard fields, the locations list, climate records.

Ways to remove it, not taken in this plan:

- **A cookie** is the only way the server can render °F in the first response. It would use the
  same request-derived factory and `[PersistentState]` hand-over that `DateLabels`/`DateLabelsFactory`
  already use, and only `UnitPreference.LoadAsync` would change. Two facts relevant to the consent
  concern: EU/UK cookie rules (ePrivacy Art. 5(3)) apply to `localStorage` exactly as they apply to
  cookies, and both are exempt from consent when they only store a preference the reader set
  themselves. A unit preference stored either way doesn't need a banner. (Separately, the default
  `gtag("config", ...)` in `App.razor` already sets Google Analytics `_ga` cookies.)
- **Rendering both values and hiding one with CSS** chosen by an early inline script. Every number
  would have to be markup rather than a string, which most current call sites aren't (`OverviewField`
  parameters, `aria-label`s, tooltips).

If the flicker proves annoying, switching to a cookie changes `UnitPreference` and the two
`Program.cs` registrations, nothing else.

### Switch UI

A `role="switch"` button in `NavMenu`'s `BarEnd`, before the CO₂ tile. On mobile it moves into the
hamburger dropdown, since `BarEnd` is crowded at ≤767px.

**Label: "US units"**, off by default, accessible name "US customary units", with `aria-checked`.
Recommended over "Metric" (on by default), because:

- A switch label should name what turning it on does. The readers who'll touch this are US readers
  looking for °F; "US units" says what they get. With "Metric" they have to infer that turning
  metric *off* gives US customary, and a UK reader could reasonably expect imperial.
- With "Metric", every reader sees an "on" (blue) switch on every page for a setting almost none of
  them use. With "US units", the switch only lights up for the readers who turned it on.
- It's still short, and needs no separate "Units:" caption.

**Colours (WCAG 1.4.11 needs 3:1 for the parts that show the state, against what's next to
them).** The nav bar is `#425f59`. Standard blues fail there: `#0071c1` (the site's link colour) is
1.37:1 and `#0d6efd` is 1.55:1. The info-icon blue already in the palette,
`--climate-color-info-icon` `#79c6f4`, is 3.72:1, so:

- On: `#79c6f4` track, `#425f59` knob, knob at the right. Knob against track is also 3.72:1.
- Off: transparent track with a white border (6.97:1), white knob at the left.
- Hover: `#cfc` border, as for other nav controls. Focus ring visible against `#425f59`.
- Text ≥ 0.75rem, touch target ≥ 44px tall.

State is shown by knob position as well as colour, so it doesn't rely on colour alone.

## Absolute values vs differences

This is the main risk. Adding 32 to a difference is wrong: a +1.0°C anomaly is +1.8°F, not +33.8°F,
and the wrong number looks plausible.

| Value | Kind |
|---|---|
| A reading, mean, median, min or max of readings; a record high/low; a period average | `Level` |
| Any anomaly (`SeriesValueOptions.Anomaly`, `CalculatedAnomaly.AnomalyValue`, `Location.WarmingAnomaly`) | `Change` |
| `DifferenceBetweenTwoSeries`, `AverageOfAnomaliesInRegion` | `Change` |
| Trend slope / per-decade rate; standard deviation; range (max − min); "x above average" | `Change` |
| Heating score warming-anomaly ranges | `Change` |
| Standard score (×), percentiles, counts, day-of-year, transformations other than `Identity` | not a `Measure`; shown as today |

**When a value can't be converted with certainty, it stays metric with its metric label.** The
failure mode of an omission is then "shown correctly in °C" rather than "wrong number in °F". This
applies to:

- Chart series with any transformation other than `Identity`, including `Negate`.
- A `DegreesCelsius` series with `Aggregation == Sum`: the offset would be added once per summed
  value, and a sum of temperatures has no sensible °F reading anyway.
- A `DegreesCelsius` bar series drawn with `absoluteValues`: `|°C|` and `|°F|` don't correspond.

## How the risk is controlled

Each of these is enforced by code or a test, not by review alone.

1. **Calculations stay metric.** Nothing upstream of the boundary changes, so every existing unit
   test keeps testing the real calculation. Converting inputs to client calculations was considered
   and rejected: client code holds metric constants (axis padding and scale-to-zero, bar colours by
   sign, recent-observations comparisons), and each would need its own conversion.
2. **Metric output is unchanged.** Metric is the default, the metric formatter does no arithmetic,
   and a test checks it against today's formatting for every `UnitOfMeasure`.
3. **Allow-list of units.** Units not in the table are never converted.
4. **No `Measure` without a kind.** Every call site states `Level` or `Change`.
5. **A number and its unit come from the same place.** `Format` returns value and label together.
   Where a number and label are separate (chart data and axis title, CSV cell and header), both come
   from one per-series object resolved once (see [Chart](#chart)), so a °F number can't get a °C
   label or the reverse.
6. **Offset-shift tests check every kind against the calculation itself.** For each boundary fed by
   a client calculation, a test runs it on input temperatures `X` and on `X + 10°C` with a
   `RecordingUnitFormatter` that captures every `Measure`. Paired by order: each `DegreesCelsius`
   `Level` must move by exactly 10, and each `Change` must not move. A mistagged anomaly fails this
   test, whatever the reviewer believed. Values that come pre-computed from the API (the `Location`
   model, climate records) can't be shifted in a client test, so each field gets a named test of its
   kind instead.
7. **No unit text outside the layer.** An architecture test scans `ClimateExplorer.Web.Client` `.cs`
   and `.razor` files for `°C`, `°F`, `"mm"`, `km²`, `mi²` and calls to
   `UnitOfMeasureLabelShort`/`UnitOfMeasureLabel`/`UnitOfMeasureRounding` outside `Units/`, against
   an allow-list. The allow-list starts with every current site (13 files call the label helpers; 14
   contain literals) and shrinks as each area is converted; prose files (`About`,
   `TrendsOverviewExplainer`, descriptions) stay on it permanently. A future display site that
   bypasses the layer fails the build.
8. **The switch stays hidden until the allow-list holds only prose.** A half-converted site, with °F
   in the chart and °C on the dashboard, is worse than no switch.
9. **Nothing cached is touched.** Conversion produces new strings, arrays and view models, and never
   writes to a `DataSet`, `BinnedRecord`, `MeasurementDefinition` or API response object.
   `DataService` and `RecentObservationsDataProvider` caches stay metric.

**Post-conversion arithmetic.** Two calculations run on converted numbers, both in the chart's
JavaScript, and both are correct in any linear unit: the tooltip's `raw − period.average` (the
difference of two levels: (1.8a + 32) − (1.8b + 32) = 1.8(a − b)), and chartjs-plugin-trendline's
least-squares line (an affine change of y gives the same line in the new unit). They can't be unit
tested here, so they're noted in comments beside the code.

## Changes by area

Each area has one boundary: the code that turns computed values into what's displayed.

### Chart

The boundary is where `ChartView` turns a `ChartDataBuildResult` into Chart.js inputs.
`ChartDataBuilder`, the trend calculation, smoothing and `ChartDataBuildResult` don't change.

- **Extract the boundary from `ChartView`.** `BuildChartDatasets` and `AddTrendDatasets` are private
  component methods (`ChartView.razor.cs`). Move them into a pure `ChartDisplayBuilder` (in
  `UiLogic`, beside `ChartLogic`) that takes the build result, the series list and an
  `IUnitFormatter`, and returns the datasets, tooltip metadata and options. `ChartView` keeps
  calling `ChartJsChart.SetDataAsync`/`SetOptionsAsync` as now. This also makes the chart's
  boundary testable without a component.
- **`SeriesDisplayUnits`**, resolved once per series from its `ChartSeriesDefinition` and
  `UnitOfMeasure`: converts or not (per the rules in
  [Absolute values vs differences](#absolute-values-vs-differences)), its `MeasureKind`, display
  label, decimals and axis key. Every number and label for that series goes through it:
  - dataset values (`ChartLogic.GetSeriesChartDataset`) and trend projections
    (`prediction.PredictedY`)
  - axis min/max (`ChartOptionsFactory.CalculateAxisMinMax`), so padding and scale-to-zero apply
    in display units (scale-to-zero means 0°F on a °F axis)
  - axis title (`ChartOptionsFactory.CreateYAxes`, wrapping `Enums.UnitOfMeasureLabel`'s
    transformation/anomaly wording with the display unit)
  - legend label (`ChartView.BuildChartDatasets`), tooltip label, rounding and anomaly-period
    averages (`ChartTooltipMetadataBuilder`), `ChartSeriesDefinition.GetTooltipLabel` and
    `GetFriendlyDescription`
  - trend unit text (`ChartDataBuilder.ResolveTrendUnit`, `ChartDataBuilder.cs:318`)
  - chart CSV download (`OnDownloadDataClicked` → `Exporter`)
- **Axis ids include the display unit.** `ChartLogic.GetYAxisId` is keyed by `UnitOfMeasure`
  today. A `DegreesCelsius` series left metric (say, a sum) must not share an axis with a converted
  one.
- **Bar colours are chosen from the metric values.** `GetBarChartColourSet` colours by sign, and
  °C and °F levels can differ in sign. Colouring before converting keeps the chart looking the same
  in both modes. (Change series keep their sign anyway.)
- **On toggle, `ChartView` redraws without refetching.** The build result is unchanged; only the
  `ChartDisplayBuilder` step and the `ChartJsChart` update re-run. This replaces the previous plan's
  full `ApplyChartStateAsync` refetch.
- `YearFilterRequested` keeps passing the source `UnitOfMeasure`, which is a data lookup key, not
  a display value.

### API field kinds (`UiLogic/ApiMeasures.cs`, new)

Values that arrive already calculated from the API can't be checked by the offset-shift tests, so
their kinds are declared in one place rather than at each component: small static methods such as
`ApiMeasures.WarmingAnomaly(Location)` → `Change` and `ApiMeasures.RecordHigh(DataRecord)` →
`Level`, plus the precipitation anomaly and climate record value/average/anomaly. The dashboard,
info panels, locations page and climate records call these instead of building `Measure`s
themselves, and each method has a test of its kind.

### Location dashboard, info panels and climate stripe

Boundaries: `LocationDashboard.OnParametersSetAsync`, the info panels' `OnParametersSet`, and
`YearlyValuesHelper`.

- `LocationDashboard` (`LocationDashboard.razor.cs:150`): `Location.WarmingAnomaly` → `Change`;
  `RecordHigh.Value` → `Level`; precipitation anomaly → `Change`. Extend the early-return check to
  include `Units`.
- `AnomalyCalculator.ValueAsString` (Core, `AnomalyCalculator.cs:39`) hard-codes `"°C"`/`"mm"` and
  is called with `DegreesCelsius` for an anomaly. Leave it in Core untouched and stop calling it
  from the UI: `LocationDashboard`, `WarmingAnomalyInfo` and `PrecipitationAnomalyInfo` format
  `Measure.Change(anomaly.AnomalyValue, unit)` with `signed: true` instead.
- `WarmingAnomalyInfo.razor` / `PrecipitationAnomalyInfo.razor`: period averages → `Level`.
- `HeatingScoreInfo.razor`: anomaly ranges table and the location's anomaly → `Change`. The score
  is unitless.
- `ClimateStripe` / `ClimateStripeInfo` / `YearlyValuesHelper`: `LocationMean` → `Level`,
  `Relative` → `Change`. Colours stay computed from metric values, so the stripe looks the same.

### Climate records, Top 100 and export

Boundary: building `ClimateRecordViewModel`s in `ClimateRecords.razor.cs` / `Top100.razor.cs`.

- Value and average → `Level`, anomaly → `Change`. The view models hold `Measure`s and format at
  render, so toggling re-renders without rebuilding. Don't modify API response objects;
  `DataService` may hold them in its cache.
- `Exporter.ExportClimateRecords` writes `ToDisplay` values and `ShortLabel` headers from the same
  `IUnitFormatter`. Check `FormatCsvValue` keeps 2 dp for inches.

### Locations page

- `Locations.razor.cs:169-198`: `FormatAnomaly` → `Change`, `FormatRecordHigh` → `Level`, both via
  `Units.Format` instead of hard-coded `"0.0"` and `"°C"`.
- Sorting stays on the raw metric values. Every conversion is increasing, so the order is the same.

### Recent observations

Boundary: `RecentObservationsCalculator.Formatting.cs` and the partials that call it (`TileBuilding`,
`MetricGroups`, `Variation`). The pure computation partials (`DailySeries`, `Periods`, `Trend`,
`HistoricalDistributions`, `Metrics`) don't change.

- `Metric` carries a hard-coded label (`"°C"`/`"mm"` in `RecentObservationsCalculator.MetricDefinitions.cs`)
  and a static `Format` delegate (`FormatTemperature`/`FormatPrecipitation`). Replace both with
  the metric's `UnitOfMeasure`; format via the `IUnitFormatter` passed into the calculator.
- `FormatAnomaly` → `Change`. Every other `metric.Format(value)` call is classified by the table
  above; the offset-shift test (control 6) checks all of them in one test. `FormatStandardScore`
  (×) and CO₂ (ppm) don't change.
- On toggle, the panel re-runs the calculator with the new formatter. The data provider's cached
  data sets are reused, so nothing is refetched.
- `Components/RecentObservations/Tab/ValueClassifier.cs` reads formatted strings to spot values
  that round to zero ("+0.0°C"). Check it handles "+0.0°F" and "+0.00 in".

### Trends

Boundary: `LocationTrendFieldBuilder`, `TemperatureTrendInfo`, `TemperatureTrendAboutPanel`.

- `TrendFormatting` (`TrendFormatting.cs:15-28`) looks up decimals and spacing by label text
  (`["°C"] = 2`). Re-key by `UnitOfMeasure` and take the display label from the formatter, so °F
  and in don't fall back to the spaced default ("0.36 °F"). All slopes are `Change`.
- Replace each `private const string Unit = "°C"` with the formatter.

### Left in metric deliberately

- Explanatory prose with worked examples: `TrendsOverviewExplainer`, `About`, dataset descriptions.
- **Thresholds that define a calculation**: "days ≥ 35°C", "frost ≤ 2.2°C", "rain days ≥ 1 mm".
  These run server-side in metric through `CustomTransformation` expressions. The counts are correct
  in both modes, and the descriptions stay as written.
- User-typed custom transformation expressions are always in metric.

## Implementation order

1. `Units/` types, `UnitFormatter` with its table, `RecordingUnitFormatter` (test project), the
   unit-literal architecture test with its full starting allow-list.
2. `UnitPreference`, DI in both projects, `CascadingValue` in `Routes`, the switch in `NavMenu`
   (hidden).
3. Chart: extract `ChartDisplayBuilder`, add `SeriesDisplayUnits`.
4. Trends.
5. Location dashboard, info panels, climate stripe.
6. Climate records, Top 100, export.
7. Locations page, recent observations.
8. Unhide the switch once the allow-list holds only prose files.

Steps 1–7 are safe to ship: with the switch hidden, every component gets `UnitFormatter.Metric`,
whose output matches today's.

## Tests

Following `MethodName_StateUnderTest_ExpectedBehavior`:

- `UnitFormatter`
  - `Format_LevelCelsiusUsCustomary_AppliesOffset` (0 → 32, 100 → 212, −40 → −40, 37 → 98.6)
  - `Format_ChangeCelsiusUsCustomary_ScalesWithoutOffset` (1 → 1.8, 0 → 0, −0.5 → −0.9)
  - `Format_LevelCelsiusAnomalyUsCustomary_NeverAppliesOffset` (the anomaly unit ignores `Level`)
  - `Format_MillimetresUsCustomary_ReturnsInches` (25.4 → 1.00 in)
  - `Format_MillionSqKmUsCustomary_ReturnsMillionSqMiles` (2.589988110336 → 1.00)
  - `Format_UnlistedUnitUsCustomary_ReturnsMetricOutput` (ppm, one per unlisted unit)
  - `Format_Metric_MatchesExistingFormattingForEveryUnit` (enumerates `UnitOfMeasure`)
  - `Format_UsCustomary_RoundsAfterConverting` (21.06°C → 69.9°F, not 21.1 → 70.0)
- Architecture: `WebClientSources_UnitText_OnlyInUnitsFolderOrAllowList`.
- Offset-shift, one per client-computed boundary:
  - `ChartDisplayBuilder_ShiftedTemperatureInput_LevelsMoveAndChangesDoNot`
  - `RecentObservationsCalculator_ShiftedTemperatureInput_LevelsMoveAndChangesDoNot`
  - `YearlyValuesHelper_ShiftedTemperatureInput_MeanMovesAndRelativeDoesNot`
- `SeriesDisplayUnits`: one test per rule (anomaly → `Change`; `Negate`/`Custom` → stays metric;
  `Sum` of °C → stays metric; °C bar with `absoluteValues` → stays metric; plain mean → `Level`).
- Chart: `ChartDisplayBuilder_UsCustomary_AxisLabelAndValuesShareUnit`;
  `ChartDisplayBuilder_UsCustomary_DoesNotModifyBuildResult`;
  `GetYAxisId_ConvertedAndMetricCelsiusSeries_UseDifferentAxes`.
- `ApiMeasures`: one test per field, e.g. `WarmingAnomaly_AnyLocation_IsChange`,
  `RecordHigh_AnyRecord_IsLevel`, and the same for climate record value/average/anomaly.
- `TrendFormatting`: `FormatPerDecadeValue_DegreesFahrenheit_UsesTwoDecimalsWithoutSpace`,
  `FormatPerDecadeValue_Inches_UsesTwoDecimalsWithoutSpace`.

## Risks

- **A `Change` tagged `Level` (or the reverse)** for a value that comes pre-computed from the API.
  These aren't covered by the offset-shift tests. Mitigation: the set is small and fixed, every one
  is declared in `ApiMeasures` with a named test, and new ones can't reach the screen without
  passing through `Measure`.
- **Flicker on full page loads** for US customary readers (see
  [Persistence and flicker](#persistence-and-flicker)). Charts are unaffected.
- **Threshold wording.** In US customary mode a preset described as "days 35°C or above" sits next
  to °F values. Accepted: descriptions don't change.
- **`ChartView` extraction** moves working code. Keep it a move with the formatter threaded
  through; the existing `ChartLogicDatasetTests`, `ChartOptionsFactoryTests` and
  `ChartTooltipMetadataBuilderTests` run in metric mode and should pass unchanged.

## Out of scope

- Defaulting from `Accept-Language` or location.
- Any change to Core, the WebApi, server-side calculations or data files.
- Rewording preset or dataset descriptions for US customary readers.

## Summary

A display layer in `ClimateExplorer.Web.Client/Units/` converts finished metric numbers to US
customary units on their way to the screen. Calculations, Core, the WebApi and data files don't
change. Each feature tags its numbers as `Level` or `Change` at one boundary. Offset-shift tests,
an `ApiMeasures` field table and a unit-text architecture test check those tags, and the nav switch
stays hidden until every display site is converted. The setting is kept in `localStorage`. US
customary readers see metric text briefly on full page loads; charts don't flicker.

### Change size

**Projects:** 3 changed (`ClimateExplorer.Web.Client`, `ClimateExplorer.Web`,
`ClimateExplorer.UnitTests`). `ClimateExplorer.Core`, `ClimateExplorer.WebApi` and
`ClimateExplorer.WebApiClient` are unchanged.

| Area | Added | Changed | Removed | Lines (approx.) |
|---|---|---|---|---|
| `Units/` types, formatter, preference | 6 | – | – | +250 |
| Switch component (`.razor`, `.razor.cs`, `.razor.css`) | 3 | – | – | +105 |
| `Routes`, `NavMenu`, `_Imports`, both `Program.cs` | – | 7 | – | ~50 |
| Chart (`ChartDisplayBuilder`, `SeriesDisplayUnits`; `ChartView`, `ChartLogic`, `ChartOptionsFactory`, `ChartTooltipMetadataBuilder`, `ChartSeriesDefinition`, `ChartDataBuilder`, `Exporter`) | 2 | 7 | – | +280 new (~150 of it moved out of `ChartView`), ~170 changed |
| `ApiMeasures` | 1 | – | – | +40 |
| Location dashboard, info panels, climate stripe | – | 9 | – | ~95 |
| Climate records, Top 100 | – | 3 | – | ~55 |
| Locations page | – | 1 | – | ~15 |
| Recent observations (calculator partials, service, panel, `ValueClassifier`) | – | 10 | – | ~140 |
| Trends (`TrendFormatting` and three callers) | – | 4 | – | ~70 |
| Tests: new (`UnitFormatter`, recording formatter, architecture, `ChartDisplayBuilder`, `SeriesDisplayUnits`, offset-shift, `ApiMeasures`, `TrendFormatting`) | 8 | – | – | +770 |
| Tests: existing, updated to pass a formatter | – | ~7 | – | ~60 |
| **Total** | **20** | **~48** | **0** | **~2,100 lines touched: ~1,445 in new files (~150 moved), ~655 changed** |

No files are removed. The metric-only formatting helpers that are replaced
(`FormatTemperature`/`FormatPrecipitation`, the `Unit` constants, `FormatAnomaly`/`FormatRecordHigh`)
are removed from inside files that stay.

## Addendum — implementation notes

Implemented 2026-10-24 on `development`, all in one pass (steps 1–8), with the switch visible.
`dotnet build` is clean (0 warnings) and all 879 unit tests pass: the 807 existing ones, 2 of them
updated only for the `TrendStatSubject` unit type, plus 72 new ones. Per `AGENTS.md`, nothing was run
in a browser, so the switch's look, and whether it fits beside the CO₂ tile at tablet widths, still
need checking by eye.

### What shipped as planned

- `Units/` holds `UnitSystem`, `MeasureKind`, `Measure`, `IUnitFormatter`, `UnitFormatter` (the
  conversion table) and `UnitPreference` (localStorage key `unitSystem`). `Routes` cascades the
  formatter. `MeasureKind` has no zero member, so a `default(Measure)` is rejected rather than
  treated as a level.
- Chart: `ChartDisplayBuilder` (moved out of `ChartView`) and `SeriesDisplayUnits`. These cover
  values, trend projections, axis range and title, legend, tooltip label, rounding and averages,
  series description, and the CSV download.
- `ApiMeasures` holds the kinds of the location model, climate record, anomaly and heating score
  fields.
- Converted: the dashboard, the warming/precipitation anomaly and heating score panels, the climate
  stripe and its info panel, extreme years, climate records, Top 100, their CSV export, the
  Locations page, recent observations (tiles and all tabs), and every trend rate (dashboard field
  and tooltip, trend info, location and chart trend panels, recent observations trend rows,
  trend notifications).
- Tests: `UnitFormatterTests`, `UnitPreferenceTests`, `SeriesDisplayUnitsTests`,
  `ChartDisplayBuilderTests`, `RecentObservationsUnitsTests`, `ClimateStripeUnitsTests`,
  `ApiMeasuresTests`, `TrendUnitsTests`, `UnitTextArchitectureTests`, plus the
  `RecordingUnitFormatter`/`OffsetShift` helpers.
- The offset-shift tests were checked by deliberately mis-tagging two values (dropping
  `AnnualChange` from `SeriesDisplayUnits.ResolveKind`; tagging "typical variation" as a `Level`).
  The chart and recent-observations shift tests both failed on the mis-tagged measure, then passed
  once the mistakes were reverted.

### Deviations

- **The About-trends regression report stays metric** (`TrendStatSectionBuilder`). Its intercepts,
  confidence intervals and X-intercepts ("reaches 0°C in …") are tied to the metric fit, and an
  X-intercept moves to a different year in °F. `TrendStatSubject` now carries the `UnitOfMeasure`,
  and the report uses its `MetricUnitLabel`. The headline rates around it do convert.
- **Trend data downloads stay metric**, to match that report. Their header now names the unit
  (`Year,Value (°C)`).
- **The chart CSV header now names each column's unit**, in metric as well, e.g. `TempMean Adjusted (°C)`.
  Before this change it had no unit, which is unsafe once a column can be °F.
- **`IChartDataBuilder.BuildAsync` takes an optional `IUnitFormatter`**, used only for the text of the
  "trend not significant" notifications it returns. The data it builds is still metric.
- **Two formatter options were added**, both rounding once after converting: `extraDecimals`
  (the anomaly worked examples and heating score table show one place more than usual) and
  `minimumDecimals` (climate records always show at least one decimal place, so a daily rain record
  keeps its 0.1 mm).
- **Small metric text changes**, all where a site used raw `ToString()` or `Math.Round`:
  - The record-high tooltip now always shows one decimal ("40.0°C", not "40°C").
  - The climate stripe info's example values keep trailing zeros.
  - The precipitation anomaly info says "0 decimal places" instead of "0 decimal place".
- **Trend notifications and the dashboard/Locations trend field** carry `CanConvert` from the
  series. A trend fitted to a series left in metric (a sum of °C, or a negated series) stays in metric.
- **Unit-text allow-list.** It includes `Components/Common/UnitSwitch.razor`, because the switch's
  accessible name says which units it switches to, and `Components/AboutDataDetails.razor.cs`,
  because it describes the unit a dataset is published in. The test also checks that each
  allow-listed file still contains unit text, and that check caught the scan matching a build-output
  folder of the same name and checking nothing.
- **Switch accessible name** is "US units (°F, inches and square miles)", so it starts with the
  visible label (WCAG 2.5.3).

### Actual change size

**Projects:** `ClimateExplorer.Web.Client`, `ClimateExplorer.Web` (DI only), `ClimateExplorer.UnitTests`.
Core, WebApi and WebApiClient are unchanged.

| Area | Added | Changed | Removed | Lines |
|---|---|---|---|---|
| Client | 12 | 59 | 0 | +827 new; +732/−490 in changed files (~150 moved out of `ChartView`) |
| Server (`Program.cs`) | 0 | 1 | 0 | ~2 |
| Tests | 10 | 2 | 0 | +1,312 new (incl. `UnitPreferenceTests`); ~4 changed |
| **Total** | **22** | **62** | **0** | **~3,360 lines touched** |

Larger than estimated (20 added, ~48 changed, ~2,100 lines). Trend rates are formatted in more places
than the plan counted (chart trend panel, notification builder, `TrendStatSubject`). The recent
observations calculator needed the formatter passed through ten builder methods. And more small
components show values than were listed (extreme years, climate stripe info, series description).

### Follow-ups

- Check the switch at 768–1024px beside the CO₂ tile and the nav links. If it crowds, hide the "US
  units" text at tablet width and keep the track plus accessible name.
- If the metric flash on full page loads for US customary readers proves annoying, move the choice
  to a cookie (see [Persistence and flicker](#persistence-and-flicker)); only `UnitPreference` and
  the two `Program.cs` registrations change.
