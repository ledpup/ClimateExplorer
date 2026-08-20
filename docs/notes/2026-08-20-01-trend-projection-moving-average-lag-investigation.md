# Trend projection start year lags true "now" when a moving average is applied

## Summary

Confirmed as a real bug, not just a look. When a chart series has moving-average
smoothing on, the trend module's projection is anchored to the **last plotted
(smoothed) year**, not the last calendar year that actually has raw data. For a
centred `N`-year moving average, the last `N / 2` years of the record never get a
smoothed value at all, so the projection is drawn starting `N / 2` years earlier
than it should be — over years for which real, measured (just unsmoothed) data
already exists. A 10-year moving average with data through 2025 plots its last
smoothed point at 2020 and starts "predicting" at 2021, five years into the past.

It looks reasonable on the chart only by coincidence: a centred moving average's
last plotted point already incorporates the years up to (in the 10-year/2020
example) 2024 into its average, so the boundary value is already fairly
"current," and a linear trend doesn't move much year to year near the boundary.
The projected dot for 2021 therefore lands close to where a real 2021 point would
have landed anyway — but that's masking the issue, not evidence the projection is
conceptually correct.

## Root cause

- The trend fit is computed over `SeriesWithData.PreProcessedDataSet`
  (the **plotted**, i.e. already-smoothed, series), per
  [ChartDataBuilder.cs:169](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuilder.cs#L169)
  calling `BuildTrendPoints(cs.PreProcessedDataSet!, binIdsToPlot)`. This is a
  deliberate, documented decision — see Assumption 1 and Assumption 3 of
  [the trend module design doc](../design/2026-08-11-01-chart-series-trend-module.md#assumptions):
  "Smoothed series are fitted as smoothed."
- `ChartSeriesTrendCalculator.Calculate` then derives `LastDataYear` from the
  **last point in that same smoothed series**
  ([ChartSeriesTrendCalculator.cs:94](../../ClimateExplorer.Web.Client/Services/Trends/ChartSeriesTrendCalculator.cs#L94)):
  ```csharp
  var lastDataYear = (int)Math.Round(ordered[^1].X);
  ```
  and the projection always starts at `lastDataYear + 1`
  ([ChartSeriesTrendCalculator.cs:165](../../ClimateExplorer.Web.Client/Services/Trends/ChartSeriesTrendCalculator.cs#L165)).
- `CalculateCentredMovingAverage`
  ([CentredMovingAverageCalculator.cs:14-20](../../ClimateExplorer.Core/Stats/CentredMovingAverageCalculator.cs#L14-L20))
  only ever computes a value where the **full** window fits inside the array:
  ```csharp
  int startIndex = 0 - (windowSize / 2);
  int endIndex = windowSize / 2;
  for (int i = 0; i < valuesArray.Length; i++, startIndex++, endIndex++)
  {
      if (startIndex < 0 || endIndex >= valuesArray.Length)
      {
          result.Add(null);   // <- no partial-window averaging at the trailing edge
          continue;
      }
      ...
  }
  ```
  There's no partial-window fallback at the edges (the `requiredDataThreshold`
  parameter only governs *internal* gaps inside an otherwise-full window, not
  edge trimming). So the last `windowSize / 2` years of every moving-averaged
  series are unconditionally `null` and never plotted — regardless of how much
  real data exists for those years.
- `ChartDataBuilder` deliberately smooths *before* computing the chart's start/end
  bins for exactly this reason (comment at
  [ChartDataBuilder.cs:382-386](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuilder.cs#L382-L386)),
  so `chartEndBin`/the chart subtitle already reflect the smoothed end year, not
  the raw one. The trend module inherits that same truncated "last year" for a
  purpose (starting a *prediction*) where it isn't right — the subtitle case is
  fine because it's describing the plotted line, but "predict from here" implies
  "this is where real knowledge ends," which isn't true when smoothing is on.

### Worked example

10-year centred moving average, raw yearly data 1910–2025 (116 points):
- `windowSize / 2 == 5`, so the last valid smoothed index is `length - 1 - 5`,
  i.e. calendar year **2020**. Years 2021–2025 all get `null` in
  `PreProcessedDataSet`, even though 2021–2025 have real, measured raw values.
- `ChartSeriesTrendCalculator.LastDataYear` → 2020.
- Projection starts at 2021 and is drawn as a "predicted" point — for a year
  that has already happened and for which the site already has (unsmoothed)
  data.
- The bug compounds: `LastDataYear` also anchors the **"Predict until"** control
  ([ChartSeriesTrendControls.razor.cs:69](../../ClimateExplorer.Web.Client/Components/Chart/Trend/ChartSeriesTrendControls.razor.cs#L69)),
  so the field's validation bounds ("enter a year from 2021 to 2120") are five
  years stale too.

### Scope check

This is specific to `SeriesSmoothingOptions.MovingAverage`. With smoothing off,
`PreProcessedDataSet == SourceDataSet`
([ChartDataBuilder.cs:438](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuilder.cs#L438)),
so `LastDataYear` already equals the true last raw year and no bug exists. It's
also proportional to window size — a 3-year moving average only lags by a year,
which is easy to miss; a 30-year one (available as a smoothing option) would lag
by 15 years, which would be obviously wrong on sight.

## Fix options

### 1. Don't support trends on smoothed series

Disable the trend module (grey out "Show trend") whenever
`Smoothing == MovingAverage`, mirroring how the module is already hidden for
non-yearly bin granularities.

- **Pro:** simplest possible fix, zero ambiguity, no new edge cases.
- **Con:** removes the feature exactly where it's most requested. A user who
  smooths a noisy series is very often doing it *in order to* see the underlying
  trend more clearly — "smooth this and show me where it's heading" is a
  completely reasonable combination to want. This also reopens a decision the
  design doc already made deliberately (Assumption 3 chose to support trends on
  smoothed series, accepting the optimistic-p-value caveat as a tradeoff, not an
  oversight).

### 2. Anchor the projection to the true last raw year, not the last plotted year (your suggestion)

Compute a second "true last data year" from the **unsmoothed** `SourceDataSet`
(max year with a finite value) and pass it into `ChartSeriesTrendCalculator`
alongside the smoothed `points`. Use it — instead of `ordered[^1].X` — as
`lastDataYear` for both the projection start (`lastDataYear + 1`) and the
"Predict until" anchor. The regression itself is unaffected: it's still fitted
over the smoothed `points`, exactly as today; only the X-value the projection
resumes from changes.

- **Pro:** directly fixes the reported problem — a projected point is never
  drawn for a year that already has real data. Small, localised change (one new
  input to `ChartSeriesTrendCalculator.Calculate`, computed once in
  `ChartDataBuilder.ApplyTrends` from `cs.SourceDataSet`); no change to the
  regression math, significance test, or existing tests' fitted values.
  No-op whenever smoothing is off (raw and smoothed last-year already agree),
  so it can't regress the unsmoothed case.
- **Con:** introduces a visible gap on the chart between the last smoothed point
  (2020) and the first projected point (2026) — `windowSize / 2` years wide, in
  which nothing is drawn for that series at all (not the smoothed line, not a
  projection). That's honest, but it needs to *read* as "nothing plottable here
  yet," not as "data missing/broken." Worth a short label or tooltip note on the
  trend series, and/or mentioning it in the About-trends panel's overview text
  for a smoothed subject.
- **Implementation note:** `binIdsToPlot` (used to build the smoothed `points`)
  is derived from `chartBins`, which stop at the smoothed end year — so the true
  last raw year has to be read from `cs.SourceDataSet.DataRecords` directly,
  unfiltered by `binIdsToPlot`, not from anything already restricted to the
  plotted range.

## Other ideas considered

### 3. Fit the regression on raw (unsmoothed) data regardless of display smoothing

Decouple "what's drawn" from "what's fitted": always regress the underlying
yearly values, so `LastDataYear` is naturally the true last raw year and the bug
disappears without any special-casing. This also removes the "smoothed p-value
is optimistic" caveat the design doc already flags as a known cost of fitting on
smoothed data.

Rejected as the primary fix here because it reverses a decision the original
design doc made deliberately and explicitly (Assumption 3), for reasons beyond
this bug — it would also mean the fitted line's slope/significance no longer
matches what the plotted (smoothed) curve visually appears to do, which is its
own source of confusion (the About-trends panel's "Data" section shows the
points that were fit; those would stop being the points on the chart). Worth
revisiting only as a deliberate, separate decision — not folded into this fix.

### 4. Give the moving average a partial-window tail instead of truncating

Today, `CalculateCentredMovingAverage` requires the **full** `windowSize`-wide
window to exist inside the array; if the window would run off either end, the
point is `null`, full stop. The alternative is to let the last few points be
computed from *whatever's available*, rather than refusing to compute them at
all. Two separate design choices are bundled inside "partial window," and they
matter independently:

**a) How is the shrinking window shaped?**
- *Symmetric/shrinking-centred* — keep it centred on `i`, just let each side
  shrink to whatever's in bounds (e.g. `effectiveStart = Math.Max(i - windowSize/2, 0)`,
  `effectiveEnd = Math.Min(i + windowSize/2, length - 1)`). No directional bias
  introduced; variance simply increases as the window narrows toward the very
  last point (which would be an average of 1 — itself).
- *Trailing/one-sided* — for points too close to the edge to centre, fall back
  to an average of only the preceding years. This is what a lot of "moving
  average" implementations default to, but it introduces **systematic lag**:
  a trailing average of a rising series is biased low relative to a centred one
  (and biased high on a falling series). For a warming record specifically,
  swapping to trailing-only right at the years people care about most (the
  most recent ones) would quietly understate recent warming — the opposite of
  what you'd want from the point that's supposed to represent "now." Symmetric
  shrinking is the better shape for this codebase.

**b) Does the existing `requiredDataThreshold` (currently `0.75f`) apply to the
window as clipped, or as originally requested?** This changes how much of the
gap actually closes:
- Denominator = the **clipped** window's own size (i.e. "are ≥75% of the slots
  I could physically have here filled?") → every point through the true last
  raw year gets *some* value, all the way to a single-point "average" for the
  final year. Closes the gap completely, but the last couple of points are
  built from very few samples.
- Denominator = the **nominal, full** `windowSize` (i.e. "are ≥75% of a full
  window's worth of data present, even though the window itself is clipped?")
  → the cutoff softens but doesn't disappear. Worked example, 10-year window,
  `threshold = 0.75`: the last raw year (`i = length-1`) can only ever see 6 of
  10 nominal slots (`i-5..i`) → 60%, still null. Two years back, 8 of 10 slots
  are available → 80%, passes. So instead of a hard 5-year cutoff, only the
  last ~2 years stay null — smaller gap, not zero, and the threshold keeps its
  current meaning ("mostly real data, not mostly padding") for every point.

Either variant of (b) shrinks or removes option 2's gap by letting the
*plotted* series itself reach closer to the true last year, which is the
appeal.

**Stability caveat, independent of (a)/(b):** every point in the *current*
implementation, once computed, is fixed forever (barring a correction to the
historical record) — its whole window already existed the day it was drawn. A
partial-window tail breaks that: the smoothed value for, say, "last year" would
be recomputed and would *change* every time a new year of data lands and its
window fills in further, only stabilising once it ages far enough to get a full
window. That's normal practice for published centred smoothing (GISTEMP and
similar records mark their most recent few points "provisional" for exactly
this reason), but it's a real behavioural change from what this site currently
guarantees, and it would need the same kind of visual callout (dashed segment,
footnote, or similar) that option 2's gap needs — "these last points will move
as more data arrives" rather than "these years aren't shown yet."

Not proposed as a fix here: it changes the shape of every moving-averaged chart
on the site near its right edge (not just trend consumers), trades a hard
cutoff for a boundary computed from a smaller and non-final sample, and (a)/(b)
above are genuine judgement calls rather than a mechanical fix — a legitimate
but separate UX/statistics decision that shouldn't ride on a trend bugfix.
Flagging it because it's the only option on this list that actually removes
option 2's gap rather than just making the gap honestly-labelled.

## Recommendation

Option 2. It fixes the actual defect (a "prediction" for a year that isn't a
prediction), costs one extra input threaded through
`ChartSeriesTrendCalculator`, doesn't touch the regression/significance math or
existing tests, and is a no-op for the large majority of series that don't use
moving-average smoothing. The resulting gap is a correct depiction of "the
smoothed line and the trend module both genuinely have nothing to say about
these years yet" and just needs a small UI note so it doesn't read as broken.
Option 4 is worth a future look as a way to shrink that gap, independent of this
fix.

## Addendum — option 2 implemented (2026-08-20)

Shipped as described, with `ChartDataBuilder` as the only caller that knows both
numbers:

- `ChartSeriesTrendCalculator.Calculate` gained an optional `lastMeasuredYear`
  parameter. When supplied, it replaces `ordered[^1].X` as the projection's
  anchor (both the projection-start year and `ChartSeriesTrend.LastDataYear`,
  which also drives the "Predict until" bounds). It's genuinely optional -
  omitted, behaviour is unchanged, which is what every existing
  `ChartSeriesTrendCalculatorTests` case relies on. The regression fit itself
  (`Windows`, `Points`) is untouched; only where the projection resumes from
  changed.
- `ChartDataBuilder.ApplyTrends` computes it per series via a new
  `GetLastMeasuredYear(cs.SourceDataSet, state)` helper:
  `SourceDataSet.GetEndYearForDataSet()` (an existing `Core.Model` extension -
  last raw year with a finite value) clamped down, but never up, by an explicit
  `state.EndYear` filter - the same "only clamps downward" rule
  `ChartLogic.GetBinRangeToPlotForGaplessRange` already applies when computing
  the chart's own bin range from the *smoothed* series. `ChartAllData` or an
  end-year filter past the true end of the record both leave it unclamped.
- No new gap-labelling UI was added. The gap is real (nothing is plotted for
  the series, historical or projected, between the last smoothed point and the
  new projection start) and reads as a blank stretch of chart rather than
  anything mislabelled - revisit if that turns out to need a callout in
  practice.

**Tests:** `ChartSeriesTrendCalculatorTests` - four new cases covering
`lastMeasuredYear` supplied vs. omitted, and that the fitted line (not just the
start year) is unaffected. `ChartDataBuilderTests` - two new end-to-end cases:
a 10-year moving average trimming 5 years off a 126-year record confirms
`LastDataYear`/`Projection.FirstYear` land on the true raw end (2025/2026, not
2020/2021), and a variant with an explicit `EndYear` filter confirms the clamp.
`dotnet build` on the solution and the full unit suite (551 tests) are both
clean.

**Not done, out of scope for this fix:** the chart subtitle's year range has a
related but distinct bug - see the note below.

## Related, separate bug: the chart subtitle reports the smoothed range, not the raw one

Noticed while implementing the fix above, not fixed here. The chart subtitle
(e.g. "1939–2020, 82 years") is built from `chartStartBin`/`chartEndBin`
([ChartDataBuilder.cs:480-489](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuilder.cs#L480-L489)),
which are deliberately computed from `PreProcessedDataSet` - the *smoothed*
series - per the comment at
[ChartDataBuilder.cs:382-386](../../ClimateExplorer.Web.Client/Services/Chart/ChartDataBuilder.cs#L382-L386).
For a moving-average series this means the subtitle reports the smoothed
series' trimmed range on **both** ends (e.g. 1939–2020), not the underlying raw
record it was computed from (e.g. 1931–2025) - the same `windowSize / 2`-year
trim on both edges that causes the trend bug above, just visible in a different
place.

This is related (same root cause: the site conflates "what's plotted" with
"what's known" wherever a moving average is active) but is a separate decision
from the trend fix: the subtitle describes the *whole chart*, not one series'
trend, so fixing it touches `ChartLogic.GetBinRangeToPlotForGaplessRange` and
what "1939–2020, 82 years" is supposed to mean for every moving-average chart
on the site, whether or not trends are involved - and, like fix option 2 above,
it would leave the actual plotted line still stopping at 2020, so a subtitle
reading "1931–2025" while the line itself only reaches 2020 needs the same kind
of "why does the line stop before the subtitle's end year" explanation this
note flagged for the trend's gap. Worth its own investigation rather than
folding into this one.
