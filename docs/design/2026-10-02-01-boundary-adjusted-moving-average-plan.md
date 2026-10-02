# Boundary-adjusted moving average

- **Date:** 2026-10-02
- **Status:** Implemented 2026-10-02 (see addendum)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Core/Stats` (new `Smoothing` folder: interface, shared window
  logic, boundary-adjusted moving average; `CentredMovingAverageCalculator` moves here and implements
  the interface), `ClimateExplorer.UnitTests` (new
  smoother tests), `ClimateExplorer.Web.Client` — `UiModel/SeriesSmoothingOptions`,
  `UiModel/ChartSeriesDefinition`, `UiModel/SuggestedPresetLists.*`, `UiLogic/ChartSeriesListSerializer`,
  `Services/Chart/ChartDataBuilder`, `Components/Chart/ChartSeriesView*`, `Pages/Index.razor.cs`,
  `Pages/Global.razor.cs`, plus a new `Services/Chart/SeriesSmootherFactory`
- **Builds on:** [Trend projection / moving average lag investigation](../notes/2026-08-20-01-trend-projection-moving-average-lag-investigation.md)
- **Branch context:** `development`

## Goal

Add a second moving-average smoother, the **boundary-adjusted moving average**, which gives a
value for every year from the first year of data to the last. Today's centred moving average
leaves the first and last `windowSize / 2` points empty. Make the new smoother the default in
presets. Keep the centred moving average as a user-selectable option, renamed
`CentredMovingAverage` in the enum.

Put both behind a shared interface. The next smoother, **local linear regression with a uniform
window** ([Wikipedia: Local regression](https://en.wikipedia.org/wiki/Local_regression)), should
then need only one new class, one enum value and one dropdown item.

## Current state (recap)

- `CentredMovingAverageCalculator.CalculateCentredMovingAverage(values, windowSize, requiredDataThreshold)`
  ([CentredMovingAverageCalculator.cs](../../ClimateExplorer.Core/Stats/CentredMovingAverageCalculator.cs))
  is a static extension method. For point `i` it takes the window `i - windowSize/2` …
  `i - windowSize/2 + windowSize - 1`. That window is exactly centred for odd sizes; for even sizes
  it has one extra slot before `i`. If the window would run off either end, the result is `null`.
  Otherwise the result is the average of the non-null values in the window, provided
  `present / windowSize >= requiredDataThreshold`.
- `ChartDataBuilder` calls it with a hard-coded threshold of `0.75f` when
  `Smoothing == SeriesSmoothingOptions.MovingAverage` and the bin granularity is linear
  ([ChartDataBuilder.cs:431-436](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuilder.cs#L431-L436)).
  If fewer than 10 smoothed values survive, it falls back to unsmoothed data and shows a warning.
- `SeriesSmoothingOptions` is `None, MovingAverage, Trendline`
  ([SeriesSmoothingOptions.cs](../../ClimateExplorer.Web.Client/UiModel/SeriesSmoothingOptions.cs)).
  `Trendline` is not offered in the dropdown but is still handled in titles.
- The enum is persisted **by name** in the chart URL (segment 9 of each series, parsed with
  `Enum.Parse` in [ChartSeriesListSerializer.cs:104](../../ClimateExplorer.Web.Client/UiLogic/ChartSeriesListSerializer.cs#L104)).
  Shared links contain `MovingAverage`, and so do published blog posts, for example
  `BlogPosts/2023-07-25-temperature-anomaly.md`.

## The algorithm

### Window placement

Every point uses a window exactly `windowSize` slots wide. In the middle of the series the
window is placed as in the centred calculator. Near either end it is moved inward until it fits
inside the data:

```
start(i) = clamp(i - windowSize / 2, firstIndex, lastIndex - windowSize + 1)
window(i) = start(i) … start(i) + windowSize - 1
```

`firstIndex` and `lastIndex` are the first and last positions that have a **non-null** value
(see [Gaps](#gaps-and-requireddatathreshold)).

Worked example, `windowSize = 7`, data from 1950 to 2025:

| Point     | Window      | Shape                                   |
|-----------|-------------|-----------------------------------------|
| 1950      | 1950 – 1956 | forward: the first 7 years              |
| 1952      | 1950 – 1956 | forward                                 |
| 1953      | 1950 – 1956 | centred (3 before, 3 after)             |
| 1954      | 1951 – 1957 | centred                                 |
| …         | …           | centred                                 |
| 2022      | 2019 – 2025 | centred                                 |
| 2023      | 2019 – 2025 | trailing: the last 7 years              |
| 2025      | 2019 – 2025 | trailing                                |

This is the same method NOAA uses for the Mauna Loa seasonal cycle (quoted in
[the-one-number blog post](../../ClimateExplorer.Web/BlogPosts/2026-09-30-the-one-number.md)):
"…except for the first and last three and one-half years of the record, where the seasonal cycle
has been averaged over the first and last SEVEN years, respectively."

Properties that follow from this rule:

1. **The middle matches the centred average exactly.** Wherever the centred calculator returns
   a value, the boundary-adjusted smoother returns the same value. This includes the existing
   even-window convention. A unit test checks this.
2. **Every point is averaged over `windowSize` slots**, including points near the ends. The
   threshold therefore always has the same denominator (see below).
3. **The ends are flat.** The first `windowSize / 2` points all share the first window, so
   they get the same value. The last points all share the last window in the same way. On a
   rising series, the start is pulled up and the end is pulled down. This known limitation of
   any boundary-adjusted average is what local linear regression fixes
   ([below](#extension-point-local-linear-regression-with-a-uniform-window)).

#### Alternative considered: a sliding trailing/forward window

Another reading of "forward/trailing switching" gives each end point its own window: point `i`
near the end uses `i - windowSize + 1 … i`. This was rejected. At the switch from centred to
trailing, the window jumps **backwards** in time. With `windowSize = 7`, the window for point
`n-4` is `n-7 … n-1`, and the window for point `n-3` is `n-9 … n-3`. That puts a visible kink
in the curve, and the end of the curve lags behind the data. The investigation note discusses
the same lag. The clamped window never moves backwards, it uses the latest years for the last
point, and it matches the NOAA method.

### Gaps and `requiredDataThreshold`

A single rule covers interior gaps, padding and both ends:

> A point gets a value only if at least `requiredDataThreshold` of its `windowSize` slots hold a
> non-null value. The value is the mean of those non-null values.

- **Interior gaps:** handled as in the centred calculator, with the same denominator
  (`windowSize`) and the same `>=` comparison.
- **Missing value at the point itself:** if the window still passes the threshold, the point
  gets a smoothed value even though its raw value is null. The centred calculator already does
  this, and the new smoother keeps that behaviour.
- **Leading and trailing nulls (padding):** the boundaries are the first and last *non-null*
  values, not the ends of the array. Points outside that range are always `null`. Without this
  rule, a series padded with nulls (for example, a regional average whose bins start before
  any station reports) would put its forward window over the padding, fail the threshold, and
  lose the start of the record.
- **Data range shorter than `windowSize`:** no window fits, so every point is `null`. This
  follows from property 2: a window always has `windowSize` slots, and slots outside the data
  range are never counted. The 10-point fallback in `ChartDataBuilder` then shows the
  unsmoothed series with the existing warning.
- **`windowSize = 1`:** returns the input unchanged, nulls included.
- **Output length** always equals input length. `ChartDataBuilder` depends on this because it
  `Zip`s the result back onto the bin IDs.
- **Argument validation (new smoothers only):** `windowSize < 1`, or `requiredDataThreshold`
  outside `(0, 1]`, throws `ArgumentOutOfRangeException`. The centred calculator stays as it is.

Like today, smoothing works on array positions, not on bin IDs. It assumes the source records
are a contiguous run of bins, with missing bins present as null values. `ChartDataBuilder` and
the centred average already rely on this.

## Code design

### Core (`ClimateExplorer.Core/Stats/Smoothing/`)

```csharp
namespace ClimateExplorer.Core.Stats.Smoothing;

