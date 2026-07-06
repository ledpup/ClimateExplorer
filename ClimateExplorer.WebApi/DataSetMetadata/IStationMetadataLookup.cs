#nullable enable

namespace ClimateExplorer.WebApi.Metadata;

using System.Threading.Tasks;
using ClimateExplorer.Core.Model;

internal interface IStationMetadataLookup
{
    Task<Station?> GetStationAsync(DataSetDefinition dataSetDefinition, string stationId);
}
