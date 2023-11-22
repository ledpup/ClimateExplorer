using DPBlazorMapLibrary.JsInterops.Events;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    public class LayerFactory : IMarkerFactory,
                                ITileLayerFactory,
                                IVideoOverlayFactory,
                                IImageOverlayFactory,
                                IPolylineFactory,
                                IPolygoneFactory,
                                IRectangleFactory,
                                ICircleFactory,
                                ICircleMarkerFactory,
                                IGeoJSONFactory
    {
        private const string _createMarkerJsFunction = "L.marker";
        private const string _crateTileLayerJsFunction = "L.tileLayer";
        private const string _createVideoOverlayJsFunction = "L.videoOverlay";
        private const string _createImageOverlayJsFunction = "L.imageOverlay";
        private const string _createPolylineJsFunction = "L.polyline";
        private const string _createPolygonJsFunction = "L.polygon";
        private const string _createRectangleJsFunction = "L.rectangle";
        private const string _createCircleJsFunction = "L.circle";
        private const string _createCircleMarkerJsFunction = "L.circleMarker";
        private const string _createGeoJSONLayerJsFunction = "L.geoJSON";


        private readonly IJSRuntime _jsRuntime;
        private readonly IEventedJsInterop _eventedJsInterop;
        

        public LayerFactory(
            IJSRuntime jsRuntime,
            IEventedJsInterop eventedJsInterop)
        {
            this._jsRuntime = jsRuntime;
            this._eventedJsInterop = eventedJsInterop;
        }

        #region Marker
        public async Task<Marker> CreateMarker(LatLng latLng, MarkerOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createMarkerJsFunction, latLng, options);
            return new Marker(jsReference, _eventedJsInterop);
        }

        public async Task<Marker> CreateMarkerAndAddToMap(LatLng latLng, Map map, MarkerOptions? options)
        {
            Marker marker = await CreateMarker(latLng, options);
            await marker.AddTo(map);
            return marker;
        }
        #endregion

        #region Tile
        public async Task<TileLayer> CreateTileLayer(string urlTemplate, TileLayerOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_crateTileLayerJsFunction, urlTemplate, options);
            return new TileLayer(jsReference);
        }

        public async Task<TileLayer> CreateTileLayerAndAddToMap(string urlTemplate, Map map, TileLayerOptions? options)
        {
            TileLayer tileLayer = await CreateTileLayer(urlTemplate, options);
            await tileLayer.AddTo(map);
            return tileLayer;
        }
        #endregion

        #region Video Overlay
        public async Task<VideoOverlay> CreateVideoOverlay(string video, LatLngBounds bounds, VideoOverlayOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createVideoOverlayJsFunction, video, bounds.ToLatLng(), options);
            return new VideoOverlay(jsReference, _eventedJsInterop);
        }


        public async Task<VideoOverlay> CreateVideoOverlayAndAddToMap(string video, Map map, LatLngBounds bounds, VideoOverlayOptions? options)
        {
            VideoOverlay videoOverlay = await CreateVideoOverlay(video, bounds, options);
            await videoOverlay.AddTo(map);
            return videoOverlay;
        }

        #endregion

        #region Image Overlay 

        public async Task<ImageOverlay> CreateImageOverlay(string imageUrl, LatLngBounds bounds, ImageOverlayOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createImageOverlayJsFunction, imageUrl, bounds.ToLatLng(), options);
            return new ImageOverlay(jsReference, _eventedJsInterop);
        }

        public async Task<ImageOverlay> CreateImageOverlayAndAddToMap(string imageUrl, Map map, LatLngBounds bounds, ImageOverlayOptions? options)
        {
            var imageOverlay = await CreateImageOverlay(imageUrl, bounds, options);
            await imageOverlay.AddTo(map);
            return imageOverlay;
        }

        #endregion

        #region Polyline

        public async Task<Polyline> CreatePolyline(IEnumerable<LatLng> latLngs, PolylineOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createPolylineJsFunction, latLngs, options);
            return new Polyline(jsReference, _eventedJsInterop);
        }

        public async Task<Polyline> CreatePolylineAndAddToMap(IEnumerable<LatLng> latLngs, Map map, PolylineOptions? options)
        {
            var polyline = await CreatePolyline(latLngs, options);
            await polyline.AddTo(map);
            return polyline;
        }

        #endregion

        #region Polygone

        public async Task<Polygon> CreatePolygon(IEnumerable<LatLng> latLngs, PolygonOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createPolygonJsFunction, latLngs, options);
            return new Polygon(jsReference, _eventedJsInterop);
        }

        public async Task<Polygon> CreatePolygonAndAddToMap(IEnumerable<LatLng> latLngs, Map map, PolygonOptions? options)
        {
            var polygone = await CreatePolygon(latLngs, options);
            await polygone.AddTo(map);
            return polygone;
        }

        #endregion

        #region Rectangle
        public async Task<Rectangle> CreateRectangle(LatLngBounds latLngBounds, RectangleOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createRectangleJsFunction, latLngBounds.ToLatLng(), options);
            return new Rectangle(jsReference, _eventedJsInterop);
        }

        public async Task<Rectangle> CreateRectangleAndAddToMap(LatLngBounds latLngBounds, Map map, RectangleOptions? options)
        {
            var rectangle = await CreateRectangle(latLngBounds, options);
            await rectangle.AddTo(map);
            return rectangle;
        }


        #endregion

        #region Circle
        public async Task<Circle> CreateCircle(LatLng latLng, CircleOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createCircleJsFunction, latLng, options);
            return new Circle(jsReference, _eventedJsInterop);
        }

        public async Task<Circle> CreateCircleAndAddToMap(LatLng latLng, Map map, CircleOptions? options)
        {
            var circle = await CreateCircle(latLng, options);
            await circle.AddTo(map);
            return circle;
        }
        #endregion

        #region Create Circle Marker

        public async Task<CircleMarker> CreateCircleMarker(LatLng latLng, CircleMarkerOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createCircleMarkerJsFunction, latLng, options);
            return new CircleMarker(jsReference, _eventedJsInterop);
        }

        public async Task<CircleMarker> CreateCircleMarkerAndAddToMap(LatLng latLng, Map map, CircleMarkerOptions? options)
        {
            var circleMarker = await CreateCircleMarker(latLng, options);
            await circleMarker.AddTo(map);
            return circleMarker;
        }

        #endregion

        #region GeoJSON layer

        public async Task<GeoJSONLayer> CreateGeoJSONLayer(object geojson, GeoJSONOptions? options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createGeoJSONLayerJsFunction, geojson, options);
            return new GeoJSONLayer(jsReference);
        }

        public async Task<GeoJSONLayer> CreateGeoJSONLayerAndAddToMap(object geojson, Map map, GeoJSONOptions? options)
        {
            var geoJson = await CreateGeoJSONLayer(geojson, options);
            await geoJson.AddTo(map);
            return geoJson;
        }

        #endregion
    }
}
