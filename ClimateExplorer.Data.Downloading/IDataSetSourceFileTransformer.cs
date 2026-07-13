namespace ClimateExplorer.Data.Downloading;

public interface IDataSetSourceFileTransformer
{
    Task TransformAsync(string rawFilePath, string outputFilePath, CancellationToken cancellationToken);
}
