using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// A set of methods from the Layer base class that all Leaflet layers use.
    /// Inherits all methods, options and events from L.Evented.
    /// </summary>
    public abstract class Layer : Evented
    {
        private const string _addToJsFunction = "addTo";
        private const string _removeJsFunction = "remove";
        private const string _removeFromJsFunction = "removeFrom";
        private const string _bindPopupJsFunction = "bindPopup";
        private const string _unbindPopupJsFunction = "unbindPopup";
        private const string _openPopupJsFunction = "openPopup";
        private const string _closePopupJsFunction = "closePopup";
        private const string _togglePopupJsFunction = "togglePopup";
        private const string _isPopupOpenJsFunction = "isPopupOpen";
        private const string _setPopupContentJsFunction = "setPopupContent";
        private const string _bindTooltipJsFunction = "bindTooltip";
        private const string _unbindTooltipJsFunction = "unbindTooltip";
        private const string _openTooltipJsFunction = "openTooltip";
        private const string _closeTooltipJsFunction = "closeTooltip";
        private const string _toggleTooltipJsFunction = "toggleTooltip";
        private const string _isTooltipOpenJsFunction = "isTooltipOpen";
        private const string _setTooltipContentJsFunction = "setTooltipContent";

        /// <summary>
        /// Adds the layer to the given map or layer group.
        /// </summary>
        /// <param name="map">current map</param>
        /// <returns>current object</returns>
        public async Task<Layer> AddTo(Map map)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_addToJsFunction, map.MapReference);
            return this;
        }

        /// <summary>
        /// Removes the layer from the map it is currently active on.
        /// </summary>
        /// <returns></returns>
        public async Task<Layer> Remove()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_removeJsFunction);
            await JsReference.DisposeAsync();
            return this;
        }

        /// <summary>
        /// Removes the layer from the given map
        /// </summary>
        /// <param name="map">current map</param>
        /// <returns>this</returns>
        public async Task<Layer> RemoveFrom(Map map)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_removeFromJsFunction, map.MapReference);
            return this;
        }

        //TODO: add RemoveFrom(<LayerGroup> group)

        //TODO: add Extension methods: onAdd, onRemove, getEvents, getAttribution, getAttribution


        #region Popup
        /// <summary>
        /// Binds a popup to the layer with the passed content and sets up the necessary event listeners.
        /// If a Function is passed it will receive the layer as the first argument and should return a String or HTMLElement.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        //TODO:  add <Popup options> options?
        public async Task<Layer> BindPopup(string content)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_bindPopupJsFunction, content);
            return this;
        }

        /// <summary>
        /// Removes the popup previously bound with bindPopup.
        /// </summary>
        /// <returns>this</returns>
        public async Task<Layer> UnbindPopup()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_unbindPopupJsFunction);
            return this;
        }

        /// <summary>
        /// Opens the bound popup at the specified latlng or at the default popup anchor if no latlng is passed.
        /// </summary>
        /// <param name="latLng"></param>
        /// <returns>this</returns>
        public async Task<Layer> OpenPopup(LatLng? latLng)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_openPopupJsFunction, latLng);
            return this;
        }

        /// <summary>
        /// Closes the popup bound to this layer if it is open.
        /// </summary>
        /// <returns></returns>
        public async Task<Layer> ClosePopup()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_closePopupJsFunction);
            return this;
        }

        /// <summary>
        /// Opens or closes the popup bound to this layer depending on its current state.
        /// </summary>
        /// <returns></returns>
        public async Task<Layer> TogglePopup()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_togglePopupJsFunction);
            return this;
        }

        /// <summary>
        /// Returns true if the popup bound to this layer is currently open.
        /// </summary>
        /// <returns>true if the popup bound to this layer is currently open</returns>
        public async Task<bool> IsPopupOpen()
        {
            return await JsReference.InvokeAsync<bool>(_isPopupOpenJsFunction);
        }

        /// <summary>
        /// Sets the content of the popup bound to this layer.
        /// </summary>
        /// <param name="content"><String|HTMLElement|Popup></param>
        /// <returns>this</returns>
        public async Task<Layer> SetPopupContent(string content)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_setPopupContentJsFunction, content);
            return this;
        }

        //TODO: getPopup()

        #endregion

        #region Tooltip
        /// <summary>
        /// Binds a tooltip to the layer with the passed content and sets up the necessary event listeners. If a Function is passed it will receive the layer as the first argument and should return a String or HTMLElement.
        /// </summary>
        /// <param name="content"><String|HTMLElement|Function|Tooltip> content</param>
        /// <returns>this</returns>
        //TODO: <Tooltip options> options?
        public async Task<Layer> BindTooltip(string content)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_bindTooltipJsFunction, content);
            return this;
        }

        /// <summary>
        /// Removes the tooltip previously bound with bindTooltip.
        /// </summary>
        /// <returns>this</returns>
        public async Task<Layer> UnbindTooltip()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_unbindTooltipJsFunction);
            return this;
        }

        /// <summary>
        /// Opens the bound tooltip at the specified latlng or at the default tooltip anchor if no latlng is passed.
        /// </summary>
        /// <param name="latLng"></param>
        /// <returns>this</returns>
        public async Task<Layer> OpenTooltip(LatLng? latLng)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_openTooltipJsFunction, latLng);
            return this;
        }

        /// <summary>
        /// Closes the tooltip bound to this layer if it is open.
        /// </summary>
        /// <returns>this</returns>
        public async Task<Layer> CloseTooltip()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_closeTooltipJsFunction);
            return this;
        }

        /// <summary>
        /// Opens or closes the tooltip bound to this layer depending on its current state.
        /// </summary>
        /// <returns>this</returns>
        public async Task<Layer> ToggleTooltip()
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_toggleTooltipJsFunction);
            return this;
        }

        /// <summary>
        /// Returns true if the tooltip bound to this layer is currently open.
        /// </summary>
        /// <returns>true if the tooltip bound to this layer is currently open</returns>
        public async Task<bool> IsTooltipOpen()
        {
            return await JsReference.InvokeAsync<bool>(_isTooltipOpenJsFunction);
        }

        /// <summary>
        /// Sets the content of the tooltip bound to this layer.
        /// </summary>
        /// <param name="content"><String|HTMLElement|Tooltip></param>
        /// <returns>this</returns>
        public async Task<Layer> SetTooltipContent(string content)
        {
            await JsReference.InvokeAsync<IJSObjectReference>(_setTooltipContentJsFunction, content);
            return this;
        }

        //TODO: getTooltip()

        #endregion
    }
}
