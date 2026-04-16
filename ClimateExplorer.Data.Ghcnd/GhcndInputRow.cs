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

    [Name("PRCP_ATTRIBUTES")]
    public string? PrcpAttributes { get; set; }

    public bool PrcpQflag => HasQflag(PrcpAttributes);

    [Name("TMAX")]
    public string? Tmax { get; set; }

    [Name("TMAX_ATTRIBUTES")]
    public string? TmaxAttributes { get; set; }

    public bool TmaxQflag => HasQflag(TmaxAttributes);

    [Name("TMIN")]
    public string? Tmin { get; set; }

    [Name("TMIN_ATTRIBUTES")]
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