namespace ClimateExplorer.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using ClimateExplorer.Core;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Data.Ecad;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ClimateExplorer.Core.Enums;

[TestClass]
public sealed class EcadStationMatcherTests
{
    private static readonly DateOnly Today = new(2026, 7, 1);
    private static readonly DateOnly LongAgo = new(2004, 12, 18);

    [TestMethod]
    public void Match_StationAtTheSamePlaceWithACorroboratingName_Matches()
    {
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("NLM00006260", "DE_BILT_1", 52.10140, 5.18670)],
            [CreateEcadStation("ecad_0000162", "De Bilt", 52.09889, 5.17944)],
            CreateOptions());

        var match = report.Matches.Single();
        Assert.AreEqual("NLM00006260", match.GhcnStationId);
        Assert.AreEqual("ecad_0000162", match.EcadStationId);
        Assert.AreEqual(EcadStationMatchKind.Unique, match.Kind);
        Assert.IsEmpty(report.Rejections);
    }

    [TestMethod]
    public void Match_MatchedStation_SelectsTheOneVariantItReportsForEachMeasurement()
    {
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("NLM00006260", "DE_BILT_1", 52.10140, 5.18670)],
            [CreateEcadStation("ecad_0000162", "De Bilt", 52.09889, 5.17944, "tg21", "tx3", "tn3", "rr7")],
            CreateOptions());

        var parameterCodes = report.Matches.Single().ParameterCodes;
        Assert.AreEqual("tg21", parameterCodes[DataType.TempMean]);
        Assert.AreEqual("tx3", parameterCodes[DataType.TempMax]);
        Assert.AreEqual("tn3", parameterCodes[DataType.TempMin]);
        Assert.AreEqual("rr7", parameterCodes[DataType.Precipitation]);
    }

    [TestMethod]
    public void Match_TwoDifferentlyNamedStationsScoringEqually_IsRejectedAsAmbiguousRatherThanGuessed()
    {
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("FIM00002801", "ENONTEKIO_KILPISJARVI", 69.045, 20.804)],
            [
                CreateEcadStation("ecad_0000001", "Enontekio Kilpisjarvi Kylakeskus", 69.045, 20.810),
                CreateEcadStation("ecad_0000002", "Enontekio Kilpisjarvi Saana", 69.047, 20.850),
            ],
            CreateOptions());

        Assert.IsEmpty(report.Matches);
        Assert.AreEqual(EcadStationRejectionReason.Ambiguous, report.Rejections.Single().Reason);
    }

    [TestMethod]
    public void Match_NothingNearbyHasASimilarName_IsRejectedRatherThanMatchedOnProximityAlone()
    {
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("UKM00003862", "BOURNEMOUTH", 50.779, -1.834)],
            [CreateEcadStation("ecad_0001867", "Hurn", 50.779, -1.835)],
            CreateOptions());

        Assert.IsEmpty(report.Matches);
        Assert.AreEqual(EcadStationRejectionReason.NameNotCorroborated, report.Rejections.Single().Reason);
    }

    [TestMethod]
    public void Match_NoStationWithinTolerance_ProducesNeitherMatchNorRejection()
    {
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("ASN00023000", "ADELAIDE", -34.926, 138.600)],
            [CreateEcadStation("ecad_0000162", "De Bilt", 52.09889, 5.17944)],
            CreateOptions());

        Assert.IsEmpty(report.Matches);
        Assert.IsEmpty(report.Rejections);
    }

    [TestMethod]
    public void Match_SameStationRegisteredTwice_ResolvesToTheRegistrationReportingMostRecently()
    {
        // Both registrations are live, so neither is filtered out and the tie has to be broken explicitly.
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("AU000005010", "KREMSMUENSTER", 48.056, 14.133)],
            [
                CreateEcadStation("ecad_0000011", "Kremsmuenster (Tawes)", 48.055, 14.132, lastDate: Today.AddDays(-10)),
                CreateEcadStation("ecad_0024838", "Kremsmuenster (Tawes)", 48.056, 14.133),
            ],
            CreateOptions());

        var match = report.Matches.Single();
        Assert.AreEqual("ecad_0024838", match.EcadStationId);
        Assert.AreEqual(EcadStationMatchKind.DuplicateRegistration, match.Kind);
    }

    [TestMethod]
    public void Match_NearestStationStoppedReporting_PrefersALiveStationOverTheDeadOne()
    {
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("EZ000011464", "MILESOVKA", 50.555, 13.931)],
            [
                CreateEcadStation("ecad_0000510", "Milesovka", 50.555, 13.931, lastDate: LongAgo),
                CreateEcadStation("ecad_0000511", "Milesovka", 50.557, 13.935),
            ],
            CreateOptions());

        Assert.AreEqual("ecad_0000511", report.Matches.Single().EcadStationId);
    }

    [TestMethod]
    public void Match_OnlyCandidateLacksAMeasurement_IsRejectedBecauseAllFourShareOneSourceFile()
    {
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("NLM00006260", "DE_BILT_1", 52.10140, 5.18670)],
            [CreateEcadStation("ecad_0000162", "De Bilt", 52.09889, 5.17944, "tg21", "tx3", "tn3")],
            CreateOptions());

        Assert.IsEmpty(report.Matches);
        var rejection = report.Rejections.Single();
        Assert.AreEqual(EcadStationRejectionReason.IncompleteMeasurements, rejection.Reason);
        StringAssert.Contains(rejection.Detail, "Precipitation");
    }

    [TestMethod]
    public void Match_CandidateReportsTwoVariantsOfOneMeasurement_IsRejectedRatherThanPickingOne()
    {
        var report = EcadStationMatcher.Match(
            [CreateGhcnStation("NLM00006260", "DE_BILT_1", 52.10140, 5.18670)],
            [CreateEcadStation("ecad_0000162", "De Bilt", 52.09889, 5.17944, "tg21", "tg5", "tx3", "tn3", "rr7")],
            CreateOptions());

        Assert.IsEmpty(report.Matches);
        Assert.AreEqual(EcadStationRejectionReason.AmbiguousParameterVariant, report.Rejections.Single().Reason);
    }

    [TestMethod]
    public void Match_OneEcadStationIsTheBestMatchForTwoGhcnStations_DropsBothRatherThanServeItTwice()
    {
        var report = EcadStationMatcher.Match(
            [
                CreateGhcnStation("XX000000001", "DE_BILT", 52.10140, 5.18670),
                CreateGhcnStation("XX000000002", "DE_BILT", 52.10150, 5.18680),
            ],
            [CreateEcadStation("ecad_0000162", "De Bilt", 52.09889, 5.17944)],
            CreateOptions());

        Assert.IsEmpty(report.Matches);
        Assert.HasCount(2, report.Rejections);
        Assert.IsTrue(report.Rejections.All(x => x.Reason == EcadStationRejectionReason.Contested));
    }

    private static EcadStationMatchOptions CreateOptions()
    {
        return new EcadStationMatchOptions { ObservedOnOrAfter = Today.AddDays(-31) };
    }

    private static Station CreateGhcnStation(string id, string name, double latitude, double longitude)
    {
        return new Station
        {
            Id = id,
            Name = name,
            Coordinates = new Coordinates { Latitude = latitude, Longitude = longitude },
        };
    }

    private static EcadStation CreateEcadStation(
        string id,
        string name,
        double latitude,
        double longitude,
        params string[] parameterCodes)
    {
        return CreateEcadStation(id, name, latitude, longitude, Today, parameterCodes);
    }

    private static EcadStation CreateEcadStation(
        string id,
        string name,
        double latitude,
        double longitude,
        DateOnly lastDate)
    {
        return CreateEcadStation(id, name, latitude, longitude, lastDate, []);
    }

    private static EcadStation CreateEcadStation(
        string id,
        string name,
        double latitude,
        double longitude,
        DateOnly lastDate,
        string[] parameterCodes)
    {
        var codes = parameterCodes.Length > 0 ? parameterCodes : ["tg6", "tx2", "tn2", "rr9"];
        return new EcadStation(
            id,
            name,
            "NL",
            new Coordinates { Latitude = latitude, Longitude = longitude },
            [.. codes.Select(x => new EcadStationSeries(x, new DateOnly(1900, 1, 1), lastDate))]);
    }
}
