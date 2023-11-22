using Microsoft.JSInterop;

namespace DPBlazorMapLibrary.JsInterops.LeftletDefaultIconFactories
{
    public interface IIconFactoryJsInterop
    {
        ValueTask<IJSObjectReference> CreateDefaultIcon();
    }
}
