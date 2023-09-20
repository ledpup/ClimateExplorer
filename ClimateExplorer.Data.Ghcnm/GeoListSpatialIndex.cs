using Dbscan;
using GeoCoordinatePortable;

namespace ClimateExplorer.Data.Ghcnm;

public delegate double DistanceFunction(in IPointData p1, in IPointData p2);

internal static class DistanceFunctions
{
    public static double GeoSpatialDistance(in IPointData a, in IPointData b)
    {
        var geoCoordinate = new GeoCoordinate(a.Point.X, a.Point.Y);
        var distance = geoCoordinate.GetDistanceTo(new GeoCoordinate(b.Point.X, b.Point.Y)) / 1000.0;
        return distance;
    }
}

public class GeoListSpatialIndex<T> : ISpatialIndex<T> where T : IPointData
{
    private readonly IReadOnlyList<T> _list;
    private readonly DistanceFunction _distanceFunction;

    public GeoListSpatialIndex(IEnumerable<T> data) : this(data, DistanceFunctions.GeoSpatialDistance) { }

    public GeoListSpatialIndex(IEnumerable<T> data, DistanceFunction distanceFunction)
    {
        _list = data.ToList();
        _distanceFunction = distanceFunction;
    }

    public IReadOnlyList<T> Search() => _list;

    public IReadOnlyList<T> Search(in IPointData p, double epsilon)
    {
        var l = new List<T>();
        foreach (var q in _list)
            if (_distanceFunction(p, q) < epsilon)
                l.Add(q);
        return l;
    }
}

public class GeoPoint : IPointData
{
    public GeoPoint(string id, double x, double y)
    {
        Id = id;
        Point = new Point(x, y);
    }

    public string Id { get; }
    public Point Point { get; }
}