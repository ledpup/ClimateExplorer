namespace ClimateExplorer.Data.Downloading;

using ClimateExplorer.Core.Model;

public interface IDataSetSourceStateStore
{
    Task<DataSetSourceState?> GetAsync(string assetKey, CancellationToken cancellationToken);

    Task PutAsync(DataSetSourceState state, CancellationToken cancellationToken);
}
