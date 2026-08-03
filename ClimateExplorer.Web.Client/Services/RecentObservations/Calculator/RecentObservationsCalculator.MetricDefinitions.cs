#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;

// The concrete Metric/MetricDomain instances for each observation domain. Kept apart from
// the Metric/MetricDomain type definitions (RecentObservationsCalculator.Metrics.cs) since
// this is pure data - what each domain measures and how it's labelled - not behaviour.
public sealed partial class RecentObservationsCalculator
{
    private static readonly Metric MeanTemperatureMetric = new(
        "temp.mean",
        "Mean",
        "Average mean",
        x => x.Mean,
        MetricAggregation.Mean,
        FormatTemperature,
        "Average mean",
        "Mean",
        "°C");

    private static readonly Metric AverageMaxTemperatureMetric = new(
        "temp.max",
        "Maximum",
        "Average maximum",
        x => x.Max,
        MetricAggregation.Mean,
        FormatTemperature,
        "Average maximum",
        "Maximum",
        "°C");

    private static readonly Metric AverageMinTemperatureMetric = new(
        "temp.min",
        "Minimum",
        "Average minimum",
        x => x.Min,
        MetricAggregation.Mean,
        FormatTemperature,
        "Average minimum",
        "Minimum",
        "°C");

    private static readonly Metric HighestDailyMaxTemperatureMetric = new(
        "temp.daily-max-high",
        "Highest daily max",
        "Highest daily max",
        x => x.Max,
        MetricAggregation.Max,
        FormatTemperature,
        "Highest daily maximum",
        "Highest daily maximum",
        "°C");

    private static readonly Metric LowestDailyMaxTemperatureMetric = new(
        "temp.daily-max-low",
        "Lowest daily max",
        "Lowest daily max",
        x => x.Max,
        MetricAggregation.Min,
        FormatTemperature,
        "Lowest daily maximum",
        "Lowest daily maximum",
        "°C");

    private static readonly Metric HighestDailyMinTemperatureMetric = new(
        "temp.daily-min-high",
        "Highest daily min",
        "Highest daily min",
        x => x.Min,
        MetricAggregation.Max,
        FormatTemperature,
        "Highest daily minimum",
        "Highest daily minimum",
        "°C");

    private static readonly Metric LowestDailyMinTemperatureMetric = new(
        "temp.daily-min-low",
        "Lowest daily min",
        "Lowest daily min",
        x => x.Min,
        MetricAggregation.Min,
        FormatTemperature,
        "Lowest daily minimum",
        "Lowest daily minimum",
        "°C");

    // Daily tiles describe a single day, which has a maximum, a minimum and a mean
    // — not aggregates across days. These reuse the period-metric keys (so their
    // historical distributions are computed once, by calendar date) but carry
    // single-day labels for the expanded view.
    private static readonly Metric DailyMaxTemperatureMetric = new(
        "temp.daily-max-high",
        "Maximum",
        "Maximum",
        x => x.Max,
        MetricAggregation.Max,
        FormatTemperature,
        "Maximum",
        "Maximum",
        "°C");

    private static readonly Metric DailyMinTemperatureMetric = new(
        "temp.daily-min-low",
        "Minimum",
        "Minimum",
        x => x.Min,
        MetricAggregation.Min,
        FormatTemperature,
        "Minimum",
        "Minimum",
        "°C");

    private static readonly Metric DailyMeanTemperatureMetric = new(
        "temp.mean",
        "Mean",
        "Mean",
        x => x.Mean,
        MetricAggregation.Mean,
        FormatTemperature,
        "Mean",
        "Mean",
        "°C");

    private static readonly Metric PrecipitationMetric = new(
        "precip.total",
        "Precipitation total",
        "Precipitation total",
        x => x.Precipitation,
        MetricAggregation.Sum,
        FormatPrecipitation,
        "Total precipitation",
        "Precipitation",
        "mm");

