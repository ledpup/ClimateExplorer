namespace ClimateExplorer.Data.Downloading.Transformers;

public interface IDataSetSourceFileTransformer
{
    Task TransformAsync(string rawFilePath, string outputFilePath, CancellationToken cancellationToken);
}
