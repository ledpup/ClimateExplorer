namespace ClimateExplorer.WebApi.Model;

public class BinThenCount : StatsParameters
{
    public BinThenCount(short? numberOfBins, short? binSize)
    {
        NumberOfBins = numberOfBins;
        BinSize = binSize;
    }

    public short? NumberOfBins { get; set; }
    public short? BinSize { get; set; }
}
