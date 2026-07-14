namespace ClimateExplorer.UnitTests;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Downloading.Orchestration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataSetDownloadMetadataTests
{
    [TestMethod]
    public async Task ResolveAllAsync_MigratedMetadata_ResolvesOneAssetPerPhysicalSource()
    {
        var resolver = CreateResolver();

        var assets = await resolver.ResolveAllAsync(CancellationToken.None);

        // 92 closed BOM stations (205 total mapped BOM stations - 113 currently open, one per BOM location)
        // are deliberately excluded from automatic retrieval; see IsAutomaticallyRetrievable.
        Assert.HasCount(2000, assets);
        CollectionAssert.AreEquivalent(
            new[] { "bom-station", "direct-http", "ghcnd-station", "greenland-melt", "noaa-global-temperature", "ocean-acidity", "ozone", "sea-level" },
            assets.Select(x => x.DownloaderKey).Distinct().ToArray());
        Assert.AreEqual(assets.Count, assets.Select(x => x.AssetKey).Distinct().Count());
        Assert.IsTrue(assets.All(x => !x.RelativePath.Contains('[') && (x.DownloadUrl == null || !x.DownloadUrl.Contains('['))));
    }

    [TestMethod]
    public async Task ResolveAllAsync_MultiStationBomLocation_ExcludesClosedStations()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);
        var bomRelativePaths = assets.Where(x => x.DownloaderKey == "bom-station").Select(x => x.RelativePath).ToArray();

        // Location cbb11150-ec74-4401-8357-fae6fef70768 was served by station 001021 until 1998-09-15,
        // then by station 001019 (still open, EndDate: null) from 1998-09-16 onward.
        Assert.Contains(@"BOM\001019.zip", bomRelativePaths);
        Assert.DoesNotContain(@"BOM\001021.zip", bomRelativePaths);
    }

    [TestMethod]
    public async Task ResolveAllAsync_BomMeasurements_ShareOneArchivePerMappedStation()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);

        var asset = assets.Single(x => x.RelativePath == @"BOM\001019.zip");

        Assert.HasCount(5, asset.Measurements);
        CollectionAssert.AreEquivalent(
            new[] { "TempMean", "TempMax", "TempMin", "Precipitation", "SolarRadiation" },
            asset.Measurements.Select(x => x.MeasurementDefinition.DataType.ToString()).ToArray());
    }

    [TestMethod]
    public async Task ResolveAllAsync_HadleyMeasurements_UsesFourMeasurementSpecificUrls()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);
        var metAssets = assets.Where(x => x.RelativePath.StartsWith(@"Met\")).ToList();

        Assert.HasCount(4, metAssets);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "https://www.metoffice.gov.uk/hadobs/hadcet/data/meantemp_monthly_totals.txt",
                "https://www.metoffice.gov.uk/hadobs/hadcet/data/maxtemp_daily_totals.txt",
                "https://www.metoffice.gov.uk/hadobs/hadcet/data/mintemp_daily_totals.txt",
                "https://www.metoffice.gov.uk/hadobs/hadukp/data/daily/HadCEP_daily_totals.txt",
            },
            metAssets.Select(x => x.DownloadUrl).ToArray());
    }

    [TestMethod]
    public async Task ResolveAllAsync_SharedMaunaLoaSource_ResolvesOneAssetForBothMeasurements()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);

        var asset = assets.Single(x => x.RelativePath == @"CO2\co2_mm_mlo.txt");

        Assert.HasCount(2, asset.Measurements);
        CollectionAssert.AreEquivalent(
            new[] { "CO2", "CO2Deseasoned" },
            asset.Measurements.Select(x => x.MeasurementDefinition.DataType.ToString()).ToArray());
    }

    [TestMethod]
    public async Task ResolveAllAsync_Odgi_ResolvesOnlyTheMappedTable1Asset()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);

        var odgiAssets = assets.Where(x => x.RelativePath.StartsWith(@"ODGI\", StringComparison.Ordinal)).ToList();

        Assert.HasCount(1, odgiAssets);
        Assert.AreEqual(@"ODGI\odgi_table1.csv", odgiAssets[0].RelativePath);
        Assert.AreEqual("https://gml.noaa.gov/odgi/odgi_table1.csv", odgiAssets[0].DownloadUrl);
    }

    [TestMethod]
    public async Task ResolveAllAsync_Greenland_ResolvesOneStableAsset()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);

        var greenlandAssets = assets.Where(x => x.DownloaderKey == "greenland-melt").ToList();

        Assert.HasCount(1, greenlandAssets);
        Assert.AreEqual(@"Greenland\greenland-melt-area.csv", greenlandAssets[0].RelativePath);
        Assert.IsNull(greenlandAssets[0].DownloadUrl);
    }

    [TestMethod]
    public async Task ValidateAsync_StageOnePackagedSources_AllMatchTheirConfiguredReaders()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);
        var validator = new DataSetDownloadValidator();

        foreach (var asset in assets.Where(x => x.DownloaderKey != "ghcnd-station"))
        {
            await validator.ValidateAsync(asset, Folders.SourceDataFolder, CancellationToken.None);
        }

        var ghcndAssets = assets.Where(x => x.DownloaderKey == "ghcnd-station").ToList();
        await validator.ValidateAsync(
            ghcndAssets.First(x => x.Measurements.Count == 3),
            Folders.SourceDataFolder,
            CancellationToken.None);
        await validator.ValidateAsync(
            ghcndAssets.First(x => x.Measurements.All(y => y.MeasurementDefinition.DataType != Core.Enums.DataType.Precipitation)),
            Folders.SourceDataFolder,
            CancellationToken.None);
        await validator.ValidateAsync(
            ghcndAssets.First(x => x.Measurements.Count == 1 && x.Measurements[0].MeasurementDefinition.DataType == Core.Enums.DataType.Precipitation),
            Folders.SourceDataFolder,
            CancellationToken.None);
    }

    [TestMethod]
    public async Task ResolveAllAsync_NoaaGlobalTemp_ResolvesOneStableAssetPerMappedArea()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);

        var noaaAssets = assets.Where(x => x.DownloaderKey == "noaa-global-temperature").ToList();

        Assert.HasCount(13, noaaAssets);
        Assert.IsTrue(noaaAssets.All(x => x.DownloadUrl == null));
        Assert.IsTrue(noaaAssets.All(x => x.Measurements.Count == 1));
        Assert.Contains(@"NOAAGlobalTemp\aravg.mon.land_ocean.90S.90N.v6.0.0.asc", noaaAssets.Select(x => x.RelativePath).ToArray());
    }

    [TestMethod]
    public async Task ResolveAllAsync_GhcndTemperatureAndPrecipitationMappings_ShareStationArchive()
    {
        var assets = await CreateResolver().ResolveAllAsync(CancellationToken.None);

        var sharedAsset = assets.Single(x => x.RelativePath == @"GHCNd\AE000041196.zip");

        Assert.HasCount(3, sharedAsset.Measurements);
        CollectionAssert.AreEquivalent(
            new[] { "TempMax", "TempMin", "Precipitation" },
            sharedAsset.Measurements.Select(x => x.MeasurementDefinition.DataType.ToString()).ToArray());
    }

    [TestMethod]
    public async Task GetDataFileMappings_EveryLocationOfAnAutomaticallyManagedDataset_HasAtMostOneOpenStation()
    {
        // Automatic-retrieval resolution (see IsAutomaticallyRetrievable) assumes there is never more than
        // one open (EndDate: null, non-blank Id) station per location; a location with two or more would
        // resolve ambiguously as to which one is "the" currently-refreshed station. Zero is legitimate: a
        // handful of locations are present in a mapping purely as an all-null placeholder (no real station
        // for that provider at that location). This only applies to datasets that opt into automatic
        // retrieval at all (a DataDownloaderKey somewhere) - manually-curated datasets such as ACORN-SAT
        // model their entire station history as closed segments with no "currently open" concept.
        var definitions = await DataSetDefinition.GetDataSetDefinitions(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
        var automaticallyManaged = definitions.Where(x =>
            x.DataDownloaderKey != null || (x.MeasurementDefinitions?.Any(m => m.DataDownloaderKey != null) ?? false));

        foreach (var definition in automaticallyManaged)
        {
            foreach (var (locationId, filters) in definition.DataLocationMapping!.LocationIdToDataFileMappings)
            {
                var openFilterCount = filters.Count(x => !string.IsNullOrWhiteSpace(x.Id) && x.EndDate is null);
                Assert.IsTrue(
                    openFilterCount <= 1,
                    $"Dataset {definition.ShortName} location {locationId} has {openFilterCount} open stations, expected at most 1.");
            }
        }
    }

    private static DataSetSourceAssetResolver CreateResolver()
    {
        return new DataSetSourceAssetResolver(Path.Combine(Folders.MetaDataFolder, "DataFileMapping"));
    }
}
