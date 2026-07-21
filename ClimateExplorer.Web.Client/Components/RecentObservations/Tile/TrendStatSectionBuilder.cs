namespace ClimateExplorer.Web.Client.Components.RecentObservations;

using System.Globalization;
using ClimateExplorer.Core.Stats;
using ClimateExplorer.Core.Stats.Model;
using ClimateExplorer.Web.Client.Services.RecentObservations;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// Builds the GraphPad-style full statistical breakdown shown in the About-trends modal for one
// metric/window combination. The 95% CI for slope/Y-intercept/X-intercept is deliberately not a
// section of its own - a stat and its own interval are one concept, so that content is folded into
// the explanation of the corresponding Best-fit-values row instead of repeated as separate rows.
internal static class TrendStatSectionBuilder
{
    public static IReadOnlyList<TrendStatSection> Build(RecentObservationTrendViewModel metric, LinearRegressionResult trend)
    {
        return
        [
            BuildBestFitValues(metric, trend),
            BuildGoodnessOfFit(metric, trend),
            BuildSignificance(trend),
            BuildEquation(metric, trend),
            BuildData(metric, trend),
        ];
    }

    private static TrendStatSection BuildBestFitValues(RecentObservationTrendViewModel metric, LinearRegressionResult trend)
    {
        var unit = metric.Unit;
        var slope = trend.Line.Slope;
        var intercept = trend.Line.Intercept;
        var interceptStats = LinearRegressionCalculator.CalculateInterceptStatistics(trend);
        var xIntercept = LinearRegressionCalculator.CalculateXIntercept(trend);

        var slopeWorkedExample = trend.Significance.IsSlopeSignificant
            ? $"At this rate, the fitted line implies a change of about {FormatSigned(slope * 100, 1)}{unit} per century."
            : null;

        var slopeRow = new TrendStatRow(
            "Slope",
            TrendFormatting.FormatPerDecade(trend, unit),
            IsEmphasized: true,
            AbstractExplanation: $"The slope is the change in the fitted value for every one-unit increase in X - here, the average change from one calendar year to the next. The per-year rate is {FormatSigned(slope, 5)}{unit}/year, with a 95% confidence interval of {FormatSigned(trend.Significance.SlopeConfidenceInterval.Lower, 5)}{unit} to {FormatSigned(trend.Significance.SlopeConfidenceInterval.Upper, 5)}{unit} per year - the range of per-year rates the data are consistent with.",
            ClimateExplanation: $"This site shows rates per decade because a per-decade number is large enough to read without implying year-to-year precision.",
            WorkedExamples: slopeWorkedExample is null ? null : [slopeWorkedExample]);

        var yInterceptRow = new TrendStatRow(
            "Y-intercept",
            TrendFormatting.FormatValue(intercept, unit),
            IsEmphasized: false,
            AbstractExplanation: "The Y-intercept is the fitted value of Y when X = 0 - the value the line predicts for calendar year 0.",
            ClimateExplanation: $"Year 0 is thousands of years before this record began, so this is a mathematical artefact of extending the fitted line backwards, not a real prediction for any actual year. Its own 95% confidence interval is {TrendFormatting.FormatValue(interceptStats.ConfidenceInterval.Lower, unit)} to {TrendFormatting.FormatValue(interceptStats.ConfidenceInterval.Upper, unit)}.",
            WorkedExamples: null);

        var xInterceptClimateExplanation = xIntercept.ConfidenceInterval is { } xInterceptCi
            ? $"0{unit} crossing the fitted line for an absolute {metric.Label.ToLowerInvariant()} lands far outside any plausible year and carries no climate meaning; it's shown only because it's part of the standard regression report this table mirrors. Its 95% confidence interval is {xInterceptCi.Lower.ToString("0", CultureInfo.InvariantCulture)} to {xInterceptCi.Upper.ToString("0", CultureInfo.InvariantCulture)}."
            : $"0{unit} crossing the fitted line for an absolute {metric.Label.ToLowerInvariant()} lands far outside any plausible year and carries no climate meaning; it's shown only because it's part of the standard regression report this table mirrors. Its confidence interval is undefined here: the slope isn't estimated precisely enough, relative to its own size, for Fieller's theorem to produce a finite range.";

        var xInterceptRow = new TrendStatRow(
            "X-intercept",
            xIntercept.Value.ToString("0", CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: "The X-intercept is the X value (year) where the fitted line crosses Y = 0.",
            ClimateExplanation: xInterceptClimateExplanation,
            WorkedExamples: null);

        var reciprocalRow = BuildReciprocalSlopeRow(metric, slope);

        return new TrendStatSection("Best-fit values", [slopeRow, yInterceptRow, xInterceptRow, reciprocalRow]);
    }

    private static TrendStatRow BuildReciprocalSlopeRow(RecentObservationTrendViewModel metric, double slope)
    {
        var reciprocal = 1 / slope;
        var perUnitLabel = metric.Unit == "°C" ? "1°C" : "1mm/decade";

        return new TrendStatRow(
            "1/Slope",
            reciprocal.ToString("0.00", CultureInfo.InvariantCulture),
            IsEmphasized: false,
            AbstractExplanation: "1/Slope is the reciprocal of the rate - how many units of X it takes for Y to change by one unit.",
            ClimateExplanation: $"In years, this reads as \"about {Math.Abs(reciprocal).ToString("0", CultureInfo.InvariantCulture)} years for {perUnitLabel} of change at this rate\" - the most directly tangible number in this table.",
            WorkedExamples: null);
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

        var fRow = new TrendStatRow("F", fStatisticText, IsEmphasized: true, null, null, null);
        var dfRow = new TrendStatRow("DFn, DFd", $"1, {trend.Significance.DegreesOfFreedom}", IsEmphasized: true, null, null, null);

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
            IsEmphasized: true,
            AbstractExplanation: "This is the best-fit line itself - plug in any X (year) to get the fitted Y for that year.",
            ClimateExplanation: "The range shown alongside each prediction is the 95% range for one year's actual value, not just the uncertainty in the fitted line itself - it's wider than the slope's own confidence interval because it also accounts for ordinary year-to-year natural variability.",
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
