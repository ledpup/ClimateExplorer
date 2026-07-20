namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core;
using ClimateExplorer.Core.InputOutput;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DataPackageDefinitionTests
{
    private static readonly IReadOnlyDictionary<string, string> StationByDataSet = new Dictionary<string, string>
    {
        ["ACORN-SAT"] = "001019",
        ["BOM-CDO"] = "001019",
        ["GHCNm"] = "AE000041196",
        ["GHCNmp"] = "AE000041196",
        ["GHCNd"] = "AE000041196",
        ["GHCNdp"] = "AE000041196",
        ["Global temp"] = "land.90S.90N",
        ["ODGI"] = "table1",
    };

    [TestMethod]
    public async Task BuildDataSetDefinitions_PackagedSources_AllResolveFromSingleTargetLayout()
    {
        var datasetsFolder = Path.Combine(GetSolutionRoot(), "ClimateExplorer.WebApi", "Datasets");
        var definitions = DataSetDefinitionsBuilder.BuildDataSetDefinitions();

        foreach (var dataSet in definitions)
        {
            var station = StationByDataSet.GetValueOrDefault(dataSet.ShortName, string.Empty);
            foreach (var measurement in dataSet.MeasurementDefinitions!)
            {
                var lines = await DataReaderFunctions.GetLinesInDataFileSource(measurement.DataFileSource, station, datasetsFolder);

                Assert.IsNotNull(lines, $"Source for '{dataSet.ShortName}' was not found.");
                Assert.IsGreaterThan(0, lines.Length, $"Source for '{dataSet.ShortName}' was empty.");
            }
        }
    }

    [TestMethod]
    public void DatasetsFolder_TargetLayout_ContainsNoLegacyDataTypeArchives()
    {
        var datasetsFolder = Path.Combine(GetSolutionRoot(), "ClimateExplorer.WebApi", "Datasets");
        string[] legacyArchives =
        [
            "Atmosphere.zip",
            "Ice.zip",
            "Ocean.zip",
            "Precipitation.zip",
            "Solar.zip",
            "Temperature.zip",
            "Temperature_BOM.zip",
        ];

        foreach (var archive in legacyArchives)
        {
            Assert.IsFalse(File.Exists(Path.Combine(datasetsFolder, archive)), $"Legacy archive '{archive}' still exists.");
        }
    }

    [TestMethod]
    public void BuildDataSetDefinitions_SharedCo2Measurements_ResolveToSamePhysicalAsset()
    {
        var definition = DataSetDefinitionsBuilder.BuildDataSetDefinitions().Single(x => x.ShortName == "Carbon Dioxide (CO₂)");
        var physicalPaths = definition.MeasurementDefinitions!
            .Select(x => x.DataFileSource.FilePathFormat)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.HasCount(1, physicalPaths);
    }

    private static string GetSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClimateExplorer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the solution root.");
    }
}