/// Smooths a series of evenly spaced values. Output has the same length as the input.
public interface ISeriesSmoother
{
    double?[] Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold);
}
```

```csharp
/// Where the window for each point sits. Kept separate so it can be tested on its own and
/// reused by any smoother that uses the same window placement.
public static class BoundaryAdjustedWindow
{
    /// Index of the first slot in the window for the point at <paramref name="index"/>,
    /// given data running from firstIndex to lastIndex inclusive.
    public static int GetStart(int index, int windowSize, int firstIndex, int lastIndex);
}
```

```csharp
/// Shared loop for smoothers that use a boundary-adjusted window: finds the data range,
/// places the window, applies the threshold, and hands the window to Estimate.
public abstract class BoundaryAdjustedWindowCalculator : ISeriesSmoother
{
    public double?[] Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold);

    /// Estimate for the point at targetIndex, from values[windowStart .. windowStart + windowSize - 1].
    /// Called only when the window passes the threshold. Return null if no estimate is possible.
    protected abstract double? Estimate(IReadOnlyList<double?> values, int windowStart, int windowSize, int targetIndex);
}

public sealed class BoundaryAdjustedMovingAverageCalculator : BoundaryAdjustedWindowCalculator
{
    // Estimate = mean of the non-null values in the window (targetIndex is not used).
}
```

```csharp
public sealed class CentredMovingAverageCalculator : ISeriesSmoother
{
    public double?[] Smooth(IReadOnlyList<double?> values, int windowSize, float requiredDataThreshold);
}
```

**`CentredMovingAverageCalculator` implements the interface directly.** The class becomes a
non-static `sealed` class in `Stats/Smoothing/`. Its algorithm becomes the body of `Smooth`.
The static extension method `CalculateCentredMovingAverage` goes, because a non-static class
can't hold extension methods and one way to call it is cleaner. Its only production caller is
`ChartDataBuilder`, which moves to the interface anyway. Behaviour doesn't change: the window
placement, the edge `null`s, and the threshold rule (including no argument validation) stay
exactly as they are, and the existing tests prove that. While the body is being moved, the
`Skip(start).Take(windowSize).ToArray()` per point can become an indexed loop over the
window. That's a small tidy-up that doesn't change results.

The centred calculator does **not** derive from `BoundaryAdjustedWindowCalculator`. It
measures edges from the ends of the array, not from the first and last non-null values.
Forcing it into the base class would change its output for padded series, and keeping that
output was the point of keeping the calculator.

Naming: each concrete smoother is a `…Calculator`, matching `CentredMovingAverageCalculator`
and the other classes in `Stats` (`PolynomialRegressionCalculator`, `StandardDeviationCalculator`).
The enum value stays `BoundaryAdjustedMovingAverage`.

Smoothers are stateless. Callers create them with `new`. They are not registered in DI: they
are pure maths, like `PolynomialRegressionCalculator`.

### Web.Client

**Enum** ([SeriesSmoothingOptions.cs](../../ClimateExplorer.Web.Client/UiModel/SeriesSmoothingOptions.cs)):

```csharp
public enum SeriesSmoothingOptions
{
    None,
    CentredMovingAverage,          // was MovingAverage. Keeps ordinal 1.
    Trendline,
    BoundaryAdjustedMovingAverage, // appended, so existing ordinals do not change
}
```

**Old URLs.** Add a `ParseSmoothing(string)` to `ChartSeriesListSerializer` that maps the old
token `MovingAverage` to `CentredMovingAverage` and passes anything else to `ParseEnum`. Old
links and blog-post links then show exactly what they showed before: a centred average with
trimmed ends. New URLs write the new names.

**Factory**, in a new `Services/Chart/SeriesSmootherFactory.cs`. The mapping lives here
because Core must not reference the Web enum:

```csharp
public static class SeriesSmootherFactory
{
    /// null for options that are not window smoothers (None, Trendline).
    public static ISeriesSmoother? Create(SeriesSmoothingOptions option) => option switch
    {
        SeriesSmoothingOptions.CentredMovingAverage          => new CentredMovingAverageCalculator(),
        SeriesSmoothingOptions.BoundaryAdjustedMovingAverage => new BoundaryAdjustedMovingAverageCalculator(),
        _ => null,
    };
}
```

Add `SeriesSmoothingOptionsExtensions.UsesWindow()`. It returns
`SeriesSmootherFactory.Create(o) != null`. Code that today asks "is this `MovingAverage`?"
calls it instead.

**`ChartDataBuilder`**: replace the `== SeriesSmoothingOptions.MovingAverage` test and the
direct `CalculateCentredMovingAverage` call with `SeriesSmootherFactory.Create(...)?.Smooth(...)`.
Keep the 10-point fallback. Move `0.75f` into a named constant
(`SmoothingRequiredDataThreshold`). Make the fallback warning say "smoothing" rather than
"moving average", so the text stays correct when local regression is added.

**`ChartSeriesView`**:

```razor
<SelectItem Value="SeriesSmoothingOptions.None">None</SelectItem>
<SelectItem Value="SeriesSmoothingOptions.BoundaryAdjustedMovingAverage">Moving average</SelectItem>
<SelectItem Value="SeriesSmoothingOptions.CentredMovingAverage">Centred moving average (trims ends)</SelectItem>
```

The default gets the plain label. The centred option's label says what is different about it.
`ShouldDisableSmoothingWindow` becomes `!csd.Smoothing.UsesWindow()`. The `Select` keeps its
existing accessible name from the "Smoothing" form label.

**Titles** (`ChartSeriesDefinition`, both the friendly-title and short-title switches): add a
case for each smoother. The boundary-adjusted one shows `"{N} year moving average"`, the same
text as today. The centred one shows `"{N} year centred moving average"`.

**Presets and defaults → `BoundaryAdjustedMovingAverage`**: every `SeriesSmoothingOptions.MovingAverage`
in `SuggestedPresetLists.Global.cs`, `SuggestedPresetLists.LocationBased*.cs` (all four
files), `Pages/Index.razor.cs` and `Pages/Global.razor.cs`.

**Docs:** in [CONTEXT.md](../../CONTEXT.md), change "after moving average" in the
`SeriesWithData` pipeline row to "after smoothing".

## Unit tests

The aim is that a reader can work out each expected value in their head from the test alone.
Each smoother test:

- uses short, small-integer inputs (mostly `1, 2, 3, …`) so window means are obvious;
- puts a comment above the expected values that shows each point's window;
- tests one rule.

New file `ClimateExplorer.UnitTests/Smoothing/BoundaryAdjustedMovingAverageCalculatorTests.cs`. Example of
the style:

```csharp
[TestMethod]
public void Smooth_Window3_UsesForwardWindowAtStartAndTrailingWindowAtEnd()
{
    double?[] values = [1, 2, 3, 4, 5, 6, 7, 8, 9];

    var result = Smooth(values, windowSize: 3);

    // index:   0        1        2        …  7        8
    // window:  [1 2 3]  [1 2 3]  [2 3 4]  …  [7 8 9]  [7 8 9]
    //          forward  centred  centred     centred  trailing
    CollectionAssert.AreEqual(new double?[] { 2, 2, 3, 4, 5, 6, 7, 8, 8 }, result);
}
```

### `BoundaryAdjustedWindowTests` (window placement only)

| Test | Case | Expected start |
|------|------|----------------|
| `GetStart_PointInMiddle_WindowIsCentred` | i=5, w=3, data 0..9 | 4 |
| `GetStart_FirstPoint_WindowStartsAtFirstValue` | i=0, w=7, data 0..9 | 0 |
| `GetStart_PointWithinHalfWindowOfStart_WindowStartsAtFirstValue` | i=2, w=7, data 0..9 | 0 |
| `GetStart_LastPoint_WindowEndsAtLastValue` | i=9, w=7, data 0..9 | 3 |
| `GetStart_EvenWindowInMiddle_MatchesCentredConvention` | i=5, w=4, data 0..9 | 3 |
| `GetStart_DataRangeOffsetByPadding_ClampsToDataRange` | i=2, w=3, data 2..6 | 2 |

### `BoundaryAdjustedMovingAverageCalculatorTests`

**Window shape**

| Test | Input | w | Threshold | Expected |
|------|-------|---|-----------|----------|
| `Smooth_Window3_UsesForwardWindowAtStartAndTrailingWindowAtEnd` | 1..9 | 3 | 0.75 | `2,2,3,4,5,6,7,8,8` |
| `Smooth_Window7_MatchesSevenYearExample` (the example from the request) | 1..10 | 7 | 0.75 | `4,4,4,4,5,6,7,7,7,7` |
| `Smooth_EvenWindow4_UsesSameConventionAsCentredAverage` | 1..6 | 4 | 0.75 | `2.5,2.5,2.5,3.5,4.5,4.5` |
| `Smooth_PointsWithFullCentredWindow_MatchCentredMovingAverage` | a realistic 30-value series with a few gaps | 3, 4, 7, 10 | 0.75 | equal to `CentredMovingAverageCalculator.Smooth` wherever that is non-null |

**Gaps and threshold**

| Test | Input | w | Threshold | Expected |
|------|-------|---|-----------|----------|
| `Smooth_InteriorGapBelowThreshold_GivesNullAroundGap` | `1,2,3,_,5,6,7` | 3 | 0.75 | `2,2,_,_,_,6,6` (the centred average gives `_,2,_,_,_,6,_`) |
| `Smooth_InteriorGapWithSofterThreshold_AveragesRemainingValues` | `1,2,3,_,_,6,7` | 3 | 0.6 | `2,2,2.5,_,_,6.5,6.5` |
| `Smooth_MissingValueWithEnoughNeighbours_IsFilledBySmoothedValue` | `1,2,3,_,5,6,7,8` | 5 | 0.75 | index 3 = mean(2,3,5,6) = 4 |
| `Smooth_ProportionExactlyAtThreshold_GivesValue` | `1,_,3,4` | 4 | 0.75 | every point = mean(1,3,4) |
| `Smooth_LeadingAndTrailingNulls_BoundariesFollowFirstAndLastValues` | `_,_,1,2,3,4,5,_` | 3 | 0.75 | `_,_,2,2,3,4,4,_` |
| `Smooth_GapAtVeryStart_ForwardWindowStillAppliesThreshold` | `1,_,_,4,5,6,7` | 3 | 0.6 | index 0 window `[1,_,_]` = 1/3 → `_` |

**Edge cases**

| Test | Input | Expected |
|------|-------|----------|
| `Smooth_DataShorterThanWindow_GivesAllNull` | `1,2`, w=3 | `_,_` |
| `Smooth_Window1_ReturnsInputUnchanged` | `1,_,3` | `1,_,3` |
| `Smooth_AllNull_GivesAllNull` | `_,_,_` | `_,_,_` |
| `Smooth_EmptyInput_GivesEmptyOutput` | `[]` | `[]` |
| `Smooth_WindowSizeZero_Throws` | — | `ArgumentOutOfRangeException` |
| `Smooth_ThresholdOutOfRange_Throws` | 0 and 1.01 | `ArgumentOutOfRangeException` |

### `CentredMovingAverageCalculatorTests` (existing)

Keep every case and expected value. Only these change:

- The calls go from `values.CalculateCentredMovingAverage(w, t)` to
  `new CentredMovingAverageCalculator().Smooth(values, w, t)`. Use the same small `Smooth(...)`
  helper as the new tests.
- The tests move to `UnitTests/Smoothing/`.
- The method names are renamed to the `MethodName_StateUnderTest_ExpectedBehavior` convention,
  for example `Window3GivesExpectedValuesAroundCentralNull` →
  `Smooth_Window3WithCentralNull_GivesNullAroundGap`.

These unchanged expected values are what prove the move into the interface kept the behaviour.

### `SeriesSmootherContractTests`

These tests run against **every** `ISeriesSmoother` through `[DynamicData]`. Local linear
regression gets the shared contract checks by adding one row:

- `Smooth_AnyInput_OutputLengthEqualsInputLength`
- `Smooth_Window1_ReturnsInputUnchanged`
- `Smooth_AllNull_GivesAllNull`
- `Smooth_ConstantSeries_ReturnsSameConstantWhereDefined`

### Web.Client tests

- `ChartSeriesListSerializer`: `Parse_LegacyMovingAverageToken_MapsToCentredMovingAverage`, and
  a round-trip for `BoundaryAdjustedMovingAverage`.
- `ChartDataBuilderTests`: rename the existing `MovingAverage` tests to `CentredMovingAverage`
  and keep their assertions. Add
  `BuildAsync_BoundaryAdjustedMovingAverage_SmoothedSeriesRunsToLastRawYear`, which checks that
  the last bin is non-null, unlike the centred average.
- `SeriesSmootherFactory`: each enum value maps to the expected type, or to null.
- Update the remaining test references (`ChartSeriesLocationSubstitutionServiceTests`,
  `ChartStateUrlServiceTests`) mechanically.

Verify with `dotnet build` and the unit test suite only, as AGENTS.md requires.

## Extension point: local linear regression with a uniform window

Adding the next smoother needs:

1. `LocalLinearRegressionCalculator : BoundaryAdjustedWindowCalculator`. `Estimate` fits an
   ordinary-least-squares line to the `(k - targetIndex, values[k])` pairs for the non-null
   values in the window. All weights are equal (a "uniform window", or rectangular kernel, not
   LOESS's tricube weights). It returns the **intercept**, which is the fitted value at
   `targetIndex`. Measuring x from the target keeps the numbers small and makes the intercept
   the answer. The closed-form sums are short. `PolynomialRegressionCalculator` is not reused,
   because it computes significance statistics on every call, and this runs once per point.
   If fewer than two distinct x values are present, it returns `null`.
2. One enum value (appended), one factory case, one dropdown item, one title case.
3. One row in `SeriesSmootherContractTests`, plus its own readable tests. The key test is that
   a perfectly linear input (`1..9`) comes back **unchanged**, ends included, while the
   boundary-adjusted average flattens the ends to `2` and `8`. That shows the local linear
   fit has no boundary bias.

The window placement, threshold, gap handling and data-range rules are inherited unchanged,
so "consistent gap handling" holds for every smoother. That is the reason the loop is in the
base class and only `Estimate` varies.

## Risks

- **Trend fits on smoothed series.** The trend module fits the plotted, smoothed series (see
  the investigation note). The flat ends add up to `windowSize / 2` nearly identical points at
  each end, which makes fitted slopes flatter, most of all for short trend windows such as
  RecentDecade with a large smoothing window. For trends, the gain (the series reaches the
  last raw year, so the projection lag in the investigation note goes away for this option)
  should be weighed against this bias. Local linear regression removes it. This plan does not
  change how trends are fitted.
- **Enum rename.** Old URLs depend on the legacy-token mapping, which needs a test.
  No integer persistence of the enum was found, but the ordinals are kept stable anyway.
- **Visual change to presets.** Every preset chart will now reach the ends of its data. The
  last few points are less smoothed than they look, because they share one window. The
  dropdown label and title make the method visible. A tooltip note about the ends is an
  optional addition.

## Addendum — implementation notes (2026-10-02)

Implemented as planned. `dotnet build` is clean and all 655 unit tests pass. The UI was not
checked in a browser (AGENTS.md).

**Deviations from the plan**

- **Centred calculator off-by-one fixed.** The old code checked
  `i - windowSize/2 >= 0 && i + windowSize/2 < length` but averaged
  `i - windowSize/2 … i - windowSize/2 + windowSize - 1`. For even window sizes, the check
  covered one more slot than the average used, so the last point that had a full window was
  needlessly `null`. `CentredMovingAverageCalculator.Smooth` now checks the window it averages.
  Odd windows are unaffected, and so are all the original test cases. Even windows now show one
  more point at the end: a 10-year centred average over 1900–2025 now ends at 2021, not 2020.
  `BuildAsync_TrendOnCentredMovingAverageSmoothedSeries_ProjectsFromAfterTheTrueLastRawYear`
  was updated to match, and `Smooth_EvenWindow4_HasExtraSlotBeforePoint` pins the new behaviour.
  The plan's "behaviour doesn't change" was therefore not quite true for even windows.
- **No `SeriesSmootherFactory` class.** The enum-to-smoother mapping is
  `SeriesSmoothingOptionsExtensions.CreateSmoother()`, next to `UsesWindow()` in
  `UiModel/SeriesSmoothingOptionsExtensions.cs`. One small file instead of two, and callers
  read naturally: `cs.ChartSeries.Smoothing.CreateSmoother()`.
- **Shared window maths.** `SmoothingWindow` (internal, in `Stats/Smoothing`) holds
  `MeetsThreshold` and `Mean`. Both calculators use it, so they apply the threshold the same
  way, even though the centred calculator does not derive from `BoundaryAdjustedWindowCalculator`.
- **Test placement.** The new serializer tests
  (`ParseChartSeriesDefinitionList_LegacyMovingAverageToken_MapsToCentredMovingAverage` and the
  smoothing round-trip) are in `ChartSeriesListSerializerTrendTests`, to reuse its URL fixtures.
  The factory tests are `SeriesSmoothingOptionsExtensionsTests`.
- **Fallback warning.** The message now says "not enough … data for smoothing", and the
  existing test asserts on "smoothing".
