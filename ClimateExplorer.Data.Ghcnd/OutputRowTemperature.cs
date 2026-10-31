using CsvHelper;

namespace ClimateExplorer.Data.Ghcnd;

[CsvRecord]
public partial class OutputRowTemperature
{
    [CsvColumn("Date")]
    public string? Date { get; set; }

    [CsvColumn("TMax")]
    public int? Tmax { get; set; }

    [CsvColumn("TMin")]
    public int? Tmin { get; set; }
}