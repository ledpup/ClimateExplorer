# Polynomial regression for chart trends

- **Date:** 2026-08-14
- **Status:** Implemented 2026-08-14 (see addendum)
- **Author:** Patrick Lea (with Claude)
- **Scope:** `ClimateExplorer.Core/Stats` (calculator + model rename/generalisation), `ClimateExplorer.UnitTests`
  (new fixtures + tests), `ClimateExplorer.Web.Client` — `Services/Trends/ChartSeriesTrendCalculator`,
  `Services/Trends/TrendFormatting`, `Services/Trends/TrendStatSectionBuilder`,
  `Services/Trends/ChartSeriesTrendNotificationBuilder`, `UiModel/ChartSeriesDefinition`,
  `UiModel/Trends/*`, `UiLogic/ChartSeriesListSerializer`,
  `Services/Chart/ChartSeriesLocationSubstitutionService`, `Components/Chart/ChartSeriesView*`,
  `Components/Chart/ChartSeriesListView.razor.cs`, `Components/Chart/Trend/ChartTrendPanel.razor*`,
  new `Components/Common/Trends/CurvedTrendsOverviewExplainer.razor*`.
  `RecentObservationsCalculator.Trend.cs` / `RecentObservationTrendViewModel` /
  `Components/RecentObservations/Tab/AboutTrends/AboutTrends.razor*` get the mechanical type rename
  only and are otherwise unaffected — `TrendStatSectionBuilder` is shared and now fully generalised,
  but the tile only ever feeds it degree-1 results, so its degree>1 branches simply never execute
  there. See "Decisions" — the statistics reporting is now **fully in scope**, not guard-clauses-only.
- **Builds on:** [Linear regression utility](2026-07-11-01-linear-regression.md),
  [Chart series trend module](2026-08-11-01-chart-series-trend-module.md)
- **Branch context:** `issues/chart-trends`

## Goal

