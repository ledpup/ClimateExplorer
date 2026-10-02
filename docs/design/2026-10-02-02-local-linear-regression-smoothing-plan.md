# Local linear regression smoothing

- **Date:** 2026-10-02
- **Status:** Implemented 2026-10-02 (see addendum)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Core/Stats/Smoothing` (new `LocalLinearRegressionCalculator`,
  `BoundaryAdjustedMovingAverageCalculator` removed), `ClimateExplorer.UnitTests` (new calculator
  tests, updated references), `ClimateExplorer.Web.Client` — `UiModel/SeriesSmoothingOptions`,
  `UiModel/SeriesSmoothingOptionsExtensions`, `UiModel/ChartSeriesDefinition`,
  `UiModel/SuggestedPresetLists.*`, `Components/Chart/ChartSeriesView.razor`, `Pages/Index.razor.cs`,
  `Pages/Global.razor.cs`
- **Builds on:** [Boundary-adjusted moving average](2026-10-02-01-boundary-adjusted-moving-average-plan.md)
- **Branch context:** `issues/smoothing`

## Goal

Replace the boundary-adjusted moving average with **local linear regression with a uniform
window** ([Wikipedia: Local regression](https://en.wikipedia.org/wiki/Local_regression)) as the
default smoothing. It gives a value for every year from the first year of data to the last, and
the values near the ends follow the data instead of repeating.

## Why the boundary-adjusted moving average was rejected

The point of the earlier plan was that a smoothed chart should not look as though data is
missing at its ends. The boundary-adjusted moving average filled the ends, but with the same
number over and over: with a 20-year window, the last 10 years all share one window, so they all
get the same value. A flat line at the end of a warming record is misleading. It is better to
show nothing there, as the centred moving average does, than to show that.

This follows directly from the method (the earlier plan lists it as property 3), but its effect
on real charts was only clear once it was running.

### Alternative considered: kernel-weighted average

Keep the shifted window but weight each value by its distance from the target year, for example
with tricube weights (a Nadaraya-Watson estimator). The end values then differ from each other.
Rejected for two reasons:

- **It still lags at the ends.** At the last year, every other value in the window is from
  the past. Weighting recent years more reduces the lag but does not remove it, so the end of
  a rising series still reads low.
- **It changes the middle of the curve.** Every point becomes a weighted average, so the curve
  no longer matches a plain moving average anywhere.

On the series `1, 2, …, 9` with a window of 3, the last two values (true values 8 and 9) are:

| Method | Last two values |
|---|---|
| Boundary-adjusted moving average | 8, 8 |
| Tricube-weighted average | 8, about 8.6 |
| Local linear regression | 8, 9 |

## The algorithm

For each point, fit a straight line by ordinary least squares through the non-null values in its
window, with every value weighted equally. The smoothed value is the line's value at that point.

The window, the threshold and the gap rules are unchanged from the earlier plan. They live in
`BoundaryAdjustedWindowCalculator`, which the new calculator derives from:

- The window is always `windowSize` slots wide. It is centred in the middle of the data and
  moved inward near either end so that it fits.
- A point gets a value only if at least `requiredDataThreshold` of its window's slots hold a
  value.
- The data range runs from the first to the last non-null value. Points outside it are `null`,
  and so is every point if the data range is shorter than the window.

What the line fit adds:

- **Near the ends** the point is off-centre in its window, so the line carries the local trend
  out to it. The last years of a record each get their own value. A series that is a straight
  line comes back unchanged, ends included.
- **In the middle**, when the window is symmetric about the point and has no gaps, the fitted
  value is the mean of the window. For odd window sizes the curve therefore equals the centred
  moving average.
- **Even window sizes** (10 and 20 are the common ones) have one more slot before the point
  than after it. The centred moving average ignores this, which shifts its curve half a period
  late. The line fit corrects for it, so the two curves differ slightly in the middle.
- **Gaps** in a window move the fit's centre. The line is fitted through the values that are
  present and read at the target, so a gap no longer biases the result toward the side with
  more data.
- **A window with one value** (possible only with a low threshold, or a window size of 1)
  cannot define a slope. The result is that value.

The fit measures x from the target point (`x = index - targetIndex`), so the line's intercept
is the answer and the sums stay small. It is computed with the closed-form sums.
`PolynomialRegressionCalculator` is not used: it computes significance statistics on every
call, and this runs once per point.

## Code changes

**Core**

- New `LocalLinearRegressionCalculator : BoundaryAdjustedWindowCalculator`, overriding `Estimate`.
- `BoundaryAdjustedMovingAverageCalculator` is deleted, with its tests.
- `BoundaryAdjustedWindow`, `BoundaryAdjustedWindowCalculator`, `SmoothingWindow`,
  `ISeriesSmoother` and `CentredMovingAverageCalculator` are unchanged.

**Web.Client**

- `SeriesSmoothingOptions.BoundaryAdjustedMovingAverage` is renamed `LocalLinearRegression`,
  keeping its ordinal. The old name was never on `master`, so no saved URLs contain it and no
  legacy mapping is needed. The `MovingAverage` → `CentredMovingAverage` mapping stays.
- `CreateSmoother()` returns the new calculator for the new value.
- Dropdown: `None`, `Local linear regression`, `Centred moving average`. The "(trims ends)"
  suffix is dropped because the other option's name no longer says "moving average".
- Titles: `"{N} year local linear regression"` and `"{N} year centred moving average"`.
- Tooltips say "smoothing" and "rolling window" instead of "averaging" and "rolling average".
- Every preset and page default that used `BoundaryAdjustedMovingAverage` uses
  `LocalLinearRegression`.

## Unit tests

`LocalLinearRegressionCalculatorTests` follows the style of the earlier plan: small integer
inputs, with a comment above each expected result showing the window and the fitted line.

| Test | Shows |
|------|-------|
| `Smooth_StraightLine_ReturnsInputUnchangedIncludingEnds` | `1..9`, window 3, comes back as `1..9` |
| `Smooth_StraightLineWithEvenWindow_ReturnsInputUnchanged` | the same for window 4 |
| `Smooth_OneWindow_GivesLineOfBestFitAtEachPoint` | `1,2,6` → `0.5, 3, 5.5`, with the fit worked in the comment |
| `Smooth_PointCentredInOddWindow_EqualsMeanOfWindow` | the middle matches a moving average |
| `Smooth_LastPointsShareOneWindow_EachGetsADifferentValue` | the reason this smoother exists |
| `Smooth_InteriorGapBelowThreshold_GivesNullAroundGap` | threshold unchanged |
| `Smooth_InteriorGapWithSofterThreshold_FitsLineThroughRemainingValues` | fit through the values present |
| `Smooth_MissingValueWithEnoughNeighbours_IsFilledFromLine` | a gap is filled from the line |
| `Smooth_WindowWithSingleValue_GivesThatValue` | the one-value rule |
| `Smooth_LeadingAndTrailingNulls_BoundariesFollowFirstAndLastValues` | padding |
| `Smooth_DataShorterThanWindow_GivesAllNull` (and after trimming padding) | short data |
| `Smooth_Window1_ReturnsInputUnchanged`, `Smooth_AllNull_GivesAllNull`, `Smooth_EmptyInput_GivesEmptyOutput` | edge cases |
| `Smooth_WindowSizeZero_Throws`, `Smooth_ThresholdOutOfRange_Throws` | argument checks |

Expected values are compared with a tolerance of `1e-9`, because a fitted value is the result of
several floating-point operations.

`SeriesSmootherContractTests` gets the new calculator in place of the removed one.
`BoundaryAdjustedWindowTests` is unchanged. `ChartDataBuilderTests`, the serializer round-trip
test and `SeriesSmoothingOptionsExtensionsTests` switch to the new enum value.

## Risks

- **The ends are less certain than the middle.** The last points are read from the end of a
  fitted line, not its centre. They will move when a new year of data arrives, and an unusual
  final year tilts the line. With a 20-year window the effect is modest. With a 3- or 5-year
  window the ends will be jumpy.
- **Trend fits on smoothed series.** The trend module fits the plotted, smoothed series. The
  smoothed series now reaches the last raw year, and its end values are no longer repeated, so
  the flattening risk from the earlier plan is gone. The end values are still estimates, so a
  short trend period over a heavily smoothed series deserves the same caution as before.
- **Default charts change.** In the middle, an even-window curve differs slightly from the old
  centred moving average (see above). Charts opened from old URLs still use the centred moving
  average and are unchanged.

## Addendum — implementation notes (2026-10-02)

Implemented as planned. `dotnet build` is clean and all 652 unit tests pass. The UI was not
checked in a browser (AGENTS.md).

- `BoundaryAdjustedMovingAverageCalculator.cs` and its test file were renamed with `git mv` and
  rewritten, so their history carries over to the new calculator.
- The test count dropped from 655 to 652 because the removed calculator's tests included a
  four-row cross-check against the centred moving average. Its replacement is
  `Smooth_PointCentredInOddWindow_EqualsMeanOfWindow`.
