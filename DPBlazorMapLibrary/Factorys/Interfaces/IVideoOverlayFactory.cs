namespace DPBlazorMapLibrary
{
    public interface IVideoOverlayFactory
    {
        public Task<VideoOverlay> CreateVideoOverlay(string video, LatLngBounds bounds, VideoOverlayOptions? options);
        public Task<VideoOverlay> CreateVideoOverlayAndAddToMap(string video, Map map, LatLngBounds bounds, VideoOverlayOptions? options);
    }
}
