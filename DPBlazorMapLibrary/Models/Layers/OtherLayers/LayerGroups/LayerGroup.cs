using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    public class LayerGroup : Layer
    {
        private const string _addLayerJsFunction = "addLayer";
        private const string _removeLayerJsFunction = "removeLayer";
        private const string _hasLayerJsFunction = "hasLayer";

        //TODO: clearLayers
        //TODO: invoke
        //TODO: eachLayer
        //TODO: getLayer
        //TODO: getLayers
        //TODO: setZIndex
        //TODO: getLayerId

        public LayerGroup(IJSObjectReference jsReference)
        {
            JsReference = jsReference;
        }

        /// <summary>
        /// Adds the given layer to the group.
        /// </summary>
        /// <returns></returns>
        public async Task<IJSObjectReference> AddLayer(Layer layer)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_addLayerJsFunction, layer);
        }

        /// <summary>
        /// Removes the given layer from the group.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public async Task<IJSObjectReference> RemoveLayer(Layer layer)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_removeLayerJsFunction, layer);
        }

        /// <summary>
        /// Removes the layer with the given internal ID from the group.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IJSObjectReference> RemoveLayer(int id)
        {
            return await JsReference.InvokeAsync<IJSObjectReference>(_removeLayerJsFunction, id);
        }

        /// <summary>
        ///  Returns true if the given layer is currently added to the group.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public async Task<bool> HasLayer(Layer layer)
        {
            return await JsReference.InvokeAsync<bool>(_hasLayerJsFunction, layer);
        }

        /// <summary>
        /// Returns true if the given internal ID is currently added to the group.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> HasLayer(int id)
        {
            return await JsReference.InvokeAsync<bool>(_hasLayerJsFunction, id);
        }
    }
}
