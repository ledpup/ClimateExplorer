using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// Used to load and display tile layers on the map. Note that most tile servers require attribution, which you can set under Layer.
    /// </summary>
    public class TileLayer : GridLayer
    {
        public TileLayer(IJSObjectReference jsReference)
        {
            JsReference = jsReference;
        }
    }
}
