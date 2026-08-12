# Chart series trend module

- **Date:** 2026-08-11
- **Status:** Implemented 2026-08-12 (see addendum)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Web.Client` — `UiModel/ChartSeriesDefinition`, `UiModel/SeriesWithData`,
  `Services/Chart/ChartDataBuilder`, `UiLogic/ChartLogic`, `UiLogic/ChartOptionsFactory`,
  `UiLogic/ChartSeriesListSerializer`, `Components/Chart/ChartSeriesView`,
  `Components/Chart/ChartView`, new `Components/Chart/Trend/*`, and the
  `Services/RecentObservations/TrendFormatting` + `AboutTrends/TrendStatSectionBuilder`
  pair which get generalised for a second consumer. No changes to `ClimateExplorer.Core`
  regression internals.
- **Builds on:** [Recent Observations trend tab](2026-07-16-01-recent-observations-trend-tab-plan.md),
  [About-trends button](2026-07-20-01-about-trends-button-plan.md)
- **Branch context:** `development`

## Goal

Add a per-series "trend" module to the chart: the user picks one of three fixed trend
periods, the regression for that period is fitted using the same engine and the same
significance rule as the Recent Observations trend tab, and — if it is significant — a
**separate, forward-projecting chart series** is drawn starting the year after the last
real data point and running for a user-specified number of years. If it is not
significant, nothing is drawn and a `UserNotification` explains why.

## Reused code, and exactly what it already does

Everything below is existing behaviour that this plan adopts unchanged, so the chart and
the Recent Observations tile describe a trend identically.

| Piece | Where | What it gives us |
| --- | --- | --- |
| `LinearRegressionCalculator.Calculate` | [LinearRegressionCalculator.cs:10](../../ClimateExplorer.Core/Stats/LinearRegressionCalculator.cs#L10) | OLS fit + fit stats + significance, `alpha = 0.05` default. Used as-is. |
| `LinearRegressionCalculator.Predict` | [LinearRegressionCalculator.cs:23](../../ClimateExplorer.Core/Stats/LinearRegressionCalculator.cs#L23) | For any X: `PredictedY`, `MeanConfidenceInterval`, `ObservationPredictionInterval`. This is the projection engine — no new maths needed. |
| `TrendWindowCalculator.Calculate` | [TrendWindowCalculator.cs:9](../../ClimateExplorer.Core/Stats/TrendWindowCalculator.cs#L9) | Fits all three windows in one call and returns `null` when there are too few points. |
| `TrendFormatting` | [TrendFormatting.cs](../../ClimateExplorer.Web.Client/Services/RecentObservations/TrendFormatting.cs) | Shared vocabulary: per-decade rate, "No significant trend", p-value formatting. |
| `TrendStatSectionBuilder` | [TrendStatSectionBuilder.cs](../../ClimateExplorer.Web.Client/Components/RecentObservations/Tab/AboutTrends/TrendStatSectionBuilder.cs) | The full GraphPad-style stats breakdown rendered by `TrendStatTable`. |
| `SidePanel`, `TrendsOverviewExplainer`, `TrendStatTable` | `Components/Common`, `Components/.../AboutTrends` | The slide-out panel and its overview/stat rendering. |
| `UserNotification` + `ChartDataBuildResult.Messages` | [ChartDataBuildResult.cs:27](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuildResult.cs#L27), [ChartablePage.cs:148](../../ClimateExplorer.Web.Client/Pages/ChartablePage.cs#L148) | The chart pipeline **already** returns notifications in the build result and `ChartablePage` already pumps them into `IUserNotificationService`. The trend module adds messages to that same list — no new mechanism. |

### The three periods — defined explicitly

`TrendWindowCalculator` orders the points by X and produces:

1. **Full period** — `HistoricalTrend`: every point.
2. **Last 30 years** — `RecentTrend`: `ordered.TakeLast(Math.Min(30, count))`. The window
   size is the caller's `recentWindowSize`; Recent Observations passes
   `RecentTrendWindowYears = 30` ([RecentObservationsCalculator.cs:19](../../ClimateExplorer.Web.Client/Services/RecentObservations/Calculator/RecentObservationsCalculator.cs#L19)).
3. **Early period** — `FirstHalfTrend`: `ordered.Take(count / 2)`.
   **This is the first half of the available data *points*, not the first half of the
   calendar span and not "the first 30 years".** Integer division, so with an odd number
   of points the middle point belongs to neither half. The UI label already in use for
   this is "Early period" ([AboutTrends.razor:34](../../ClimateExplorer.Web.Client/Components/RecentObservations/Tab/AboutTrends/AboutTrends.razor#L34)); the
   internal enum member is `TrendWindow.FirstHalf`. The chart module uses the same enum
   and the same three labels — "Full period", "Last 30 years", "Early period".

### The significance rule — defined explicitly

`RegressionSignificance.IsSlopeSignificant` is `pValue < alpha` with `alpha = 0.05`
([LinearRegressionCalculator.cs:228](../../ClimateExplorer.Core/Stats/LinearRegressionCalculator.cs#L228)),
where the p-value is the two-tailed t-test on the slope with `n - 2` degrees of freedom.
The chart module tests exactly this flag. No new threshold, no per-period variation.

### The minimum-data rule — defined explicitly

Recent Observations refuses to fit anything below
`AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly = 60` points, with a specific
explanatory message ([RecentObservationsCalculator.Trend.cs:60-75](../../ClimateExplorer.Web.Client/Services/RecentObservations/Calculator/RecentObservationsCalculator.Trend.cs#L60-L75)).
The chart module adopts the same 60-point minimum and reuses the wording of that message
in the `UserNotification` it raises. This is deliberately strict — many charted series
will not qualify, and that is the same answer the tile gives for the same data.

## Decisions on the open questions

**Where does trend state live?**
Split by input vs. derived:

- **Input (user intent)** lives on `ChartSeriesDefinition` — two new properties,
  `TrendWindow? TrendPeriod` (null = no trend) and `int TrendPredictionYears`. That type is
  the single serialised unit of chart state: it flows into `ChartState`, into the URL, into
  `ChartDataBuilder.BuildAsync`, and it is what clone/duplicate/location-substitution copy.
  Putting trend intent anywhere else would mean a trend that survives neither a page reload
  nor a "duplicate series". This mirrors how the existing `ShowTrendline` flag is carried.
- **Derived (computed results)** lives on `SeriesWithData` — a new
  `ChartSeriesTrend? Trend` property, produced fresh by `ChartDataBuilder` on every build
  and never serialised. It carries all three fitted regressions (the About panel needs all
  three regardless of which is displayed) plus the projected points for the selected one.

Nothing trend-related is stored on `ChartView`; the component only renders what the build
result hands it.

**When are the trends calculated, and what does the dropdown offer?** *(revised 2026-08-12)*
All three windows are fitted in one step, and that same step decides what the dropdown can
offer. The flow is:

1. The user ticks **Show trend** on a series — this is the "request" that triggers
   calculation. A checkbox is needed because there is otherwise a chicken-and-egg: the
   dropdown cannot list only the *available* periods until the periods have been fitted, and
   fitting every series on every page load would fire warnings about trends nobody asked for.
   It sits directly beside the existing "Show fitted line" checkbox and matches it.
2. `ChartDataBuilder` fits all three windows once and, in the same pass, emits the
   `UserNotification` for every window that is unavailable or not significant.
3. The dropdown is populated **only with the windows that came back significant**. An
   unavailable period is not a selectable option — the notification raised in step 2 is where
   its absence is explained, and the About-trends panel carries its full statistics.
4. The first available window is auto-selected in priority order **Full period → Last 30
   years → Early period**, so ticking the checkbox immediately draws a trend rather than
   leaving the user on an empty dropdown. If a previously-selected window stops being
   available (a location switch, a changed year filter), the selection falls back the same
   way and the notification explains the change.
5. **When none of the three are significant**, there is nothing to select: the dropdown is
   disabled and reads "No significant trend", the years-to-predict input is disabled, no
   series is drawn, and a single notification states that none of the three periods produce
   a significant trend for this series. The About-trends button stays enabled — the panel
   still shows all three periods' full statistics, which is where "why not?" is answered.

Notifications are therefore raised only for series whose trend module the user has switched
on, and at most once per build per series.

**Can more than one trend period be displayed at once?**
No — mutually exclusive, one trend series per parent series, single-select dropdown.
Switching the selection **replaces** the rendered trend series. Rationale: the periods
overlap heavily (the full period contains both others), so overlaying them produces three
near-parallel lines in three shades of one colour with no way to tell them apart; and each
would need its own colour offset and legend entry. All three are still *computed* on every
build — the fit is cheap next to the data fetch, and the About panel needs them all — so
switching the dropdown is a re-render, not a re-fit. `TrendWindow?` can become a
`HashSet<TrendWindow>` later without touching the calculation layer if this is revisited.

**What happens when "years to predict" changes after a trend is displayed?**
Recalculate live, on commit. The regression itself does not depend on the year count — only
the projected point list does — so no re-fit and no re-significance-test happens, and the
user is never re-notified about significance for a change that could not affect it. The
input commits on blur/Enter (`Immediate="false"`, matching the custom-transformation
`TextInput` at [ChartSeriesView.razor:157](../../ClimateExplorer.Web.Client/Components/Chart/ChartSeriesView.razor#L157)),
and an invalid value leaves the chart untouched and shows a validation error — same pattern
as `ValidateCustomTransformation`. No separate "apply" button. Note this *does* force a full
chart rebuild because the x-axis bin/label array changes length (see Phase 3).

**Years to predict — bounds and default.**
Default **20**, minimum **1**, maximum **100**. 100 is the ceiling because the stats panel
already frames extrapolation at the century scale ("a change of about X per century",
[TrendStatSectionBuilder.cs:40](../../ClimateExplorer.Web.Client/Components/RecentObservations/Tab/AboutTrends/TrendStatSectionBuilder.cs#L40)),
and because the observation prediction interval widens with distance from `MeanX` — past a
century the band is wide enough that the line implies precision the data cannot support.
20 as the default keeps the projection visually secondary to the record. Values arriving
from a URL are clamped into range rather than rejected, so an edited link never breaks the
chart.

**Trend colour transform — deterministic, per series.**
Derived from the parent series' assigned hex (`ChartSeriesDefinition.Colour`, set by
`ColourServer`), in HSL:

```
h' = h                                   (hue unchanged — it reads as "the same series")
s' = max(s * 0.55, 0.20)                 (desaturated)
l' = l <= 0.5 ? min(l + 0.22, 0.92)      (moved away from mid-lightness,
                : max(l - 0.22, 0.08)     so it separates from the parent in both themes)
```

Black (`#000000`, s = 0, l = 0) becomes a dark grey rather than an off-hue colour, which is
why saturation is scaled rather than fixed. Colour alone is not the only cue: the trend
dataset is also drawn dashed (`BorderDash = [6, 4]`) and thinner (`BorderWidth = 3` vs. the
parent's 5), so the projection is still distinguishable in greyscale or with colour-vision
deficiency, and "dashed" reads as "not measured". Implemented as a pure static
`TrendSeriesColour.Derive(string parentHexColour) → string` in `UiLogic`, unit-tested for
determinism.

**Prediction interval storage.**
Store the whole `RegressionPrediction` per projected year rather than inventing a narrower
record: it already carries `X`, `PredictedY`, `MeanConfidenceInterval`,
`ObservationPredictionInterval` and `Alpha`. The future area-chart overlay can then choose
either band without a model change, and nothing new needs testing. Only `PredictedY` is
rendered in this piece of work.

## Phases

### Phase 1 — Generalise the shared trend vocabulary (no behaviour change)

`TrendFormatting` and `TrendStatSectionBuilder` are currently typed to
`RecentObservationTrendViewModel`. Loosen them so a chart series can be a second consumer,
without inventing a parallel stats vocabulary.

- Introduce `TrendStatSubject(string Label, string Unit)` and change
  `TrendStatSectionBuilder.Build(RecentObservationTrendViewModel, LinearRegressionResult)`
  to `Build(TrendStatSubject subject, LinearRegressionResult trend, IReadOnlyList<DataPoint> points)`.
  The private `GetPoints(metric, trend)` reference-equality lookup
  ([TrendStatSectionBuilder.cs:335-348](../../ClimateExplorer.Web.Client/Components/RecentObservations/Tab/AboutTrends/TrendStatSectionBuilder.cs#L335-L348))
  disappears in favour of the explicit `points` parameter — a simplification for the
  existing caller too.
- Extend `TrendFormatting` beyond its hard-coded `°C`/`mm` branches
  ([TrendFormatting.cs:24-38](../../ClimateExplorer.Web.Client/Services/RecentObservations/TrendFormatting.cs#L24-L38)).
  Chart series carry units the tile never sees (days, ppm, unitless custom transformations).
  Add a decimal-places lookup keyed on unit label, defaulting to 2 dp with the label
  appended; `°C` (2 dp) and `mm` (0 dp) keep their current output byte-for-byte.
- Move both files to `Services/Trends/` (namespace `ClimateExplorer.Web.Client.Services.Trends`)
  now that they have two consumers. Mechanical; `TrendFormatting` stays `internal`.

**Files:** `Services/RecentObservations/TrendFormatting.cs` → `Services/Trends/TrendFormatting.cs`;
`Components/.../AboutTrends/TrendStatSectionBuilder.cs` → `Services/Trends/TrendStatSectionBuilder.cs`;
new `Services/Trends/TrendStatSubject.cs`; callers `AboutTrends.razor.cs`,
`RecentObservationsCalculator.Trend.cs`.

**Verification:** existing Recent Observations trend tests must pass unchanged — this phase
is behaviour-preserving by construction.

### Phase 2 — Trend state and the calculation service

- `ChartSeriesDefinition`: add `TrendWindow? TrendPeriod` and `int TrendPredictionYears = 20`.
  Add both to **both** equality comparers — `BaseComparer`
  ([ChartSeriesDefinition.cs:313](../../ClimateExplorer.Web.Client/UiModel/ChartSeriesDefinition.cs#L313))
  and the two `GetHashCode` overrides at lines 414 and 467. Omitting them would let
  `CreateNewListWithoutDuplicates` silently collapse two series that differ only by trend.
- New `Services/Trends/ChartSeriesTrendCalculator` (static, pure, unit-testable):
  - input: the ordered `(year, value)` points of one series, the selected `TrendWindow`,
    and the prediction year count;
  - runs `TrendWindowCalculator.Calculate(points, 60, 30)`;
  - returns `ChartSeriesTrend`: the three `LinearRegressionResult`s, the point list per
    window (for the About panel's Data section), the selected window, an
    `IReadOnlyList<RegressionPrediction> Projections` for the selected window, and an
    `UnavailableReason`/`NotSignificantReason` string where applicable;
  - `Projections` is `Enumerable.Range(lastYear + 1, n).Select(y => LinearRegressionCalculator.Predict(trend, y))`.
  - When the selected window's `IsSlopeSignificant` is false, `Projections` is empty and
    `NotSignificantReason` is populated.
- New `UiModel/ChartSeriesTrend.cs`, and `SeriesWithData` gains `ChartSeriesTrend? Trend`.

**Assumption:** the regression is fitted over the **plotted** series — the gap-filled
`ProcessedDataSet` records restricted to the chart's bins — not the untrimmed source record.
Rationale: everything the user can see and reason about is on the chart, and a regression
drawn from years that are not plotted cannot be explained by the chart itself; it also makes
"the year after the last real data" unambiguous. Consequence to accept: with a start/end year
filter active, "Full period" means the full *plotted* period. With the default "chart all
data" behaviour the two coincide.

**Files:** `UiModel/ChartSeriesDefinition.cs`, `UiModel/SeriesWithData.cs`,
new `UiModel/ChartSeriesTrend.cs`, new `Services/Trends/ChartSeriesTrendCalculator.cs`.

### Phase 3 — Wire trends into the data build (including the future bins)

All of this lands in `ChartDataBuilder.BuildProcessedDataSets`, which already owns bin
construction, gap filling and the `messages` list.

- Restrict to `BinGranularities.ByYear`. The regression's X is a calendar year and the
  projection is one value per year; monthly/daily/modular granularities have no such axis.
  When a series has a `TrendPeriod` set at another granularity, skip it silently in the UI
  (the module is hidden — see Phase 5) and, if it arrives from a URL, add one informational
  `UserNotification` saying trends are available on yearly charts only.
- After `chartBins = BinHelpers.EnumerateBinsInRange(...)`
  ([ChartDataBuilder.cs:332](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuilder.cs#L332)),
  append `new YearBinIdentifier(lastYear + i)` for `i` in `1..maxPredictionYears` across all
  series with an active, significant trend. Everything downstream then works unmodified: the
  gap-fill loop already builds each dataset from `chartBins` with null records for bins that
  have no data, and `binIdsToPlot` is derived from `chartBins`.
- **Leave `chartEndBin` at the real data end.** It feeds the chart subtitle
  ("1910-2024, 114 years", [ChartLogic.cs:44](../../ClimateExplorer.Web.Client/UiLogic/ChartLogic.cs#L44));
  extending it would report projected years as years of record.
- Run `ChartSeriesTrendCalculator` per series, assign `SeriesWithData.Trend`, and for each
  non-significant or unavailable trend add a `UserNotification` to `messages`.

**Notification content** (`NotificationType.Warning`, `LocationName` set from the series'
geographical entity, so it groups like the existing chart warnings):

> **Hobart, Maximum temperature:** no trend line was added for the **last 30 years**
> (1995–2024). The fitted rate is +0.21°C /decade, but p = 0.0912, above the 0.05 threshold
> this site uses — the year-to-year scatter is too large relative to the number of years for
> this to be distinguished from no trend at all. Open **About trends** for the full statistics.

Period label, year range, fitted rate and p-value all come from `TrendFormatting` and
`FormatYearRange`, so the wording matches the tile's tooltips.

**Files:** `Services/Chart/ChartDataBuilder.cs`, `Services/Chart/ChartDataBuildResult.cs`
(no change needed — `Messages` already exists).

### Phase 4 — Render the trend series

- `ChartLogic`: new `GetTrendChartDataset(label, values, ChartColor, ...)` — a line dataset
  with `BorderDash = [6, 4]`, `BorderWidth = 3`, `PointRadius = 0`, `Fill = false`, and the
  **same `YAxisID` as its parent series** so it shares the parent's axis and scale.
- `ChartView.AddDataSetsToChart`: after the existing loop completes, append one trend dataset
  per series that has `Trend.Projections` non-empty. **Appending after the loop is required,
  not stylistic** — `ChartLogic.CreateTrendline(dataSetIndex, ...)` for the existing
  `ShowTrendline` overlay addresses datasets by index
  ([ChartView.razor.cs:482-487](../../ClimateExplorer.Web.Client/Components/Chart/ChartView.razor.cs#L482-L487)),
  so interleaving trend datasets would silently point those overlays at the wrong series.
- Value array: `null` for every historical bin, then `PredictedY` for each projected year, so
  the dataset aligns with the shared label array.
- Legend label: `"{parent short title} | {period label} trend"`.
- **Y-axis range:** `ChartOptionsFactory.CalculateAxisMinMax` currently derives min/max from
  `PreProcessedDataSet` values only ([ChartOptionsFactory.cs:42-46](../../ClimateExplorer.Web.Client/UiLogic/ChartOptionsFactory.cs#L42-L46)),
  and the axis `Min`/`Max` are set explicitly. A projection that rises above the historical
  maximum would be clipped. Fold `Trend.Projections` (`PredictedY` now; the interval bounds
  too once the band is rendered) into the same min/max accumulation.
- **Click handling:** `OnLineChartClicked` maps `e.Index` to `startYear + e.Index` and raises
  a year filter ([ChartView.razor.cs:521-536](../../ClimateExplorer.Web.Client/Components/Chart/ChartView.razor.cs#L521-L536)).
  Guard against indices in the projected range — a click there must be a no-op, not a filter
  request for a year with no data.

**Files:** `UiLogic/ChartLogic.cs`, `UiLogic/ChartOptionsFactory.cs`,
`Components/Chart/ChartView.razor.cs`.

### Phase 5 — The trend module UI

In the right-hand `edit-col` of `ChartSeriesView.razor`, grouped under a "Trend" sub-heading,
following the existing `DelayedTooltip` + `form-row` + `form-label` + `form-control-wrap`
pattern used by every other control there. Each control gets an explicit `aria-label`
(AGENTS.md requires an accessible name).

1. **Period** — Blazorise `Select TValue="TrendWindow?"`, `Size.Small`, options: *None*,
   *Full period*, *Last 30 years*, *Early period*. Selecting one triggers
   `OnSeriesChanged` → full rebuild.
2. **Years to predict** — `TextInput` wrapped in `Validation` (min 1, max 100, integer),
   `Immediate="false"`, with a `<Feedback><ValidationError>` message, disabled when no period
   is selected.
3. **About trends** — a `series-control` button in the existing `series-controls` column
   (alongside About/Clone/Remove), opening the side panel from Phase 6.

The whole group is hidden when `BinGranularity != ByYear`.

**Relationship to the existing "Show trendline" checkbox:** it stays, but is relabelled
**"Show fitted line"** to end the collision with the new module. It is a different thing — a
Blazorise overlay fitted across the plotted data with no significance test and no projection
— and it is referenced by chart presets
([SuggestedPresetLists.LocationBased.cs:95](../../ClimateExplorer.Web.Client/UiModel/SuggestedPresetLists.LocationBased.cs#L95)),
so removing it is out of scope here. Flagging for a later decision: once the trend module
exists, the fitted line could be folded into it as a "draw the fit across the record too"
toggle, and `ShowTrendline` retired.

**Files:** `Components/Chart/ChartSeriesView.razor`, `ChartSeriesView.razor.cs`,
`ChartSeriesView.razor.css`.

### Phase 6 — The About trends side panel for chart series

New `Components/Chart/Trend/ChartTrendPanel.razor` + `.razor.cs`, closely modelled on
`AboutTrends.razor` and composed from the same parts:

- `SidePanel` titled "About trends", `Width="min(60%, 1024px)"`.
- `TrendsOverviewExplainer` for the overview text — reused verbatim, so the explanation of
  what a trend is stays in one place.
- The same three-button window toggle markup (`recent-observation-detail-toggle`) with
  *Full period* / *Last 30 years* / *Early period*.
- `TrendStatTable` fed by `TrendStatSectionBuilder.Build(subject, trend, points)` from
  Phase 1.

Unlike the tile's version there are no metric tabs — the chart panel is scoped to the single
series it was opened from. **Stats for all three periods are shown regardless of significance**,
including the period currently rendered on the chart and the ones that were rejected; the
`Is slope significantly non-zero?` section already states "Significant"/"Not significant"
plainly, so a rejected period explains itself here.

The ~20 lines of toggle markup are duplicated from `AboutTrends.razor` rather than extracted.
Extracting a shared component for two call sites with different surrounding structure (tabs
vs. no tabs, download button vs. none) would cost more indirection than it saves; revisit at
a third consumer.

**Files:** new `Components/Chart/Trend/ChartTrendPanel.razor`, `.razor.cs`, `.razor.css`.

### Phase 7 — Persistence and propagation

- `ChartSeriesListSerializer`: append `TrendPeriod` and `TrendPredictionYears` as segments
  **19 and 20** in `BuildChartSeriesUrlComponent`, and parse them **tolerantly** —
  `segments.Length > 19 ? ... : default`. The parser is positional
  ([ChartSeriesListSerializer.cs:84-113](../../ClimateExplorer.Web.Client/UiLogic/ChartSeriesListSerializer.cs#L84-L113)),
  so every already-shared ClimateExplorer link has exactly 19 segments and would throw on a
  naive `segments[19]`. Clamp `TrendPredictionYears` into 1–100 on parse.
- `ChartSeriesLocationSubstitutionService`: copy both new properties in the series-rebuild at
  [line 187](../../ClimateExplorer.Web.Client/Services/Chart/ChartSeriesLocationSubstitutionService.cs#L187),
  so switching location keeps the user's trend selection. The trend is refitted against the
  new location's data — including its significance test — which is correct.

**Files:** `UiLogic/ChartSeriesListSerializer.cs`,
`Services/Chart/ChartSeriesLocationSubstitutionService.cs`.

### Phase 8 — Tests

New/updated tests in `ClimateExplorer.UnitTests`, names per AGENTS.md
(`MethodName_StateUnderTest_ExpectedBehavior`):

- `ChartSeriesTrendCalculatorTests` — projection starts at `lastYear + 1`; produces exactly
  N points; `PredictedY` matches `Line.Predict`; each point carries both intervals; a
  non-significant window yields no projections and a populated reason; fewer than 60 points
  yields the unavailable reason; the early-period window matches
  `TrendWindowCalculator`'s `FirstHalfTrend` for the same input (guards the definition
  against drift).
- `TrendSeriesColourTests` — determinism, distinctness from the parent, correct handling of
  black and of fully-saturated inputs.
- `ChartSeriesListSerializerTests` — round-trip of the new fields; a legacy 19-segment
  string still parses; out-of-range year counts clamp.
- `ChartSeriesDefinitionTests` — two definitions differing only by `TrendPeriod` are not
  equal under either comparer.
- Existing `TrendWindowCalculatorTests`, `LinearRegressionCalculatorTests` and the Recent
  Observations tests must pass unchanged.

Verification is `dotnet build` plus the unit test suite only — no dev server, no browser
tests (AGENTS.md).

## Out of scope

- Rendering the upper/lower prediction band as an area chart. The data is computed and
  carried on `ChartSeriesTrend.Projections`; only the visual is deferred.
- Any change to `LinearRegressionCalculator`'s internals.
- Retiring the existing `ShowTrendline` / "Show fitted line" overlay.
- Trends on non-yearly bin granularities.

## Assumptions

1. Trends are fitted over the **plotted** series (post-smoothing, post-secondary-calculation,
   within the chart's bin range), not the untrimmed source record — see Phase 2.
2. The 60-point minimum from Recent Observations applies unchanged, which means many shorter
   chart series will get a "not enough data" notification rather than a trend. Chosen for
   consistency over availability; a chart-specific lower minimum would be a deliberate
   divergence and is not proposed here.
3. Smoothed series are fitted as smoothed. A moving average removes year-to-year scatter, so
   the p-value on a smoothed series is optimistic relative to the raw data — the same caveat
   the site already carries wherever smoothing and statistics meet, and not something this
   plan changes. Worth a sentence in the About panel's overview.
4. `alpha` stays at the calculator's 0.05 default throughout; it is not exposed to the user.
5. One trend series per parent series; a chart with four series can show four trends, one each.

## Addendum — implementation notes (2026-08-12)

Shipped as planned, with the revised availability-driven flow described above. Deviations and
details worth recording:

### The "Show trend" checkbox

The plan had the dropdown alone as the entry point. Making the dropdown list only *available*
periods creates a chicken-and-egg — availability isn't known until the windows are fitted — and
fitting every series on every page load would raise warnings about trends nobody asked for. A
`ShowTrend` checkbox resolves both: it is the explicit request that triggers fitting, gates the
notification so page loads stay silent, and gives the all-unavailable case somewhere to be
reported. It sits next to the existing checkbox, which was relabelled "Show fitted line".

### Colour transform simplified

The planned saturation floor (`max(s * 0.55, 0.20)`) was dropped in favour of plain
`s' = s * 0.55`. The floor would have given an achromatic parent (black `#000000`, grey `#666666`)
a saturation of 0.20 against an undefined hue of 0 — turning black into dark red. Scaling alone
keeps greys grey by construction and never binds for the real palette, whose chromatic entries all
land between 0.22 and 0.55 after scaling. `TrendSeriesColourTests` locks this in.

### Statistics builder generalisation

`TrendStatSectionBuilder.Build` took an extra parameter beyond the plan: both the window's own
points *and* the full-period points. The "in {this year} it predicts X, the measured value was Y"
worked example needs the current year's actual value, which isn't in the early-period window. Its
private reference-equality point lookup went away as a result. `TrendFormatting` grew unit-aware
decimal places (°C and mm keep their existing output byte-for-byte; ppm/ppb get 1 dp; anything else
2 dp with a space before the unit).

### Files moved

Now that two features share them, these moved to neutral homes:
`Services/Trends/` (TrendFormatting, TrendStatSectionBuilder, TrendWindowLabel, and the new
calculator/notification builder), `UiModel/Trends/` (TrendWindow, TrendStatRow, TrendStatSection,
TrendStatSubject, and the new trend models), `Components/Common/Trends/` (TrendStatTable,
TrendsOverviewExplainer, SlopeFormulaFigure). The `.trends-*` CSS moved out of
`RecentObservationTile.razor.css`, where it was scoped behind `.recent-observation-tile ::deep`,
into `app.css` under a `.trend-stats` wrapper class that both panels now apply — the chart panel
could not otherwise pick up any of it. Declarations are unchanged; only the ancestor selector
differs.

### Two things found during implementation, beyond the four called out in the plan

- **CSV export.** `OnDownloadDataClicked` passed `ChartBins` straight through, so the projected
  bins would have appended rows of empty values to every export. It now filters to bins at or
  before `chartEndBin`.
- **`TrendWindowLabel`.** Window names were about to exist in three places (the chart dropdown, the
  chart panel, the notifications) on top of the literals already in `AboutTrends.razor`. They are
  now generated from one helper, and `AboutTrends.razor` was switched over to it so the two panels
  cannot drift.

### Not done

The prediction interval bounds are computed and carried (each projected year holds a full
`RegressionPrediction`, with both the mean confidence interval and the wider observation prediction
interval) but nothing renders them — as scoped. `ShowTrendline` was kept and relabelled rather than
retired.

### Verification

`dotnet build` on the solution is clean, and the full unit suite passes at 483 tests, including
~35 new ones across `ChartSeriesTrendCalculatorTests`, `TrendSeriesColourTests`,
`ChartSeriesTrendNotificationBuilderTests` and `ChartSeriesListSerializerTrendTests`. Per
AGENTS.md, no dev server or browser testing was run — the rendering path (Chart.js dataset
construction, the side panel, the new controls) is covered by compilation and by the unit-tested
logic behind it, not by visual verification.
