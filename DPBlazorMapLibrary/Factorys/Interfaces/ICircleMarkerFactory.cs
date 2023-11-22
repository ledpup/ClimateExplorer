namespace DPBlazorMapLibrary
{
    public interface ICircleMarkerFactory
    {
        Task<CircleMarker> CreateCircleMarker(LatLng latLng, CircleMarkerOptions? options);
        Task<CircleMarker> CreateCircleMarkerAndAddToMap(LatLng latLng, Map map, CircleMarkerOptions? options);
    }
}
