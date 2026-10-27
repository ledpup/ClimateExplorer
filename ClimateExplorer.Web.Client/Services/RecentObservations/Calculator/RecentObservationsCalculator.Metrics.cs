#pragma warning disable SA1201, SA1204
namespace ClimateExplorer.Web.Client.Services;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.Client.UiModel.RecentObservations;
using ClimateExplorer.Web.Client.Units;
using static ClimateExplorer.Core.Enums;

// The metric model: a Metric picks a value off a DailyObservation and says how to aggregate
// and format it; a MetricDomain groups the metrics that make up one tab (temperature,
// precipitation, CO2) along with the domain-specific headline/tone logic. Concrete metric
// and domain instances live in RecentObservationsCalculator.MetricDefinitions.cs.
public sealed partial class RecentObservationsCalculator
{
    private static IReadOnlyDictionary<string, MetricObservationValue> ComputeMetrics(IReadOnlyList<DailyObservation> records, MetricDomain domain)
    {
        var result = new Dictionary<string, MetricObservationValue>();

        foreach (var metric in domain.AllMetrics)
        {
            var values = records
                .Select(record => new MetricObservationValue(record.Date, metric.Select(record)))
                .Where(x => x.Value.HasValue)
                .ToList();

            if (values.Count > 0)
            {
                result[metric.Key] = Aggregate(values, metric.Aggregation);
            }
        }

        return result;
    }

    private static MetricObservationValue Aggregate(IReadOnlyList<MetricObservationValue> values, MetricAggregation aggregation)
    {
        return aggregation switch
        {
            MetricAggregation.Mean => new MetricObservationValue(null, values.Average(x => x.Value!.Value)),
            MetricAggregation.Sum => new MetricObservationValue(null, values.Sum(x => x.Value!.Value)),
            MetricAggregation.Max => values.OrderByDescending(x => x.Value!.Value).ThenBy(x => x.OccurredOn).First(),
            MetricAggregation.Min => values.OrderBy(x => x.Value!.Value).ThenBy(x => x.OccurredOn).First(),
            _ => throw new NotImplementedException(),
        };
    }

    private enum MetricAggregation
    {
        Mean,
        Sum,
        Max,
        Min,
    }

    /// <param name="Unit">The metric unit the observations are in. All of the calculator's arithmetic is in
    /// this unit; it's converted only when formatted, through <see cref="FormatLevel"/> or <see cref="FormatChange"/>.</param>
    private sealed record Metric(
        string Key,
        string SingularLabel,
        string PluralLabel,
        Func<DailyObservation, double?> Select,
        MetricAggregation Aggregation,
        UnitOfMeasure Unit,
        string DetailLabel,
        string VariationLabel)
    {
        /// <summary>An observation, record, range end or average of observations.</summary>
        public string FormatLevel(double value, IUnitFormatter units) => units.Format(Measure.Level(value, Unit));

        /// <summary>A difference from a level: an anomaly or a spread.</summary>
        public string FormatChange(double value, IUnitFormatter units, bool signed = false) => units.Format(Measure.Change(value, Unit), signed);
    }

    private sealed record MetricGroup(MetricGroupKey Key, string Title, IReadOnlyList<Metric> Metrics);

    private sealed record MetricDomain(
        Metric Primary,
        IReadOnlyList<Metric> Supporting,
        IReadOnlyList<MetricGroup> Groups,
        IReadOnlyList<MetricGroup> DailyGroups,
        IReadOnlyList<Metric> VariationMetrics,
        IReadOnlyList<Metric> DailyVariationMetrics,
        bool ShowHistoricalMin,
        string HistoricalMaxWord,
        string HistoricalMinWord,
        Func<string, RecentObservationComparisonResult, string> BuildHeadline,
        Func<string, int?, RecentObservationComparisonResult, string> BuildPercentileSentence,
        Func<RecentObservationComparisonResult?, RecentObservationTileTone> GetTone)
    {
        public IReadOnlyList<Metric> AllMetrics
        {
            get
            {
                var seen = new HashSet<string>();
                var result = new List<Metric>();
                var all = new[] { Primary }
                    .Concat(Supporting)
                    .Concat(Groups.SelectMany(x => x.Metrics))
                    .Concat(DailyGroups.SelectMany(x => x.Metrics))
                    .Concat(VariationMetrics)
                    .Concat(DailyVariationMetrics);
                foreach (var metric in all)
                {
                    if (seen.Add(metric.Key))
                    {
                        result.Add(metric);
                    }
                }

                return result;
            }
        }
    }
}
#pragma warning restore SA1201, SA1204
