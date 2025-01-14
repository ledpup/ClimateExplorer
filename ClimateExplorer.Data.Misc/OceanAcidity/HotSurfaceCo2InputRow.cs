namespace ClimateExplorer.Data.Misc.OceanAcidity;

using CsvHelper.Configuration.Attributes;

public class HotSurfaceCo2InputRow
{
    [Name("cruise")]
    public string? Cruise { get; set; }

    [Name("days")]
    public string? Days { get; set; }

    [Name("date")]
    public string? Date { get; set; }

    [Name("temp")]
    public string? Temp { get; set; }

    [Name("sal")]
    public string? Salinity { get; set; }

    [Name("phos")]
    public string? Phosphate { get; set; }

    [Name("sil")]
    public string? Sil { get; set; }

    [Name("DIC")]
    public string? Dic { get; set; }

    [Name("TA")]
    public string? Ta { get; set; }

    [Name("nDIC")]
    public string? Ndic { get; set; }

    [Name("nTA")]
    public string? Nta { get; set; }

    [Name("pHmeas_25C")]
    public string? Phmeas25c { get; set; }

    [Name("pHmeas_insitu")]
    public string? PhmeasInsitu { get; set; }

    [Name("pHcalc_25C")]
    public string? Phcalc25c { get; set; }
}