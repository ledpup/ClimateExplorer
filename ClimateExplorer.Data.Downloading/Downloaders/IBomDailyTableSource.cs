namespace ClimateExplorer.Data.Downloading.Downloaders;

public interface IBomDailyTableSource
{
    /// <summary>
    /// Returns the content of one BOM daily station table, or null when the file doesn't exist (e.g. a month that
    /// hasn't started yet, or a station BOM doesn't publish a table for).
    /// </summary>
    Task<string?> DownloadAsync(string remotePath, CancellationToken cancellationToken);
}
