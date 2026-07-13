using ClimateExplorer.DataPipeline;

var solutionRoot = GetSolutionRoot();
var sourceRoot = Path.Combine(solutionRoot, "ClimateExplorer.SourceData");
var outputRoot = Path.Combine(solutionRoot, "ClimateExplorer.WebApi", "Datasets");

var result = new DataPackageBuilder(sourceRoot, outputRoot).Build();

Console.WriteLine($"Created {result.PhysicalAssetCount} physical assets containing {result.LogicalFileCount} logical files.");

static string GetSolutionRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "ClimateExplorer.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not find ClimateExplorer.sln from the application directory.");
}
