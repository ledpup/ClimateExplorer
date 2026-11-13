namespace ClimateExplorer.DataPipeline;

using System.Diagnostics;
using System.IO.Compression;

public sealed class DataPackageBuilder(string sourceRoot, string outputRoot, TextWriter log)
{
    private static readonly DateTimeOffset ArchiveTimestamp = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly string sourceRoot = Path.GetFullPath(sourceRoot);
    private readonly string outputRoot = Path.GetFullPath(outputRoot);
    private readonly DataPackageRegistry registry = new();
    private string stagingRoot = string.Empty;

    public DataPackageBuildResult Build()
    {
        var stopwatch = Stopwatch.StartNew();
        log.WriteLine($"Source: {sourceRoot}");
        log.WriteLine($"Output: {outputRoot}");
        ValidateRoots();

        stagingRoot = outputRoot + $".staging-{Guid.NewGuid():N}";
        Directory.CreateDirectory(stagingRoot);
        log.WriteLine($"Staging: {stagingRoot}");

        try
        {
            RunStep("BOM station archives", BuildBomArchives);
            RunStep("GHCNd station archives", BuildGhcndArchives);
            RunStep("ACORN-SAT.zip", BuildAcornSatArchive);
            RunStep("GHCNm.zip", BuildGhcnmArchive);
            RunStep("Loose files", CopyLooseFiles);
            RunStep("Validate layout", ValidateTargetLayout);
            RunStep("Replace output", ReplaceOutput);

            log.WriteLine($"Package: {FormatSize(GetDirectorySize(outputRoot))} in {outputRoot}. Total time {stopwatch.Elapsed:mm\\:ss}.");

            return new DataPackageBuildResult(registry.PhysicalAssets.Count, registry.LogicalFileCount);
        }
        catch (Exception ex)
        {
            log.WriteLine($"FAILED: {ex.Message}");
            log.WriteLine($"Removing {stagingRoot}.");
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, true);
            }

