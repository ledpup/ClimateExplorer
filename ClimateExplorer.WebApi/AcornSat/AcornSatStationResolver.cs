#nullable enable
namespace ClimateExplorer.WebApi.AcornSat;

using System;
using System.Linq;
using ClimateExplorer.Core.Model;

/// <summary>
/// Resolves the single adjusted ACORN-SAT station and the single currently open CDO station for a location,
/// per eligibility rule 2: extension is only possible when the adjusted mapping has exactly one non-blank
/// station and the CDO mapping has exactly one open (null <see cref="DataFileFilterAndAdjustment.EndDate"/>),
/// non-blank station. Either side returning null (a missing location, an all-null placeholder location, or a
/// CDO history without a single currently open station) means the location is not eligible for extension.
/// </summary>
internal static class AcornSatStationResolver
{
    public static AcornSatStationResolution Resolve(DataSetDefinition acornSatDataSet, DataSetDefinition cdoDataSet, Guid locationId)
    {
        return new AcornSatStationResolution(
            ResolveSingleStation(acornSatDataSet, locationId, requireOpen: false),
            ResolveSingleStation(cdoDataSet, locationId, requireOpen: true));
    }

    private static string? ResolveSingleStation(DataSetDefinition dataSet, Guid locationId, bool requireOpen)
    {
        if (dataSet.DataLocationMapping?.LocationIdToDataFileMappings.TryGetValue(locationId, out var filters) != true)
        {
            return null;
        }

        var candidates = filters!
            .Where(x => !string.IsNullOrWhiteSpace(x.Id) && (!requireOpen || x.EndDate is null))
            .ToList();

        return candidates.Count == 1 ? candidates[0].Id : null;
    }
}
