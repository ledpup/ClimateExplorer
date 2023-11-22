using DPBlazorMapLibrary.JsInterops.Events;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// A class for drawing polygon overlays on a map. Extends Polyline.
    /// 
    /// Note that points you pass when creating a polygon shouldn't have an additional last point equal to the first one — it's better to filter out such points.
    /// </summary>
    public class Polygon : Polyline
    {
        public Polygon(IJSObjectReference jsReference, IEventedJsInterop eventedJsInterop)
            : base(jsReference, eventedJsInterop)
        {
        }
    }
}
