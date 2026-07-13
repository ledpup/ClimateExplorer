namespace ClimateExplorer.Data.Downloading.Orchestration;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Models;

public interface IDataSetSourceUpdateCoordinator
{
    Task<DataSetSourcePreparationResult> PrepareAsync(
        PostDataSetsRequestBody request,
        DataSet? cachedData,
        CancellationToken cancellationToken);
}
