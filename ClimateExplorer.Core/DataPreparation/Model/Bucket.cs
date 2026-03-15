namespace ClimateExplorer.Core.DataPreparation;

public class Bucket
{
    public Cup[]? Cups { get; set; }
    public DateOnly FirstDayInBucket { get; set; }
    public DateOnly LastDayInBucket { get; set; }

    public override string ToString()
    {
        return FirstDayInBucket.ToString("yyyy-MM-dd") + " -> " + LastDayInBucket.ToString("yyyy-MM-dd");
    }
}
