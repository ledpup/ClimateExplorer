namespace ClimateExplorer.WebApi.DataRetrieval;

using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;

internal interface IDataSetSourceUpdateCoordinator
{
    Task<DataSetSourcePreparationResult> PrepareAsync(
        PostDataSetsRequestBody request,
        DataSet cachedData,
        CancellationToken cancellationToken);
}
