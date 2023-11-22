namespace DPBlazorMapLibrary
{
    public interface IPolygoneFactory
    {
        Task<Polygon> CreatePolygon(IEnumerable<LatLng> latLngs, PolygonOptions? options);

        Task<Polygon> CreatePolygonAndAddToMap(IEnumerable<LatLng> latLngs, Map map, PolygonOptions? options);
    }
}
