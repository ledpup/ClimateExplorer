namespace ClimateExplorer.Data.Downloading;

public interface IDataSetBatchRefresher
{
    Task RefreshAllAsync(CancellationToken cancellationToken = default);
}
