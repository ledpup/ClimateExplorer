namespace ClimateExplorer.Web.Client.Services.Trends;

using System.Globalization;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.UiModel.Trends;

// Builds the GraphPad-style full statistical breakdown shown for one subject/window combination -
// used by both the Recent Observations About-trends panel (always a degree-1 fit) and the chart's
// trend panel (degree 1, 2 or 3). Slope/Y-intercept/X-intercept/1-Slope each appear in three tiers:
// Summary (what does this mean), Best-fit values (what are the numbers and how precise is each one),
// and 95% Confidence Intervals (what range are we confident in) - deliberately three separate
// sections rather than one, so each answers a single question. A degree-2/3 fit gets a fourth,
// "Coefficients" section (every fitted term with its own standard error), since the Summary/Best-fit
// rows above necessarily compress a curve down to a single "current rate" number - see the "About
// curved trends" tab for why.
internal static class TrendStatSectionBuilder
{
    /// <param name="subject">What the trend describes - supplies the label and unit.</param>
    /// <param name="trend">The regression for the window being described.</param>
    /// <param name="windowPoints">The points <paramref name="trend"/> was fitted to.</param>
    /// <param name="fullPeriodPoints">
    /// Every point in the record, regardless of window. Used only to look up the current year's
    /// measured value, so that a trend fitted to an early window can still be compared against what
    /// actually happened.
    /// </param>
    public static IReadOnlyList<TrendStatSection> Build(
        TrendStatSubject subject,
        PolynomialRegressionResult trend,
        IReadOnlyList<DataPoint> windowPoints,
        IReadOnlyList<DataPoint> fullPeriodPoints)
    {
        var isLinear = trend.Degree == 1;
        var interceptStats = PolynomialRegressionCalculator.CalculateInterceptStatistics(trend);
        var xIntercept = isLinear ? PolynomialRegressionCalculator.CalculateXIntercept(trend) : null;
        var roots = isLinear ? null : PolynomialRootFinder.FindRealRoots(trend.Curve.Coefficients);
        var rate = PolynomialRegressionCalculator.CalculateRateOfChange(trend, trend.Input.MaximumX);

        var sections = new List<TrendStatSection>
        {
            BuildSummary(subject, trend, rate, xIntercept, roots),
            BuildBestFitValues(subject, trend, rate, interceptStats, xIntercept, roots),
            BuildConfidenceIntervals(subject, trend, rate, interceptStats, xIntercept, roots),
        };

        if (!isLinear)
        {
            sections.Add(BuildCoefficients(trend));
        }

        sections.Add(BuildGoodnessOfFit(subject, trend));
        sections.Add(BuildSignificance(trend));
        sections.Add(BuildEquation(subject, trend, fullPeriodPoints));
        sections.Add(BuildData(windowPoints, trend));

        return sections;
    }

    private static string RateLabel(PolynomialRegressionResult trend)
    {
        return trend.Degree == 1
            ? "Slope"
            : $"Current rate (at {trend.Input.MaximumX.ToString("0", CultureInfo.InvariantCulture)})";
    }

