namespace DPBlazorMapLibrary
{
    internal interface IImageOverlayFactory
    {
        public Task<ImageOverlay> CreateImageOverlay(string imageUrl, LatLngBounds bounds, ImageOverlayOptions? options);
        public Task<ImageOverlay> CreateImageOverlayAndAddToMap(string imageUrl, Map map, LatLngBounds bounds, ImageOverlayOptions? options);
    }
}
