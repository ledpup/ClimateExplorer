using DPBlazorMapLibrary.JsInterops.Events;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    public class Marker : InteractiveLayer
    {
        private const string _getLatLngJsFunction = "getLatLng";
        private const string _setLatLngJsFunction = "setLatLng";
        private const string _setZIndexOffsetJsFunction = "setZIndexOffset";
        private const string _getIconJsFunction = "getIcon";
        private const string _setIconJsFunction = "setIcon";
        private const string _setOpacityJsFunction = "setOpacity";

        public Marker(IJSObjectReference jsReference, IEventedJsInterop eventedJsInterop)
        {
            JsReference = jsReference;
            EventedJsInterop = eventedJsInterop;
        }

        public async Task<LatLng> GetLatLng()
        {
            return await JsReference.InvokeAsync<LatLng>(_getLatLngJsFunction);
        }

        public async Task<IJSObjectReference> SetLatLng(LatLng latLng)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_setLatLngJsFunction, latLng);
        }

        public async Task<IJSObjectReference> SetZIndexOffset(int number)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_setZIndexOffsetJsFunction, number);
        }

        public async Task<Icon> GetIcon()
        {
            return await JsReference.InvokeAsync<Icon>(_getIconJsFunction);
        }

        public async Task<IJSObjectReference> SetIcon(Icon icon)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_setIconJsFunction, icon);
        }

        public async Task<IJSObjectReference> SetOpacity(double number)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_setOpacityJsFunction, number);
        }
    }
}
