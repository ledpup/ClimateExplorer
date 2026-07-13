namespace ClimateExplorer.WebApi.DataRetrieval;

using System;
using System.IO;

internal sealed class DataSetDownloadWorkspaceFactory
{
    public DataSetDownloadWorkspace Create()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ClimateExplorer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return new DataSetDownloadWorkspace(path);
    }
}
