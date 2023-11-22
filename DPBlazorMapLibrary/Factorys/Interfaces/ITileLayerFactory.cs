namespace DPBlazorMapLibrary
{
    public interface ITileLayerFactory
    {
        public Task<TileLayer> CreateTileLayer(string urlTemplate, TileLayerOptions? options);
        public Task<TileLayer> CreateTileLayerAndAddToMap(string urlTemplate, Map map, TileLayerOptions? options);
    }
}
