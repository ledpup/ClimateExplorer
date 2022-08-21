// See https://aka.ms/new-console-template for more information

using System.IO.Compression;



var foldersToProcess =
    new string[]
    {
        @"..\..\..\..\ClimateExplorer.WebApi\Temperature"
    };

foreach (var folder in foldersToProcess)
{
    var filesToProcess =
        Directory.GetFiles(
            folder,
            "*.csv",
            new EnumerationOptions()
            {
                RecurseSubdirectories = true
            }
        );

    foreach (var file in filesToProcess)
    {
        Console.WriteLine(file);

        CompressFile(file);
    }
}

void CompressFile(string file)
{
    var zipFile = Path.ChangeExtension(file, "zip");

    Console.WriteLine("Creating " + zipFile);

    using (FileStream zipFileStream = new FileStream(zipFile, FileMode.Create))
    {
        using (ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
        {
            ZipArchiveEntry siteFileEntry = archive.CreateEntry(Path.GetFileName(file));

            // This could be optimised (smaller buffer, fewer allocations)
            using (Stream s = siteFileEntry.Open())
            {
                var b = File.ReadAllBytes(file);

                s.Write(b, 0, b.Length);
            }
        }
    }
}