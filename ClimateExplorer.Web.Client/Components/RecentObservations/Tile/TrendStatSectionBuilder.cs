namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using System.Globalization;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Builds the GraphPad-style full statistical breakdown shown in the About-trends modal for one
// metric/window combination. Slope/Y-intercept/X-intercept/1-Slope each appear in three tiers:
// Summary (what does this mean), Best-fit values (what are the numbers and how precise is each
// one), and 95% Confidence Intervals (what range are we confident in) - deliberately three
// separate sections rather than one, so each answers a single question.
internal static class TrendStatSectionBuilder
{
    public static IReadOnlyList<TrendStatSection> Build(RecentObservationTrendViewModel metric, LinearRegressionResult trend)
    {
        var interceptStats = LinearRegressionCalculator.CalculateInterceptStatistics(trend);
        var xIntercept = LinearRegressionCalculator.CalculateXIntercept(trend);

        return
        [
            BuildSummary(metric, trend, xIntercept),
            BuildBestFitValues(metric, trend, interceptStats, xIntercept),
            BuildConfidenceIntervals(metric, trend, interceptStats, xIntercept),
            BuildGoodnessOfFit(metric, trend),
            BuildSignificance(trend),
            BuildEquation(metric, trend),
            BuildData(metric, trend),
        ];
    }

