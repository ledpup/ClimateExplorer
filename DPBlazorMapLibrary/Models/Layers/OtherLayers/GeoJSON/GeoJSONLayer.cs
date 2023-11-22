using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// Represents a GeoJSON object or an array of GeoJSON objects. Allows you to parse GeoJSON data and display it on the map. Extends FeatureGroup.
    /// </summary>
    public class GeoJSONLayer : FeatureGroup
    {
        public GeoJSONLayer(IJSObjectReference jsReference) : base(jsReference)
        {
        }

    }
}
