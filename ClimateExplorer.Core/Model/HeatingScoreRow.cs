namespace ClimateExplorer.Core.Model;

public class HeatingScoreRow
{
    required public double MinimumWarmingAnomaly { get; set; }
    required public double MaximumWarmingAnomaly { get; set; }
    required public int Score { get; set; }
}
