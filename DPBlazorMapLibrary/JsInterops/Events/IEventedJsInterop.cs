using Microsoft.JSInterop;

namespace DPBlazorMapLibrary.JsInterops.Events
{
    public interface IEventedJsInterop
    {
        ValueTask OnCallback(DotNetObjectReference<Evented> eventedClass, IJSObjectReference eventedReference, string eventType);
    }
}
