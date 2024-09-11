using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace ClimateExplorer.Data.Ghcnd;

public class GhcndInputRow
{
    [Name("STATION")]
    public string? Station { get; set; }

    [Name("DATE")]
    public string? Date { get; set; }

    [Name("PRCP")]
    public string? Prcp { get; set; }

    [Name("TMAX")]
    public string? Tmax { get; set; }

    [Name("TMIN")]
    public string? Tmin { get; set; }
}