# Chart series labels without repeated text

- **Date:** 2026-10-26
- **Status:** Implemented 2026-10-26 (see addendum)
- **Author:** Patrick (with Claude)
- **Scope:** `ChartSeriesDefinition` label builders, new `SeriesLabeller`, `ChartDisplayBuilder`,
  `ChartOptionsRequest`/`ChartOptionsFactory`, `ChartView`, `ChartSeriesView` (dropdown label),
  `ChartJsChart.razor.js` (new `seriesNote` plugin), vendored Chart.js (4.5.0 → 4.6.1), `AGENTS.md`
- **Builds on:** [Chart title shows only places](2026-10-26-02-chart-title-places-plan.md). Its title names every
  place on the chart, which is what makes it safe to drop the location from the legend.
- **Branch context:** written on `development`

## Problem

Every legend label repeats text that the other labels, or the title, already show. A chart of Canberra's maximum
and minimum temperature, both adjusted and smoothed, has the legend:

```
Canberra | Maximum temperature | Adjusted | 20 year centred moving average | °C
Canberra | Minimum temperature | Adjusted | 20 year centred moving average | °C
```

The title is already `Canberra, Australia` and the y-axis title is `Degrees celsius (°C)`. The only text that tells
the two series apart is the data type.

## Rules

1. **One place:** if every series on the chart is for the same place, remove the location from every label. The
   title names the place.
2. **Shared parts:** if there are at least two series and a part of the label other than the location or unit is
   the same in **every** series, show it once in a new `seriesNote` and remove it from each label.
3. **Shared unit:** if every series has the same unit, remove it from every label. It isn't added to `seriesNote`:
   every y-axis title already shows its unit. This applies to a single series too.
4. **Mobile:** apply rule 1 only. Mobile labels are otherwise unchanged (they have no unit) and `seriesNote` is
   null.

`seriesNote` is drawn like `cornerNote` (secondary-text colour `#595959`), at the bottom left of the chart on the
legend's row, after the prefix `All series:` (e.g. `All series: Adjusted | 20 year centred moving average`). The
examples below show the note without the prefix.

### Examples (desktop)

| Chart | Legend | `seriesNote` |
|---|---|---|
| Canberra mean temperature | `Mean temperature \| Adjusted` | – |
| Canberra max + min temperature, adjusted, 20 year CMA | `Maximum temperature`, `Minimum temperature` | `Adjusted \| 20 year centred moving average` |
| Canberra + Hobart mean temperature, adjusted | `Canberra`, `Hobart` | `Mean temperature \| Adjusted` |
| Canberra mean temperature + rainfall | `Mean temperature \| Adjusted \| °C`, `Rainfall \| mm` | – |
| Canberra + Hobart mean temperature, each with a trend | `Canberra \| Adjusted`, `Canberra \| {period} trend`, `Hobart \| Adjusted`, `Hobart \| {period} trend` | `Mean temperature` |
| Canberra mean temperature, adjusted, 10 year moving average, two trends (the "temperature + trend" preset) | `Adjusted \| 10 year centred moving average`, `{regression} {period} trend to {year}` ×2 | `Mean temperature` |
| Canberra rainfall with a trend | `Rainfall`, `Rainfall \| {period} trend` | – |
| Canberra mean temperature, adjusted + unadjusted, each with a trend | `Adjusted`, `Adjusted \| {period} trend`, `Unadjusted`, `Unadjusted \| {period} trend` | `Mean temperature` |

Data type names are illustrative; they come from `DataType.ToFriendlyName()`.

### Examples (mobile)

| Chart | Legend |
|---|---|
| Canberra max + min temperature | `Maximum temperature`, `Minimum temperature` |
| Canberra + Hobart mean temperature | `Canberra \| Mean temperature`, `Hobart \| Mean temperature` |

## How labels are built today

