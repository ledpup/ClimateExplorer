# Shrinking centred moving average (with a floor) as the default smoother

- **Date:** 2026-10-02
- **Status:** Implemented 2026-10-02 (see addendum). Dashed ends and tooltip window size added 2026-10-03 (see second addendum)
- **Author:** Patrick Lea / Claude
- **Scope:** `ClimateExplorer.Core/Stats/Smoothing` (new folder), `ChartDataBuilder`,
  `SeriesSmoothingOptions`, `ChartSeriesListSerializer`, `ChartSeriesDefinition`, `ChartSeriesView`,
  `SuggestedPresetLists.*`, `Index`/`Global` default series, unit tests
- **Builds on:** the two plans on branch `issues/smoothing`
  (`2026-10-02-01-boundary-adjusted-moving-average-plan.md` and
  `2026-10-02-02-local-linear-regression-smoothing-plan.md`). They are not on `development`, so
  they are named here rather than linked. This doc is numbered `03` so it won't collide if they
  are ever merged.
- **Branch context:** written on `development` (at `eb8b3906a`). Implement on a new branch from
  `development`, not on `issues/smoothing`.

## Problem

A centred moving average only produces a value where the full window fits, so a 20 year window on
data to 2026 stops at 2016. Two attempts to fill the ends are on `issues/smoothing` and both were
rejected:

| Attempt | What the ends look like | Why |
| --- | --- | --- |
| Boundary-adjusted moving average | Flat horizontal line | The last `windowSize / 2` points all share one clamped window, so they all get the same value |
| Local linear regression | Straight sloped line | The same clamped window, with the value read off one fitted line |

Neither is better than stopping at 2016. Both fail for the same reason: the window stops moving
near the end. A shrinking window keeps moving, so each end point gets a different average.

## Proposal

Add a **shrinking centred moving average**. In the middle of the data it is identical to the
current centred moving average. Near an end, the window shrinks on both sides so it stays centred
on the point, until it reaches a minimum half-width (the floor). Past that, the inner side stops
shrinking and only the outer side is cut off by the end of the data.

Make it the default in the presets and default series. Keep the current centred moving average as
a selectable option in `ChartSeriesView`.

### Window rule

For a point at index `i`, with data running from `first` to `last` (see "Data ends" below):

```
halfWindow = windowSize / 2                     // slots either side of the point in a full window
floor      = min(MinimumHalfWindow, halfWindow) // MinimumHalfWindow is a hardcoded const, initially 4

room       = min(i - first, last - i)           // distance to the nearer end
reach      = min(halfWindow, max(room, floor))  // half-width after shrinking, never below the floor
shrunkSize = min(windowSize, 2 * reach + 1)     // the full window, or an odd shrunk one
winStart   = max(first, i - reach)
winEnd     = min(last,  i + reach)
```

A full even window is a 2xN moving average, as in `CentredMovingAverageCalculator`: N + 1 slots
with the two end slots at half weight. Both calculators get this from `SmoothingWindow.CentredMean`.

`MinimumHalfWindow` is a `private const int` in the new calculator, so it can be changed in one
place while experimenting. The `min(..., halfWindow)` stops the floor making a small window (3, 5)
wider than the user asked for.

Worked example, odd window (`windowSize = 21`, so `halfWindow = 10`, floor 4, data to 2026).
This matches the table in the original description:

| Year | Window used | Points |
| --- | --- | --- |
| 2016 | 2006-2026 | 21 (full) |
| 2021 | 2016-2026 | 11 |
| 2022 | 2018-2026 | 9 (last symmetric one) |
| 2023 | 2019-2026 | 8 |
| 2024 | 2020-2026 | 7 |
| 2025 | 2021-2026 | 6 |
| 2026 | 2022-2026 | 5 |

Even window (`windowSize = 20`, the common preset: `halfWindow = 10`):

| Year | Window used | Size |
| --- | --- | --- |
| 2016 | 2006-2026, end points at half weight | 20 (full) |
| 2017 | 2008-2026 | 19 |
| 2018 | 2010-2026 | 17 |
| 2021 | 2016-2026 | 11 |
| 2022 | 2018-2026 | 9 (last symmetric one) |
| 2023 | 2019-2026 | 8 |
| 2026 | 2022-2026 | 5 |

### Decisions in the rule

