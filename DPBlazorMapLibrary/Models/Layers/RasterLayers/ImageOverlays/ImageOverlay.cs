using DPBlazorMapLibrary.JsInterops.Events;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// Used to load and display a single image over specific bounds of the map. Extends Layer.
    /// </summary>
    public class ImageOverlay : InteractiveLayer
    {
        private const string _setOpacityJsFunction = "setOpacity";
        private const string _bringToFrontJsFunction = "bringToFront";
        private const string _bringToBackJsFunction = "bringToBack";
        private const string _setUrlJsFunction = "setUrl";
        private const string _setBoundsJsFunction = "setBounds";
        private const string _setZIndexJsFunction = "setZIndex";
        private const string _getBoundsJsFunction = "getBounds";
        private const string _getElementJsFunction = "getElement";

        //TODO: Add events load, error

        public ImageOverlay(IJSObjectReference jsReference, IEventedJsInterop eventedJsInterop)
        {
            JsReference = jsReference;
            EventedJsInterop = eventedJsInterop;
        }

        /// <summary>
        /// Sets the opacity of the overlay.
        /// </summary>
        /// <param name="opacity"></param>
        /// <returns></returns>
        public async Task<IJSObjectReference> SetOpacity(double opacity)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_setOpacityJsFunction, opacity);
        }

        /// <summary>
        /// Brings the layer to the top of all overlays.
        /// </summary>
        /// <returns></returns>
        public async Task<IJSObjectReference> BringToFront()
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_bringToFrontJsFunction);
        }

        /// <summary>
        /// Brings the layer to the bottom of all overlays.
        /// </summary>
        /// <returns></returns>
        public async Task<IJSObjectReference> BringToBack()
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_bringToBackJsFunction);
        }

        /// <summary>
        /// Changes the URL of the image.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<IJSObjectReference> SetUrl(string url)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_setUrlJsFunction, url);
        }

        /// <summary>
        /// Update the bounds that this ImageOverlay covers
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public async Task<IJSObjectReference> SetBounds(LatLngBounds bounds)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_setBoundsJsFunction, bounds);
        }

        /// <summary>
        /// Changes the zIndex of the image overlay.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<IJSObjectReference> SetZIndex(int value)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_setZIndexJsFunction, value);
        }

        //TODO: getBounds()

        //TODO: getElement()
    }
}