| Label | Built by | Desktop | Mobile |
|---|---|---|---|
| Series legend | `ChartDisplayBuilder.BuildDatasets` | `{FriendlyTitle} \| {unit}` | `GetFriendlyTitleShort()` |
| Trend legend | `ChartDisplayBuilder.BuildTrendLabel` | `{GetFriendlyTitleShort()} \| {period} trend` | same |
| Override (frost, custom) | `GetTransformationOverrideLabel()` replaces the whole label | same | same |
| Tooltip | `GetTooltipLabel(unit)` — `Location \| Data type \| Unit` | same | same |

`FriendlyTitle` and `GetFriendlyTitleShort()` build a list of segments and `string.Join(" | ", …)` them. The
segment list is thrown away, so nothing downstream can tell which part is the location and which is the adjustment.

## Issues and gaps

1. **Labels are joined before anything can compare them.** The builders already build a segment list and join it.
   The rules are applied to the segments, before they're joined: the builders return segments tagged with what
   they are (see [Design](#design)), and segments are compared by part **and** text. No string is ever split.
2. **One series.** With one series and no trend, everything in its label is trivially shared, so rule 2 would move
   the whole label into `seriesNote` and leave an empty legend entry. **Decided: rule 2 needs at least two legend
   entries, counting trend overlays.** One series without a trend still gets rules 1 and 3. *(Revised: it
   originally needed two series, which left the "temperature + trend" preset repeating `Mean temperature` on all
   three entries.)*
3. **Labels that end up identical or empty.** Two series that differ only in something the label doesn't show
   (display style, colour, or smoothing on a derived series, see 5) share every segment. Rule 2 would empty both
   labels. **If any label would be empty after rule 2, skip rule 2 for the whole chart** and keep rule 1. These
   labels are already identical today.
4. **Trend overlays.** Trend datasets are legend entries too, but they aren't series:
   - A trend entry counts towards the shared parts by its prefix, the short title, which never has the
     adjustment, transformation or smoothing. So those stay on the series' labels when any series has a trend, and
     the note never claims a trend is smoothed. A trend's own text (`{period} trend`, regression type,
     predict-until year) is never moved.
   - A trend label's prefix (today `GetFriendlyTitleShort()`) drops the same parts as its parent: the location
     under rule 1 and the shared parts under rule 2.
   - That can leave the prefix empty. In the last desktop example, the short title is `Canberra | Mean temperature`,
     and both parts are removed, so two trends would both read `{period} trend`. **When the stripped short title is
     empty, the prefix is the parent's stripped legend label** (`Adjusted`). It can't be empty, because of 3. With
     one series there's no other trend to confuse it with, so the prefix stays empty and the trend label is just
     its own text.
5. **Override and derived series.**
   - An override label (DayOfYearIfFrost, Custom) replaces the whole label, as now. It is one opaque segment that
     is never moved, so in practice a chart with one has no `seriesNote`. It has no location to remove.
   - DifferenceBetweenTwoSeries is `[A] minus [B]` and AverageOfMultipleSeries is
     `Location | Average X, Y | Adjustment`. Both are one opaque segment for rule 2. For rule 1 they are rebuilt
     without the location: `[Maximum temperature] minus [Minimum temperature]`.
   - Today `GetFriendlyTitle` returns early for multi-source series, so their labels never show transformation,
     smoothing etc. That's an existing gap and is left as it is.
6. **What "one place" means.** Use the same rule as `BuildChartTitle`: distinct `LocationId` across every source
   series spec. A region is a place, so a chart of two atmosphere series drops `Atmosphere` from both labels (the
   title is `Atmosphere`). A location plus a region is two places, so labels keep them. Extract the distinct-places
   query from `BuildChartTitle` into a `ChartLogic` helper so the title and labels can't disagree.
7. **The unit.** **Decided (rule 3):** a unit shared by every series is dropped from the labels and not put in
   `seriesNote`. Every y-axis title shows its unit (`ChartOptionsFactory.GetYAxisLabel`), including when series with
   the same unit are on separate axes, so nothing is lost. Units that differ stay in the labels, because they tell
   the reader which axis a series is read against.
8. **"Aggregation" means two things.** The series' aggregation (`SeriesAggregationOptions`: how values within each
   period are combined into one point) shows in the label as `Aggregation: Maximum`. The chart-level grouping
   settings (how daily data is bundled, with a completeness threshold) show in `cornerNote` as
   `Aggregation: Fortnightly at 70%`, and have the chart's **Aggregation** button and info panel. Moving the first
   into `seriesNote` puts both on the chart with the same word.
   **Decided:** rename the series one to **Statistic** (`Statistic: Maximum`), in the label and in the series
   panel's dropdown label in `ChartSeriesView.razor`, so the label matches the control that sets it. The chart-level
   name stays **Aggregation**.
