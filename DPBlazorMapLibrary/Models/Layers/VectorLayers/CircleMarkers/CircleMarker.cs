using DPBlazorMapLibrary.JsInterops.Events;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// A circle of a fixed size with radius specified in pixels. Extends Path.
    /// </summary>
    public class CircleMarker : Path
    {
        private const string _toGeoJSONJsFunction = "toGeoJSON";
        private const string _setLatLngJsFunction = "setLatLng";
        private const string _getLatLngJsFunction = "getLatLng";
        private const string _setRadiusJsFunction = "setRadius";
        private const string _getRadiusJsFunction = "getRadius";

        public CircleMarker(IJSObjectReference jsReference, IEventedJsInterop eventedJsInterop)
        {
            JsReference = jsReference;
            EventedJsInterop = eventedJsInterop;
        }

        /// <summary>
        /// precision is the number of decimal places for coordinates. The default value is 6 places. Returns a GeoJSON representation of the circle marker (as a GeoJSON Point Feature).
        /// </summary>
        /// <param name="precision"></param>
        /// <returns></returns>
        public async Task<object> ToGeoJSON(int precision = 6)
        {
            return await this.JsReference.InvokeAsync<object>(_toGeoJSONJsFunction, precision);
        }

        /// <summary>
        /// Sets the position of a circle marker to a new location.
        /// </summary>
        /// <param name="latLng"></param>
        /// <returns></returns>
        public async Task<CircleMarker> SetLatLng(LatLng latLng)
        {
            await this.JsReference.InvokeAsync<IJSObjectReference>(_setLatLngJsFunction, latLng);
            return this;
        }

        /// <summary>
        /// Returns the current geographical position of the circle marker
        /// </summary>
        /// <returns></returns>
        public async Task<LatLng> GetLatLng()
        {
            return await this.JsReference.InvokeAsync<LatLng>(_getLatLngJsFunction);
        }

        /// <summary>
        /// Sets the radius of a circle marker. Units are in pixels.
        /// </summary>
        /// <param name="radius"></param>
        /// <returns></returns>
        public async Task<CircleMarker> SetRadius(double radius)
        {
            await this.JsReference.InvokeAsync<IJSObjectReference>(_setRadiusJsFunction, radius);
            return this;
        }

        /// <summary>
        /// Returns the current radius of the circle
        /// </summary>
        /// <returns></returns>
        public async Task<double> GetRadius()
        {
            return await this.JsReference.InvokeAsync<double>(_getRadiusJsFunction);
        }
    }
}
