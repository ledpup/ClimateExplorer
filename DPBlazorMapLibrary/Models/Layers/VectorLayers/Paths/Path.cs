using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// An abstract class that contains options and constants shared between vector overlays (Polygon, Polyline, Circle). Do not use it directly.
    /// </summary>
    public abstract class Path : InteractiveLayer
    {
        private const string _redrawJsFunction = "redraw";
        private const string _setStyleJsFunction = "setStyle";
        private const string _bringToFrontJsFunction = "bringToFront";
        private const string _bringToBackJsFunction = "bringToBack";
        private const string _getBoundsJsFunction = "getBounds";

        /// <summary>
        /// Redraws the layer. Sometimes useful after you changed the coordinates that the path uses.
        /// </summary>
        /// <returns>this</returns>
        public async Task<Path> Redraw()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_redrawJsFunction);
            return this;
        }

        /// <summary>
        /// Changes the appearance of a Path based on the options in the Path options object.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<Path> SetStyle(PathOptions options)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_setStyleJsFunction, options);
            return this;
        }

        /// <summary>
        /// Brings the layer to the top of all path layers.
        /// </summary>
        /// <returns></returns>
        public async Task<Path> BringToFront()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_bringToFrontJsFunction);
            return this;
        }

        /// <summary>
        /// Brings the layer to the bottom of all path layers.
        /// </summary>
        /// <returns></returns>
        public async Task<Path> BringToBack()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_bringToBackJsFunction);
            return this;
        }

        /// <summary>
        /// Leaflet getBounds() function returns  
        /// {"_southWest":{"lat":-23.601783040147975,"lng":-46.537071217637845}, "_northEast":{"lat":-23.556816959852032,"lng":-46.48800878236214}}"
        /// </summary>
        /// <returns>LatLngBounds</returns>
        public virtual async Task<LatLngBounds> GetBounds()
        {

            return await this.JsReference.InvokeAsync<LatLngBounds>(_getBoundsJsFunction);
        }
    }
}