1. **Even windows shrink to odd windows.** A full even window is centred by giving its two end
   slots half weight (development commit `7332d387c`). Once it shrinks, the window is a plain odd
   one with every slot at full weight, so the size steps 20, 19, 17, 15, ... and the shrunk
   windows are the same as for the next odd size up. Keeping the half-weighted ends while
   shrinking would step evenly (20, 18, 16, ...) but gives no real benefit for the extra rule.
2. **Both ends shrink, not only the recent end.** The rule is mirrored at the start of the data,
   so a series that starts in 1910 is plotted from 1910 rather than 1920. This moves the chart's
   start year earlier for every default chart. If that is unwanted, the alternative is to apply
   the rule to the trailing end only and leave the leading `windowSize / 2` points null.
3. **Data ends are the first and last non-null values, not the array bounds.** A source series
   can have leading or trailing null bins. Shrinking towards the array bound would shrink towards
   bins with no data. Points outside `first..last` stay null. A series with no values returns all
   nulls.
4. **The data threshold applies to the window actually used.** A point is smoothed when
   `present / slotsInWindow >= requiredDataThreshold`, where `slotsInWindow = winEnd - winStart + 1`.
   At 0.75, a 5 slot window needs 4 values and a 6 slot window needs 5. So a gap in the last few
   years can still blank an end point, which is the same behaviour as gaps in the middle.

## Design

### Interface

Reuse the interface shape from `issues/smoothing`. Both calculators take the same parameters:

```csharp
namespace ClimateExplorer.Core.Stats.Smoothing;

public interface ISeriesSmoother
{
    double?[] Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold);
}
```

(Changed on 2026-10-03 to return a `SmoothedSeries` with the window size of each point. See the
second addendum.)

The output is always the same length as the input, with `null` where there is no smoothed value.

### Core files (`ClimateExplorer.Core/Stats/Smoothing/`)

| File | Source | Notes |
| --- | --- | --- |
| `ISeriesSmoother.cs` | copy from `issues/smoothing` | unchanged |
| `SmoothingWindow.cs` | copy from `issues/smoothing` | internal helper. Change `MeetsThreshold` and `Mean` to take `windowStart`/`windowEnd` or keep `windowStart`/`windowSize`; either works for both calculators |
| `CentredMovingAverageCalculator.cs` | copy from `issues/smoothing` | replaces the static extension class in `Stats/`. Same results as today |
| `ShrinkingCentredMovingAverageCalculator.cs` | new | implements the window rule above. Holds `MinimumHalfWindow` |

Not carried over: `BoundaryAdjustedWindow`, `BoundaryAdjustedWindowCalculator`,
`LocalLinearRegressionCalculator` and their tests.

The files can be taken with `git checkout issues/smoothing -- <path>` rather than cherry-picking
the commits, since both commits on that branch also contain the rejected calculators.

### Enum and URL compatibility

```csharp
// Persisted by name in chart URLs. Old URLs use "MovingAverage", which ChartSeriesListSerializer
// maps to CentredMovingAverage. Append new values so existing ordinals don't change.
public enum SeriesSmoothingOptions
{
    None,
    CentredMovingAverage,           // renamed from MovingAverage
    Trendline,
    ShrinkingCentredMovingAverage,
}
```

`ChartSeriesListSerializer` writes the enum by name and reads it with `Enum.Parse`. Add the
`ParseSmoothing` helper from the branch so the old token `MovingAverage` still parses, mapped to
`CentredMovingAverage`. Existing shared links and the blog post
`2023-07-25-temperature-anomaly.md` then keep showing exactly what they showed when they were
shared. The alternative, mapping old links to the new default, would change the look of every
existing link.

### Choosing the smoother

Take `SeriesSmoothingOptionsExtensions` from the branch (`CreateSmoother()` and `UsesWindow()`),
with the `LocalLinearRegression` arm replaced by `ShrinkingCentredMovingAverage`. It stays in
`ClimateExplorer.Web.Client/UiModel` next to the enum, so no dependency injection is needed.

### `ChartDataBuilder`

