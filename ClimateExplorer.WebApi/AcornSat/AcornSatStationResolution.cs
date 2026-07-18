#nullable enable
namespace ClimateExplorer.WebApi.AcornSat;

internal sealed record AcornSatStationResolution(string? AdjustedStationId, string? OpenCdoStationId)
{
    public bool IsResolved => !string.IsNullOrWhiteSpace(AdjustedStationId) && !string.IsNullOrWhiteSpace(OpenCdoStationId);
}
