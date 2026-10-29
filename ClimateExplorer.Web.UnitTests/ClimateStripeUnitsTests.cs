namespace ClimateExplorer.Web.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Web.Client.Services;
using ClimateExplorer.Web.Client.UiModel;
using ClimateExplorer.Web.Client.Units;
using ClimateExplorer.Web.UiModel;
using ClimateExplorer.WebApiClient.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static ClimateExplorer.Core.Enums;

[TestClass]
public class ClimateStripeUnitsTests
{
    private static readonly Guid LocationId = Guid.Parse("60000000-0000-0000-0000-000000000004");

    [TestMethod]
    public async Task YearlyValuesHelper_ShiftedTemperatureInput_MeanMovesAndRelativeDoesNot()
    {
        var original = await RecordStripeMeasures(shift: 0);
        var shifted = await RecordStripeMeasures(shift: 10);

        OffsetShift.AssertLevelsMoveAndChangesDoNot(original, shifted, 10, minimumLevels: 60, minimumChanges: 60);
    }

    [TestMethod]
    public void GetTooltip_UsCustomary_AbsoluteOffsetAndDistanceScaled()
    {
        var values = new YearlyValues(2020, -0.5, 14.5, 97);

        var tooltip = YearlyValuesHelper.GetTooltip(values, UnitOfMeasure.DegreesCelsius, UnitFormatter.UsCustomary);

        Assert.AreEqual("Year 2020<br>Mean temperature of 58.1°F<br>0.9°F below average", tooltip);
    }

    [TestMethod]
    public void GetTooltip_Metric_Unchanged()
    {
        var values = new YearlyValues(2020, 31.4, 652.4, 105);

        var tooltip = YearlyValuesHelper.GetTooltip(values, UnitOfMeasure.Millimetres);

        Assert.AreEqual("Year 2020<br>Precipitation total of 652mm<br>31mm above average<br>105% of the average", tooltip);
    }

    // Everything the stripe and its info panel show for each year, as they ask for it.
    private static async Task<List<Measure>> RecordStripeMeasures(double shift)
    {
        var summary = await ClimateDataHelper.CalculateAnomaly(
            CreateDataService(shift).Object,
            [CreateDataSetDefinition()],
            new Location { Id = LocationId, Name = "Testville", CountryCode = "TC", Coordinates = new Coordinates(1, 2) },
            [new DataSubstitute { DataType = DataType.TempMean, DataAdjustment = DataAdjustment.Adjusted }],
            ContainerAggregationFunctions.Mean);

        var units = new RecordingUnitFormatter(UnitFormatter.UsCustomary);
        var mean = summary!.DataSet!.DataRecords.Average(x => x.Value)!.Value;

        units.Format(YearlyValuesHelper.LocationMean(mean, UnitOfMeasure.DegreesCelsius));

        foreach (var values in summary.AnomalyRecords!)
        {
            units.Format(YearlyValuesHelper.Absolute(values, UnitOfMeasure.DegreesCelsius));
            units.Format(YearlyValuesHelper.Relative(values, UnitOfMeasure.DegreesCelsius), signed: true);
            _ = YearlyValuesHelper.GetTooltip(values, UnitOfMeasure.DegreesCelsius, units);
        }

        return units.Measures;
    }

    private static Mock<IDataService> CreateDataService(double shift)
    {
        var dataSet = new DataSet
        {
            MeasurementDefinition = CreateMeasurementDefinition(),
            DataRecords = [.. Enumerable.Range(1950, 70).Select(year => new BinnedRecord(new YearBinIdentifier((short)year).Id, 12 + ((year - 1950) * 0.03) + Math.Sin(year * 1.7) + shift))],
        };

        var dataService = new Mock<IDataService>();
        dataService
            .Setup(x => x.PostDataSet(
                It.IsAny<BinGranularities>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<ContainerAggregationFunctions>(),
                It.IsAny<SeriesValueOptions>(),
                It.IsAny<SeriesSpecification[]>(),
                It.IsAny<SeriesDerivationTypes>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<int>(),
                It.IsAny<SeriesTransformations>(),
                It.IsAny<string?>(),
                It.IsAny<short?>(),
                It.IsAny<DataResolution?>()))
            .ReturnsAsync(dataSet);

        return dataService;
    }

    private static MeasurementDefinitionViewModel CreateMeasurementDefinition()
    {
        return new MeasurementDefinitionViewModel
        {
            DataType = DataType.TempMean,
            DataAdjustment = DataAdjustment.Adjusted,
            DataResolution = DataResolution.Monthly,
            UnitOfMeasure = UnitOfMeasure.DegreesCelsius,
        };
    }

    private static DataSetDefinitionViewModel CreateDataSetDefinition()
    {
        return new DataSetDefinitionViewModel
        {
            Id = Guid.NewGuid(),
            Name = "Test data set",
            ShortName = "Test",
            LocationIds = [LocationId],
            MeasurementDefinitions = [CreateMeasurementDefinition()],
        };
    }
}
