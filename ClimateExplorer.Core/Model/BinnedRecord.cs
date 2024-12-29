namespace ClimateExplorer.Core.Model;

using ClimateExplorer.Core.DataPreparation;
using System.Text.Json.Serialization;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Record type")]
public record BinnedRecord(string BinId, double? Value)
{
    private BinIdentifier? cachedParsedBinId;
    private short? cachedYear;

    [JsonIgnore]
    public BinIdentifier? BinIdentifier
    {
        get
        {
            cachedParsedBinId ??= BinIdentifier.Parse(BinId!);

            return cachedParsedBinId;
        }
    }

    [JsonIgnore]
    public short? Year
    {
        get
        {
            if (BinId.StartsWith('y'))
            {
                cachedYear ??= Convert.ToInt16(BinId.Substring(1, 4));
            }

            return cachedYear;
        }
    }
}