    private static TrendStatSection BuildSummary(
        TrendStatSubject subject,
        PolynomialRegressionResult trend,
        RateOfChangeEstimate rate,
        XInterceptStatistics? xIntercept,
        IReadOnlyList<double>? roots)
    {
        var unit = subject.Unit;
        var isLinear = trend.Degree == 1;
        var intercept = trend.Curve.Intercept;

        var rateWorkedExample = trend.Significance.IsSlopeSignificant
            ? (isLinear
                ? $"At this rate, the fitted line implies a change of about {FormatSignedValue(rate.Rate * 100, unit)} per century."
                : $"At the current rate, the fitted curve implies a change of about {FormatSignedValue(rate.Rate * 100, unit)} per century if that rate held steady - which, for a curve, it generally won't (see \"About curved trends\").")
            : null;

        var rateClimateExplanation = !trend.Significance.IsSlopeSignificant
            ? $"The fitted rate here is {TrendFormatting.FormatPerDecadeValue(trend, trend.Input.MaximumX, unit)}, but it isn't statistically significant (see \"Is slope significantly non-zero?\" below), so it's shown as \"No significant trend\" above rather than as a number that could be mistaken for a reliable rate."
            : isLinear
                ? "This site shows rates per decade because a per-decade number is large enough to read without implying year-to-year precision."
                : "This is the curve's instantaneous rate of change at the most recent year (the slope of the tangent there), not one constant rate across the whole window - a quadratic/cubic fit doesn't have a single rate. See the \"About curved trends\" tab.";

        var rateAbstractExplanation = isLinear
            ? $"The slope is the change in the fitted value for every one-unit increase in X - here, the average change from one calendar year to the next. The per-year rate is {TrendFormatting.FormatPerYearRate(rate.Rate, unit)}."
            : $"The rate of change for every one-unit increase in X, read at this specific year rather than averaged across the window. The per-year rate here is {TrendFormatting.FormatPerYearRate(rate.Rate, unit)}.";

        var rateRow = new TrendStatRow(
            RateLabel(trend),
            TrendFormatting.FormatPerDecade(trend, trend.Input.MaximumX, unit),
            IsEmphasized: true,
            AbstractExplanation: rateAbstractExplanation,
            ClimateExplanation: rateClimateExplanation,
            WorkedExamples: rateWorkedExample is null ? null : [rateWorkedExample]);

        var yInterceptRow = new TrendStatRow(
            "Y-intercept",
            TrendFormatting.FormatValue(intercept, unit),
            IsEmphasized: false,
            AbstractExplanation: "The Y-intercept is the fitted value of Y when X = 0 - the value the curve predicts for calendar year 0.",
            ClimateExplanation: "Year 0 is thousands of years before this record began, so this is a mathematical artefact of extending the fitted curve backwards, not a real prediction for any actual year.",
            WorkedExamples: null);

        var xInterceptRow = BuildXInterceptRow(subject, trend, xIntercept, roots, includeStandardError: false);

        var reciprocalRow = BuildReciprocalRateSummaryRow(subject, trend, rate.Rate);

        return new TrendStatSection("Summary", [rateRow, yInterceptRow, xInterceptRow, reciprocalRow]);
    }

