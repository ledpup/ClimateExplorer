using CsvHelper.Configuration.Attributes;

namespace ClimateExplorer.Data.Ghcnd;

public class OutputRowTemperature
{
    [Name("Date")]
    public string? Date { get; set; }

    [Name("TMax")]
    public string? Tmax { get; set; }

    [Name("TMin")]
    public string? Tmin { get; set; }
}