    private static TrendStatSection BuildSummary(RecentObservationTrendViewModel metric, LinearRegressionResult trend, XInterceptStatistics xIntercept)
    {
        var unit = metric.Unit;
        var slope = trend.Line.Slope;
        var intercept = trend.Line.Intercept;

        var slopeWorkedExample = trend.Significance.IsSlopeSignificant
            ? $"At this rate, the fitted line implies a change of about {FormatSigned(slope * 100, 1)}{unit} per century."
            : null;

        var slopeClimateExplanation = trend.Significance.IsSlopeSignificant
            ? "This site shows rates per decade because a per-decade number is large enough to read without implying year-to-year precision."
            : $"The fitted rate here is {TrendFormatting.FormatPerDecadeValue(trend, unit)}, but it isn't statistically significant (see \"Is slope significantly non-zero?\" below), so it's shown as \"No significant trend\" above rather than as a number that could be mistaken for a reliable rate.";

        var slopeRow = new TrendStatRow(
            "Slope",
            TrendFormatting.FormatPerDecade(trend, unit),
            IsEmphasized: true,
            AbstractExplanation: $"The slope is the change in the fitted value for every one-unit increase in X - here, the average change from one calendar year to the next. The per-year rate is {FormatSigned(slope, 5)}{unit}/year.",
            ClimateExplanation: slopeClimateExplanation,
            WorkedExamples: slopeWorkedExample is null ? null : [slopeWorkedExample]);

        var yInterceptRow = new TrendStatRow(
            "Y-intercept",
            TrendFormatting.FormatValue(intercept, unit),
            IsEmphasized: false,
            AbstractExplanation: "The Y-intercept is the fitted value of Y when X = 0 - the value the line predicts for calendar year 0.",
            ClimateExplanation: "Year 0 is thousands of years before this record began, so this is a mathematical artefact of extending the fitted line backwards, not a real prediction for any actual year.",
            WorkedExamples: null);

        var xInterceptRow = new TrendStatRow(
            "X-intercept",
            xIntercept.Value.ToString("0", CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: "The X-intercept is the X value (year) where the fitted line crosses Y = 0.",
            ClimateExplanation: $"0{unit} crossing the fitted line for an absolute {metric.Label.ToLowerInvariant()} lands far outside any plausible year and carries no climate meaning; it's shown only because it's part of the standard regression report this table mirrors.",
            WorkedExamples: null);

        var reciprocalRow = BuildReciprocalSlopeSummaryRow(metric, slope);

        return new TrendStatSection("Summary", [slopeRow, yInterceptRow, xInterceptRow, reciprocalRow]);
    }

    private static TrendStatRow BuildReciprocalSlopeSummaryRow(RecentObservationTrendViewModel metric, double slope)
    {
        var reciprocal = 1 / slope;
        var perUnitLabel = metric.Unit == "°C" ? "1°C" : "1mm/decade";

        return new TrendStatRow(
            "1/Slope",
            reciprocal.ToString("0.00", CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: "1/Slope is the reciprocal of the rate - how many units of X it takes for Y to change by one unit.",
            ClimateExplanation: $"In years, this reads as \"about {Math.Abs(reciprocal).ToString("0", CultureInfo.InvariantCulture)} years for {perUnitLabel} of change at this rate\".",
            WorkedExamples: null);
    }

    private static TrendStatSection BuildBestFitValues(RecentObservationTrendViewModel metric, LinearRegressionResult trend, InterceptStatistics interceptStats, XInterceptStatistics xIntercept)
    {
        var unit = metric.Unit;
        var slope = trend.Line.Slope;
        var intercept = trend.Line.Intercept;
        var slopeSe = trend.Significance.SlopeStandardError;

        var slopeRow = new TrendStatRow(
            "Slope",
            $"{FormatSigned(slope, 5)}{unit}/year ± {slopeSe.ToString("0.00000", CultureInfo.InvariantCulture)}{unit}/year",
            IsEmphasized: true,
            AbstractExplanation: "The standard error (SE) measures how precisely the slope is estimated from this data - a smaller SE means the fitted rate is more tightly pinned down.",
            ClimateExplanation: $"Here the per-year rate is {FormatSigned(slope, 5)}{unit}/year with a standard error of {slopeSe.ToString("0.00000", CultureInfo.InvariantCulture)}{unit}/year, meaning repeated sampling of similarly-sized records would be expected to produce slope estimates clustered within about one SE of this value.",
            WorkedExamples: [$"Slope ÷ SE = {trend.Significance.TStatistic.ToString("0.0", CultureInfo.InvariantCulture)} is the t-statistic used to test whether this trend differs from zero (see \"Is slope significantly non-zero?\" below)."]);

        var yInterceptRow = new TrendStatRow(
            "Y-intercept",
            $"{TrendFormatting.FormatValue(intercept, unit)} ± {TrendFormatting.FormatValue(interceptStats.StandardError, unit)}",
            IsEmphasized: false,
            AbstractExplanation: "The Y-intercept's SE reflects how precisely the fitted line's value at X = 0 is known.",
            ClimateExplanation: "This SE is typically large relative to the intercept's own size, because year 0 is so far outside the observed data that extending the line there amplifies even small uncertainty in the slope.",
            WorkedExamples: null);

        var xInterceptRow = new TrendStatRow(
            "X-intercept",
            xIntercept.Value.ToString("0", CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: "The X-intercept is a ratio of two fitted quantities (-intercept ÷ slope) rather than something with its own standard error, so no ± SE is shown here.",
            ClimateExplanation: "GraphPad-style tables show only the point estimate for a ratio like this; its range is deferred to Fieller's theorem in the confidence-interval section below.",
            WorkedExamples: null);

        var reciprocalRow = new TrendStatRow(
            "1/Slope",
            (1 / slope).ToString("0.00", CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: "1/Slope is also a ratio rather than a directly fitted quantity, so it has no simple standard error either.",
            ClimateExplanation: "Its uncertainty is asymmetric - a slope estimated too low pushes 1/Slope up more than a symmetrically-too-high slope pushes it down - which is another reason only a point estimate is shown here.",
            WorkedExamples: null);

        return new TrendStatSection("Best-fit values", [slopeRow, yInterceptRow, xInterceptRow, reciprocalRow]);
    }

    private static TrendStatSection BuildConfidenceIntervals(RecentObservationTrendViewModel metric, LinearRegressionResult trend, InterceptStatistics interceptStats, XInterceptStatistics xIntercept)
    {
        var unit = metric.Unit;
        var slopeCi = trend.Significance.SlopeConfidenceInterval;

        var slopeRow = new TrendStatRow(
            "Slope",
            $"{FormatSigned(slopeCi.Lower, 5)}{unit}/year to {FormatSigned(slopeCi.Upper, 5)}{unit}/year",
            IsEmphasized: true,
            AbstractExplanation: "The 95% confidence interval is the range of slopes the data are consistent with - the range you'd expect to capture the true rate 95% of the time, were this sampling process repeated.",
            ClimateExplanation: $"The data here are consistent with a per-year rate anywhere from {FormatSigned(slopeCi.Lower, 5)}{unit} to {FormatSigned(slopeCi.Upper, 5)}{unit} per year.",
            WorkedExamples: null);

        var yInterceptRow = new TrendStatRow(
            "Y-intercept",
            $"{TrendFormatting.FormatValue(interceptStats.ConfidenceInterval.Lower, unit)} to {TrendFormatting.FormatValue(interceptStats.ConfidenceInterval.Upper, unit)}",
            IsEmphasized: false,
            AbstractExplanation: "Same idea as the slope's interval, applied to the fitted value at X = 0.",
            ClimateExplanation: "Like the Y-intercept point estimate itself, this range describes a mathematical extrapolation to year 0, not a real climate quantity.",
            WorkedExamples: null);

        var xInterceptClimateExplanation = xIntercept.ConfidenceInterval is { } xInterceptCi
            ? $"The data are consistent with the fitted line crossing 0{unit} anywhere from {xInterceptCi.Lower.ToString("0", CultureInfo.InvariantCulture)} to {xInterceptCi.Upper.ToString("0", CultureInfo.InvariantCulture)} - as with the point estimate, this carries no climate meaning."
            : "This interval is undefined here: the slope isn't estimated precisely enough, relative to its own size, for Fieller's theorem to produce a finite range.";

        var xInterceptValue = xIntercept.ConfidenceInterval is { } ci
            ? $"{ci.Lower.ToString("0", CultureInfo.InvariantCulture)} to {ci.Upper.ToString("0", CultureInfo.InvariantCulture)}"
            : "Undefined";

        var xInterceptRow = new TrendStatRow(
            "X-intercept",
            xInterceptValue,
            IsEmphasized: false,
            AbstractExplanation: "Because the X-intercept is a ratio of two correlated estimates, its interval is computed using Fieller's theorem rather than a simple ± SE, and can come out asymmetric around the point estimate.",
            ClimateExplanation: xInterceptClimateExplanation,
            WorkedExamples: null);

        return new TrendStatSection("95% Confidence Intervals", [slopeRow, yInterceptRow, xInterceptRow]);
    }

    private static TrendStatSection BuildGoodnessOfFit(RecentObservationTrendViewModel metric, LinearRegressionResult trend)
    {
        var rSquaredPercent = (trend.Fit.RSquared * 100).ToString("0", CultureInfo.InvariantCulture);
        var noisePercent = (100 - (trend.Fit.RSquared * 100)).ToString("0", CultureInfo.InvariantCulture);

        var rSquaredRow = new TrendStatRow(
            "R²",
            trend.Fit.RSquared.ToString("0.00", CultureInfo.InvariantCulture),
            IsEmphasized: true,
            AbstractExplanation: "R² is the proportion of year-to-year variance explained by the straight line, from 0 to 1.",
            ClimateExplanation: $"{rSquaredPercent}% of the year-to-year variation lines up with the long-term trend; the remaining {noisePercent}% is short-term natural variability the straight line doesn't capture.",
            WorkedExamples: null);

        var syxRow = new TrendStatRow(
            "Sy.x",
            TrendFormatting.FormatValue(trend.Fit.ResidualStandardError, metric.Unit),
            IsEmphasized: false,
            AbstractExplanation: "Sy.x is the typical size of a residual - how far a single year's value scatters from the fitted line, in the same units as Y.",
            ClimateExplanation: $"A typical year here differs from the smooth long-term trend by about {TrendFormatting.FormatValue(trend.Fit.ResidualStandardError, metric.Unit)}, which is why a single unusually hot, cold, wet or dry year doesn't by itself change the assessment of the long-term trend.",
            WorkedExamples: null);

        return new TrendStatSection("Goodness of Fit", [rSquaredRow, syxRow]);
    }

    private static TrendStatSection BuildSignificance(LinearRegressionResult trend)
    {
        var fStatisticText = double.IsPositiveInfinity(trend.Significance.FStatistic)
            ? "∞"
            : trend.Significance.FStatistic.ToString("0.0", CultureInfo.InvariantCulture);

        var count = trend.Significance.DegreesOfFreedom + 2;
        var tStatisticText = double.IsInfinity(trend.Significance.TStatistic)
            ? "∞"
            : trend.Significance.TStatistic.ToString("0.0", CultureInfo.InvariantCulture);

        var fRow = new TrendStatRow(
            "F",
            fStatisticText,
            IsEmphasized: false,
            AbstractExplanation: "F is a ratio comparing how much variance the regression line explains to how much variance is left over as noise - essentially variance explained by the slope ÷ residual variance, each divided by their degrees of freedom. A larger F means the line is capturing a real pattern relative to the scatter around it.",
            ClimateExplanation: $"F = {fStatisticText} here, {(double.IsPositiveInfinity(trend.Significance.FStatistic) ? "which is as far above 1 as this can go" : "well above the 1 you'd expect if the slope were truly zero")}. For a simple linear regression like this, it's mathematically the same test as \"is the slope different from zero\" (see the Slope ÷ SE worked example in Best-fit values above) - F equals the square of the slope's t-statistic, so F = {fStatisticText} corresponds to t ≈ {tStatisticText}.",
            WorkedExamples: null);

        var dfRow = new TrendStatRow(
            "DFn, DFd",
            $"1, {trend.Significance.DegreesOfFreedom}",
            IsEmphasized: false,
            AbstractExplanation: "DFn (degrees of freedom, numerator) is the number of predictors in the model. DFd (degrees of freedom, denominator) is the residual degrees of freedom: n − 2 for simple linear regression (n data points, minus 2 for the two parameters estimated - slope and intercept).",
            ClimateExplanation: $"DFn = 1 because this is a simple linear regression with a single X variable (year). DFd = {trend.Significance.DegreesOfFreedom} tells you there are {count} data points here (n − 2 = {trend.Significance.DegreesOfFreedom}).",
            WorkedExamples: null);

        var pValueRow = new TrendStatRow(
            "P value",
            FormatPValue(trend.Significance.PValue),
            IsEmphasized: true,
            AbstractExplanation: "The p-value is the probability that a trend this strong could show up by random chance, even if there's no real change happening over time.",
            ClimateExplanation: $"ClimateExplorer calls a trend \"significant\" when that likelihood is below 5% (the p-value <  {trend.Significance.Alpha.ToString("0.00", CultureInfo.InvariantCulture)}). \"Significant\" means the trend is probably real, not that it's necessarily large. It's a statement about how surprising the data would be under the assumption of no effect.",
            WorkedExamples: null);

        var deviationRow = new TrendStatRow(
            "Deviation from zero?",
            trend.Significance.IsSlopeSignificant ? "Significant" : "Not significant",
            IsEmphasized: true,
            null,
            null,
            null);

        return new TrendStatSection("Is slope significantly non-zero?", [fRow, dfRow, pValueRow, deviationRow]);
    }

    private static TrendStatSection BuildEquation(RecentObservationTrendViewModel metric, LinearRegressionResult trend)
    {
        var slope = trend.Line.Slope;
        var intercept = trend.Line.Intercept;
        var equationText = $"Y = {slope.ToString("0.00000", CultureInfo.InvariantCulture)}·X {(intercept >= 0 ? "+" : "-")} {Math.Abs(intercept).ToString("0.00", CultureInfo.InvariantCulture)}";

        var earlyX = trend.Input.MinimumX - 25;
        var lateX = trend.Input.MaximumX + 25;
        var laterX = trend.Input.MaximumX + 50;
        var nowX = DateTime.Now.Year;
        var earlyPrediction = LinearRegressionCalculator.Predict(trend, earlyX);
        var latePrediction = LinearRegressionCalculator.Predict(trend, lateX);
        var laterPrediction = LinearRegressionCalculator.Predict(trend, laterX);
        var nowPrediction = LinearRegressionCalculator.Predict(trend, nowX);

        var workedExamples = new List<string>
        {
            $"In {earlyX.ToString("0", CultureInfo.InvariantCulture)} this line predicts {TrendFormatting.FormatValue(earlyPrediction.PredictedY, metric.Unit)} (95% range for that year's actual value: {TrendFormatting.FormatValue(earlyPrediction.ObservationPredictionInterval.Lower, metric.Unit)} to {TrendFormatting.FormatValue(earlyPrediction.ObservationPredictionInterval.Upper, metric.Unit)}).",
            BuildNowExample(metric, nowX, nowPrediction),
            $"In {lateX.ToString("0", CultureInfo.InvariantCulture)} it predicts {TrendFormatting.FormatValue(latePrediction.PredictedY, metric.Unit)} ({TrendFormatting.FormatValue(latePrediction.ObservationPredictionInterval.Lower, metric.Unit)} to {TrendFormatting.FormatValue(latePrediction.ObservationPredictionInterval.Upper, metric.Unit)}).",
            $"In {laterX.ToString("0", CultureInfo.InvariantCulture)} it predicts {TrendFormatting.FormatValue(laterPrediction.PredictedY, metric.Unit)} ({TrendFormatting.FormatValue(laterPrediction.ObservationPredictionInterval.Lower, metric.Unit)} to {TrendFormatting.FormatValue(laterPrediction.ObservationPredictionInterval.Upper, metric.Unit)}).",
        };

        var row = new TrendStatRow(
            "Equation",
            equationText,
            IsEmphasized: false,
            AbstractExplanation: "This is the best-fit line itself - plug in any X (year) to get the fitted Y for that year.",
            ClimateExplanation: "The range shown alongside each prediction is the 95% prediction range for one year's actual value, not just the uncertainty in the fitted line itself - it's wider than the slope's own confidence interval because it also accounts for ordinary year-to-year natural variability.",
            WorkedExamples: workedExamples);

        return new TrendStatSection("Equation", [row]);
    }

    private static string BuildNowExample(RecentObservationTrendViewModel metric, int nowX, RegressionPrediction nowPrediction)
    {
        var unit = metric.Unit;
        var predictedText = $"In {nowX.ToString("0", CultureInfo.InvariantCulture)}, it predicts {TrendFormatting.FormatValue(nowPrediction.PredictedY, metric.Unit)} ({TrendFormatting.FormatValue(nowPrediction.ObservationPredictionInterval.Lower, metric.Unit)} to {TrendFormatting.FormatValue(nowPrediction.ObservationPredictionInterval.Upper, metric.Unit)}).";
        var actualPoint = metric.FullPeriodPoints.FirstOrDefault(p => (int)Math.Round(p.X) == nowX);
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

    private static TrendStatSection BuildData(RecentObservationTrendViewModel metric, LinearRegressionResult trend)
    {
        var points = GetPoints(metric, trend);
        var maxReplicates = points.Count == 0 ? 1 : points.GroupBy(p => p.X).Max(g => g.Count());

        var countRow = new TrendStatRow("Number of X values", trend.Input.Count.ToString(CultureInfo.InvariantCulture), IsEmphasized: false, null, null, null);
        var totalRow = new TrendStatRow("Total number of values", points.Count.ToString(CultureInfo.InvariantCulture), IsEmphasized: false, null, null, null);
        var replicatesRow = new TrendStatRow("Maximum number of Y replicates", maxReplicates.ToString(CultureInfo.InvariantCulture), IsEmphasized: false, null, null, null);

        var (minYear, maxYear, missingYears) = DescribeMissingYears(points);
        var yearSpan = maxYear - minYear + 1;
        var missingText = missingYears.Count == 0
            ? "No years are missing."
            : $"Missing years: {string.Join(", ", missingYears)}.";

        var missingRow = new TrendStatRow(
            "Number of missing values",
            missingYears.Count.ToString(CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: $"Out of the {yearSpan} calendar years from {minYear} to {maxYear}, this many have no data point.",
            ClimateExplanation: missingText,
            WorkedExamples: null);

        return new TrendStatSection("Data", [countRow, totalRow, replicatesRow, missingRow]);
    }

    private static IReadOnlyList<DataPoint> GetPoints(RecentObservationTrendViewModel metric, LinearRegressionResult trend)
    {
        if (ReferenceEquals(trend, metric.FullPeriodTrend))
        {
            return metric.FullPeriodPoints;
        }

        if (ReferenceEquals(trend, metric.RecentTrend))
        {
            return metric.RecentTrendPoints;
        }

        return metric.FirstHalfTrendPoints;
    }

    private static (int MinYear, int MaxYear, IReadOnlyList<int> MissingYears) DescribeMissingYears(IReadOnlyList<DataPoint> points)
    {
        var years = points.Select(x => (int)Math.Round(x.X)).OrderBy(x => x).ToList();
        var minYear = years[0];
        var maxYear = years[^1];
        var missingYears = Enumerable.Range(minYear, maxYear - minYear + 1).Except(years).ToList();

        return (minYear, maxYear, missingYears);
    }

    private static string FormatSigned(double value, int decimalPlaces)
    {
        var format = "0." + new string('0', decimalPlaces);
        var text = value.ToString(format, CultureInfo.InvariantCulture);
        return value >= 0 ? $"+{text}" : text;
    }

    private static string FormatPValue(double pValue)
    {
        return pValue < 0.0001
            ? "< 0.0001"
            : pValue.ToString("0.0000", CultureInfo.InvariantCulture);
    }
}
