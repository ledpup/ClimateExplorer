using DPBlazorMapLibrary.JsInterops.Base;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary.JsInterops.Maps
{
    public interface IMapJsInterop : IBaseJsInterop
    {
        ValueTask<IJSObjectReference> Initialize(string id, MapOptions mapOptions);
    }
}
