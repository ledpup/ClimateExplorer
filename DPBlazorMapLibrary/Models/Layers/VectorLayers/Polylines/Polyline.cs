using DPBlazorMapLibrary.JsInterops.Events;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// A class for drawing polyline overlays on a map. Extends Path.
    /// </summary>
    public class Polyline : Path
    {
        private const string _toGeoJSONJsFunction = "toGeoJSON";
        private const string _getLatLngsJsFunction = "getLatLngs";
        private const string _setLatLngsJsFunction = "setLatLngs";
        private const string _isEmptyJsFunction = "isEmpty";
        private const string _getCenterJsFunction = "getCenter";
        private const string _addLatLngJsFunction = "addLatLng";

        public Polyline(IJSObjectReference jsReference, IEventedJsInterop eventedJsInterop)
        {
            EventedJsInterop = eventedJsInterop;
            JsReference = jsReference;
        }

        /// <summary>
        /// Precision is the number of decimal places for coordinates. The default value is 6 places. Returns a GeoJSON representation of the polyline (as a GeoJSON LineString or MultiLineString Feature).
        /// </summary>
        /// <returns></returns>
        public async Task<object> ToGeoJSON(int precision = 6)
        {
            return await JsReference.InvokeAsync<object>(_toGeoJSONJsFunction, precision);
        }

        /// <summary>
        /// Returns an array of the points in the path, or nested arrays of points in case of multi-polyline.
        /// </summary>
        /// <returns></returns>
        public async Task<LatLng[]> GetLatLngs()
        {
            return await JsReference.InvokeAsync<LatLng[]>(_getLatLngsJsFunction);
        }

        /// <summary>
        /// Replaces all the points in the polyline with the given array of geographical points.
        /// </summary>
        /// <param name="latLngs"></param>
        /// <returns></returns>
        public async Task<Polyline> SetLatLngs(IEnumerable<LatLng> latLngs)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_setLatLngsJsFunction, latLngs);
            return this;
        }

        /// <summary>
        /// Returns true if the Polyline has no LatLngs.
        /// </summary>
        /// <returns>true if the Polyline has no LatLngs</returns>
        public async Task<bool> IsEmpty()
        {
            return await JsReference.InvokeAsync<bool>(_isEmptyJsFunction);
        }

        //TODO: closestLayerPoint(<Point> p)	

        /// <summary>
        /// Returns the center (centroid) of the polyline.
        /// </summary>
        /// <returns>lat lng</returns>
        public async Task<LatLng> GetCenter()
        {
            return await JsReference.InvokeAsync<LatLng>(_getCenterJsFunction);
        }

        /// <summary>
        /// Returns the LatLngBounds of the path.
        /// </summary>
        /// <returns></returns>
        public override Task<LatLngBounds> GetBounds()
        {
            return base.GetBounds();
        }

        /// <summary>
        /// Adds a given point to the polyline. By default, adds to the first ring of the polyline in case of a multi-polyline, but can be overridden by passing a specific ring as a LatLng array (that you can earlier access with getLatLngs).
        /// </summary>
        /// <param name="latLng"></param>
        /// <returns></returns>
        public async Task<Polyline> AddLatLng(LatLng latLng)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_addLatLngJsFunction, latLng);
            return this;
        }

        /// <summary>
        /// Adds a given point to the polyline. By default, adds to the first ring of the polyline in case of a multi-polyline, but can be overridden by passing a specific ring as a LatLng array (that you can earlier access with getLatLngs).
        /// </summary>
        /// <param name="latLng"></param>
        /// <param name="latLngs"></param>
        /// <returns>this</returns>
        public async Task<Polyline> AddLatLng(LatLng latLng, IEnumerable<LatLng> latLngs)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_addLatLngJsFunction, latLng, latLngs);
            return this;
        }
    }
}