9. **Layout: the legend uses the whole width.** Chart.js 4.5's horizontal legend wraps its rows at the full chart
   width and centres each row (`Legend._fitRows`, `adjustHitBoxes`). Text drawn at the left in `afterDraw`, the way
   `cornerNote` does it, would overlap the legend whenever a row is wide, and the legend's click hit boxes wouldn't
   know. Space has to be reserved during layout. See [The `seriesNote` plugin](#the-seriesnote-plugin).
10. **Narrow desktop windows and tablets.** `IsMobileDevice` comes from `CurrentDeviceService.Mobile()` (the device,
    not the width), so a tablet or a narrow desktop window gets `seriesNote`. The plugin has to cope with a note that
    doesn't fit beside the legend.
11. **Tooltips.** **Decided:** tooltip labels are left unchanged. They are already trimmed to `Location | Data type | Unit`, and
    they're read next to a point rather than next to the title and legend.
12. **Other places that show series titles** are unchanged: the series list (`ChartSeriesView`), the Add trend
    dropdown, the trend stats panel (`TrendStatSubject`), About data, and the CSV export header. None of them is next
    to the chart title.
13. **Accessibility.** The canvas is `role="img"` with `aria-label="Chart"`; the legend text isn't in the
    accessible name either, so `seriesNote` doesn't make this worse. Out of scope.
14. **Dead parameter.** `GetFriendlyTitle(string? locationTitle)` has no callers that pass `locationTitle` since the
    title rewrite. Remove it while changing the method.

## Design

### Segments

New in `UiModel`:

```csharp
public enum SeriesLabelPart
{
    Location, Year, DataType, Adjustment, Transformation, Aggregation,
    TemporalCalculation, Value, Smoothing, Unit,

    /// <summary>An override or derived label that is kept whole.</summary>
    Whole,
}

public sealed record SeriesLabelSegment(SeriesLabelPart Part, string Text);
```

`ChartSeriesDefinition`:

- `GetFriendlyTitleSegments(bool includeLocation = true)` holds today's `GetFriendlyTitle` logic and returns
  segments. A multi-source series returns one `Whole` segment. `FriendlyTitle` joins the segments, so its text is
  unchanged.
- `GetFriendlyTitleShortSegments(bool includeLocation = true)`, likewise for `GetFriendlyTitleShort()`. The
  difference and average titles are one `Whole` segment, built with or without the location.
- `BuildFriendlyTitleShortForSeries` gets an `includeLocation` parameter (default `true`, so `AboutData` is
  unchanged).

### `SeriesLabeller`

New static class in `UiLogic`. It takes the series, `isMobileDevice` and the unit formatter, and returns each series'
legend label, each series' trend prefix and the `seriesNote`:

1. `onePlace` = the chart has one distinct place (6).
2. For each series: the override label as one `Whole` segment if there is one; otherwise on desktop the full title
   segments plus a `Unit` segment, and on mobile the short title segments. Location is left out when `onePlace`.
3. On desktop, if every series has the same `Unit` segment, remove it (rule 3). An override series has no unit,
   so a chart with one keeps the other series' units.
4. On desktop with two or more legend entries (series plus series with a trend): the shared parts are the
   `(Part, Text)` pairs, other than `Location`, `Unit` and `Whole`, that every series label and the short title of
   every series with a trend has. Remove them from every label; if any label is then empty, keep them
   all (issue 3).
5. `seriesNote` is the shared segments joined with ` | `, in the order of the first series. Null when there are none
   and always on mobile.
6. Trend prefix: the series' short title segments without the location (if `onePlace`) and without the shared
   parts; if that is empty, the series' legend label (4), unless it's the only series.

### `ChartDisplayBuilder`

`BuildDatasets` calls `SeriesLabeller` once and uses its labels in place of `defaultLabel` and the trend prefixes in
`BuildTrendLabel`. The override behaviour stays in `GetTransformationOverrideLabel`, which the labeller calls. It
returns a `ChartDisplayBuildResult` record (`Datasets`, `TooltipMetadata`, `SeriesNote`) instead of the tuple, and
`ChartView` passes `SeriesNote` into `ChartOptionsRequest`.

`ChartOptionsRequest` gets `public string? SeriesNote { get; init; }`, and `ChartOptionsFactory` adds the plugin
options next to `CornerNote`:

```csharp
// Drawn by the seriesNote plugin in ChartJsChart.razor.js, on the legend's row. Null on mobile.
SeriesNote = new
{
    Text = request.SeriesNote,
    Color = "#595959",
},
```

### Upgrade Chart.js first

Upgrade the vendored `wwwroot/lib/chartjs/chart.umd.min.js` from 4.5.0 to 4.6.1 as its own commit, before the
plugin. 4.5.1 to 4.6.1 are bug fixes and small enhancements, with nothing marked breaking: integer canvas
dimensions, a Chrome zoom shrinking fix, filler and doughnut fixes, scale title stroke. Neither adds a public way to
reserve legend space. `Legend.update`, `_fitRows` and `adjustHitBoxes` are byte-for-byte the same in the 4.5.0 and
4.6.1 minified builds (checked by diffing them), so the plugin below is the same on either. Check that
`chartjs-plugin-trendline` 3.2.0 still loads and draws with 4.6.1.

### The `seriesNote` plugin

In `ChartJsChart.razor.js`, added to `plugins` after `cornerNote`. It uses the legend's label font, so the note is
the same size as the labels (12px, the smallest allowed).

- **Reserve space** (`beforeLayout`): wrap the chart's legend `_fitRows` once per legend instance. When the plugin
  has text, the wrapper measures it and calls the original with `maxWidth` reduced by twice the note's width plus a
  gap. Rows then wrap early and, being centred, stay clear of the note on the left, and the legend stays centred
  under the chart. The legend's `width` stays the full width, so the hit boxes and drawing need no changes.
- **Doesn't fit** (10): if that would leave less than half the width for the legend, call the original at full
  width and add one row's height to what it returns. The note gets its own row under the legend items.
- **Draw** (`afterDraw`): left-aligned at `legend.left + labels.padding`, vertically centred on the last legend row
  (from `legendHitBoxes`), or on the added row.

`_fitRows` is a private Chart.js method. The vendored file is not edited: the plugin wraps the method on each chart's
legend at runtime. Chart.js is vendored, so it won't change underneath us, but the dependency has to be visible to
whoever upgrades it next:

- a comment on the wrapper saying it relies on `Legend._fitRows` and `maxWidth`;
- a line in `AGENTS.md`: *Chart.js is vendored in `wwwroot/lib/chartjs`. The `seriesNote` plugin in
  `ChartJsChart.razor.js` wraps the private `Legend._fitRows`; check it still exists and behaves the same when
  upgrading Chart.js.*

Chart.js has no public hook for this: `legend.maxWidth` only clamps the box, not the wrap width, and a legend
`title` sits on its own row above the items rather than beside them.

There are no JS tests in the repo; the plugin is checked by hand.

## Tests

New `SeriesLabellerTests`:

- `Build_OnePlace_RemovesLocationFromLabels`
- `Build_OneSeries_KeepsAllPartsButLocationAndUnitAndHasNoNote`
- `Build_SharedUnit_RemovedFromLabelsAndNotInNote`
- `Build_DifferentUnits_KeptInLabels`
- `Build_OverrideSeriesWithOthers_KeepsOtherUnits`
- `Build_TwoLocations_KeepsLocationsAndMovesSharedPartsToNote`
- `Build_SharedParts_NoteKeepsOrderOfFirstSeries`
- `Build_PartMissingFromOneSeries_StaysInLabels`
- `Build_SameTextInDifferentParts_IsNotShared`
- `Build_LabelsEmptyAfterSharing_SkipsNote`
- `Build_Mobile_RemovesLocationOnlyAndHasNoNote`
- `Build_MobileTwoLocations_LabelsUnchanged`
- `Build_OverrideLabel_IsKeptWholeAndNotShared`
- `Build_DifferenceSeriesOnePlace_RemovesLocationInsideBrackets`
- `Build_OneRegion_RemovesRegionName`
- `Build_LocationAndRegion_KeepsBoth`
- `Build_Trends_PrefixDropsLocationAndSharedParts`
- `Build_TrendPrefixEmpty_UsesParentLabel`
- `Build_Trends_PartNotInTrendPrefixStaysInLabels`
- `Build_OneSeriesWithTrend_MovesPartsInTrendPrefixToNote`
- `Build_OneSeriesWithTrendLabelEmptyAfterSharing_SkipsNote`

`ChartDisplayBuilderTests`: update the tuple deconstructions for the new result record, and add one test that the
note reaches `ChartOptionsFactory`'s output. The single-series `StringAssert.EndsWith(datasets[0].Label, "| °F")` and
`"| °C"` assertions change: rule 3 drops the unit, so they assert the label has no unit. The same tests still check
the unit through the tooltip label and the axis title. The two-series sum test keeps its `"| °C"` assertion: its
series are in °F and °C.

Any test asserting `Aggregation:` in a series label changes to `Statistic:` (none were found).

## Summary

Legend labels stop repeating text. A chart of one place drops the location from every label (desktop and mobile).
On desktop, a unit every series shares is dropped (the axis titles show it), and other parts every series shares
move into a new `seriesNote`, drawn at the bottom left on the legend's row, with space reserved in the legend layout
so they never overlap. The rules work on the label segments before they're joined: the label builders return tagged
segments, and a new `SeriesLabeller` applies the rules, including fallbacks for single series, identical labels and
trend overlays. The series' `Aggregation` label becomes `Statistic`, so it can't be confused with the chart-level
aggregation. Chart.js is upgraded to 4.6.1 first.

### Change size

**Projects:** `ClimateExplorer.Web.Client` and `ClimateExplorer.UnitTests`.

Unchanged: `ClimateExplorer.Core`, tooltips, the series list and other places that show series titles.

| Area | Added | Changed | Removed | Lines (approx.) |
|---|---|---|---|---|
| Chart.js 4.5.0 → 4.6.1 (vendored, minified) | – | 1 | – | whole file replaced |
| `SeriesLabelPart` / `SeriesLabelSegment` | 1 | – | – | +25 |
| `ChartSeriesDefinition` segment builders, `Statistic:` | – | 1 | – | ~80 |
| `ChartSeriesView.razor` dropdown label `Statistic` | – | 1 | – | ~1 |
| `SeriesLabeller` | 1 | – | – | +130 |
| `ChartLogic` distinct-places helper | – | 1 | – | ~10 |
| `ChartDisplayBuilder` + result record | – | 1 | – | ~35 |
| `ChartOptionsRequest`, `ChartOptionsFactory`, `ChartView` | – | 3 | – | ~15 |
| `seriesNote` plugin in `ChartJsChart.razor.js` | – | 1 | – | ~70 |
| `AGENTS.md` note on the Chart.js dependency | – | 1 | – | ~3 |
| Tests: `SeriesLabellerTests` | 1 | – | – | +290 |
| Tests: `ChartDisplayBuilderTests` | – | 1 | – | ~25 |
| **Total** | **3** | **12** | **0** | **~685** (plus the Chart.js file) |

Most of the `ChartSeriesDefinition` change is existing code reshaped to return segments rather than new logic.

**Risk:** the plugin depends on a private Chart.js method (pinned by vendoring, and unchanged in 4.6.1). The Chart.js
upgrade touches every chart, so it goes in its own commit and is checked by hand on its own. Everything else is
label text, which is covered by unit tests.

## Addendum — implementation notes

Implemented as planned, with these differences:

- **Chart.js upgrade in the same change.** 4.6.1 replaced 4.5.0 directly, not as a separate commit.
- **More, smaller files.** `SeriesLabelPart`, `SeriesLabelSegment`, `SeriesLabels` and `ChartDisplayBuildResult`
  each have their own file, following the one-type-per-file pattern (`ChartOptionsBuildResult`). That's 6 new
  files, not 3. `SeriesLabelSegment.Join` replaces the `string.Join(" | ", …)` calls in the title builders.
- **`SeriesLabeller.Build` takes the series definitions and their unit labels**, not `SeriesWithData`, so its tests
  don't need data sets. `ChartDisplayBuilder` resolves `SeriesDisplayUnits` once per series and passes the labels
  in. `ChartLogic.GetPlaces` takes series definitions too.
- **An empty unit label adds no `Unit` segment.** Before, such a label ended in a dangling ` | `.
- **Rules 2 and 3 together.** The shared unit is removed first, then the "would a label be empty?" check runs on
  what's left. So a metric chart of mean temperature and its sum (both °C, one an extra `Statistic: Sum`) gets no
  note: the mean's label would be empty. Its labels are `Mean temperature | Adjusted` and
  `Mean temperature | Adjusted | Statistic: Sum`. In US customary the units differ (°F and °C), so the note is
  `Mean temperature | Adjusted` and the labels are `°F` and `Statistic: Sum | °C`.
- **Series panel tooltip.** Besides the dropdown label, the disabled-state tooltip in `ChartSeriesView.razor.cs`
  now says "every statistic would produce the same chart".
- **Two `ChartDisplayBuilderTests` added:** the note reaching the options object, and a trend label leaving out
  the location on a one-place chart.

Verified with `dotnet build` and the unit tests (929 passing). Per `AGENTS.md`, the site wasn't run, so the
`seriesNote` plugin's layout (rows wrapping around the note, the own-row fallback on narrow charts) and Chart.js
4.6.1 with the trendline plugin still need checking by eye.

### Follow-up 2026-10-27 — units left out of every label

Rule 3 (and decision 7) is replaced: the unit is never in a legend label, shared or not, and never in
`seriesNote`. Every y-axis title shows its unit, and the axis title swatches show which series read against which
axis, so a unit that differs no longer needs to say it. `SeriesLabeller.Build` no longer takes unit labels and the
`SeriesLabelPart.Unit` segment is gone. Tooltips are unchanged and keep their unit: a tooltip shows a number with no
axis beside it.

The US customary mean-and-sum chart above now gets the same labels as the metric one, with no note:
`Mean temperature | Adjusted` and `Mean temperature | Adjusted | Statistic: Sum`.

The same day, y-axis titles changed too: an anomaly of temperature is titled `Temperature anomaly (°C)` whether
the API calculated it or the dataset was published as anomalies, and on mobile an anomaly title uses the short
form (`°C anomaly`, `ppm anomaly`). Other titles are unchanged on both desktop and mobile.
