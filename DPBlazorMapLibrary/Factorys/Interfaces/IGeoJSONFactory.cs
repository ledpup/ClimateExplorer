namespace DPBlazorMapLibrary
{
    public interface IGeoJSONFactory
    {
        public Task<GeoJSONLayer> CreateGeoJSONLayer(object geojson, GeoJSONOptions? options);
        public Task<GeoJSONLayer> CreateGeoJSONLayerAndAddToMap(object geojson, Map map, GeoJSONOptions? options);
    }
}
