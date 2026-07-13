namespace ClimateExplorer.Data.Downloading.Orchestration;

public interface IDataSetBatchRefresher
{
    Task RefreshAllAsync(CancellationToken cancellationToken = default);
}
