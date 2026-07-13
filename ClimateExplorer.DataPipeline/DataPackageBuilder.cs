namespace ClimateExplorer.DataPipeline;

using System.IO.Compression;

public sealed class DataPackageBuilder(string sourceRoot, string outputRoot)
{
    private static readonly DateTimeOffset ArchiveTimestamp = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly string sourceRoot = Path.GetFullPath(sourceRoot);
    private readonly string outputRoot = Path.GetFullPath(outputRoot);
    private readonly DataPackageRegistry registry = new();
    private string stagingRoot = string.Empty;

    public DataPackageBuildResult Build()
    {
        ValidateRoots();

        stagingRoot = outputRoot + $".staging-{Guid.NewGuid():N}";
        Directory.CreateDirectory(stagingRoot);

        try
        {
            BuildBomArchives();
            BuildGhcndArchives();
            BuildAcornSatArchive();
            BuildGhcnmArchive();
            CopyLooseFiles();
            ValidateTargetLayout();
            ReplaceOutput();

            return new DataPackageBuildResult(registry.PhysicalAssets.Count, registry.LogicalFileCount);
        }
        catch
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, true);
            }

            throw;
        }
    }

    private void BuildBomArchives()
    {
        var sourceFolders = new[]
        {
            @"Temperature_BOM\daily_tempmean",
            @"Temperature_BOM\daily_tempmax",
            @"Temperature_BOM\daily_tempmin",
            @"Precipitation\BOM",
            @"Solar\BOM",
        };

        var filesByFolder = sourceFolders.ToDictionary(
            folder => folder,
            folder => GetFilesByStation(folder, file => Path.GetFileName(file).Split('_')[0]),
            StringComparer.OrdinalIgnoreCase);

        var stations = filesByFolder[sourceFolders[0]].Keys.Order(StringComparer.Ordinal).ToArray();
        foreach (var folder in sourceFolders.Skip(1))
        {
            if (!stations.SequenceEqual(filesByFolder[folder].Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            {
                throw new InvalidDataException($"BOM station inventory in '{folder}' does not match the other BOM measurements.");
            }
        }

        foreach (var station in stations)
        {
            var entries = sourceFolders.Select(folder => new ArchiveSource(
                filesByFolder[folder][station],
                Path.GetFileName(filesByFolder[folder][station])));

            CreateArchive($@"BOM\{station}.zip", entries);
        }
    }

    private void BuildGhcndArchives()
    {
        var temperatureFiles = GetFilesByStation(@"GHCNd\Temperature", Path.GetFileNameWithoutExtension);
        var precipitationFiles = GetFilesByStation(@"GHCNd\Precipitation", Path.GetFileNameWithoutExtension);
        var stations = temperatureFiles.Keys
            .Concat(precipitationFiles.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal);

        foreach (var station in stations)
        {
            var entries = new List<ArchiveSource>();
            if (temperatureFiles.TryGetValue(station, out var temperatureFile))
            {
                entries.Add(new ArchiveSource(temperatureFile, $"Temperature/{Path.GetFileName(temperatureFile)}"));
            }

            if (precipitationFiles.TryGetValue(station, out var precipitationFile))
            {
                entries.Add(new ArchiveSource(precipitationFile, $"Precipitation/{Path.GetFileName(precipitationFile)}"));
            }

            CreateArchive($@"GHCNd\{station}.zip", entries);
        }
    }

    private void BuildAcornSatArchive()
    {
        var sourceDirectory = SourcePath(@"Temperature\ACORN-SAT");
        var entries = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
            .Select(file => new ArchiveSource(file, Path.GetRelativePath(sourceDirectory, file)));

        CreateArchive("ACORN-SAT.zip", entries);
    }

    private void BuildGhcnmArchive()
    {
        var temperatureDirectory = SourcePath(@"Temperature\GHCNm");
        var precipitationDirectory = SourcePath(@"Precipitation\GHCNm");
        var temperatureEntries = Directory.GetFiles(temperatureDirectory, "*", SearchOption.AllDirectories)
            .Select(file => new ArchiveSource(file, Path.Combine("Temperature", Path.GetRelativePath(temperatureDirectory, file))));
        var precipitationEntries = Directory.GetFiles(precipitationDirectory, "*", SearchOption.AllDirectories)
            .Select(file => new ArchiveSource(file, Path.Combine("Precipitation", Path.GetRelativePath(precipitationDirectory, file))));

        CreateArchive("GHCNm.zip", temperatureEntries.Concat(precipitationEntries));
    }

    private void CopyLooseFiles()
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [@"CO2\co2_mm_mlo.txt"] = @"CO2\co2_mm_mlo.txt",
            [@"Methane\ch4_mm_gl.txt"] = @"Methane\ch4_mm_gl.txt",
            [@"NitrousOxide\n2o_mm_gl.txt"] = @"NitrousOxide\n2o_mm_gl.txt",
            [@"Nino34\nino34.long.anom.data.txt"] = @"Nino34\nino34.long.anom.data.txt",
            [@"IOD\dmi.had.long.data.txt"] = @"IOD\dmi.had.long.data.txt",
            [@"AMO\ersst.v5.amo.dat"] = @"AMO\ersst.v5.amo.dat",
            [@"OceanAcidity\HOT_surface_CO2_reduced.csv"] = @"OceanAcidity\HOT_surface_CO2_reduced.csv",
            [@"SeaLevel\slr_sla_gbl_free_ref_90_reduced.csv"] = @"SeaLevel\slr_sla_gbl_free_ref_90_reduced.csv",
            [@"Greenland\greenland-melt-area.csv"] = @"Greenland\greenland-melt-area.csv",
            [@"ArcticSeaIce\N_seaice_extent_daily_v4.0.csv"] = @"ArcticSeaIce\N_seaice_extent_daily_v4.0.csv",
            [@"AntarcticSeaIce\S_seaice_extent_daily_v4.0.csv"] = @"AntarcticSeaIce\S_seaice_extent_daily_v4.0.csv",
            [@"OzoneHoleArea\cams_ozone_monitoring_sh_ozone_area_reduced.csv"] = @"OzoneHoleArea\cams_ozone_monitoring_sh_ozone_area_reduced.csv",
            [@"OzoneHoleColumn\cams_ozone_monitoring_sh_ozone_minimum_reduced.csv"] = @"OzoneHoleColumn\cams_ozone_monitoring_sh_ozone_minimum_reduced.csv",
            [@"AtmosphericTransmission\mauna_loa_transmission.dat"] = @"AtmosphericTransmission\mauna_loa_transmission.dat",
            [@"CO2Emissions\GCB2025v15_MtCO2_flat.csv"] = @"CO2Emissions\GCB2025v15_MtCO2_flat.csv",
            [@"Sunspots\SN_d_tot_V2.0.txt"] = @"Sunspots\SN_d_tot_V2.0.txt",
            [@"TSI\tsi-ssi_v02r01_observed-tsi-composite.txt"] = @"TSI\tsi-ssi_v02r01_observed-tsi-composite.txt",
            [@"ODGI\odgi_table1.csv"] = @"ODGI\odgi_table1.csv",
            [@"ODGI\odgi_table2.csv"] = @"ODGI\odgi_table2.csv",
            [@"Met\meantemp_monthly_totals.txt"] = @"Met\meantemp_monthly_totals.txt",
            [@"Met\maxtemp_daily_totals.txt"] = @"Met\maxtemp_daily_totals.txt",
            [@"Met\mintemp_daily_totals.txt"] = @"Met\mintemp_daily_totals.txt",
            [@"Met\HadCEP_daily_totals.txt"] = @"Met\HadCEP_daily_totals.txt",
        };

        foreach (var file in files)
        {
            CopyLooseFile(file.Key, file.Value);
        }

        CopyLooseDirectory("NOAAGlobalTemp", "NOAAGlobalTemp");
    }

    private void CopyLooseDirectory(string sourceDirectory, string targetDirectory)
    {
        var fullSourceDirectory = SourcePath(sourceDirectory);
        foreach (var file in Directory.GetFiles(fullSourceDirectory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            CopyLooseFile(
                Path.GetRelativePath(sourceRoot, file),
                Path.Combine(targetDirectory, Path.GetRelativePath(fullSourceDirectory, file)));
        }
    }

    private void CopyLooseFile(string sourceRelativePath, string targetRelativePath)
    {
        var sourceFile = SourcePath(sourceRelativePath);
        var normalizedTargetPath = NormalizeRelativePath(targetRelativePath);
        registry.RegisterLogicalFile(sourceFile, normalizedTargetPath);
        registry.RegisterPhysicalAsset(normalizedTargetPath);

        var targetFile = Path.Combine(stagingRoot, normalizedTargetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
        File.Copy(sourceFile, targetFile);
    }

    private void CreateArchive(string targetRelativePath, IEnumerable<ArchiveSource> sourceEntries)
    {
        var normalizedTargetPath = NormalizeRelativePath(targetRelativePath);
        registry.RegisterPhysicalAsset(normalizedTargetPath);

        var entries = sourceEntries
            .Select(entry => entry with { EntryPath = NormalizeArchiveEntry(entry.EntryPath) })
            .OrderBy(entry => entry.EntryPath, StringComparer.Ordinal)
            .ToArray();
        if (entries.Length == 0)
        {
            throw new InvalidDataException($"Archive '{normalizedTargetPath}' has no entries.");
        }

        var archivePath = Path.Combine(stagingRoot, normalizedTargetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var fileStream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        foreach (var sourceEntry in entries)
        {
            registry.RegisterLogicalFile(sourceEntry.SourcePath, $"{normalizedTargetPath}!/{sourceEntry.EntryPath}");

            var entry = archive.CreateEntry(sourceEntry.EntryPath, CompressionLevel.Optimal);
            entry.LastWriteTime = ArchiveTimestamp;
            using var input = File.OpenRead(sourceEntry.SourcePath);
            using var output = entry.Open();
            input.CopyTo(output);
        }
    }

    private Dictionary<string, string> GetFilesByStation(string sourceDirectory, Func<string, string> stationSelector)
    {
        return Directory.GetFiles(SourcePath(sourceDirectory), "*.csv", SearchOption.TopDirectoryOnly)
            .ToDictionary(stationSelector, StringComparer.OrdinalIgnoreCase);
    }

    private void ValidateTargetLayout()
    {
        string[] obsoleteAssets =
        [
            "Atmosphere.zip",
            "Ice.zip",
            "Ocean.zip",
            "Precipitation.zip",
            "Solar.zip",
            "Temperature.zip",
            "Temperature_BOM.zip",
        ];

        foreach (var obsoleteAsset in obsoleteAssets)
        {
            if (registry.PhysicalAssets.Contains(obsoleteAsset) || File.Exists(Path.Combine(stagingRoot, obsoleteAsset)))
            {
                throw new InvalidDataException($"Obsolete data-type archive '{obsoleteAsset}' was emitted.");
            }
        }

        var sourceFiles = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(file => !IsBuildArtifact(file) && !file.EndsWith("ClimateExplorer.SourceData.csproj", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var omittedFiles = sourceFiles.Except(registry.SourcePaths, StringComparer.OrdinalIgnoreCase).ToArray();
        if (omittedFiles.Length > 0)
        {
            throw new InvalidDataException($"{omittedFiles.Length} source files were not packaged. First omitted file: '{omittedFiles[0]}'.");
        }
    }

    private bool IsBuildArtifact(string file)
    {
        var relativePath = Path.GetRelativePath(sourceRoot, file);
        return relativePath.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || relativePath.EndsWith(".lscache", StringComparison.OrdinalIgnoreCase);
    }

    private void ReplaceOutput()
    {
        var backupRoot = outputRoot + $".backup-{Guid.NewGuid():N}";
        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(backupRoot);

        try
        {
            MoveDirectoryContents(outputRoot, backupRoot);
            MoveDirectoryContents(stagingRoot, outputRoot);
            Directory.Delete(stagingRoot);
            Directory.Delete(backupRoot, true);
        }
        catch
        {
            if (Directory.Exists(outputRoot) && Directory.Exists(backupRoot))
            {
                MoveDirectoryContents(outputRoot, stagingRoot);
                MoveDirectoryContents(backupRoot, outputRoot);
                Directory.Delete(backupRoot);
            }

            throw;
        }
    }

    private static void MoveDirectoryContents(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory))
        {
            File.Move(file, Path.Combine(destinationDirectory, Path.GetFileName(file)));
        }

        foreach (var directory in Directory.GetDirectories(sourceDirectory))
        {
            Directory.Move(directory, Path.Combine(destinationDirectory, Path.GetFileName(directory)));
        }
    }

    private void ValidateRoots()
    {
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Source data folder not found: {sourceRoot}");
        }

        if (string.Equals(sourceRoot, outputRoot, StringComparison.OrdinalIgnoreCase)
            || outputRoot.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The package output folder must not be the source data folder or one of its descendants.");
        }
    }

    private string SourcePath(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(sourceRoot, relativePath));
        if (!path.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Source path '{relativePath}' resolves outside the source data folder.");
        }

        return path;
    }

    private static string NormalizeRelativePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            throw new InvalidOperationException($"Package path '{path}' must be relative.");
        }

        var segments = path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or ".."))
        {
            throw new InvalidOperationException($"Package path '{path}' contains an invalid segment.");
        }

        return Path.Combine(segments);
    }

    private static string NormalizeArchiveEntry(string path)
    {
        return NormalizeRelativePath(path).Replace(Path.DirectorySeparatorChar, '/');
    }

    private sealed record ArchiveSource(string SourcePath, string EntryPath);
}

public sealed record DataPackageBuildResult(int PhysicalAssetCount, int LogicalFileCount);
