namespace ClimateExplorer.Core.Model;

public class HeatingScoreRow
{
    public required float MinimumWarmingAnomaly { get;set; }
    public required float MaximumWarmingAnomaly { get; set; }
    public required int Score { get; set; }
}
