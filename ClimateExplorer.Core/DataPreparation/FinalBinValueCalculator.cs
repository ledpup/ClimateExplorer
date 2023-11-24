using ClimateExplorer.Core.DataPreparation.Model;

namespace ClimateExplorer.Core.DataPreparation
{
    public static class FinalBinValueCalculator
    {
        public static Bin[] CalculateFinalBinValues(Bin[] rawBins, bool anomaly)
        {
            if (!anomaly)
            {
                // Clone to avoid side-effects
                return rawBins.Select(bin => new Bin { Identifier = bin.Identifier, Value = bin.Value }).ToArray();
            }

            var averageForAnomalyCalculation = rawBins.Average(bin => bin.Value);

            return rawBins.Select(bin => new Bin { Identifier = bin.Identifier, Value = bin.Value - averageForAnomalyCalculation }).ToArray();
        }
    }
}