    private static TrendStatRow BuildReciprocalRateSummaryRow(TrendStatSubject subject, PolynomialRegressionResult trend, double rate)
    {
        var reciprocal = 1 / rate;
        var perUnitLabel = TrendFormatting.FormatUnitAmount(1, subject.Unit);
        var isLinear = trend.Degree == 1;

        var reciprocalClimateExplanation = isLinear
            ? $"In years, this reads as \"about {Math.Abs(reciprocal).ToString("0", CultureInfo.InvariantCulture)} years for {perUnitLabel} of change at this rate\"."
            : $"At the current rate, this reads as \"about {Math.Abs(reciprocal).ToString("0", CultureInfo.InvariantCulture)} years for {perUnitLabel} of change\" - an instantaneous approximation, since a curve's rate itself keeps changing.";

        return new TrendStatRow(
            $"1/{(isLinear ? "Slope" : "Rate")}",
            reciprocal.ToString("0.00", CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: $"1/{(isLinear ? "Slope" : "Rate")} is the reciprocal of the rate - how many units of X it takes for Y to change by one unit.",
            ClimateExplanation: reciprocalClimateExplanation,
            WorkedExamples: null);
    }

    private static TrendStatSection BuildBestFitValues(
        TrendStatSubject subject,
        PolynomialRegressionResult trend,
        RateOfChangeEstimate rate,
        InterceptStatistics interceptStats,
        XInterceptStatistics? xIntercept,
        IReadOnlyList<double>? roots)
    {
        var unit = subject.Unit;
        var isLinear = trend.Degree == 1;
        var intercept = trend.Curve.Intercept;
        var rateSeText = TrendFormatting.FormatPerYearMagnitude(rate.StandardError, unit);
        var rateWord = isLinear ? "slope" : "rate";

        var rateRow = new TrendStatRow(
            RateLabel(trend),
            $"{TrendFormatting.FormatPerYearRate(rate.Rate, unit)} ± {rateSeText}",
            IsEmphasized: true,
            AbstractExplanation: $"The standard error (SE) measures how precisely the {rateWord} is estimated from this data - a smaller SE means the fitted {rateWord} is more tightly pinned down.",
            ClimateExplanation: $"Here the per-year rate is {TrendFormatting.FormatPerYearRate(rate.Rate, unit)} with a standard error of {rateSeText}, meaning repeated sampling of similarly-sized records would be expected to produce {rateWord} estimates clustered within about one SE of this value.",
            WorkedExamples: [$"{(isLinear ? "Slope" : "Rate")} ÷ SE = {(rate.Rate / rate.StandardError).ToString("0.0", CultureInfo.InvariantCulture)} is the t-statistic used to test whether this {rateWord} differs from zero{(isLinear ? " (see \"Is slope significantly non-zero?\" below)" : string.Empty)}."]);

        var yInterceptRow = new TrendStatRow(
            "Y-intercept",
            $"{TrendFormatting.FormatValue(intercept, unit)} ± {TrendFormatting.FormatValue(interceptStats.StandardError, unit)}",
            IsEmphasized: false,
            AbstractExplanation: "The Y-intercept's SE reflects how precisely the fitted curve's value at X = 0 is known.",
            ClimateExplanation: "This SE is typically large relative to the intercept's own size, because year 0 is so far outside the observed data that extending the curve there amplifies even small uncertainty in the fit.",
            WorkedExamples: null);

        var xInterceptRow = BuildXInterceptRow(subject, trend, xIntercept, roots, includeStandardError: true);

        var reciprocalRow = new TrendStatRow(
            $"1/{(isLinear ? "Slope" : "Rate")}",
            (1 / rate.Rate).ToString("0.00", CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: $"1/{(isLinear ? "Slope" : "Rate")} is also a ratio rather than a directly fitted quantity, so it has no simple standard error either.",
            ClimateExplanation: "Its uncertainty is asymmetric - an estimate too low pushes the reciprocal up more than a symmetrically-too-high estimate pushes it down - which is another reason only a point estimate is shown here.",
            WorkedExamples: null);

        return new TrendStatSection("Best-fit values", [rateRow, yInterceptRow, xInterceptRow, reciprocalRow]);
    }

    private static TrendStatSection BuildConfidenceIntervals(
        TrendStatSubject subject,
        PolynomialRegressionResult trend,
        RateOfChangeEstimate rate,
        InterceptStatistics interceptStats,
        XInterceptStatistics? xIntercept,
        IReadOnlyList<double>? roots)
    {
        var unit = subject.Unit;

        var rateRow = new TrendStatRow(
            RateLabel(trend),
            $"{TrendFormatting.FormatPerYearRate(rate.ConfidenceInterval.Lower, unit)} to {TrendFormatting.FormatPerYearRate(rate.ConfidenceInterval.Upper, unit)}",
            IsEmphasized: true,
            AbstractExplanation: "The 95% confidence interval is the range this rate is consistent with - the range you'd expect to capture the true rate 95% of the time, were this sampling process repeated.",
            ClimateExplanation: $"The data here are consistent with a per-year rate anywhere from {TrendFormatting.FormatPerYearRate(rate.ConfidenceInterval.Lower, unit)} to {TrendFormatting.FormatPerYearRate(rate.ConfidenceInterval.Upper, unit)}.",
            WorkedExamples: null);

        var yInterceptRow = new TrendStatRow(
            "Y-intercept",
            $"{TrendFormatting.FormatValue(interceptStats.ConfidenceInterval.Lower, unit)} to {TrendFormatting.FormatValue(interceptStats.ConfidenceInterval.Upper, unit)}",
            IsEmphasized: false,
            AbstractExplanation: "Same idea as the rate's interval, applied to the fitted value at X = 0.",
            ClimateExplanation: "Like the Y-intercept point estimate itself, this range describes a mathematical extrapolation to year 0, not a real climate quantity.",
            WorkedExamples: null);

        var xInterceptRow = BuildXInterceptConfidenceIntervalRow(subject, trend, xIntercept, roots);

        return new TrendStatSection("95% Confidence Intervals", [rateRow, yInterceptRow, xInterceptRow]);
    }

    /// <summary>
    /// The X-intercept row, shared across the Summary and Best-fit values sections. Degree 1 keeps
    /// today's Fieller's-theorem point estimate; degree 2/3 lists every real root found by
    /// <see cref="PolynomialRootFinder"/> instead, since a curve can cross zero more than once.
    /// </summary>
    private static TrendStatRow BuildXInterceptRow(
        TrendStatSubject subject,
        PolynomialRegressionResult trend,
        XInterceptStatistics? xIntercept,
        IReadOnlyList<double>? roots,
        bool includeStandardError)
    {
        var unit = subject.Unit;

        if (xIntercept is not null)
        {
            return new TrendStatRow(
                "X-intercept",
                xIntercept.Value.ToString("0", CultureInfo.InvariantCulture),
                IsEmphasized: false,
                AbstractExplanation: "The X-intercept is the X value (year) where the fitted line crosses Y = 0.",
                ClimateExplanation: $"{TrendFormatting.FormatUnitAmount(0, unit)} crossing the fitted line for an absolute {subject.Label.ToLowerInvariant()} lands far outside any plausible year and carries no climate meaning; it's shown only because it's part of the standard regression report this table mirrors.",
                WorkedExamples: null);
        }

        var rootsText = FormatRoots(roots!);

        return new TrendStatRow(
            "X-intercept",
            rootsText,
            IsEmphasized: false,
            AbstractExplanation: $"Where the fitted curve crosses Y = 0 - a curve can cross zero more than once, so every real crossing is listed. {(includeStandardError ? "Unlike a straight line's X-intercept, these are point estimates only - see the climate note." : string.Empty)}".Trim(),
            ClimateExplanation: $"{TrendFormatting.FormatUnitAmount(0, unit)} crossing the fitted curve for an absolute {subject.Label.ToLowerInvariant()} lands far outside any plausible year and carries no climate meaning. No confidence interval is shown: Fieller's theorem (used for a straight line, above) is specific to a ratio of two correlated coefficients and doesn't extend cleanly to three or four, so these are point estimates only.",
            WorkedExamples: null);
    }

    private static TrendStatRow BuildXInterceptConfidenceIntervalRow(
        TrendStatSubject subject,
        PolynomialRegressionResult trend,
        XInterceptStatistics? xIntercept,
        IReadOnlyList<double>? roots)
    {
        var unit = subject.Unit;

        if (xIntercept is not null)
        {
            var xInterceptClimateExplanation = xIntercept.ConfidenceInterval is { } xInterceptCi
                ? $"The data are consistent with the fitted line crossing {TrendFormatting.FormatUnitAmount(0, unit)} anywhere from {xInterceptCi.Lower.ToString("0", CultureInfo.InvariantCulture)} to {xInterceptCi.Upper.ToString("0", CultureInfo.InvariantCulture)} - as with the point estimate, this carries no climate meaning."
                : "This interval is undefined here: the slope isn't estimated precisely enough, relative to its own size, for Fieller's theorem to produce a finite range.";

            var xInterceptValue = xIntercept.ConfidenceInterval is { } ci
                ? $"{ci.Lower.ToString("0", CultureInfo.InvariantCulture)} to {ci.Upper.ToString("0", CultureInfo.InvariantCulture)}"
                : "Undefined";

            return new TrendStatRow(
                "X-intercept",
                xInterceptValue,
                IsEmphasized: false,
                AbstractExplanation: "Because the X-intercept is a ratio of two correlated estimates, its interval is computed using Fieller's theorem rather than a simple ± SE, and can come out asymmetric around the point estimate.",
                ClimateExplanation: xInterceptClimateExplanation,
                WorkedExamples: null);
        }

        return new TrendStatRow(
            "X-intercept",
            FormatRoots(roots!),
            IsEmphasized: false,
            AbstractExplanation: "Not computed for a quadratic/cubic fit - see \"Best-fit values\" above.",
            ClimateExplanation: "Fieller's theorem doesn't generalise to more than two correlated coefficients without a materially different derivation, so no interval is offered here rather than an unreliable one.",
            WorkedExamples: null);
    }

    private static string FormatRoots(IReadOnlyList<double> roots)
    {
        return roots.Count == 0
            ? "No real crossing"
            : string.Join(", ", roots.Select(root => root.ToString("0", CultureInfo.InvariantCulture)));
    }

    private static TrendStatSection BuildCoefficients(PolynomialRegressionResult trend)
    {
        var coefficientStats = PolynomialRegressionCalculator.CalculateCoefficientStatistics(trend);
        var rows = coefficientStats.Select(BuildCoefficientRow).ToList();

        return new TrendStatSection("Coefficients", rows);
    }

    private static TrendStatRow BuildCoefficientRow(CoefficientStatistics stat)
    {
        var label = stat.Power switch
        {
            0 => "Intercept (c₀)",
            1 => "Linear term (c₁)",
            2 => "Quadratic term (c₂)",
            3 => "Cubic term (c₃)",
            _ => $"c{stat.Power.ToString(CultureInfo.InvariantCulture)}",
        };

        var valueText = stat.Value.ToString("G6", CultureInfo.InvariantCulture);
        var seText = stat.StandardError.ToString("G6", CultureInfo.InvariantCulture);
        var ciText = $"{stat.ConfidenceInterval.Lower.ToString("G6", CultureInfo.InvariantCulture)} to {stat.ConfidenceInterval.Upper.ToString("G6", CultureInfo.InvariantCulture)}";

        return new TrendStatRow(
            label,
            $"{valueText} ± {seText}",
            IsEmphasized: stat.Power == 0,
            AbstractExplanation: $"The coefficient of X^{stat.Power.ToString(CultureInfo.InvariantCulture)} in the fitted equation below, with its own standard error and a t-statistic of {stat.TStatistic.ToString("0.00", CultureInfo.InvariantCulture)} against zero.",
            ClimateExplanation: $"95% CI: {ciText}. Raw coefficients like this are expressed in calendar-year units, so their size depends on the arbitrary choice of year zero and isn't meaningful to compare in magnitude against the other terms here, or against a fit of a different degree. The equation and worked examples below show what the fitted curve actually predicts.",
            WorkedExamples: null);
    }

    private static TrendStatSection BuildGoodnessOfFit(TrendStatSubject subject, PolynomialRegressionResult trend)
    {
        var rSquaredPercent = (trend.Fit.RSquared * 100).ToString("0", CultureInfo.InvariantCulture);
        var noisePercent = (100 - (trend.Fit.RSquared * 100)).ToString("0", CultureInfo.InvariantCulture);

        var rSquaredRow = new TrendStatRow(
            "R²",
            trend.Fit.RSquared.ToString("0.00", CultureInfo.InvariantCulture),
            IsEmphasized: true,
            AbstractExplanation: "R² is the proportion of year-to-year variance explained by the fitted curve, from 0 to 1.",
            ClimateExplanation: $"{rSquaredPercent}% of the year-to-year variation lines up with the fitted curve; the remaining {noisePercent}% is short-term natural variability the curve doesn't capture.",
            WorkedExamples: null);

        var syxRow = new TrendStatRow(
            "Sy.x",
            TrendFormatting.FormatValue(trend.Fit.ResidualStandardError, subject.Unit),
            IsEmphasized: false,
            AbstractExplanation: "Sy.x is the typical size of a residual - how far a single year's value scatters from the fitted curve, in the same units as Y.",
            ClimateExplanation: $"A typical year here differs from the smooth fitted curve by about {TrendFormatting.FormatValue(trend.Fit.ResidualStandardError, subject.Unit)}, which is why a single unusually hot, cold, wet or dry year doesn't by itself change the assessment of the fitted trend.",
            WorkedExamples: null);

        return new TrendStatSection("Goodness of Fit", [rSquaredRow, syxRow]);
    }

    private static TrendStatSection BuildSignificance(PolynomialRegressionResult trend)
    {
        var isLinear = trend.Degree == 1;
        var fStatisticText = double.IsPositiveInfinity(trend.Significance.FStatistic)
            ? "∞"
            : trend.Significance.FStatistic.ToString("0.0", CultureInfo.InvariantCulture);

        var count = trend.Significance.DegreesOfFreedom + trend.Degree + 1;
        var tStatisticText = double.IsInfinity(trend.Significance.TStatistic)
            ? "∞"
            : trend.Significance.TStatistic.ToString("0.0", CultureInfo.InvariantCulture);

        var fClimateExplanation = isLinear
            ? $"F = {fStatisticText} here, {(double.IsPositiveInfinity(trend.Significance.FStatistic) ? "which is as far above 1 as this can go" : "well above the 1 you'd expect if the slope were truly zero")}. For a simple linear regression like this, it's mathematically the same test as \"is the slope different from zero\" (see the Slope ÷ SE worked example in Best-fit values above) - F equals the square of the slope's t-statistic, so F = {fStatisticText} corresponds to t ≈ {tStatisticText}."
            : $"F = {fStatisticText} here, {(double.IsPositiveInfinity(trend.Significance.FStatistic) ? "which is as far above 1 as this can go" : "well above the 1 you'd expect if none of the fitted terms mattered")}. For this degree-{trend.Degree} fit, F tests whether the curve as a whole - every non-constant term together - explains significantly more variance than a flat line; no single coefficient's own t-statistic can answer that on its own.";

        var fAbstractExplanation = isLinear
            ? "F is a ratio comparing how much variance the regression line explains to how much variance is left over as noise - essentially variance explained by the slope ÷ residual variance, each divided by their degrees of freedom. A larger F means the line is capturing a real pattern relative to the scatter around it."
            : "F is a ratio comparing how much variance the fitted curve explains to how much variance is left over as noise - essentially variance explained by the curve's non-constant terms ÷ residual variance, each divided by their degrees of freedom. A larger F means the curve is capturing a real pattern relative to the scatter around it.";

        var fRow = new TrendStatRow(
            "F",
            fStatisticText,
            IsEmphasized: false,
            AbstractExplanation: fAbstractExplanation,
            ClimateExplanation: fClimateExplanation,
            WorkedExamples: null);

        var dfAbstractExplanation = isLinear
            ? "DFn (degrees of freedom, numerator) is the number of predictors in the model. DFd (degrees of freedom, denominator) is the residual degrees of freedom: n − 2 for simple linear regression (n data points, minus 2 for the two parameters estimated - slope and intercept)."
            : $"DFn (degrees of freedom, numerator) is the number of non-constant terms fitted - {trend.Degree} here. DFd (degrees of freedom, denominator) is the residual degrees of freedom: n − {(trend.Degree + 1).ToString(CultureInfo.InvariantCulture)} (n data points, minus {(trend.Degree + 1).ToString(CultureInfo.InvariantCulture)} for the {(trend.Degree + 1).ToString(CultureInfo.InvariantCulture)} fitted parameters).";

        var dfClimateExplanation = isLinear
            ? $"DFn = 1 because this is a simple linear regression with a single X variable (year). DFd = {trend.Significance.DegreesOfFreedom} tells you there are {count} data points here (n − 2 = {trend.Significance.DegreesOfFreedom})."
            : $"DFn = {trend.Degree} because this fit has {trend.Degree} non-constant terms. DFd = {trend.Significance.DegreesOfFreedom} tells you there are {count} data points here.";

        var dfRow = new TrendStatRow(
            "DFn, DFd",
            $"{trend.Degree.ToString(CultureInfo.InvariantCulture)}, {trend.Significance.DegreesOfFreedom.ToString(CultureInfo.InvariantCulture)}",
            IsEmphasized: false,
            AbstractExplanation: dfAbstractExplanation,
            ClimateExplanation: dfClimateExplanation,
            WorkedExamples: null);

        var pValueRow = new TrendStatRow(
            "P value",
            TrendFormatting.FormatPValue(trend.Significance.PValue),
            IsEmphasized: true,
            AbstractExplanation: "The p-value is the probability that a trend this strong could show up by random chance, even if there's no real change happening over time.",
            ClimateExplanation: $"This site calls a trend \"significant\" when that likelihood is below 5% (the p-value <  {trend.Significance.Alpha.ToString("0.00", CultureInfo.InvariantCulture)}). \"Significant\" means the trend is probably real, not that it's necessarily large. It's a statement about how surprising the data would be under the assumption of no effect.",
            WorkedExamples: null);

        var deviationRow = new TrendStatRow(
            "Deviation from zero?",
            trend.Significance.IsSlopeSignificant ? "Significant" : "Not significant",
            IsEmphasized: false,
            null,
            null,
            null);

        return new TrendStatSection("Is slope significantly non-zero?", [fRow, dfRow, pValueRow, deviationRow]);
    }

    private static TrendStatSection BuildEquation(TrendStatSubject subject, PolynomialRegressionResult trend, IReadOnlyList<DataPoint> fullPeriodPoints)
    {
        var equationText = FormatEquation(trend.Curve.Coefficients);

        var earlyX = trend.Input.MinimumX - 25;
        var lateX = trend.Input.MaximumX + 25;
        var laterX = trend.Input.MaximumX + 50;
        var nowX = DateTime.Now.Year;
        var earlyPrediction = PolynomialRegressionCalculator.Predict(trend, earlyX);
        var latePrediction = PolynomialRegressionCalculator.Predict(trend, lateX);
        var laterPrediction = PolynomialRegressionCalculator.Predict(trend, laterX);
        var nowPrediction = PolynomialRegressionCalculator.Predict(trend, nowX);

        var workedExamples = new List<string>
        {
            $"In {earlyX.ToString("0", CultureInfo.InvariantCulture)} this {(trend.Degree == 1 ? "line" : "curve")} predicts {TrendFormatting.FormatValue(earlyPrediction.PredictedY, subject.Unit)} (95% range for that year's actual value: {TrendFormatting.FormatValue(earlyPrediction.ObservationPredictionInterval.Lower, subject.Unit)} to {TrendFormatting.FormatValue(earlyPrediction.ObservationPredictionInterval.Upper, subject.Unit)}).",
            BuildNowExample(subject, nowX, nowPrediction, fullPeriodPoints),
            $"In {lateX.ToString("0", CultureInfo.InvariantCulture)} it predicts {TrendFormatting.FormatValue(latePrediction.PredictedY, subject.Unit)} ({TrendFormatting.FormatValue(latePrediction.ObservationPredictionInterval.Lower, subject.Unit)} to {TrendFormatting.FormatValue(latePrediction.ObservationPredictionInterval.Upper, subject.Unit)}).",
            $"In {laterX.ToString("0", CultureInfo.InvariantCulture)} it predicts {TrendFormatting.FormatValue(laterPrediction.PredictedY, subject.Unit)} ({TrendFormatting.FormatValue(laterPrediction.ObservationPredictionInterval.Lower, subject.Unit)} to {TrendFormatting.FormatValue(laterPrediction.ObservationPredictionInterval.Upper, subject.Unit)}).",
        };

        var row = new TrendStatRow(
            "Equation",
            equationText,
            IsEmphasized: false,
            AbstractExplanation: "This is the best-fit curve itself - plug in any X (year) to get the fitted Y for that year.",
            ClimateExplanation: "The range shown alongside each prediction is the 95% prediction range for one year's actual value, not just the uncertainty in the fitted curve itself - it's wider than the rate's own confidence interval because it also accounts for ordinary year-to-year natural variability, and for a curve it widens faster the further a prediction sits from the fitted window.",
            WorkedExamples: workedExamples);

        return new TrendStatSection("Equation", [row]);
    }

    private static string FormatEquation(IReadOnlyList<double> coefficients)
    {
        var terms = new List<string>();

        for (var power = coefficients.Count - 1; power >= 0; power--)
        {
            var value = coefficients[power];
            var magnitude = Math.Abs(value).ToString(power == 0 ? "0.00" : "0.00000", CultureInfo.InvariantCulture);
            var variable = power switch
            {
                0 => string.Empty,
                1 => "·X",
                2 => "·X²",
                3 => "·X³",
                _ => $"·X^{power.ToString(CultureInfo.InvariantCulture)}",
            };

            terms.Add(terms.Count == 0
                ? $"{(value < 0 ? "-" : string.Empty)}{magnitude}{variable}"
                : $"{(value >= 0 ? "+" : "-")} {magnitude}{variable}");
        }

        return "Y = " + string.Join(" ", terms);
    }

    private static string BuildNowExample(TrendStatSubject subject, int nowX, RegressionPrediction nowPrediction, IReadOnlyList<DataPoint> fullPeriodPoints)
    {
        var unit = subject.Unit;
        var predictedText = $"In {nowX.ToString("0", CultureInfo.InvariantCulture)}, it predicts {TrendFormatting.FormatValue(nowPrediction.PredictedY, unit)} ({TrendFormatting.FormatValue(nowPrediction.ObservationPredictionInterval.Lower, unit)} to {TrendFormatting.FormatValue(nowPrediction.ObservationPredictionInterval.Upper, unit)}).";
        var actualPoint = fullPeriodPoints.FirstOrDefault(p => (int)Math.Round(p.X) == nowX);
        if (actualPoint is null)
        {
            return predictedText;
        }

        var difference = actualPoint.Y - nowPrediction.PredictedY;
        var comparison = Math.Abs(difference) < 0.005
            ? "almost exactly matches the predicted value"
            : difference > 0
                ? $"is {TrendFormatting.FormatValue(difference, unit)} above the predicted value"
                : $"is {TrendFormatting.FormatValue(Math.Abs(difference), unit)} below the predicted value";

        return
            $"{predictedText} The {nowX.ToString("0", CultureInfo.InvariantCulture)} measured value is {TrendFormatting.FormatValue(actualPoint.Y, unit)}, which {comparison}.";
    }

    private static TrendStatSection BuildData(IReadOnlyList<DataPoint> points, PolynomialRegressionResult trend)
    {
        var maxReplicates = points.Count == 0 ? 1 : points.GroupBy(p => p.X).Max(g => g.Count());

        var countRow = new TrendStatRow("Number of X values", trend.Input.Count.ToString(CultureInfo.InvariantCulture), IsEmphasized: false, null, null, null);

        var totalRowClimateExplanation = maxReplicates > 1
            ? $"Here {maxReplicates} years have more than one observation, so this total is higher than the number of distinct years."
            : "Here every year in this window has at most one data point, so this total is the same as \"Number of X values\" above.";

        var totalRow = new TrendStatRow(
            "Total number of values",
            points.Count.ToString(CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: "This is the total number of (X, Y) observations that went into the fit, counting every Y value even if several share the same X. It matches \"Number of X values\" above unless some years have more than one observation (see \"Maximum number of Y replicates\" below).",
            ClimateExplanation: totalRowClimateExplanation,
            WorkedExamples: null);
        var replicatesRow = new TrendStatRow("Maximum number of Y replicates", maxReplicates.ToString(CultureInfo.InvariantCulture), IsEmphasized: false, null, null, null);

        var (minYear, maxYear, missingYears) = DescribeMissingYears(points);
        var yearSpan = maxYear - minYear + 1;

        var missingAbstractExplanation = missingYears.Count == 0
            ? $"There are {yearSpan} calendar years from {minYear} to {maxYear}, and every one of them has a data point."
            : $"Out of the {yearSpan} calendar years from {minYear} to {maxYear}, this many have no data point.";

        var missingRow = new TrendStatRow(
            "Number of missing values",
            missingYears.Count.ToString(CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: missingAbstractExplanation,
            ClimateExplanation: missingYears.Count == 0 ? null : $"Missing years: {string.Join(", ", missingYears)}.",
            WorkedExamples: null);

        return new TrendStatSection("Data", [countRow, totalRow, replicatesRow, missingRow]);
    }

    private static (int MinYear, int MaxYear, IReadOnlyList<int> MissingYears) DescribeMissingYears(IReadOnlyList<DataPoint> points)
    {
        var years = points.Select(x => (int)Math.Round(x.X)).OrderBy(x => x).ToList();
        var minYear = years[0];
        var maxYear = years[^1];
        var missingYears = Enumerable.Range(minYear, maxYear - minYear + 1).Except(years).ToList();

        return (minYear, maxYear, missingYears);
    }

    private static string FormatSignedValue(double value, string unit)
    {
        var sign = value >= 0 ? "+" : string.Empty;
        return $"{sign}{TrendFormatting.FormatValue(value, unit)}";
    }
}
