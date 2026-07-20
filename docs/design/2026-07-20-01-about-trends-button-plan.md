# "About trends" button and full-statistics modal

- **Date:** 2026-07-20
- **Status:** Proposed
- **Author:** Claude
- **Scope:** `ClimateExplorer.Core/Stats` (two new derived-statistics methods), `ClimateExplorer.Web.Client/Components/RecentObservations/Tile/RecentObservationTrend.razor*` (one new grid item, no other changes), `ClimateExplorer.Web.Client/UiModel/RecentObservations/RecentObservationTrendViewModel.cs`, `ClimateExplorer.Web.Client/Services/RecentObservations/RecentObservationsCalculator.cs`, `ClimateExplorer.Web.Client/Components/Common/ClimateButton*`, `ClimateExplorer.Web.Client/Services/Exporter.cs`/`IExporter.cs`, `ClimateExplorer.Web.Client/Components/RecentObservations/RecentObservationsPanel.razor*`
- **Builds on:** [Linear Regression Utility](2026-07-11-01-linear-regression.md), [Recent Observations: Trend expanded tab](2026-07-16-01-recent-observations-trend-tab-plan.md)
- **Branch context:** `development`

## Goal

Add a single **"About trends"** button to the existing **Trend** tab of a
Recent Observation tile (`RecentObservationTrend.razor`) that opens a large
modal giving a full, GraphPad-style statistical breakdown of any of that
tile's temperature/precipitation trends — abstract meaning, climate-specific
meaning, a worked example using the calculation's own numbers, and a
"download data" button so a user could reproduce the regression
independently.

**No redesign of the existing Trend tab is wanted or required.** Its current
`<dl>` of per-metric rows (full period / last 30 years / early period, all
inline) stays exactly as it is. The only change to that tab is the addition
of the About button. Everything the task describes as "the trends tab for
temperature" (the row/column grid position) refers to that existing `<dl>`,
not a new layout. All of the two-level tab navigation the task asks for
(Overview / Mean / Max / Min, then Full recordset / Last 30 years / First
half of recordset) lives **inside the modal**, not on the tile.

## Investigation summary

### `RecentObservationTrend.razor` is the trends tab

The component tree, outside-in:

- `RecentObservationsPanel.razor:9` renders `RecentObservationTabs`, the
  existing Temperature/Precipitation switch (`RecentObservationTabs.razor:6-11`,
  a Blazorise `<Tabs>` pair). "The trends tab for temperature" means: any tile
  shown while this switch is on Temperature. "Precipitation" means: any tile
  shown while it's on Precipitation. Nothing here changes.
- Below that, `RecentObservationsPanel.razor:62-83` lays out a
  `.recent-observation-grid` of `RecentObservationTile` cards — one per period
  (Today, Latest 7 days, This month, previous months/seasons/years, ...).
  Each tile is capped at `max-width: 500px`
  (`RecentObservationTile.razor.css:6`).
- Expanding a tile reveals `.recent-observation-detail`
  (`RecentObservationTile.razor:14-49`), which — when the tile has more than
  one available tab — shows a button group,
  `.recent-observation-detail-toggle`/`.recent-observation-detail-toggle-option`
  (`RecentObservationTile.razor:17-27`), to switch between whichever of
  Records / Variation / **Trend** the tile supports. Selecting Trend renders
  `<RecentObservationTrend Metrics="@trendTab.Metrics" />`
  (`RecentObservationTile.razor:47`).
- **`RecentObservationTrend.razor` is "the trends tab."** It renders a flat
  `<dl class="recent-observation-detail-metrics">`: one
  `.recent-observation-detail-metric` row per metric (3 rows for temperature —
  "Average max temp" / "Average min temp" / "Mean temperature", or
  "Maximum"/"Minimum"/"Mean" for daily tiles; 1 row for precipitation —
  "Precipitation"), each showing the full-period headline plus "Last 30
  years" and "Early period" lines inline, every line wrapped in its own
  `DelayedTooltip` giving the exact year range and missing years (added in the
  tip commit, `2a0e031f8`). **This plan keeps every bit of this as-is** and
  adds one more item to the same grid.

### The existing grid this button drops into

`.recent-observation-detail-metrics` (`RecentObservationTile.razor.css:285-290,365-368`)
is a real CSS Grid already, confirmed from the stylesheet directly:

```css
.recent-observation-tile ::deep .recent-observation-detail-metrics {
    display: grid;
    gap: 0.6rem 0.8rem;
    grid-template-columns: 1fr;      /* default: single column (mobile) */
    margin: 0;
}

@media (min-width: 768px) {
    .recent-observation-tile ::deep .recent-observation-detail-metrics {
        grid-template-columns: repeat(2, minmax(0, 1fr));   /* 2 columns at 768px+ */
    }
}
```

