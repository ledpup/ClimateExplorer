namespace DPBlazorMapLibrary
{
    public interface IMarkerFactory
    {
        public Task<Marker> CreateMarker(LatLng latLng, MarkerOptions? options);
        public Task<Marker> CreateMarkerAndAddToMap(LatLng latLng, Map map, MarkerOptions? options);
    }
}
