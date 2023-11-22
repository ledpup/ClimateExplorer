using DPBlazorMapLibrary.JsInterops.Events;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// A class for drawing rectangle overlays on a map. Extends Polygon.
    /// </summary>
    public class Rectangle : Polygon
    {
        public Rectangle(IJSObjectReference jsReference, IEventedJsInterop eventedJsInterop)
            : base(jsReference, eventedJsInterop)
        {
        }

        public override Task<LatLngBounds> GetBounds()
        {
            return base.GetBounds();
        }
    }
}
