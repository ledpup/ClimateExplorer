using CsvHelper;

namespace ClimateExplorer.Data.Ghcnd;

[CsvRecord]
public partial class GhcndInputRow
{
    [CsvColumn("STATION", Optional = true)]
    public string? Station { get; set; }

    [CsvColumn("DATE", Optional = true)]
    public string? Date { get; set; }

    [CsvColumn("PRCP", Optional = true)]
    public string? Prcp { get; set; }

    [CsvColumn("PRCP_ATTRIBUTES", Optional = true)]
    public string? PrcpAttributes { get; set; }

    public bool PrcpQflag => HasQflag(PrcpAttributes);

    [CsvColumn("TMAX", Optional = true)]
    public string? Tmax { get; set; }

    [CsvColumn("TMAX_ATTRIBUTES", Optional = true)]
    public string? TmaxAttributes { get; set; }

    public bool TmaxQflag => HasQflag(TmaxAttributes);

    [CsvColumn("TMIN", Optional = true)]
    public string? Tmin { get; set; }

    [CsvColumn("TMIN_ATTRIBUTES", Optional = true)]
    public string? TminAttributes { get; set; }
    public bool TminQflag => HasQflag(TminAttributes);

    private static bool HasQflag(string? attributes)
    {
        if (attributes is null)
            return false;
        var parts = attributes.Split(',');
        if (parts.Length < 2)
            return false;
        return !string.IsNullOrWhiteSpace(parts[1]);
    }
}