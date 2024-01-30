namespace ClimateExplorer.Core.Model;

public class HeatingScoreRow
{
    public required double MinimumWarmingAnomaly { get;set; }
    public required double MaximumWarmingAnomaly { get; set; }
    public required int Score { get; set; }
}
