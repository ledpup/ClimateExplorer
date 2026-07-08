namespace ClimateExplorer.WebApi.Model;

public class BinThenCount(short? numberOfBins, short? binSize) : StatsParameters
{
    public short? NumberOfBins { get; set; } = numberOfBins;
    public short? BinSize { get; set; } = binSize;
}
