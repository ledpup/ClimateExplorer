namespace ClimateExplorer.Data.Downloading.Downloaders;

using System.Text;
using FluentFTP;

/// <summary>
/// Reads BOM daily station tables from the Bureau's anonymous FTP server, the channel BOM provides for automated
/// access (its Climate Data Online web pages block automated requests).
/// </summary>
public sealed class BomFtpDailyTableSource : IBomDailyTableSource
{
    private const string Host = "ftp.bom.gov.au";

    public async Task<string?> DownloadAsync(string remotePath, CancellationToken cancellationToken)
    {
        await using var client = new AsyncFtpClient(Host);
        await client.Connect(cancellationToken);
        if (!await client.FileExists(remotePath, cancellationToken))
        {
            return null;
        }

        var bytes = await client.DownloadBytes(remotePath, cancellationToken);

        // The tables are Latin-1 (e.g. the degree sign in the column headers).
        return Encoding.Latin1.GetString(bytes);
    }
}
