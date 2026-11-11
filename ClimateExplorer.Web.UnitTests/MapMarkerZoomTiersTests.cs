namespace ClimateExplorer.Web.UnitTests;

using System;
using System.Linq;
using ClimateExplorer.Core.Model;
using ClimateExplorer.Web.Client.Services.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MapMarkerZoomTiersTests
{
    [TestMethod]
    public void Calculate_FarApartLocations_AllShowAtMinimumZoom()
    {
        var sydney = CreateLocation(-33.86, 151.21, recordLength: 100);
        var london = CreateLocation(51.51, -0.13, recordLength: 60);

        var zooms = MapMarkerZoomTiers.Calculate([sydney, london]);

        Assert.AreEqual(MapMarkerZoomTiers.MinimumZoom, zooms[sydney.Id]);
        Assert.AreEqual(MapMarkerZoomTiers.MinimumZoom, zooms[london.Id]);
    }

    [TestMethod]
    public void Calculate_NeighbourTwentyKmAway_LongerRecordShowsFirstAndNeighbourFromZoomEight()
    {
        // 20 km east of Sevilla is about 20 px at zoom 7 (overlapping) and 41 px at zoom 8 (clear).
        var sevilla = CreateLocation(37.42, -5.88, recordLength: 129);
        var neighbour = CreateLocation(37.42, -5.88 + 0.2254, recordLength: 70);

        var zooms = MapMarkerZoomTiers.Calculate([neighbour, sevilla]);

        Assert.AreEqual(MapMarkerZoomTiers.MinimumZoom, zooms[sevilla.Id]);
        Assert.AreEqual(8, zooms[neighbour.Id]);
    }

    [TestMethod]
    public void Calculate_NoTemperatureTrend_RanksBelowLocationWithRecord()
    {
        var withRecord = CreateLocation(37.42, -5.88, recordLength: 40);
        var withoutRecord = CreateLocation(37.42, -5.80, recordLength: null);

        var zooms = MapMarkerZoomTiers.Calculate([withoutRecord, withRecord]);

        Assert.AreEqual(MapMarkerZoomTiers.MinimumZoom, zooms[withRecord.Id]);
        Assert.IsTrue(zooms[withoutRecord.Id] > MapMarkerZoomTiers.MinimumZoom);
    }

    [TestMethod]
    public void Calculate_SameCoordinates_SecondShowsAtMaximumZoom()
    {
        var first = CreateLocation(10, 10, recordLength: 90);
        var second = CreateLocation(10, 10, recordLength: 80);

        var zooms = MapMarkerZoomTiers.Calculate([first, second]);

        Assert.AreEqual(MapMarkerZoomTiers.MinimumZoom, zooms[first.Id]);
        Assert.AreEqual(MapMarkerZoomTiers.MaximumZoom, zooms[second.Id]);
    }

    [TestMethod]
    public void Calculate_PolarLatitude_DoesNotThrow()
    {
        var southPole = CreateLocation(-90, 0, recordLength: 60);

        var zooms = MapMarkerZoomTiers.Calculate([southPole]);

        Assert.AreEqual(MapMarkerZoomTiers.MinimumZoom, zooms[southPole.Id]);
    }

    [TestMethod]
    public void Calculate_ManyLocations_NoTwoMarkersShownAtAZoomOverlap()
    {
        var random = new Random(1);
        var locations = Enumerable.Range(0, 400)
            .Select(_ => CreateLocation(35 + (random.NextDouble() * 10), -10 + (random.NextDouble() * 20), random.Next(30, 150)))
            .ToList();

        var zooms = MapMarkerZoomTiers.Calculate(locations);

        Assert.AreEqual(locations.Count, zooms.Count);
        for (var zoom = MapMarkerZoomTiers.MinimumZoom; zoom < MapMarkerZoomTiers.MaximumZoom; zoom++)
        {
            var shown = locations.Where(x => zooms[x.Id] <= zoom).Select(x => Project(x.Coordinates, zoom)).ToList();
            for (var i = 0; i < shown.Count; i++)
            {
                for (var j = i + 1; j < shown.Count; j++)
                {
                    var overlaps = Math.Abs(shown[i].X - shown[j].X) < MapMarkerZoomTiers.MarkerWidthPixels
                        && Math.Abs(shown[i].Y - shown[j].Y) < MapMarkerZoomTiers.MarkerHeightPixels;
                    Assert.IsFalse(overlaps, $"Two markers overlap at zoom {zoom}");
                }
            }
        }
    }

    [TestMethod]
    public void GetRecordLength_FullTrendFrom1900To2024_Returns125()
    {
        var location = CreateLocation(0, 0, recordLength: 125);

        Assert.AreEqual(125, MapMarkerZoomTiers.GetRecordLength(location));
    }

    [TestMethod]
    public void GetRecordLength_NoTrend_ReturnsZero()
    {
        var location = CreateLocation(0, 0, recordLength: null);

        Assert.AreEqual(0, MapMarkerZoomTiers.GetRecordLength(location));
    }

    private static Location CreateLocation(double latitude, double longitude, int? recordLength)
    {
        const int lastYear = 2024;
        return new Location
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            CountryCode = "ES",
            Coordinates = new Coordinates(latitude, longitude),
            LastYear = lastYear,
            TemperatureTrends = recordLength is int length
                ? [new TrendSummary(TrendWindow.Full, lastYear - length + 1, 0.01, true)]
                : null,
        };
    }

    private static (double X, double Y) Project(Coordinates coordinates, int zoom)
    {
        var scale = 256 * Math.Pow(2, zoom);
        var sinLatitude = Math.Sin(coordinates.Latitude * Math.PI / 180);
        var x = (coordinates.Longitude + 180) / 360 * scale;
        var y = (0.5 - (Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI))) * scale;
        return (x, y);
    }
}
