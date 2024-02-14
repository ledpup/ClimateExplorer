namespace ClimateExplorer.Core.DataPreparation;

public class Bucket
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public Cup[]? Cups { get; set; }
    public DateOnly FirstDayInBucket { get; set; }
    public DateOnly LastDayInBucket { get; set; }

    public override string ToString()
    {
        return FirstDayInBucket.ToString("yyyy-MM-dd") + " -> " + LastDayInBucket.ToString("yyyy-MM-dd");
    }
}