This single fact resolves the task's row/column numbers exactly, with no new
grid, no new CSS, and no invented sibling content:

- **Temperature** tiles have 3 existing metric rows. Appending the About
  button as a **4th** `.recent-observation-detail-metric` item after them
  lands it at **row 2 / column 2** once the grid is 2 columns wide (≥768px),
  and at **row 4** when it collapses to 1 column below that (mobile) — both
  are ordinary CSS Grid auto-flow, nothing explicit needed.
- **Precipitation** tiles have exactly 1 existing metric row. **Prepending**
  the About button *before* it — rather than appending — makes it the grid's
  **1st** item, which is why it's **row 1 / column 1** and, unlike the
  temperature case, doesn't move between breakpoints (item 1 is always
  row 1/col 1, 1 column or 2). This also explains why the task only needed to
  give one position for precipitation and needed a separate mobile position
  for temperature.

**Implementation rule:** in `RecentObservationTrend.razor`, render the About
button's grid item before the `@foreach` when `Metrics.Count == 1`
(precipitation shape) and after it otherwise (temperature shape). No new CSS
class or breakpoint is needed — the existing grid already does the rest.

### Where the "About" button pattern already exists

`ChartSeriesView.razor:219-224` — the button this task says to copy:

```razor
<DelayedTooltip Text="About this data" Style="display: inline-block">
    <button type="button" class="series-control" aria-label="About this data" @onclick="@OnAboutThisDataClicked">
        <i class="fas fa-info"></i>
        <div class="label">About</div>
    </button>
</DelayedTooltip>
```

