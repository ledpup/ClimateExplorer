using CsvHelper;

namespace ClimateExplorer.Data.Ghcnd;

[CsvRecord]
public partial class OutputRowPrecipitation
{
    [CsvColumn("Date")]
    public string? Date { get; set; }

    [CsvColumn("Precipitation")]
    public int? Precipitation { get; set; }
}