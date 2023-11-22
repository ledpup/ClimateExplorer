namespace DPBlazorMapLibrary
{
    public interface IPolylineFactory
    {
        Task<Polyline> CreatePolyline(IEnumerable<LatLng> latLng, PolylineOptions? options);
        Task<Polyline> CreatePolylineAndAddToMap(IEnumerable<LatLng> latLng, Map map, PolylineOptions? options);

    }
}