    private static readonly Metric HighestDailyPrecipitationMetric = new(
        "precip.daily-high",
        "Highest daily precipitation",
        "Highest daily precipitation",
        x => x.Precipitation,
        MetricAggregation.Max,
        FormatPrecipitation,
        "Highest daily precipitation",
        "Highest daily precipitation",
        "mm");

    private static readonly Metric DailyPrecipitationMetric = new(
        "precip.total",
        "Precipitation",
        "Precipitation",
        x => x.Precipitation,
        MetricAggregation.Sum,
        FormatPrecipitation,
        "Precipitation",
        "Precipitation",
        "mm");

    private static readonly Metric Co2Metric = new(
        "co2.value",
        "Mean CO₂",
        "Mean CO₂",
        x => x.Co2,
        MetricAggregation.Mean,
        FormatCo2,
        "Mean CO₂",
        "CO₂",
        "ppm");

    private static readonly Metric DailyCo2Metric = new(
        "co2.value",
        "CO₂",
        "CO₂",
        x => x.Co2,
        MetricAggregation.Mean,
        FormatCo2,
        "CO₂",
        "CO₂",
        "ppm");

    private static readonly MetricDomain TemperatureDomain = new(
        MeanTemperatureMetric,
        [AverageMaxTemperatureMetric, AverageMinTemperatureMetric],
        [
            new MetricGroup(MetricGroupKey.Ranking, "Ranking", [AverageMaxTemperatureMetric, AverageMinTemperatureMetric, MeanTemperatureMetric]),
            new MetricGroup(MetricGroupKey.DailyRankings, "Daily ranking", [HighestDailyMaxTemperatureMetric, LowestDailyMaxTemperatureMetric, HighestDailyMinTemperatureMetric, LowestDailyMinTemperatureMetric]),
        ],
        [
            new MetricGroup(MetricGroupKey.Ranking, "Ranking", [DailyMaxTemperatureMetric, DailyMinTemperatureMetric, DailyMeanTemperatureMetric]),
        ],
        [AverageMaxTemperatureMetric, AverageMinTemperatureMetric, MeanTemperatureMetric],
        [DailyMaxTemperatureMetric, DailyMinTemperatureMetric, DailyMeanTemperatureMetric],
        ShowHistoricalMin: true,
        "Warmest",
        "Coolest",
        RecentObservationComparison.BuildTemperatureHeadline,
        RecentObservationComparison.BuildTemperaturePercentileSentence,
        GetTemperatureTone);

    private static readonly MetricDomain PrecipitationDomain = new(
        PrecipitationMetric,
        [],
        [
            new MetricGroup(MetricGroupKey.Ranking, "Ranking", [PrecipitationMetric]),
            new MetricGroup(MetricGroupKey.DailyRankings, "Daily ranking", [HighestDailyPrecipitationMetric]),
        ],
        [
            new MetricGroup(MetricGroupKey.Ranking, "Ranking", [DailyPrecipitationMetric]),
        ],
        [PrecipitationMetric],
        [DailyPrecipitationMetric],
        ShowHistoricalMin: true,
        "Wettest",
        "Driest",
        RecentObservationComparison.BuildPrecipitationHeadline,
        (_, startYear, ranking) => RecentObservationComparison.BuildPrecipitationPercentileSentence(startYear, ranking),
        GetPrecipitationTone);

    private static readonly MetricDomain Co2Domain = new(
        Co2Metric,
        [],
        [
            new MetricGroup(MetricGroupKey.Ranking, "Ranking", [Co2Metric]),
        ],
        [
            new MetricGroup(MetricGroupKey.Ranking, "Ranking", [DailyCo2Metric]),
        ],
        [Co2Metric],
        [DailyCo2Metric],
        ShowHistoricalMin: true,
        "Highest",
        "Lowest",
        RecentObservationComparison.BuildCo2Headline,
        (_, startYear, ranking) => RecentObservationComparison.BuildCo2PercentileSentence(startYear, ranking),
        GetCo2Tone);
}
#pragma warning restore SA1201, SA1204
