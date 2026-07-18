namespace ClimateExplorer.Data.Downloading.Orchestration;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Interface;
using ClimateExplorer.Data.Downloading.Models;

public interface IDataSetSourceUpdateCoordinator
{
    Task<DataSetSourcePreparationResult> PrepareAsync(
        PostDataSetsRequestBody request,
        ICachedData? cachedData,
        bool permitSourceUpdate,
        CancellationToken cancellationToken);
}
