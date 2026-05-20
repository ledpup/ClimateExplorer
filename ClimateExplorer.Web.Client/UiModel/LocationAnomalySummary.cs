namespace ClimateExplorer.Web.Client.UiModel;

using ClimateExplorer.Core.Calculators;
using ClimateExplorer.Web.UiModel;

public sealed record LocationAnomalySummary
{
    public CalculatedAnomaly? CalculatedAnomaly { get; set; }

    public Core.Model.DataSet? DataSet { get; set; }

    public List<YearlyValues>? AnomalyRecords { get; set; }
}
