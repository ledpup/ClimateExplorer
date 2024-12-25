using CsvHelper.Configuration.Attributes;

namespace ClimateExplorer.Data.Ghcnd;

public class OutputRowPrecipitation
{
    [Name("Date")]
    public string? Date { get; set; }

    [Name("Precipitation")]
    public string? Precipitation { get; set; }
}