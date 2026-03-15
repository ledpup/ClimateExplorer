namespace ClimateExplorer.Core.DataPreparation;

public class RawBin
{
    public BinIdentifier? Identifier { get; set; }
    public Bucket[]? Buckets { get; set; }

    public override string ToString()
    {
        return Identifier!.ToString();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Not important")]
public class RawBinWithDataAdequacyFlag : RawBin
{
    public bool MeetsDataRequirements { get; set; }
}