Generalise `LinearRegressionCalculator` into a `PolynomialRegressionCalculator` that fits degree 1
(linear), 2 (quadratic) or 3 (cubic) ordinary least squares polynomials, and use it to add a
**regression type** dropdown to the chart's trend module, next to the existing trend-period
dropdown. `RecentObservationTrend` (the Recent Observations tile's trend tab) keeps calling the
calculator at degree 1 only and is otherwise unaffected — this is a charting-module feature.

## Current state (recap)

`ChartSeriesTrendCalculator.Calculate` ([ChartSeriesTrendCalculator.cs:42](../../ClimateExplorer.Web.Client/Services/Trends/ChartSeriesTrendCalculator.cs#L42))
fits four fixed windows — Full, Recent (30y), RecentDecade (10y), FirstHalf — all with
`LinearRegressionCalculator.Calculate`, gates each on `Significance.IsSlopeSignificant`
(p < 0.05 two-tailed t-test on the slope), and projects the user's selected, significant window
forward with `LinearRegressionCalculator.Predict`. Three of the four windows are shared with the
Recent Observations tile via `TrendWindowCalculator`; the fourth (RecentDecade) is fitted directly
in `ChartSeriesTrendCalculator` because `TrendWindowCalculator`'s three-window shape is a
contract the tile also depends on (see Addendum 3 of the trend-module doc).

`TrendStatSectionBuilder`/`TrendFormatting` render that fit as a GraphPad-style breakdown: a single
slope, a single Y-intercept (value at calendar year 0), an X-intercept via Fieller's theorem, a
`Y = slope·X + intercept` equation, and "±X /decade" rate text. All of this assumes exactly one
linear coefficient. It is shared, unchanged, between the Recent Observations tab's `AboutTrends`
panel and the chart's `ChartTrendPanel`.

## Decisions

### What the new dropdown controls

Asked whether picking Quadratic/Cubic should change only the drawn projection (windows keep being
fit and gated at degree 1, as today) or refit everything at the chosen degree. You confirmed the
second: **you want a window hidden from the dropdown when its quadratic/cubic fit isn't
statistically significant**, which the "only re-fit the projection" option cannot do — significance
has to be evaluated at the degree that's actually going to be drawn.

**Decision: refit everything at the chosen degree.** `ChartSeriesTrendCalculator` fits all four
windows at the series' selected degree (default: Linear, unchanged from today), and gates each
window's availability on a significance test evaluated at that degree (see "Generalising the
significance test" below). Picking Quadratic can make a window newly available (a curve that
wasn't a significant straight line can be a significant curve) or newly unavailable, and the
dropdown's contents change accordingly — same mechanism as today, just re-evaluated per degree.

### `TrendWindowCalculator` stops being shared with the chart

`TrendWindowCalculator` is degree-1-only by contract with the Recent Observations tile
(`RecentObservationsCalculator.Trend.cs`, `TrendWindowSet`, `RecentObservationTrendViewModel` are
all typed around a single linear fit). It cannot grow a `degree` parameter without touching the
tile's own view model shape, which is explicitly out of scope. `ChartSeriesTrendCalculator` already
fits its fourth window (RecentDecade) directly, bypassing `TrendWindowCalculator`, for exactly this
kind of reason (Addendum 3). This plan extends that: **`ChartSeriesTrendCalculator` fits all four
windows directly** with `PolynomialRegressionCalculator.Calculate(points, degree, alpha)`, and stops
calling `TrendWindowCalculator` altogether. The window-slicing logic it duplicates
(`ordered`, `ordered.TakeLast(n)`, `ordered.Take(count / 2)`) is a few lines, already written once
today for RecentDecade, and is the only way to keep `TrendWindowCalculator`/`TrendWindowSet`
untouched for the tile.

`TrendWindowCalculator` itself keeps existing exactly as-is other than the mechanical rename of the
type it returns (`LinearRegressionResult` → `PolynomialRegressionResult`, always constructed with
`degree: 1`). `RecentObservationsCalculator.Trend.cs` and `RecentObservationTrendViewModel` need the
same mechanical rename and nothing else — their numbers are unchanged.

### `AboutTrends`/`TrendStatSectionBuilder` — now fully in scope *(revised — see below)*

Originally scoped as guard-clauses-only. Revised after discussion: **the chart's "About trends"
panel (`ChartTrendPanel`) gets full, honest statistical reporting for quadratic and cubic fits, not
a degraded fallback.** The Recent Observations tile's own panel (`AboutTrends.razor`) is unaffected
either way — it only ever feeds `TrendStatSectionBuilder` degree-1 results, so none of the new
branches below ever execute there, and its output is provably unchanged (see "Degree 1 stays
byte-identical" throughout this doc).

**`PolynomialRegressionCalculator.CalculateXIntercept` stays degree-1-only** (Fieller's theorem is
specifically a linear-fit method — a ratio-of-two-correlated-normal-estimates argument that has no
direct equivalent for a curve) and throws `NotSupportedException` for `Degree != 1`. Root-finding for
a curve is a different, simpler question — "where does the fitted curve cross zero" — and
**`TrendStatSectionBuilder` computes that separately** rather than routing it through
`CalculateXIntercept`:

- **Quadratic** — the quadratic formula on `(c2, c1, c0)` gives 0, 1, or 2 real roots directly.
- **Cubic** — a closed-form (Cardano's formula) or a numerically-solved companion-matrix eigenvalue
  approach gives up to 3 real roots. Cardano's formula is preferred if its numerical edge cases
  (three real roots via the trigonometric case, repeated roots) can be handled cleanly; falling back
  to a bisection/Newton search across the plotted X range plus a reasonable margin is the fallback
  if not.
- These are **point estimates only, with no confidence interval** — Fieller's theorem doesn't
  generalise to multiple correlated coefficients without a materially bigger derivation (a delta-
  method or bootstrap approach), which is a genuinely separate piece of numerical work. The row
  states plainly that these are point estimates, explains why (the CI derivation isn't done), and
  lists every real root found in a labelled sub-row per root (a parabola opening upward with no real
  root prints "No real crossing in the real number line" rather than an empty/confusing row).

**Generalised section design** (`TrendStatSectionBuilder.Build`):

- **Summary** — the headline "Slope" row becomes **the fitted curve's instantaneous rate of change
  at the window's most recent year** for degree > 1 (same `Curve.Derivative(lastX)` value
  `TrendFormatting` now uses everywhere — one source of truth, not a second copy of the same idea).
  Label stays "Slope" at degree 1 (identical row, identical value) and becomes "Current rate (at
  {lastYear})" at degree 2/3, with the abstract/climate explanations reworded to say plainly that a
  curved fit doesn't have one constant rate, and this is the tangent at the most recent point.
  "Y-intercept" is unchanged in concept for any degree (`Predict`/`CalculateInterceptStatistics`
  already generalise). "X-intercept" uses the root-finding above for degree > 1. "1/Slope" divides
  into the same current-rate value, worded the same way.
- **New "Coefficients" section, degree > 1 only** — one row per fitted coefficient (`c0` intercept
  through `c2`/`c3`), each with its own standard error, t-statistic and 95% CI, computed from the
  diagonal of the coefficient covariance matrix the fitting step already produces (see "Fit and
  significance" below) — this is the transparent, no-information-lost view of the curve that the
  Summary section's single "current rate" number necessarily compresses away. Not shown at degree 1,
  where it would just duplicate the existing Slope/Y-intercept rows — this keeps degree-1 output
  identical in section count and order to today.
- **Best-fit values / 95% Confidence Intervals** — same current-rate substitution as Summary, for
  the emphasised "Slope" row; Y-intercept/X-intercept follow the same pattern as Summary.
- **Goodness of Fit** (R², Sy.x) — unchanged; already degree-agnostic.
- **Is slope significantly non-zero?** — F/DFn/DFd/p-value/"Significant"/"Not significant" rows,
  generalised: `DFn = trend.Degree` (was hardcoded `1`), the explanation text reworded from
  "is the slope different from zero" to "does this {degree-1 line / degree-2 curve / degree-3 curve}
  explain significantly more variance than a flat line" for degree > 1 (byte-identical at degree 1,
  since that's exactly what the existing text already says in different words).
- **Equation** — generalised to print every fitted term
  (`Y = c0 + c1·X + c2·X² + c3·X³`, dropping any term that's exactly the degree's ceiling only —
  there are at most 4 terms, so no truncation logic is needed) instead of the hardcoded
  slope/intercept template. Worked examples (predictions at a few reference years) already generalise
  for free — `Predict` is Horner evaluation regardless of degree.
- **Data** — unchanged; already degree-agnostic.

### New: a curved-trends explainer, as an extra tab — not a replacement

`TrendsOverviewExplainer.razor` stays exactly as it is — it's still what a linear fit is, and every
consumer (the tile, and the chart's Linear-degree panel) keeps using it unchanged. A new
**`CurvedTrendsOverviewExplainer.razor`** (`Components/Common/Trends/`) is added alongside it,
covering what changes once the fitted curve is a quadratic or cubic rather than a straight line —
written as an *extension* of the linear explainer's material (it can reference back to it — "as
covered in the linear trend overview, ...") rather than repeating the OLS/p-value/significance
basics from scratch. Planned content, mirroring the linear explainer's structure:

- What a quadratic fit adds over a line — a curve that can bend, fitted by the same least-squares
  principle extended to more terms (β₀ + β₁x + β₂x² minimising the same sum of squared residuals).
- Why the "rate" shown is now "the current rate" rather than a single constant — the derivative-at-
  the-most-recent-year framing used throughout this feature, explained in plain language (this is
  the single most important new idea a reader needs, since it's the number they'll actually see).
- A short worked example in the same style as the linear one's three-year table, extended to show
  a bending fit (four or five points that visibly accelerate).
- A brief section on cubic fits — short, since you don't expect to use it much: what a third term
  adds (an inflection point), and a one-line caution that with few data points a cubic fits noise
  very readily (3 extra parameters vs. 2 for quadratic, so it needs more data to mean anything).
- Limitations specific to curved fits: extrapolation risk is *worse* than the existing linear
  explainer's "Extrapolation" point, not the same — a curve's projected rate of change keeps changing
  the further you extrapolate, so a 75-year projection from a 10-year window (exactly what the CO₂
  test below does) can run away in a way a straight line cannot. This deserves its own clearly-worded
  paragraph, since it's the shape of risk this whole feature introduces.
- A short note on the new "Coefficients" table above (what each row means) and that X-intercepts for
  curves are point estimates without confidence intervals, and why.

**Formula figures:** `SlopeFormulaFigure.razor` is a hand-exported MathJax SVG — replicating that by
hand for quadratic/cubic formulas is impractical without the original authoring tool. Proposed:
render the quadratic/cubic equations as plain styled text (`Y = β₀ + β₁X + β₂X²`, using `<sup>`) rather
than a new SVG figure. Flagging this as a minor implementation choice, not a design fork — happy to
revisit if it reads poorly once built.

**Where it appears:** a new tab in `ChartTrendPanel` only (the tile's `AboutTrends.razor` never offers
quadratic/cubic, so never needs it), positioned **directly after the existing "Overview" tab**, and
**only rendered when the series' regression type is Quadratic or Cubic** — `ChartSeriesTrend` gains a
`TrendRegressionType RegressionType` property so the panel knows without reaching back into
`ChartSeriesDefinition`. A series left on Linear sees exactly today's tab set (Overview + one tab per
window); switching to Quadratic/Cubic adds one tab ("About curved trends") between Overview and the
window tabs.

### Describing "the rate" for a curve, honestly

`TrendFormatting.FormatPerDecadeValue` (`trend.Line.Slope * 10`) is used in three chart-visible
places beyond the About panel, so it can't be left silently wrong for degree 2/3:
`ChartSeriesTrendNotificationBuilder.DescribeRejectedWindow` (shown whenever a window is rejected),
`ChartTrendPanel.DescribeWindow` (the per-window one-line summary), and the same builder's dropdown
dependents. A quadratic curve has no single constant rate — printing "the" linear coefficient alone,
in whatever units a raw calendar-year-scaled cubic coefficient comes out in, would be actively
misleading, not just incomplete.

**Decision: define the rate shown in prose as the fitted curve's instantaneous rate of change at
the window's most recent year** (`Curve.Derivative(window.LastYear)`), not a raw coefficient. This
is:

- **Exactly today's behaviour at degree 1** — a line's derivative is its slope everywhere, so
  `Derivative(anyX) == Slope` and every existing string is byte-identical.
- **A real, standard way to describe an accelerating trend** — "CO₂ is currently rising at about
  X ppm/decade" is how this quantity is normally reported for a curved record; it's the tangent
  at "now", not an average over a shape that visibly bent.
- **Cheap and exact** — the derivative of a ≤3rd-degree polynomial is a closed-form ≤2nd-degree
  polynomial, evaluated with the same coefficients `Predict` already carries.

`FormatPerDecadeValue`/`FormatPerDecade` take an explicit reference X rather than reading
`trend.Line.Slope` directly. Existing call sites that only ever see degree-1 results
(`RecentObservationsCalculator.Trend.cs`, `TrendStatSectionBuilder`'s own slope row) can pass any X
— it's the same number regardless — so passing `trend.Input.MaximumX` everywhere is simplest and
keeps a single call pattern.

## Design

### Core: `PolynomialRegressionCalculator`

Replaces `LinearRegressionCalculator` in `ClimateExplorer.Core/Stats`.

```
public static class PolynomialRegressionCalculator
{
    public const int MinimumDegree = 1;
    public const int MaximumDegree = 3;

    public static PolynomialRegressionResult Calculate(
        IEnumerable<DataPoint> points, int degree = 1, double alpha = 0.05);

    public static RegressionPrediction Predict(
        PolynomialRegressionResult regression, double x, double alpha = 0.05);

    public static InterceptStatistics CalculateInterceptStatistics(
        PolynomialRegressionResult regression, double alpha = 0.05);

    // Degree 1 only — throws NotSupportedException otherwise (see Decisions).
    public static XInterceptStatistics CalculateXIntercept(
        PolynomialRegressionResult regression, double alpha = 0.05);
}
```

- `degree` outside `[1, 3]` throws `ArgumentOutOfRangeException` — degrees above 3 are a
  deliberate, documented non-goal (`MaximumDegree`), not just unimplemented; the XML doc says so
  and points at this file if it's ever revisited.
- Minimum point count generalises from the current fixed `3` to **`degree + 2`** (so residual
  degrees of freedom `n - degree - 1 ≥ 1`): 3 for linear (unchanged), 4 for quadratic, 5 for cubic.

#### Fitting: centred normal equations

Design matrix columns are powers of `x' = x - meanX`, not raw `x` — the existing linear code
already centres its sums (`sumProductDeviations`, `SumSquaredXDeviations`) for exactly this reason,
and it matters far more once `x` is a calendar year and the design matrix needs `x³` (values above
`8 × 10⁹` for a year like 2025 without centring, vs. a few hundred to a few thousand once centred
on a window's own mean year). `X'ᵀX'` is built from centred power sums
`S_k = Σ(x_i - meanX)^k` for `k = 0..2·degree` (≤ 6 for cubic) and solved via Gauss-Jordan
elimination with partial pivoting — the matrix is at most 4×4, so a general solver is simple and
there's no benefit to a closed-form per degree. The same elimination also produces
`(X'ᵀX')⁻¹`, which is retained (not just the solved coefficients) because `Predict`'s leverage term
needs it for arbitrary future `x`.

Coefficients are then shifted from centred (`x' = x - meanX`) back to calendar-year scale via
binomial expansion, so `PolynomialCurve.Coefficients` is always `[c₀, c₁, c₂, c₃]` such that
`Y = c₀ + c₁·X + c₂·X² + c₃·X³` in the **original** X units — this is what makes `Predict(2100)`
a plain Horner evaluation, keeps `Coefficients[0]`/`Coefficients[1]` meaning exactly what
`Intercept`/`Slope` mean today (value at calendar year 0; coefficient of X¹), and is what a
degree-1 fit must produce to stay byte-identical with today's closed-form slope/intercept. Degree 1
is verified against the existing closed-form formulas as an implementation check (same numbers, not
just "close") before anything downstream is trusted.

`PolynomialCurve` (replaces `RegressionLine`):

```
public sealed record PolynomialCurve(IReadOnlyList<double> Coefficients)
{
    public double Slope => Coefficients[1];       // coefficient of X¹ — always well-defined
    public double Intercept => Coefficients[0];    // value at X = 0 — always well-defined

    public double Predict(double x) => Horner-evaluate Coefficients at x;
    public double Derivative(double x) => Horner-evaluate the analytic derivative at x;
}
```

`Slope`/`Intercept` are kept (not removed) because degree-1 consumers (`RecentObservationsCalculator`,
`TrendStatSectionBuilder`'s per-row text) read them directly today; they stay meaningful — "the
coefficient of X¹" and "the value at year 0" are well-defined for any degree, they're just not
*the whole curve's story* for degree > 1, which is exactly why prose that needs "the story" uses
`Derivative(referenceX)` instead (see Decisions).

#### Fit and significance

`RegressionFit` (RSquared, ResidualStandardError, ResidualSumOfSquares, TotalSumOfSquares,
RegressionSumOfSquares) is unchanged in shape — every one of those is a single number derived from
residuals regardless of how many terms produced them.

`RegressionSignificance` generalises from a slope t-test to the **overall-model F-test**:
`F = (RegressionSS / degree) / (ResidualSS / (n - degree - 1))`, numerator df = `degree`,
denominator df = `n - degree - 1`, tests "does this polynomial explain significantly more variance
than a flat mean". This is a strict generalisation, not a different test: for degree 1 it is
mathematically identical to today's two-tailed slope t-test (`F = t²`), reusing the same regularised
incomplete beta identity `StudentTDistributionCalculator.TwoTailedPValue` already relies on — see
below. `IsSlopeSignificant` keeps its name (RecentObservationTrend and every degree-1 caller reads
it as exactly what it says today) and its meaning generalises to "is this polynomial trend, as a
whole, distinguishable from no trend" for degree > 1.

`SlopeStandardError`/`TStatistic`/`SlopeConfidenceInterval` stay populated for every degree, as the
standard error/CI of `Coefficients[1]` specifically (the diagonal of `σ²·(X'ᵀX')⁻¹` for the linear
term, shifted the same way the coefficients are). They remain exactly what they are today at
degree 1. At degree > 1 they describe one term among several — genuinely useful as a per-coefficient
stat, not used anywhere as the significance gate (the overall F-test is), so nothing currently reads
them as "the whole trend" for degree > 1 except `TrendStatSectionBuilder`'s existing slope row,
which is one of the rows generalised per "Decisions" above.

#### New: `FDistributionCalculator`

`StudentTDistributionCalculator`'s regularised-incomplete-beta / log-gamma implementation
(`RegularizedIncompleteBeta`, `BetaContinuedFraction`, `LogGamma`,
[StudentTDistributionCalculator.cs:66-181](../../ClimateExplorer.Core/Stats/StudentTDistributionCalculator.cs#L66-L181))
is the same machinery an F-distribution p-value needs — an F(dfn, dfd) upper-tail p-value is
`I_{dfd/(dfd + dfn·F)}(dfd/2, dfn/2)`, the same regularised incomplete beta function. That
identity is also *why* `F = t²` gives the same p-value as today's t-test at `dfn = 1`: substituting
`dfn = 1` into the F formula reduces algebraically to the existing `TwoTailedPValue`'s
`x = df/(df + t²)`, `RegularizedIncompleteBeta(x, df/2, 0.5)` call, term for term.

Plan: extract `RegularizedIncompleteBeta`/`BetaContinuedFraction`/`LogGamma` into a shared internal
`RegularizedIncompleteBetaFunction` helper in `ClimateExplorer.Core/Stats`, used by both
`StudentTDistributionCalculator` (unchanged public surface — still used for per-coefficient
standard errors/CIs and prediction intervals, which stay t-distribution based) and a new
`FDistributionCalculator.UpperTailPValue(fStatistic, numeratorDf, denominatorDf)` (used for the
generalised `RegressionSignificance`). A dedicated test asserts the two calculators agree exactly
at `dfn = 1, F = t²` across several `(t, df)` pairs already in `LinearRegressionCalculatorTests`, so
degree-1 output is provably unchanged rather than just visually similar.

#### Result shape

```
PolynomialRegressionResult(
    RegressionInputSummary Input,
    int Degree,
    PolynomialCurve Curve,          // was RegressionLine Line — RecentObservations/AboutTrends
                                     // call sites rename `.Line` to `.Curve` mechanically
    RegressionFit Fit,
    RegressionSignificance Significance);
```

`RegressionInputSummary`, `RegressionFit`, `RegressionSignificance`, `RegressionPrediction`,
`ConfidenceInterval`, `InterceptStatistics`, `XInterceptStatistics`, `DataPoint` are unchanged in
shape. `TrendWindowSet` renames its four `LinearRegressionResult` properties'
type to `PolynomialRegressionResult` only — still always degree 1.

### Chart module

`ChartSeriesDefinition` gains:

```
public enum TrendRegressionType { Linear = 1, Quadratic = 2, Cubic = 3 }

public TrendRegressionType RegressionType { get; set; } = TrendRegressionType.Linear;
```

An enum (not a bare `int`) to match `TrendWindow`'s existing precedent — self-documenting in the
URL (`Linear`/`Quadratic`/`Cubic`, not `1`/`2`/`3`), and a natural fit for
`Select TValue="TrendRegressionType"` next to the existing `Select TValue="TrendWindow"`. A trivial
`(int)RegressionType` conversion feeds `PolynomialRegressionCalculator.Calculate`'s `degree`
parameter.

Added everywhere `TrendPeriod`/`TrendPredictionYears` were added for the same reason (chart series
identity must include it, or duplicate-detection silently collapses two series that only differ by
regression type):

- Both `ChartSeriesDefinition` equality comparers (`BaseComparer`
  [ChartSeriesDefinition.cs:322](../../ClimateExplorer.Web.Client/UiModel/ChartSeriesDefinition.cs#L322)
  and both `GetHashCode`s at lines ~440 and ~496).
- `ChartSeriesListSerializer` — new tolerant trailing segment (22nd), same pattern as the trend
  fields at segments 19-21
  ([ChartSeriesListSerializer.cs:117-120](../../ClimateExplorer.Web.Client/UiLogic/ChartSeriesListSerializer.cs#L117-L120)):
  parsed defensively so pre-existing shared links default to `Linear`.
- `ChartSeriesLocationSubstitutionService` copy-through
  ([line 192](../../ClimateExplorer.Web.Client/Services/Chart/ChartSeriesLocationSubstitutionService.cs#L192)
  area).
- `ChartSeriesListView.OnDuplicateSeries` copy-through
  ([ChartSeriesListView.razor.cs:83-85](../../ClimateExplorer.Web.Client/Components/Chart/ChartSeriesListView.razor.cs#L83-L85)).

`ChartSeriesTrendCalculator.Calculate` gains a `TrendRegressionType regressionType` parameter,
fits all four windows directly at that degree (see "Decisions" — stops delegating to
`TrendWindowCalculator`), and otherwise keeps its existing shape: same four windows in the same
priority order, same `ResolveWindow` fallback logic, same `Project` mechanism (now calling
`PolynomialRegressionCalculator.Predict`, unchanged signature).

### UI

New "Regression type" dropdown in `ChartSeriesView.razor`, placed **directly above "Trend period"**
(same `DelayedTooltip` + `form-row` pattern) — "what shape" is the more fundamental choice, and
changing it can change which periods even appear in the "Trend period" list below it, so causally it
belongs first. Enabled under the same condition (`IsTrendModuleAvailable && ChartSeries.ShowTrend`),
options Linear/Quadratic/Cubic. Changing it calls `OnSeriesChanged` → full rebuild, exactly like
changing the trend period (both now change which windows are even significant). The existing "Show
trend" tooltip ("Fit a linear trend and project it past the end of the data") is reworded since it's
no longer always linear.

## Unit tests

### Core calculator

- All existing `LinearRegressionCalculatorTests` continue to pass **unmodified** except for the
  mechanical rename (`LinearRegressionCalculator` → `PolynomialRegressionCalculator`,
  `.Line` → `.Curve`, `Calculate(points)` → `Calculate(points, degree: 1)` — or keep `degree = 1` as
  the method's default so most call sites don't change at all). Every numeric assertion in that file
  is the regression-proof that degree 1 is byte-for-byte unchanged.
- New `FDistributionCalculatorTests`: agreement with `StudentTDistributionCalculator.TwoTailedPValue`
  at `dfn = 1, F = t²` for several `(t, df)` pairs; a couple of independently-known F-table values
  (e.g. F(2, 20) critical value at α = 0.05 ≈ 3.49) as a sanity cross-check.
- New degree-2/3 tests in (renamed) `PolynomialRegressionCalculatorTests`:
  - **Exact synthetic polynomials** (no noise), mirroring the existing
    `Calculate_PerfectPositiveLine_...` pattern: points generated from a known
    `y = c0 + c1·x + c2·x² + c3·x³`, asserting `Curve.Coefficients` recovers `c0..c3` to tight
    tolerance and `R² == 1`. One for degree 2, one for degree 3 — this is the primary correctness
    check for the new maths and doesn't depend on any external reference.
  - **NIST/ITL StRD Pontius dataset** (`https://www.itl.nist.gov/div898/strd/lls/data/Pontius.shtml`)
    — a certified real-world **quadratic** polynomial regression reference (40 points), same
    external-certification pattern the original linear regression doc used for Norris. Asserts
    coefficients, R², and residual standard error against NIST's certified values.
  - **Cubic reference dataset** — NIST's StRD polynomial set doesn't include a plain cubic
    (`Wampler1-5` are 5th-degree, `Pontius` is 2nd; `Norris` is linear). Proposed approach:
    exact-synthetic cubic (above) as the primary correctness proof, plus, if a suitable small public
    cubic-fit reference turns up during implementation, add it the same way Wikipedia's
    height/mass example was added for linear. Flagging this as unresolved rather than promising a
    specific source that may not exist — will report what was found/used in the addendum.
  - Degree validation: `degree = 0` and `degree = 4` both throw `ArgumentOutOfRangeException`;
    fewer than `degree + 2` points throws `ArgumentException` with the generalised message.
  - `CalculateXIntercept` on a degree-2/3 result throws `NotSupportedException`.

### CO2 fixtures (`co2_mm_mlo.txt`) — linear vs. quadratic on the same window, both to 2100

Per your request: build **one** fixture of annual-mean CO₂ (mean of the 12 monthly averages, column
4 of `co2_mm_mlo.txt`) for every **complete** calendar year in the file, then fit the
**RecentDecade window (last 10 years)** — matching the chart's own `RecentDecade` window definition
— at both degree 1 and degree 2, and predict both forward to 2100, so the two are directly
comparable in one test class.

The file currently runs from March 1958 to July 2026, so the last complete calendar year is 2025 and
the RecentDecade window is **2016–2025**:

| Year | Annual mean CO₂ (ppm) | | Year | Annual mean CO₂ (ppm) |
|---|---|---|---|---|
| 2016 | 404.41 | | 2021 | 416.41 |
| 2017 | 406.76 | | 2022 | 418.53 |
| 2018 | 408.72 | | 2023 | 421.08 |
| 2019 | 411.65 | | 2024 | 424.60 |
| 2020 | 414.21 | | 2025 | 427.35 |

(Full precision values, and the complete 1959-2025 fixture, generated from the source file rather
than hand-typed, during implementation.)

- `PolynomialRegressionCalculatorTests` (or a new `Co2RegressionTests` class, TBD during
  implementation) reads the fixture, fits **degree 1** and **degree 2** over 2016-2025.
- Both fits assert against reference values computed independently (not by re-deriving the
  implementation's own formulas) — e.g. cross-checked with an independent tool such as `numpy.polyfit`
  if available in the implementation environment, following the same "don't grade your own homework"
  principle the NIST/Wikipedia fixtures follow for linear. If no independent tool is available, the
  fallback is documenting the manual computation method used (e.g. hand-solved normal equations) in
  the test's comments, same as the "Perfect positive/negative line" tests already do for simple exact
  cases.
- Both predict to **X = 2100** (`Predict(regression, 2100)`), and the test asserts:
  - The linear prediction is materially lower than the quadratic prediction (the quadratic should
    curve upward faster than a straight line through the same accelerating recent decade — an
    order-relationship assertion that doesn't depend on pinning an exact "expected" ppm figure the
    way the coefficient-level assertions do).
  - The quadratic degree-2 model is statistically significant over this window (`IsSlopeSignificant`
    is true at the generalised F-test) — real Mauna Loa CO₂ growth is visibly accelerating, so this
    is expected to hold, but it's an assertion worth stating explicitly since it's also a sanity
    check that the significance generalisation isn't broken.
  - The 2100 quadratic prediction falls in a wide plausibility band. Run against the real fixture,
    the computed values are: linear 2100 prediction **≈616 ppm**, quadratic 2100 prediction
    **≈866 ppm** — the initially-agreed 550-750 ppm band didn't contain the actual quadratic figure,
    so it was widened to **550-950 ppm** after seeing the real output, per your steer to run the
    tests and look before pinning a range. 866 ppm reflects how much a 10-year window's curvature
    amplifies over a 75-year extrapolation (7.5× the fitted window) — high, but within RCP8.5-scenario
    territory, not a sign of a broken fit.

## Out of scope

- Degrees above 3 (`PolynomialRegressionCalculator.MaximumDegree` documents this as deliberate).
- `RecentObservationTrend` / the tile's own `AboutTrends.razor*` gaining a regression-type selector —
  stays degree-1-only, per your instruction. (The shared `TrendStatSectionBuilder` is fully
  generalised, per "Decisions" above, but the tile never calls it with a degree > 1 result.)
- A confidence interval on quadratic/cubic X-intercepts (root point estimates only — see
  "Decisions"); a delta-method or bootstrap generalisation of Fieller's theorem for correlated
  multi-coefficient roots is a separate, non-trivial piece of numerical work.
- Any change to the trend module's minimum-data rule (`MinimumYearsForTrend = 60`) or window
  definitions (Full/Recent/RecentDecade/FirstHalf) — unchanged, just now fit at a chosen degree.
- Rendering a curved projection differently from the existing scatter-point rendering
  (Addendum 2 of the trend-module doc) — the projected points are still individual
  `RegressionPrediction`s per future year; a quadratic/cubic projection is already "curved" in the
  sense that the point-to-point spacing on the Y axis will visibly accelerate, with no chart-code
  change needed.

## Assumptions

1. `alpha` stays at the calculator's `0.05` default throughout, same as the existing trend module —
   not newly exposed to the user for the degree selector.
2. Existing chart links with no 22nd URL segment default to `Linear` — behaviourally identical to
   today for every already-shared link.
3. `ChartSeriesTrendCalculator` no longer calling `TrendWindowCalculator` is acceptable duplication
   (a handful of `OrderBy`/`TakeLast`/`Take` lines) in exchange for not touching the Recent
   Observations tile's contract — see "Decisions".

## Addendum — implementation notes (2026-08-14)

Shipped as planned, including the "AboutTrends now fully in scope" revision. `dotnet build` on the
solution is clean; the full unit suite passes at **498 tests** (up from 478 before this branch —
17 new core-engine tests, 3 new CO2 tests). No dev server or browser testing was run, per AGENTS.md.

### Core engine

- `PolynomialRegressionCalculator` replaces `LinearRegressionCalculator` exactly as designed: fits in
  coordinates centred on the window's mean X via Gauss-Jordan elimination (with partial pivoting) on
  the normal-equations matrix, shifts coefficients back to calendar-year scale via an exact binomial-
  expansion change of basis, and propagates the coefficient covariance matrix through that same shift
  (`T·Cov·Tᵀ`). **Degree 1 is proven byte-for-byte unchanged**: every pre-existing
  `LinearRegressionCalculatorTests` assertion (renamed mechanically to `PolynomialRegressionCalculatorTests`,
  `.Line` → `.Curve`) passes unmodified at its original tolerance, including the tight NIST Norris
  (1e-13/1e-15) and Canberra-temperature fixture comparisons — this is what confirms the new centred/
  shifted fitting path reproduces the old closed-form simple-linear-regression formulas exactly, not
  just approximately.
- `RegularizedIncompleteBetaFunction` was extracted out of `StudentTDistributionCalculator` (moved
  verbatim, no behaviour change) and is now shared with the new `FDistributionCalculator`, which
  implements the generalised overall-model significance test. No direct unit tests were added for
  either internal class specifically (there's no `InternalsVisibleTo` and none existed for
  `StudentTDistributionCalculator` before this branch either) — both are proven correct indirectly
  through `PolynomialRegressionCalculator`'s public output, same as before.
- Two small additions beyond the original design sketch, both needed for correctness once the
  "rate" concept had to generalise for `TrendFormatting`/`TrendStatSectionBuilder` (see below):
  - `PolynomialCurve.Derivative(x)` — the analytic derivative, Horner-evaluated.
  - `PolynomialRegressionCalculator.CalculateRateOfChange(regression, atX, alpha)` → new
    `RateOfChangeEstimate` model — the *properly propagated* standard error/CI of the derivative at a
    point, via the same quadratic-form-against-the-covariance-matrix approach `Predict` already used
    for the fitted value, with a derivative-weighted vector instead. This was necessary rather than
    optional: reusing `RegressionSignificance.SlopeStandardError` (the SE of `Coefficients[1]` alone)
    as if it were the SE of "the current rate" would have been statistically wrong for degree ≥ 2,
    where the rate is a linear combination of every non-constant coefficient. At degree 1 this
    reproduces the slope's existing SE/CI exactly, at any X.
- `PolynomialRootFinder` (quadratic formula; Cardano's method with the trigonometric case for cubics)
  was added as designed, with its own direct unit tests (it's public, no `InternalsVisibleTo` needed).

### Reference datasets

- NIST/ITL StRD **Pontius** (load-cell calibration, 40 points, certified quadratic coefficients) was
  fetched and added as `PolynomialRegressionFixtures/Public/nist-pontius.csv`, matching the original
  linear-regression doc's "certified external reference" pattern. All certified values (coefficients,
  residual standard deviation, R², F-statistic) match to the asserted tolerances.
- No small public **cubic** reference dataset was found (NIST's polynomial-regression StRD set is
  Norris/linear, Pontius/quadratic, and Wampler1-5/degree-5 — nothing at degree 3). Cubic correctness
  instead rests on exact synthetic fits (a known cubic with no noise, coefficients recovered to
  1e-6) plus the root-finder's own dedicated tests (three-real-root and one-real-root cubics with
  hand-computable factorisations). Flagging this as unresolved per the plan, rather than a gap that
  was silently dropped.

### The CO2 test numbers, and the plausibility band

Per your request, the linear and quadratic `RecentDecade` (2016-2025) fits were run before the band
was finalised. Actual output:

| | Equation | 2100 prediction |
|---|---|---|
| Linear | Y = −4678.71 + 2.5212·X | **≈616 ppm** |
| Quadratic | Y = 157084.29 − 157.6009·X + 0.039624·X² | **≈866 ppm** |

Both fits are significant over the window, and the quadratic correctly predicts higher than the
linear one, but 866 ppm sat above the originally-agreed 550-750 ppm band. You widened it to
**550-950 ppm** after seeing the real numbers — the design doc's own text on this ("a 75-year
extrapolation from a 10-year window... the point is to catch a badly broken fit, not to pin an exact
figure") holds; the initial band just hadn't been checked against the real fit yet. This also became
the concrete example used in the new curved-trends explainer's "extrapolation risk is worse for
curves" section.

### AboutTrends generalisation

Implemented per the revised "fully in scope" decision:

- `TrendStatSectionBuilder` now branches on `trend.Degree`: the Summary/Best-fit/Confidence-interval
  sections' "Slope" row becomes "Current rate (at {year})" for degree > 1, backed by
  `CalculateRateOfChange` rather than the raw slope; a new **Coefficients** section (degree > 1 only,
  so degree-1 section count/order is unchanged) lists every fitted term via
  `CalculateCoefficientStatistics`; X-intercept rows use `PolynomialRootFinder` point estimates
  (explicitly labelled as having no confidence interval) instead of throwing; the Equation section
  prints every coefficient instead of a hardcoded slope/intercept template; the significance section's
  DFn generalises from a hardcoded `1` to `trend.Degree`.
- Degree-1 prose wording changed in a few places (e.g. "how precisely the slope/rate is estimated")
  where a single shared string now branches on `isLinear` — the *numbers* are exactly unchanged (no
  test covers this file's literal strings byte-for-byte; the "byte-identical" guarantee in this doc
  always meant the numeric output, which the calculator-level tests lock in), but a few explanatory
  sentences read slightly differently than before even at degree 1. Not expected to matter in
  practice, flagging for completeness.
- New `CurvedTrendsOverviewExplainer.razor` (`Components/Common/Trends/`) added alongside, not
  replacing, `TrendsOverviewExplainer.razor`. Wired into `ChartTrendPanel` as a tab between Overview
  and the window tabs, shown only when `ChartSeriesTrend.RegressionType` is Quadratic or Cubic.
  Formula shown as styled text (`Y = β₀ + β₁X + β₂X²`) rather than a hand-exported MathJax SVG like
  `SlopeFormulaFigure`, per the plan's flagged minor decision. `ChartTrendPanel.OnParametersSet` falls
  back to the Overview tab if the curved tab was selected and the regression type then changed away
  from Quadratic/Cubic.
- The Recent Observations tile's own `AboutTrends.razor`/`RecentObservationTrendViewModel` needed only
  the mechanical `PolynomialRegressionResult` rename — it never constructs a degree > 1 result, so none
  of the new branches above ever execute there, and its rendered output is unchanged.

### Chart module wiring

- `TrendRegressionType` enum (`Linear = 1, Quadratic = 2, Cubic = 3`) added to
  `ChartSeriesDefinition` (default `Linear`), both equality comparers, `ChartSeriesListSerializer`
  (tolerant 22nd segment), `ChartSeriesLocationSubstitutionService`, and
  `ChartSeriesListView.OnDuplicateSeries` — everywhere `TrendPeriod`/`TrendPredictionYears` already
  were, per the plan.
- `ChartSeriesTrendCalculator.Calculate` gained the `regressionType` parameter and now fits all four
  windows directly via `PolynomialRegressionCalculator.Calculate(points, degree, alpha)`, no longer
  calling `TrendWindowCalculator` at all (previously it only bypassed `TrendWindowCalculator` for the
  RecentDecade window). `ChartSeriesTrend` gained a `RegressionType` property so `ChartTrendPanel`
  doesn't need to reach back into `ChartSeriesDefinition`.
- "Regression type" dropdown placed directly above "Trend period" in `ChartSeriesView.razor`, per
  your instruction, using the same `Select`/`DelayedTooltip`/`form-row` pattern as the existing
  controls. The "Show trend" tooltip was rewored from "Fit a linear trend..." to "Fit a trend...".

### Not done / deferred (unchanged from the plan's "Out of scope")

Degrees above 3; a regression-type selector on `RecentObservationTrend`; a confidence interval on
curved X-intercepts (point estimates only, as designed); any change to the 60-year minimum or the
four window definitions.
