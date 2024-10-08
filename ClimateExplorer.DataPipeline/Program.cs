﻿using System.IO.Compression;

var foldersToProcess =
    new Folder[]
    {
        new()
        {
            InputFolder = @"..\..\..\..\ClimateExplorer.SourceData\GHCNd\Temperature",
            OutputFolder = @"..\..\..\..\ClimateExplorer.WebApi\Datasets\GHCNd\Temperature",
        },
        new()
        {
            InputFolder = @"..\..\..\..\ClimateExplorer.SourceData\GHCNd\Precipitation",
            OutputFolder = @"..\..\..\..\ClimateExplorer.WebApi\Datasets\GHCNd\Precipitation",
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

public class Folder
{
    public string InputFolder { get; set; } = string.Empty;
    public string OutputFolder { get; set; } = string.Empty;
}