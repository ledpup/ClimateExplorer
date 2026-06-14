using System.IO.Compression;

var solutionRoot = GetSolutionRoot();

var archivesToCreate =
    new FolderArchive[]
    {
        new("Atmosphere"),
        new("Ice"),
        new("Ocean"),
        new("Precipitation"),
        new("Solar"),
        new("Temperature"),
        new("Temperature_BOM"),
    };

foreach (var archive in archivesToCreate)
{
    var sourceDirectory = Path.Combine(solutionRoot, "ClimateExplorer.SourceData", archive.Name);
    var destinationFile = Path.Combine(solutionRoot, "ClimateExplorer.WebApi", "Datasets", archive.Name + ".zip");

    CreateZipFromDirectory(sourceDirectory, destinationFile);
}

var foldersToProcess =
    new Folder[]
    {
        new()
        {
            InputFolder = Path.Combine(solutionRoot, "ClimateExplorer.SourceData", "GHCNd", "Temperature"),
            OutputFolder = Path.Combine(solutionRoot, "ClimateExplorer.WebApi", "Datasets", "GHCNd", "Temperature"),
        },
        new()
        {
            InputFolder = Path.Combine(solutionRoot, "ClimateExplorer.SourceData", "GHCNd", "Precipitation"),
            OutputFolder = Path.Combine(solutionRoot, "ClimateExplorer.WebApi", "Datasets", "GHCNd", "Precipitation"),
        }
    };

foreach (var folder in foldersToProcess)
{
    if (Directory.Exists(folder.OutputFolder))
    {
        Directory.Delete(folder.OutputFolder, true);
    }
    Directory.CreateDirectory(folder.OutputFolder);

    var filesToProcess =
        Directory.GetFiles(
            folder.InputFolder,
            "*.csv",
            new EnumerationOptions()
            {
                RecurseSubdirectories = true
            }
        );

    Parallel.ForEach(filesToProcess, file =>
    {
        Console.WriteLine(file);

        CompressFile(file, folder.OutputFolder);
    });
}

static void CreateZipFromDirectory(string sourceDirectory, string destinationFile)
{
    if (!Directory.Exists(sourceDirectory))
    {
        throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");
    }

    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

    if (File.Exists(destinationFile))
    {
        File.Delete(destinationFile);
    }

    Console.WriteLine("Creating " + destinationFile);
    ZipFile.CreateFromDirectory(sourceDirectory, destinationFile);
}

static void CompressFile(string file, string outFolder)
{
    var zipFile = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(file), "zip"));

    Console.WriteLine("Creating " + zipFile);

    using FileStream zipFileStream = new (zipFile, FileMode.Create);
    using ZipArchive archive = new (zipFileStream, ZipArchiveMode.Create);
    ZipArchiveEntry siteFileEntry = archive.CreateEntry(Path.GetFileName(file));

    // This could be optimised (smaller buffer, fewer allocations)
    using Stream s = siteFileEntry.Open();
    var b = File.ReadAllBytes(file);

    s.Write(b, 0, b.Length);
}

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

public record FolderArchive(string Name);

public class Folder
{
    public string InputFolder { get; set; } = string.Empty;
    public string OutputFolder { get; set; } = string.Empty;
}