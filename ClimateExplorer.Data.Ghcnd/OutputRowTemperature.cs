using CsvHelper.Configuration.Attributes;

namespace ClimateExplorer.Data.Ghcnd;

public class OutputRowTemperature
{
    [Name("Date")]
    public string? Date { get; set; }

    [Name("TMax")]
    public int? Tmax { get; set; }

    [Name("TMin")]
    public int? Tmin { get; set; }
}