Take the branch's change to the smoothing block
([ChartDataBuilder.cs:415-474](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuilder.cs#L415-L474)):
get the smoother from `cs.ChartSeries.Smoothing.CreateSmoother()`, call `Smooth`, and move the
`0.75f` literal to a `SmoothingRequiredDataThreshold` const. The "fewer than 10 smoothed values"
fallback stays. With shrinking it will trigger less often, because the ends are no longer lost.

### UI and text

- `ChartSeriesView.razor` Smoothing select, in this order: None, "Shrinking moving average",
  "Centred moving average". Tooltips as on the branch ("rolling window" instead of "rolling
  average").
- `ChartSeriesView.razor.cs`: `ShouldDisableSmoothingWindow` uses `!csd.Smoothing.UsesWindow()`.
- `ChartSeriesDefinition` title and tooltip descriptors: the new default keeps today's text
  ("20 year moving average"). The full-window option becomes "20 year centred moving average".
  Take the `GetSmoothingWindowUnit` helper from the branch.

### Defaults

Replace `SeriesSmoothingOptions.MovingAverage` with `ShrinkingCentredMovingAverage` in:

- `SuggestedPresetLists.Global.cs`, `.LocationBased.cs`, `.LocationBasedMini.cs`,
  `.LocationBasedMobile.cs`
- `Index.razor.cs` (two default series) and `Global.razor.cs` (one)

## Effects on other features

- **Trends.** Trend lines are fitted to the smoothed series (`PreProcessedDataSet`). With
  shrinking, the fit now includes the end points, which are averages of fewer values and so are
  noisier than the middle. Slopes on default charts will change slightly. The lag described in
  [the trend projection investigation](../notes/2026-08-20-01-trend-projection-moving-average-lag-investigation.md)
  disappears for the new option, because the smoothed series now ends at the last raw year.
- **Chart start and end years.** `ChartDataBuilder` works these out after smoothing, so default
  charts will run the full length of the data. Subtitles and axis ranges change accordingly.
- **Chart tooltip anomaly table.** It reads the plotted values, so it will now show values for
  the end years. No code change expected. Check when the UI is next reviewed.

## Tests

Names use `MethodName_StateUnderTest_ExpectedBehavior`.

- `Smoothing/CentredMovingAverageCalculatorTests.cs`: take from the branch (the existing tests
  ported to `Smooth`). Delete the old `CentredMovingAverageCalculatorTests.cs`.
- `Smoothing/ShrinkingCentredMovingAverageCalculatorTests.cs`, new:
  - middle points equal the centred moving average, odd and even window
  - the odd-window table above: each end point's value equals the mean of the listed window
  - the even-window table above
  - leading end mirrors the trailing end
  - window smaller than twice the floor (3 and 5): the floor does not widen the window
  - series shorter than the window: every point still gets a value
  - leading and trailing nulls: ends are taken from the first and last values, points outside stay null
  - a gap near the end that fails the threshold gives null for that point only
  - all nulls and empty input
  - consecutive end values differ for a non-constant input (the property the boundary-adjusted
    version failed)
- `Smoothing/SeriesSmootherContractTests.cs`: take from the branch, one row per calculator
  (output length equals input length, window of 1 returns the input).
- `SeriesSmoothingOptionsExtensionsTests.cs`: take from the branch, swap the option.
- `ChartSeriesListSerializerTrendTests.cs`: take the branch's test that `MovingAverage` in an old
  URL parses to `CentredMovingAverage`. Add a round trip for `ShrinkingCentredMovingAverage`.
- `ChartDataBuilderTests.cs`: rename enum uses. The fallback test at line 217 relies on a
  20 year window leaving too few points, so it must use `CentredMovingAverage` or a shorter
  series. The trend tests at lines 400 and 425 stay on `CentredMovingAverage`, since they cover
  the lag that only that option has.
- `ChartSeriesLocationSubstitutionServiceTests.cs`, `ChartStateUrlServiceTests.cs`: rename only.

Verify with `dotnet build` and the unit tests only. The website is not run.

## Risks

- **End values will move as new years arrive.** The 2026 value averages 5 or 6 years today and
  will be recalculated over a wider window each year until it is `windowSize / 2` years old. This
  is inherent to any end-filling method. The floor limits how far a single year can move it.
- **The ends are noisier than the middle.** A floor of 4 is a starting point. A higher floor is
  smoother but lags more, because the average is centred further behind the point.
- **Visible change on every default chart.** Lines get longer at both ends and trend values
  shift. Shared links are unaffected because the old token maps to the centred option.
- **Short windows.** For a 3 or 5 slot window the end points average 2 or 3 values. This is
  correct by the rule but close to the raw data.

## Out of scope

- Making the floor configurable in the UI or the URL.
- An existing oddity in the unsmoothed fallback in `ChartDataBuilder`: the fallback values are
  filtered to non-null and then zipped against the unfiltered records, so values shift to the
  wrong bins if the source has gaps. Worth a separate look.

## Addendum — implementation notes

Implemented 2026-10-02 on branch `issues/shrinking-moving-average` (from `development`). Build and
unit tests are clean. The UI has not been looked at.

- Shipped as planned, including the four decisions in "Decisions in the rule". The floor is
  `MinimumHalfWindow = 4` in `ShrinkingCentredMovingAverageCalculator`.
- `SmoothingWindow` was taken unchanged (`windowStart`/`windowSize`). The shrinking calculator
  passes the size of the window it actually used.
- A series shorter than about twice the floor comes out flat or nearly flat, because every point
  ends up averaging the whole series. `ChartDataBuilder` only falls back to unsmoothed data when
  fewer than 10 smoothed values exist, which the shrinking option rarely hits now. Raise that
  check to a minimum series length if short series look wrong.
- For an even window, the first point past the leading shrink zone has `after` slots of room
  but needs `before`, so it averages `windowSize - 1` slots. The centred option leaves it null.
- `ChartDataBuilderTests` has a new test,
  `BuildAsync_ShrinkingCentredMovingAverage_SmoothsEveryYearFromFirstToLast`.

Follow-ups: review the charts in the browser and tune the floor. The unsmoothed-fallback
misalignment listed under "Out of scope" is still open.

## Addendum 2 — dashed ends and window size in the tooltip (2026-10-03)

The shrunk ends are averages of fewer values than the rest of the line, so they are now drawn
dashed, and the tooltip shows how many bins the hovered value averages. Build and unit tests are
clean. The UI has not been looked at.

### Smoother returns the window

`ISeriesSmoother.Smooth` returns a `SmoothedSeries(double?[] Values, int[] WindowSizes)`.
`WindowSizes[i]` is the number of slots point `i` was averaged over, or 0 where the value is null.
`CentredMovingAverageCalculator` always reports the full window. The shrinking calculator reports
`slotsInWindow`, so the window size comes from the same code that computed the value.

### Data flow

1. `ChartDataBuilder` keeps the bins whose window is smaller than the requested window in
   `SeriesWithData.ShrunkSmoothingWindows` (bin id to window size). It is cleared when the series
   falls back to unsmoothed data.
2. `ChartTooltipMetadataBuilder.BuildForSeries` maps those bins onto chart point indexes, using
   the processed data set (one record per chart bin, in chart order). It adds them to
   `ChartTooltipSeriesInfo.ShrunkWindows` (index to size, sparse, so long daily series don't send a
   long array). Trend datasets get none.
3. `ChartView` passes the tooltip metadata to a new JS function, `configureShrunkWindowSegments`
   in `App.razor`, right after `configureChartTooltip`.

### Dashed line, one dataset

Blazorise's C# dataset can't hold a JS function, so the dash is set on the live Chart.js (4.5)
instance after each Blazorise update. Chart.js per-segment styling does it on the existing
dataset: `dataset.segment.borderDash` returns `[6, 4]` when either end of a segment is a shrunk
point, then `chart.update("none")`. No extra series is added, and the legend, tooltip index and
trend overlay indexes are unchanged. The legend still shows a solid line. Bar and scatter series
ignore `segment`.

### Tooltip

The tooltip is already large, so the window size is just a number with no unit. In the anomaly
table, a **Size** column appears after **Value** only when at least one hovered row has a shrunk
window. Rows with a full window leave the cell empty. In the simple layout (mobile and non-yearly
charts), which has no columns, a muted `size 6` follows the value (class
`chart-external-tooltip-window`, `0.75rem`). A first version put `6-yr avg` in the Value cell,
which took up too much room.

### Tests added

- `SeriesSmootherContractTests`: `WindowSizes` length equals input length; size is 0 exactly
  where the value is null.
- `CentredMovingAverageCalculatorTests`: full window reported for every value.
- `ShrinkingCentredMovingAverageCalculatorTests`: sizes match the odd-window table; null outputs
  report 0.
- `ChartDataBuilderTests`: shrunk bins recorded for the shrinking option, none for the centred one.
- `ChartTooltipMetadataBuilderTests`: bins mapped to chart indexes (unplotted bins dropped), null
  when there are none, and none for trend datasets.

Follow-ups: check the dash pattern and the tooltip note in the browser, on mobile as well.
