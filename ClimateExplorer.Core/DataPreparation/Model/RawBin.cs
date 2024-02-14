namespace ClimateExplorer.Core.DataPreparation;

public class RawBin
{
    public BinIdentifier? Identifier { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Rule conflict")]
    public Bucket[]? Buckets { get; set; }

    public override string ToString()
    {
        return Identifier!.ToString();
    }
}

public class RawBinWithDataAdequacyFlag : RawBin
{
    public bool MeetsDataRequirements { get; set; }
}
