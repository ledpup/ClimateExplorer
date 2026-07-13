namespace ClimateExplorer.Data.Downloading;

using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;

public interface IDataSetSourceUpdateCoordinator
{
    Task<DataSetSourcePreparationResult> PrepareAsync(
        PostDataSetsRequestBody request,
        DataSet? cachedData,
        CancellationToken cancellationToken);
}
