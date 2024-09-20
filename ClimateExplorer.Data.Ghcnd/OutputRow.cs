using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace ClimateExplorer.Data.Ghcnd;

public class OutputRow
{
    [Name("Date")]
    public string? Date { get; set; }

    [Name("Precipitation")]
    public string? Precipitation { get; set; }

    [Name("TMax")]
    public string? Tmax { get; set; }

    [Name("TMin")]
    public string? Tmin { get; set; }

}