`.series-control` (`ChartSeriesView.razor.css:150-164`) is `display:
inline-block; text-align: center` with the icon (`font-size: 18px`) followed
by a block-level `.label` div (`font-size: 11px; opacity: 0.7`) — the
icon-above-text look comes from ordinary block flow, not flexbox.
`ClimateButton` (the component this task requires instead of a raw `<button>`)
already renders `<i>` then `Text`/`ChildContent` in the same DOM order
(`ClimateButton.razor:7-18`), so the stacked look is achievable with a small
new CSS modifier rather than any change to `ClimateButton.razor`'s markup —
see [New CSS](#new-css-climate-buttonstacked).

The click target opens a large modal — `AboutData.razor` is the direct
precedent (`<Modal @ref="modal" Size="ModalSize.Large">` with `ModalHeader` /
`ModalTitle` / `CloseButton` / `ModalBody`, and a `Show()` method the parent
calls). Given how content-dense the full statistical breakdown below is
(14 rows across 6 sections, several with expandable explanation rows, times
up to 9 metric/window combinations), this plan uses `ModalSize.ExtraLarge`
instead — the same size `About.razor`'s pipeline modal already uses for dense
content — rather than `Large`.

### Where the modal's own two tab groups have patterns to copy

Both new tab groups live **inside the About modal**, not on the tile:

- **Group 1** (`Overview | Mean temperature | Max temperature | Min
  temperature`, or `Overview | Precipitation`) uses Blazorise `<Tabs>`,
  exactly like `RecentObservationTabs.razor:6-11` already does for the
  Temperature/Precipitation switch:

  ```razor
  <Tabs SelectedTab="@selectedMetricKey" SelectedTabChanged="@OnMetricTabChanged">
      <Items>
          <Tab Name="Overview">Overview</Tab>
          @foreach (var metric in Metrics)
          {
              <Tab Name="@metric.Label">@metric.Label</Tab>
          }
      </Items>
  </Tabs>
  ```

- **Group 2** (`Full recordset | Last 30 years | First half of recordset`,
  shown only once Group 1 is off Overview) reuses "the tab style on the
  expanded part of the tile" — i.e. the exact
  `.recent-observation-detail-toggle`/`.recent-observation-detail-toggle-option`
  markup and classes already used one level up for the Records/Variation/Trend
  switch (`RecentObservationTile.razor:17-27`):

  ```razor
  <div class="recent-observation-detail-toggle" role="group" aria-label="Time window">
      <button type="button" class="recent-observation-detail-toggle-option @(selectedWindow == TrendWindow.Full ? "selected" : string.Empty)" @onclick="@(() => SelectWindow(TrendWindow.Full))">Full recordset</button>
      <button type="button" class="recent-observation-detail-toggle-option @(selectedWindow == TrendWindow.Recent ? "selected" : string.Empty)" @onclick="@(() => SelectWindow(TrendWindow.Recent))">Last 30 years</button>
      <button type="button" class="recent-observation-detail-toggle-option @(selectedWindow == TrendWindow.FirstHalf ? "selected" : string.Empty)" @onclick="@(() => SelectWindow(TrendWindow.FirstHalf))">First half of recordset</button>
  </div>
  ```

  Because `AboutTrends` is rendered as a descendant component of
  `RecentObservationTile` (via `RecentObservationTrend`), Blazor's `::deep`
  scoped-CSS rules in `RecentObservationTile.razor.css` reach into it the same
  way they already reach into `RecentObservationTrend.razor`'s own markup — no
  CSS needs to be copied or duplicated, only the class names reused. This is
  worth a quick visual check once built, in case Blazorise's `Modal` does
  anything unusual with DOM placement that would interfere with scoped-CSS
  inheritance, but nothing in how Blazor scopes `::deep` styles (by component
  render-tree ancestry, not physical DOM location) suggests it would.

### Where the stats already exist — and are currently thrown away

`RecentObservationsCalculator.BuildTrendMetric`
(`RecentObservationsCalculator.cs:1032-1090`) already computes everything this
task's full breakdown needs, per metric, per window, then keeps only the
formatted display strings:

```csharp
var trendSet = points.Count >= AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly
    ? TrendWindowCalculator.Calculate(points, AnomalyCalculator.MinimumNumberOfYearsToCalculateAnomaly, RecentTrendWindowYears)
    : null;
...
var ordered = points.OrderBy(x => x.X).ToList();
var recentCount = Math.Min(RecentTrendWindowYears, ordered.Count);
var firstHalfCount = ordered.Count / 2;
```

`trendSet.HistoricalTrend`/`.RecentTrend`/`.FirstHalfTrend` are full
`LinearRegressionResult`s (input summary, best-fit line, fit, significance —
everything `LinearRegressionCalculator.Calculate` produces), and `ordered`/
`ordered.TakeLast(recentCount)`/`ordered.Take(firstHalfCount)` are the exact
`List<DataPoint>` slices each of those three results was computed from — i.e.
exactly the "relevant yearly slice" the task's download button needs. Both are
already sitting in local variables in this method; only the formatted
strings derived from them (`FormatTrendPerDecade`, `BuildYearRangeTooltip`)
currently survive into `RecentObservationTrendViewModel`. **No new data
plumbing, no new Core calculation calls, and no new data-sourcing is needed to
get the full breakdown or the download data — only carrying values that
already exist one step further.**

`RecentObservationTrendViewModel` needs three new `LinearRegressionResult?`
fields and three new point-list fields:

```csharp
public LinearRegressionResult? FullPeriodTrend { get; init; }
public LinearRegressionResult? RecentTrend { get; init; }
public LinearRegressionResult? FirstHalfTrend { get; init; }
public IReadOnlyList<DataPoint> FullPeriodPoints { get; init; } = [];
public IReadOnlyList<DataPoint> RecentTrendPoints { get; init; } = [];
public IReadOnlyList<DataPoint> FirstHalfTrendPoints { get; init; } = [];
```

populated directly from `trendSet.HistoricalTrend`/`ordered`, etc. in
`BuildTrendMetric` — a few extra lines in a method that already has every
value on hand, not a new calculation path.

### Where the expand-chevron pattern already exists

`RecentObservationMainPanel.razor:97-104`'s expand button
(`oi oi-chevron-right`, rotated 90° via `.expanded`,
`RecentObservationTile.razor.css:240-250`) is the chevron this task asks for.
This plan's per-stat expand row reuses the same icon/rotation convention.

### Where the "climate-specific worked example" pattern already exists

`WarmingAnomalyInfo.razor` is the precedent for "explain the abstract
statistic, then plug the location's own numbers into it":

```razor
<p>The warming anomaly is the temperature difference between ... </p>
<p>For example:</p>
<ul>
  <li>In the last 30 years of records at <strong>@Location?.Name</strong> ... the average temperature was <strong>...°C</strong>.</li>
  ...
</ul>
```

Every stat-row explanation in this plan follows this exact two-part shape
(plain-language definition, then "For example:" using this trend's own
numbers) rather than inventing a new explanatory format.

### Where the download pattern already exists

`IExporter`/`Exporter` (`ClimateExplorer.Web.Client/Services/Exporter.cs`)
already has the exact shape needed — `ExportClimateRecords`/`ExportChartData`
both build a `List<string>` of CSV lines, UTF-8-BOM-encode them into a
`MemoryStream`, and return a `Stream`. The caller
(`ClimateRecords.razor.cs:538-544`) wraps that in a `DotNetStreamReference` and
calls the existing JS interop function `downloadFileFromStream(fileName,
streamRef)`. A new `ExportTrendData` method follows this exactly — see below.

`RecentObservationTrend.razor.cs` currently has no injected services at all
(`RecentObservationTrend.razor.cs:6-11` — just the `Metrics` parameter), and
neither `Location` nor `Logger`/`NavigationManager`/`IJSRuntime` are available
at that depth today (`Location` stops flowing down at
`RecentObservationsPanel`; `RecentObservationTile` doesn't take it either).
Rather than thread four new parameters/injections down two more component
levels, the download action should **bubble up** the same way
`OnExpansionChanged`/`OnGroupSelected` already do
(`RecentObservationTile.razor.cs:26,29`): a new
`EventCallback<TrendDownloadRequest>` from `RecentObservationTrend`, relayed
unchanged through `RecentObservationTile`, handled at
`RecentObservationsPanel` — which already has `Location`/`Logger` and only
needs `IExporter`/`IJSRuntime`/`NavigationManager` added, the same three
things `ClimateRecords.razor.cs` already injects for its own download button.

## Design

### The About button

New reusable wrapper, `TrendsAboutButton.razor` (thin, so the
markup/tooltip/CSS trio below is written once, not duplicated between
temperature and precipitation tiles):

```razor
<DelayedTooltip Text="About this trend">
    <ClimateButton Icon="fas fa-info" Class="climate-button--stacked trends-about-button" OnClick="@OnClick" AriaLabel="About this trend">
        <span class="label">About trends</span>
    </ClimateButton>
</DelayedTooltip>
```

`OnClick` takes no arguments and simply shows the modal — since the modal
covers all of a tile's metrics/windows via its own internal tab navigation
(see below), the button doesn't need to know or pass along "which one was on
screen."

#### New CSS: `.climate-button--stacked`

A new modifier alongside the existing `.climate-button--compact`
(`ClimateExplorer.Web\wwwroot\css\app.css:159-191`), matching `.series-control`'s
icon-above-label look:

```css
.climate-button--stacked {
    display: inline-block;
    text-align: center;
    flex-direction: column;
}

.climate-button--stacked .label {
    display: block;
    font-size: 11px;
    opacity: 0.85;
    margin-top: 2px;
}
```

`ClimateButton` renders `Text` as a bare text node when no `ChildContent` is
supplied (`ClimateButton.razor:17`), so `TrendsAboutButton` passes the label
via `ChildContent` (wrapped in `<span class="label">`, as shown above) rather
than `Text`, so the existing component needs no structural change.

#### Placement

`RecentObservationTrend.razor`'s only markup change — everything else in that
file is untouched:

```razor
<dl class="recent-observation-detail-metrics">
    @if (Metrics.Count == 1)
    {
        <div class="recent-observation-detail-metric recent-observation-trend-about">
            <TrendsAboutButton OnClick="@ShowAboutTrends" />
        </div>
    }
    @foreach (var metric in Metrics)
    {
        <div class="recent-observation-detail-metric"> @* unchanged *@ </div>
    }
    @if (Metrics.Count > 1)
    {
        <div class="recent-observation-detail-metric recent-observation-trend-about">
            <TrendsAboutButton OnClick="@ShowAboutTrends" />
        </div>
    }
</dl>
<AboutTrends @ref="aboutTrends" Metrics="@Metrics" />
```

— see [the existing grid this button drops into](#the-existing-grid-this-button-drops-into)
for why this one addition reproduces every position the task specifies with
no new CSS.

### The About modal

New `AboutTrends.razor` / `.razor.cs`, structurally similar to
`AboutData.razor`/`.razor.cs` (a `Modal @ref`, a public `Show()` the parent
calls), one instance per tile (owned by `RecentObservationTrend`, same as
`AboutData` is one instance per `ChartSeriesView`):

```razor
<Modal @ref="modal" Size="ModalSize.ExtraLarge">
    <ModalContent>
        <ModalHeader>
            <ModalTitle>About this trend</ModalTitle>
            <CloseButton />
        </ModalHeader>
        <ModalBody>
            <Tabs SelectedTab="@selectedMetricKey" SelectedTabChanged="@OnMetricTabChanged">
                <Items>
                    <Tab Name="Overview">Overview</Tab>
                    @foreach (var metric in Metrics)
                    {
                        <Tab Name="@metric.Label">@metric.Label</Tab>
                    }
                </Items>
                <Content>
                    <TabPanel Name="Overview">
                        <TrendsOverviewExplainer />
                    </TabPanel>
                    @foreach (var metric in Metrics)
                    {
                        <TabPanel Name="@metric.Label">
                            <div class="recent-observation-detail-toggle" role="group" aria-label="Time window">
                                @* Full recordset / Last 30 years / First half of recordset — see markup above *@
                            </div>
                            <TrendStatTable Sections="@BuildSections(metric, selectedWindow)" />
                            <DelayedTooltip Text="Download the yearly data behind this trend">
                                <button type="button" class="trends-download" aria-label="Download the yearly data behind this trend" @onclick="@(() => OnDownloadClicked(metric, selectedWindow))">
                                    <i class="fas fa-download" aria-hidden="true"></i>
                                </button>
                            </DelayedTooltip>
                        </TabPanel>
                    }
                </Content>
            </Tabs>
        </ModalBody>
    </ModalContent>
</Modal>
```

`Metrics` is passed straight through from `RecentObservationTrend`'s own
`[Parameter] IReadOnlyList<RecentObservationTrendViewModel> Metrics` — the
modal doesn't need a separate view model built up front; `BuildSections`
derives a `TrendStatSection[]` (below) from whichever metric/window is
currently selected, computed on the fly the same way
`OverviewField.ShowPopup` defers building popup content until it's actually
shown (`OverviewField.razor.cs:50-61`).

`OnDownloadClicked` raises the `TrendDownloadRequest` event described in
[Where the download pattern already exists](#where-the-download-pattern-already-exists)
rather than calling `IExporter` directly, since neither `RecentObservationTrend`
nor `AboutTrends` have the services that requires.

### The Overview tab's content

The task's request for "on the first screen ... a climate-related explanation
... why some trends say no significant trend ... why it's per decade ...
simple linear regression with a Wikipedia link ... show and explain p-value"
is Group 1's `Overview` tab — the modal's first/default tab. Since this
content is entirely generic (no metric- or location-specific numbers), it's a
small shared `TrendsOverviewExplainer.razor` partial with no parameters,
reused by every tile's modal — the same "write once, render per instance"
shape `WarmingAnomalyInfo.razor` already uses for its own definitional
paragraph. It covers:

- Why per-decade: matches the wording every trend on this site already uses
  (`FormatTrendPerDecade`, established in the Trend tab plan) — a per-decade
  rate is large enough to read as a number without misleadingly implying
  year-to-year precision.
- Why "no significant trend" replaces a number on the tile's own rows: a
  non-significant slope means the data doesn't rule out zero trend at the 95%
  level — showing a specific "+0.08°C per decade" number in that case would
  overstate confidence the data doesn't support.
- Why precipitation shows "no significant trend" far more often than
  temperature: precipitation is dominated by year-to-year natural variability
  (rainfall is naturally far noisier than temperature — driven by ENSO and
  other short-cycle variability that swamps any long-term signal at most
  individual locations), so its `Sy.x` (residual scatter) is large relative to
  any real trend, which directly lowers statistical power to detect a
  non-zero slope at a given record length. Temperature's year-to-year
  variability is smaller relative to its trend, so more locations clear the
  significance bar.
- A brief, plain-language description of simple linear regression (ordinary
  least squares — fitting the straight line that minimises the sum of squared
  vertical distances to each year's value), linking to
  [Wikipedia: Simple linear regression](https://en.wikipedia.org/wiki/Simple_linear_regression)
  — the same reference the Core linear-regression implementation itself was
  verified against.
- What a p-value is (the probability of seeing a slope this far from zero, by
  chance alone, if the true long-term trend were actually zero) and why
  `< 0.05` is this site's cutoff for "significant" (`RegressionSignificance`'s
  default `alpha = 0.05`, used everywhere in `LinearRegressionCalculator`
  already — not a new threshold invented for this feature).
- An explicit note that the tile's own rows only ever show the per-decade rate
  or "no significant trend" — R², p-value, confidence intervals, and
  everything else in the full breakdown are *only* inside this modal, which
  is why it exists at all.

### The full-statistics table

`TrendStatSection`/`TrendStatRow` view models, one section per GraphPad-style
group, most rows carrying an optional explanation:

```csharp
public sealed record TrendStatSection(string Title, IReadOnlyList<TrendStatRow> Rows);

public sealed record TrendStatRow(
    string Label,
    string Value,
    bool IsEmphasized,
    string? AbstractExplanation,
    string? ClimateExplanation,
    string? WorkedExample);
```

`TrendStatTable.razor` renders each row as a `<dt>`/`<dd>` pair (matching the
`.recent-observation-detail-metric` `<dl>` convention already used for similar
stat lists) plus, when `AbstractExplanation` is non-null, a chevron button
copying `RecentObservationMainPanel`'s `oi oi-chevron-right`/`.expanded`
pattern; expanding inserts a row directly beneath with the abstract
definition, the climate-specific meaning, and the worked example.

**Row-by-row content** (■ = emphasized; ▸ = has an expand row):

| Section | Row | ■ | ▸ | Content |
|---|---|---|---|---|
| Best-fit values | Slope | ■ | ▸ | Abstract: change in Y per unit X. Climate: °C (or mm) per decade — reuses the exact wording `FormatTrendPerDecade` already produces. Worked example: "at this rate, {location} would be predicted to warm/cool by {slope×100} °C per century" (a scale-up of the same number, not a new calculation). |
| | Y-intercept | | ▸ | Abstract: predicted Y when X = 0 (year 0). Climate: explicitly **not** a real prediction — year 0 is thousands of years outside the observed record, so this is a mathematical artifact of extending the fitted line, not a forecast. This row's expand exists specifically to pre-empt the most common misreading of a regression readout. |
| | X-intercept | | ▸ | Abstract: X where the fitted line crosses Y = 0. Climate: same caveat as Y-intercept — 0 °C (or 0 mm) crossing the fitted line, for a location's *absolute* mean/max/min temperature or precipitation total, lands far outside any plausible year and carries no climate meaning; shown only because it's part of the standard regression report this table mirrors. |
| | 1/Slope | | ▸ | Abstract: reciprocal of the rate. Climate: "years per 1 °C of change" (temperature) or "years per 1 mm/decade of change" (precipitation) — e.g. the task's own example (`1/Slope = 48.94`) reads as "≈ 49 years for 1 °C of warming at this rate." This is the most directly tangible row in the table and gets the most concrete worked example. |
| 95% CI | Slope | ■ | | Folded into the Slope expand above (a stat and its own interval are one concept) — plain-language: "the data are consistent with a per-decade rate anywhere in this range." |
| | Y-intercept | | | Folded into Y-intercept's expand above. |
| | X-intercept | | | Folded into X-intercept's expand above; also where the Fieller's-theorem "sometimes this interval is undefined" caveat is surfaced, gated behind `IsSlopeSignificant` (see below). |
| Goodness of Fit | R² | ■ | ▸ | Abstract: proportion of year-to-year variance explained by the straight line (0–1). Climate: how much of the year-to-year ups and downs is "the trend" versus natural variability/noise — e.g. `R² = 0.56` reads as "56% of the year-to-year variation lines up with the long-term trend; the rest is short-term natural variability." |
| | Sy.x | | ▸ | Abstract: typical size of a residual (scatter around the line), same units as Y. Climate: typical year-to-year deviation from the smooth trend — directly explains, using this trend's own `Sy.x`, why a single hot or cold year doesn't change the assessment of the long-term trend. |
| Is slope significantly non-zero? | F, DFn/DFd, P value, Deviation from zero? | ■ | ▸ (one, shared) | A single expand covering all four (they're one significance test read four ways): what a p-value means, this site's `α = 0.05` cutoff, and a pointer to the modal's own Overview tab for the fuller p-value/significance explanation rather than repeating it per trend. |
| Equation | Y = slope·X + intercept | ■ | ▸ | This *is* the primary worked example the task asked for — see below. |
| Data | Number of X values, Total number of values, Maximum number of Y replicates | | | Plain counts, no climate framing needed. |
| | Number of missing values | | ▸ | Reuses `BuildYearRangeTooltip`'s exact list — expand shows the actual missing years, echoing the wording already shipped for Recent Observations (`"Missing years: 1953, 1954, 1961."` / `"No years are missing."`). |

Visual emphasis (■) is CSS only — a heavier font weight and the same accent
colour the site already uses for positive/negative trend values
(`ValueClassifier.GetValueClass`, reused rather than a new colour rule).

### The Equation row is where "plug in 1900, then 2100" actually happens

The task's request to "plug in the year 1900 ... then 2100 ... apply our
confidence or errors" maps directly onto
`LinearRegressionCalculator.Predict(regression, x, alpha)` — already built,
already returns both a fitted-mean CI and an individual-observation prediction
interval for any `x`. No new Core code is needed for this part at all:

```csharp
var y1900 = LinearRegressionCalculator.Predict(regression, 1900);
var y2100 = LinearRegressionCalculator.Predict(regression, 2100);
```

The Equation row's worked example renders both, in the same
value-then-uncertainty shape the rest of the site already uses:

> Y = 0.02043·X − 27.73
>
> For example: in **1900** this line predicts **{y1900.PredictedY}°C**
> (95% range for that year's actual value: {y1900.ObservationPredictionInterval.Lower}
> to {y1900.ObservationPredictionInterval.Upper}°C). In **2100** it predicts
> **{y2100.PredictedY}°C** ({y2100.ObservationPredictionInterval.Lower} to
> {y2100.ObservationPredictionInterval.Upper}°C).

Using `ObservationPredictionInterval` (not `MeanConfidenceInterval`) here is a
deliberate choice: the task asks for "the temperature" in a given year, which
is a single future observation, not the average of many repeated years — the
observation interval is the statistically correct one for that framing, and
the modal's expand for this distinction can note it explains why this interval
is wider than the CI on the trend line itself.

Whether 1900/2100 are used literally, or the example instead uses the trend's
own `MinimumX`/`MaximumX` +/- a fixed offset (so a tile whose comparable
history starts in 1990 doesn't get a prediction 90 years before its own data
begins), is a small implementation-time call; either reads correctly against
this design.

## New Core additions

Two new public methods on `LinearRegressionCalculator`, additive only (no
existing signatures change):

```csharp
public static InterceptStatistics CalculateInterceptStatistics(LinearRegressionResult regression, double alpha = 0.05);

public static XInterceptStatistics CalculateXIntercept(LinearRegressionResult regression, double alpha = 0.05);
```

```csharp
// ClimateExplorer.Core/Stats/Model/InterceptStatistics.cs
public sealed record InterceptStatistics(double StandardError, ConfidenceInterval ConfidenceInterval);

// ClimateExplorer.Core/Stats/Model/XInterceptStatistics.cs
public sealed record XInterceptStatistics(double Value, ConfidenceInterval? ConfidenceInterval);
```

- **Y-intercept's SE and CI need no new formula, only a new entry point.**
  `LinearRegressionCalculator.Predict(regression, x: 0)` already computes
  exactly this: `PredictedY` at `x = 0` **is** the intercept, and
  `MeanConfidenceInterval` at `x = 0` **is** the intercept's confidence
  interval — the leverage term in `Predict`
  (`(1/n) + (x - meanX)² / Sxx`) reduces to the textbook intercept-SE formula
  `(1/n) + meanX²/Sxx` exactly when `x = 0`. `CalculateInterceptStatistics` is
  implemented inside `LinearRegressionCalculator.cs` (same assembly as
  `internal StudentTDistributionCalculator`), reusing this identity — the SE
  is backed out from the CI half-width and the same t-critical value `Predict`
  already computes, rather than routed through the public `Predict`/
  `RegressionPrediction` shape (which is framed around "a prediction for X",
  not "the intercept").
- **X-intercept's confidence interval is a genuinely new piece of statistics,
  not a lookup.** `X = -intercept/slope` is a ratio of two correlated
  estimates, so its interval isn't a simple propagation — the standard
  treatment is **Fieller's theorem** (Fieller, 1954; also in Draper & Smith,
  *Applied Regression Analysis*, §5.3), which produces a *finite* interval
  only when the slope is estimated precisely enough relative to its own size
  (a `g < 1` condition). `ConfidenceInterval` is `null` when that condition
  fails — in practice, whenever the trend is far from significant. The
  Web.Client layer only renders the X-intercept CI when `IsSlopeSignificant`
  is true anyway (consistent with "we don't even show the trend if it's not
  significant"), so the null case is expected to be rare in what's actually
  displayed, but the method itself must handle it rather than throw.
- Unit tests for both should include the task's own example numbers
  (slope `0.02043`, intercept `-27.73` → Y-intercept CI `-34.81 to -20.65`,
  X-intercept `1357`, X-intercept CI `1226 to 1449`) as a fixture — these are
  independently-produced reference numbers, not this app's data, the same way
  [the linear regression doc](2026-07-11-01-linear-regression.md) added NIST
  and Wikipedia fixtures for the original calculator.

## Downloading the data behind a trend

New method on `IExporter`/`Exporter`, following `ExportClimateRecords`'s exact
shape (CSV lines → UTF-8-BOM `MemoryStream`):

```csharp
Stream ExportTrendData(ILogger logger, Location location, string dataTypeLabel, string windowLabel, IReadOnlyList<DataPoint> points, string sourceUri);
```

Two columns, `Year,Value`, one row per point in the window currently on
screen — the *exact* points fed into `LinearRegressionCalculator.Calculate`
for that trend (`FullPeriodPoints`/`RecentTrendPoints`/`FirstHalfTrendPoints`
on the extended `RecentObservationTrendViewModel`), so the export is
sufficient on its own to reproduce the regression.

```csharp
// RecentObservationsPanel.razor.cs, handling the bubbled-up TrendDownloadRequest
var fileStream = Exporter.ExportTrendData(Logger, Location, request.DataTypeLabel, request.WindowLabel, request.Points, NavManager.Uri);
var fileName = $"{Location.Name}-{request.DataTypeLabel}-{request.WindowLabel}-trend-data.csv";
using var streamRef = new DotNetStreamReference(stream: fileStream);
await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
```

— the same trigger `ClimateRecords.razor.cs:538-544` already uses, just
invoked from a bubbled-up child event instead of a local click handler (see
[Where the download pattern already exists](#where-the-download-pattern-already-exists)
for why `RecentObservationsPanel` handles this rather than `RecentObservationTrend`
calling `IExporter` directly).

Icon-only (`fas fa-download`, no label), wrapped in `DelayedTooltip`, placed
directly beneath the stat table for whichever metric/window tab is currently
open inside the modal (see the modal markup above).

## Acceptance criteria

- `RecentObservationTrend.razor`'s existing metric rows (full period / last 30
  years / early period, inline, per metric) are unchanged.
- A single "About trends" `ClimateButton` (fa-info icon, "About trends" label
  stacked beneath it, matching `ChartSeriesView`'s About button look) is added
  to the same `.recent-observation-detail-metrics` grid: appended after the
  metric rows on temperature tiles (3 metrics → row 2/col 2 desktop, row 4
  mobile) and prepended before the metric row on precipitation tiles (1
  metric → row 1/col 1 at every breakpoint) — reusing the grid's existing
  responsive behaviour, no new CSS breakpoint.
- Clicking it opens an `ExtraLarge` modal containing a Blazorise `<Tabs>` for
  Overview/Mean/Max/Min (or Overview/Precipitation), and — on any non-Overview
  tab — a second toggle-button group for Full recordset/Last 30 years/First
  half of recordset, styled with the exact
  `.recent-observation-detail-toggle`/`.recent-observation-detail-toggle-option`
  classes already used one level up on the tile.
- The Overview tab shows the shared `TrendsOverviewExplainer` content: why
  rates are per decade, why non-significant trends hide the number on the
  tile's own rows, why precipitation shows "no significant trend" more often
  than temperature, a plain-language description of simple linear regression
  with a link to Wikipedia, and what a p-value is and this site's 0.05 cutoff.
- Each non-Overview metric/window combination shows every row in the
  [row-by-row content](#the-full-statistics-table) table, with Slope, R², and
  the significance row visually emphasized.
- Every row marked ▸ above has a working chevron that inserts an explanation
  row directly beneath it (abstract meaning, climate-specific meaning, and —
  where applicable — a worked example using this trend's own numbers), reusing
  the existing `oi-chevron-right`/`.expanded` rotation convention.
- The Equation row's worked example uses `LinearRegressionCalculator.Predict`
  at two example X values and shows the observation prediction interval
  alongside each predicted value.
- Each metric/window combination has a "download data" icon button that
  downloads a two-column `Year,Value` CSV containing exactly the points used
  for that trend's regression — sufficient on its own for a user to reproduce
  the calculation.
- `RecentObservationTrendViewModel` carries the raw `LinearRegressionResult`
  and point list for all three windows per metric, not just formatted
  strings.
- `LinearRegressionCalculator.CalculateInterceptStatistics` and
  `CalculateXIntercept` are additive (no existing public signature changes)
  and covered by unit tests including the task's own example-block numbers as
  a fixture.
- `Exporter.ExportTrendData` follows the existing CSV/BOM/`downloadFileFromStream`
  convention exactly, triggered via a bubbled-up event from
  `RecentObservationTrend` through `RecentObservationTile` to
  `RecentObservationsPanel`, no new download mechanism introduced.

## Out of scope / open questions

- Whether the modal remembers its last-selected metric/window tab between
  opens (per tile, per session), or always resets to Overview — reasonable
  either way, not fixed by this task.
- Visual confirmation that the reused `.recent-observation-detail-toggle`
  button-group styling, and Blazorise `Tabs`, both read well inside an
  `ExtraLarge` modal rather than the narrower spaces they're currently used
  in — flagged above as expected to work (same component tree, same scoped
  CSS) but worth a quick check once built.
