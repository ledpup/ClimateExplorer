namespace ClimateExplorer.UnitTests;

using System;
using System.Linq;
using ClimateExplorer.Core;
using ClimateExplorer.Data.Ecad;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class EcadSourceFormatTests
{
    [TestMethod]
    public void GetDataRowRegEx_EveryPublishedMeasurement_MatchesTheDefinitionTheDatasetDeclares()
    {
        // The writer and the measurement definitions have to agree on column order, but they live in
        // different projects and neither can reference the other. This is what stops them drifting apart.
        var definition = DataSetDefinitionsBuilder.BuildDataSetDefinitions()
            .Single(x => x.Id == DataSetDefinitionsBuilder.EcadDataSetDefinitionId);

        foreach (var dataType in EcadConstants.PublishedDataTypes)
        {
            var measurement = definition.MeasurementDefinitions!.Single(x => x.DataType == dataType);
            Assert.AreEqual(
                EcadCsvFormat.GetDataRowRegEx(dataType),
                measurement.DataRowRegEx,
                $"The declared {dataType} row expression does not match the published CSV layout.");
        }
    }

    [TestMethod]
    public void Write_ObservationsWithGapsAndNegatives_RoundTripsThroughRead()
    {
        var written = EcadCsvFormat.Write(
        [
            new EcadDailyObservation(new DateOnly(2026, 6, 30)) { TempMean = -1.25, TempMax = 3, TempMin = -7.4, Precipitation = 0 },
            new EcadDailyObservation(new DateOnly(2026, 7, 1)) { TempMean = 11.2 },
        ]);

        var read = EcadCsvFormat.Read(written.Split(Environment.NewLine));

        Assert.HasCount(2, read);
        Assert.AreEqual(-1.25, read[new DateOnly(2026, 6, 30)].TempMean);
        Assert.AreEqual(0, read[new DateOnly(2026, 6, 30)].Precipitation);
        Assert.AreEqual(11.2, read[new DateOnly(2026, 7, 1)].TempMean);
        Assert.IsNull(read[new DateOnly(2026, 7, 1)].Precipitation);
    }

    [TestMethod]
    public void Write_DayWithNoValueAtAll_IsOmittedRatherThanWrittenAsAnEmptyRow()
    {
        var written = EcadCsvFormat.Write(
        [
            new EcadDailyObservation(new DateOnly(2026, 6, 30)) { TempMean = 1 },
            new EcadDailyObservation(new DateOnly(2026, 7, 1)),
        ]);

        Assert.DoesNotContain("20260701", written);
    }

    [TestMethod]
    public void GetLatestDate_PublishedFile_ReturnsTheLastDayHoldingAValue()
    {
        var latest = EcadCsvFormat.GetLatestDate(["20260629,1,2,0,0", "20260630,1,2,0,0", "not a data row"]);

        Assert.AreEqual(new DateOnly(2026, 6, 30), latest);
    }

    [TestMethod]
    public void GetMaximumDaysPerQuery_FourParameters_LeavesRoomForTheirQualityFlagParameters()
    {
        // The server bills each requested parameter twice, once for the value and once for its quality
        // flag, so four parameters allow 300,000 / 8 days rather than 300,000 / 4.
        Assert.AreEqual(37_500, EcadQueryWindowCalculator.GetMaximumDaysPerQuery(4));
    }

    [TestMethod]
    public void GetWindows_RangeLongerThanOneQueryAllows_SplitsIntoContiguousNonOverlappingWindows()
    {
        var windows = EcadQueryWindowCalculator.GetWindows(new DateOnly(1900, 1, 1), new DateOnly(2026, 7, 1), 4).ToList();

        Assert.HasCount(2, windows);
        Assert.AreEqual(new DateOnly(1900, 1, 1), windows[0].From);
        Assert.AreEqual(windows[0].To.AddDays(1), windows[1].From);
        Assert.AreEqual(new DateOnly(2026, 7, 1), windows[1].To);
        Assert.IsTrue(windows.All(x => x.To.DayNumber - x.From.DayNumber + 1 <= 37_500));
    }

    [TestMethod]
    public void GetWindows_RangeThatHasAlreadyBeenCovered_ProducesNoWindows()
    {
        var windows = EcadQueryWindowCalculator.GetWindows(new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 1), 4);

        Assert.IsEmpty(windows);
    }

    [TestMethod]
    public void IsInFamily_CodesSharingALeadingLetter_OnlyMatchesTheirOwnFamily()
    {
        Assert.IsTrue(EcadConstants.IsInFamily("tg21", "tg"));
        Assert.IsFalse(EcadConstants.IsInFamily("tn3", "tg"));
        Assert.IsFalse(EcadConstants.IsInFamily("tg", "tg"));
        Assert.IsFalse(EcadConstants.IsInFamily("tgx1", "tg"));
    }

    [TestMethod]
    public void Normalise_GhcnAndEcadSpellingsOfTheSameStation_ProduceComparableNames()
    {
        Assert.AreEqual("DEBILT", EcadStationNameComparer.Normalise("DE_BILT_1"));
        Assert.AreEqual("DEBILT", EcadStationNameComparer.Normalise("De Bilt"));
        Assert.AreEqual(1d, EcadStationNameComparer.GetSimilarity("DE_BILT_1", "De Bilt"));

        // GHCN transliterates umlauts ("VAEXJOE") where ECA&D just carries them ("Växjöe"), so stripping
        // diacritics does not make the two identical - only similar enough to corroborate.
        Assert.AreEqual("VAXJOE", EcadStationNameComparer.Normalise("Växjöe"));
        Assert.IsGreaterThanOrEqualTo(0.6d, EcadStationNameComparer.GetSimilarity("VAEXJOE", "Växjöe"));
    }

    [TestMethod]
    public void GetSimilarity_UnrelatedNames_ScoresBelowTheCorroborationThreshold()
    {
        Assert.IsLessThan(0.6d, EcadStationNameComparer.GetSimilarity("BOURNEMOUTH", "Hurn"));
        Assert.IsGreaterThanOrEqualTo(0.6d, EcadStationNameComparer.GetSimilarity("WIEN", "Wien-Hohe Warte"));
    }

    [TestMethod]
    public void PublishedDataTypes_EveryOne_HasAParameterFamilyConfigured()
    {
        foreach (var dataType in EcadConstants.PublishedDataTypes)
        {
            Assert.IsNotNull(EcadConstants.GetParameterPrefix(dataType));
        }
    }
}
