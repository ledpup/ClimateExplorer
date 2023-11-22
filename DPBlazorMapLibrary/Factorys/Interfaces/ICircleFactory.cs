namespace DPBlazorMapLibrary
{
    public interface ICircleFactory
    {
        Task<Circle> CreateCircle(LatLng latLng, CircleOptions? options);
        Task<Circle> CreateCircleAndAddToMap(LatLng latLng, Map map, CircleOptions? options);
    }
}