            throw;
        }
    }

    private void RunStep(string name, Func<string> step)
    {
        log.WriteLine($"{name}...");
        var stopwatch = Stopwatch.StartNew();
        var summary = step();
        log.WriteLine($"{name}: {summary} ({stopwatch.Elapsed.TotalSeconds:0.0}s)");
    }

    private string BuildBomArchives()
    {
        return CopyArchiveDirectory("BOM");
    }

    private string BuildGhcndArchives()
    {
        return CopyArchiveDirectory("GHCNd");
    }

    private string CopyArchiveDirectory(string relativeDirectory)
    {
        var sourceDirectory = SourcePath(relativeDirectory);
        var sourceArchivePaths = Directory.GetFiles(sourceDirectory, "*.zip", SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal).ToArray();
        if (sourceArchivePaths.Length == 0)
        {
            throw new InvalidDataException($"No station archives (*.zip) found in '{sourceDirectory}'.");
        }

        var progress = new ProgressLog(log, relativeDirectory, sourceArchivePaths.Length);
        long bytes = 0;
        var entryCount = 0;
        foreach (var sourceArchivePath in sourceArchivePaths)
        {
            var targetRelativePath = NormalizeRelativePath(Path.Combine(relativeDirectory, Path.GetFileName(sourceArchivePath)));
            registry.RegisterPhysicalAsset(targetRelativePath);
            using (var archive = ZipFile.OpenRead(sourceArchivePath))
            {
                if (archive.Entries.Count == 0)
                {
                    throw new InvalidDataException($"Source archive '{sourceArchivePath}' has no entries.");
                }

                registry.RegisterArchiveSource(
                    sourceArchivePath,
                    archive.Entries.Select(entry => $"{targetRelativePath}!/{NormalizeArchiveEntry(entry.FullName)}"));
                entryCount += archive.Entries.Count;
            }

            var targetPath = Path.Combine(stagingRoot, targetRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(sourceArchivePath, targetPath);
            bytes += new FileInfo(targetPath).Length;
            progress.Increment();
        }

        return $"copied {sourceArchivePaths.Length:N0} archives with {entryCount:N0} entries, {FormatSize(bytes)}";
    }

    private string BuildAcornSatArchive()
    {
        var sourceDirectory = SourcePath(@"Temperature\ACORN-SAT");
        var entries = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
            .Select(file => new ArchiveSource(file, Path.GetRelativePath(sourceDirectory, file)));

        return CreateArchive("ACORN-SAT.zip", entries);
    }

    private string BuildGhcnmArchive()
    {
        var temperatureDirectory = SourcePath(@"Temperature\GHCNm");
        var precipitationDirectory = SourcePath(@"Precipitation\GHCNm");
        var temperatureEntries = Directory.GetFiles(temperatureDirectory, "*", SearchOption.AllDirectories)
            .Select(file => new ArchiveSource(file, Path.Combine("Temperature", Path.GetRelativePath(temperatureDirectory, file))));
        var precipitationEntries = Directory.GetFiles(precipitationDirectory, "*", SearchOption.AllDirectories)
            .Select(file => new ArchiveSource(file, Path.Combine("Precipitation", Path.GetRelativePath(precipitationDirectory, file))));

        return CreateArchive("GHCNm.zip", temperatureEntries.Concat(precipitationEntries));
    }

    private string CopyLooseFiles()
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [@"CO2\co2_mm_mlo.txt"] = @"CO2\co2_mm_mlo.txt",
            [@"CO2\co2_daily_mlo.txt"] = @"CO2\co2_daily_mlo.txt",
            [@"Methane\ch4_mm_gl.txt"] = @"Methane\ch4_mm_gl.txt",
            [@"NitrousOxide\n2o_mm_gl.txt"] = @"NitrousOxide\n2o_mm_gl.txt",
            [@"Nino34\nino34.long.anom.data.txt"] = @"Nino34\nino34.long.anom.data.txt",
            [@"IOD\dmi.had.long.data.txt"] = @"IOD\dmi.had.long.data.txt",
            [@"AMO\ersst.v5.amo.dat"] = @"AMO\ersst.v5.amo.dat",
            [@"OceanAcidity\HOT_surface_CO2_reduced.csv"] = @"OceanAcidity\HOT_surface_CO2_reduced.csv",
            [@"SeaLevel\slr_sla_gbl_free_ref_90_reduced.csv"] = @"SeaLevel\slr_sla_gbl_free_ref_90_reduced.csv",
            [@"Greenland\greenland-melt-area.csv"] = @"Greenland\greenland-melt-area.csv",
            [@"Glaciers\wgms-glacier-mass-balance-index.csv"] = @"Glaciers\wgms-glacier-mass-balance-index.csv",
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
            [@"Met\meantemp_daily_totals.txt"] = @"Met\meantemp_daily_totals.txt",
            [@"Met\maxtemp_daily_totals.txt"] = @"Met\maxtemp_daily_totals.txt",
            [@"Met\mintemp_daily_totals.txt"] = @"Met\mintemp_daily_totals.txt",
            [@"Met\HadCEP_daily_totals.txt"] = @"Met\HadCEP_daily_totals.txt",
        };

        var fileCount = 0;
        long bytes = 0;
        foreach (var file in files)
        {
            bytes += CopyLooseFile(file.Key, file.Value);
            fileCount++;
        }

        var (directoryFileCount, directoryBytes) = CopyLooseDirectory("NOAAGlobalTemp", "NOAAGlobalTemp");

        return $"copied {fileCount + directoryFileCount:N0} files, {FormatSize(bytes + directoryBytes)}";
    }

    private (int FileCount, long Bytes) CopyLooseDirectory(string sourceDirectory, string targetDirectory)
    {
        var fullSourceDirectory = SourcePath(sourceDirectory);
        var fileCount = 0;
        long bytes = 0;
        foreach (var file in Directory.GetFiles(fullSourceDirectory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            bytes += CopyLooseFile(
                Path.GetRelativePath(sourceRoot, file),
                Path.Combine(targetDirectory, Path.GetRelativePath(fullSourceDirectory, file)));
            fileCount++;
        }

        return (fileCount, bytes);
    }

    private long CopyLooseFile(string sourceRelativePath, string targetRelativePath)
    {
        var sourceFile = SourcePath(sourceRelativePath);
        if (!File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"Loose source file '{sourceFile}' doesn't exist. Download it, or remove it from {nameof(CopyLooseFiles)}.", sourceFile);
        }

        var normalizedTargetPath = NormalizeRelativePath(targetRelativePath);
        registry.RegisterLogicalFile(sourceFile, normalizedTargetPath);
        registry.RegisterPhysicalAsset(normalizedTargetPath);

        var targetFile = Path.Combine(stagingRoot, normalizedTargetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
        File.Copy(sourceFile, targetFile);
        return new FileInfo(targetFile).Length;
    }

    private string CreateArchive(string targetRelativePath, IEnumerable<ArchiveSource> sourceEntries)
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
        var progress = new ProgressLog(log, normalizedTargetPath, entries.Length);
        long sourceBytes = 0;
        using (var fileStream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
        {
            foreach (var sourceEntry in entries)
            {
                registry.RegisterLogicalFile(sourceEntry.SourcePath, $"{normalizedTargetPath}!/{sourceEntry.EntryPath}");

                var entry = archive.CreateEntry(sourceEntry.EntryPath, CompressionLevel.Optimal);
                entry.LastWriteTime = ArchiveTimestamp;
                using var input = File.OpenRead(sourceEntry.SourcePath);
                using var output = entry.Open();
                input.CopyTo(output);
                sourceBytes += input.Length;
                progress.Increment();
            }
        }

        return $"{entries.Length:N0} entries, {FormatSize(sourceBytes)} compressed to {FormatSize(new FileInfo(archivePath).Length)}";
    }

    private string ValidateTargetLayout()
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
        var omittedFiles = sourceFiles.Except(registry.SourcePaths, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        if (omittedFiles.Length > 0)
        {
            throw new InvalidDataException(DescribeOmittedFiles(sourceRoot, omittedFiles));
        }

        return $"all {sourceFiles.Count:N0} source files packaged into {registry.PhysicalAssets.Count:N0} assets ({registry.LogicalFileCount:N0} logical files)";
    }

    /// <summary>
    /// Every file in SourceData must end up in the package, so a dataset can't be silently left out. Lists the
    /// unpackaged files by folder, so the message says which dataset is missing rather than naming one file.
    /// </summary>
    public static string DescribeOmittedFiles(string sourceRoot, IReadOnlyList<string> omittedFiles)
    {
        const int examplesPerFolder = 3;
        var byFolder = omittedFiles
            .GroupBy(file => Path.GetDirectoryName(Path.GetRelativePath(sourceRoot, file)) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var examples = string.Join(", ", group.Take(examplesPerFolder).Select(Path.GetFileName));
                var more = group.Count() > examplesPerFolder ? ", ..." : string.Empty;
                return $"  {group.Key}: {group.Count():N0} files ({examples}{more})";
            });

        return $"{omittedFiles.Count:N0} source files in {sourceRoot} aren't packaged:{Environment.NewLine}"
            + string.Join(Environment.NewLine, byFolder) + Environment.NewLine
            + $"Package them in {nameof(DataPackageBuilder)} (station archives, an annual archive, or {nameof(CopyLooseFiles)}), or delete them from SourceData if nothing uses them.";
    }

    private bool IsBuildArtifact(string file)
    {
        var relativePath = Path.GetRelativePath(sourceRoot, file);
        return relativePath.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || relativePath.EndsWith(".lscache", StringComparison.OrdinalIgnoreCase);
    }

    private string ReplaceOutput()
    {
        var backupRoot = outputRoot + $".backup-{Guid.NewGuid():N}";
        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(backupRoot);

        try
        {
            log.WriteLine($"  Moving the current contents to {backupRoot}");
            MoveDirectoryContents(outputRoot, backupRoot);
            log.WriteLine($"  Moving the new package from {stagingRoot}");
            MoveDirectoryContents(stagingRoot, outputRoot);
            Directory.Delete(stagingRoot);
            log.WriteLine($"  Deleting {backupRoot}");
            Directory.Delete(backupRoot, true);

            return $"replaced the contents of {outputRoot}";
        }
        catch
        {
            log.WriteLine($"Replacing the output failed. Restoring the previous contents from {backupRoot}.");
            if (Directory.Exists(outputRoot) && Directory.Exists(backupRoot))
            {
                MoveDirectoryContents(outputRoot, stagingRoot);
                MoveDirectoryContents(backupRoot, outputRoot);
                Directory.Delete(backupRoot);
                log.WriteLine($"Restored {outputRoot} to its previous contents.");
            }

            throw;
        }
    }

    private void MoveDirectoryContents(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory))
        {
            var destination = Path.Combine(destinationDirectory, Path.GetFileName(file));
            MoveWithRetry(file, destination, () => File.Move(file, destination));
        }

        foreach (var directory in Directory.GetDirectories(sourceDirectory))
        {
            var destination = Path.Combine(destinationDirectory, Path.GetFileName(directory));
            MoveWithRetry(directory, destination, () => Directory.Move(directory, destination));
        }
    }

    /// <summary>
    /// Windows refuses to move a folder while any file in it is open. Just after thousands of archives are
    /// written, antivirus or the search indexer is usually still reading some of them, so wait and retry.
    /// </summary>
    private void MoveWithRetry(string source, string destination, Action move)
    {
        TimeSpan[] delays = [.. Enumerable.Range(1, 12).Select(attempt => TimeSpan.FromSeconds(Math.Min(attempt, 5)))];

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                move();
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && attempt < delays.Length)
            {
                log.WriteLine(
                    $"  Couldn't move {source} to {destination} ({ex.Message.TrimEnd()}). Something has a file in it open, " +
                    $"probably antivirus or the search indexer scanning new files. Retry {attempt + 1}/{delays.Length} in {delays[attempt].TotalSeconds:0}s.");
                Thread.Sleep(delays[attempt]);
            }
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

    private static long GetDirectorySize(string directory)
    {
        return Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);
    }

    private static string FormatSize(long bytes)
    {
        return bytes >= 1024 * 1024
            ? $"{bytes / (1024.0 * 1024):N1} MB"
            : $"{bytes / 1024.0:N1} KB";
    }

    private sealed record ArchiveSource(string SourcePath, string EntryPath);

    /// <summary>Logs "name: done/total" about every 10%, so a long step shows it's still going.</summary>
    private sealed class ProgressLog(TextWriter log, string name, int total)
    {
        private readonly int interval = Math.Max(1, total / 10);
        private int done;

        public void Increment()
        {
            done++;
            if (done < total && done % interval == 0)
            {
                log.WriteLine($"  {name}: {done:N0}/{total:N0}");
            }
        }
    }
}

public sealed record DataPackageBuildResult(int PhysicalAssetCount, int LogicalFileCount);
