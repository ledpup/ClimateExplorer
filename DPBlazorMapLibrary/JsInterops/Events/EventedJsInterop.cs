using DPBlazorMapLibrary.JsInterops.Base;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary.JsInterops.Events
{
    public class EventedJsInterop : BaseJsInterop, IEventedJsInterop
    {
        private static readonly string _jsFilePath = $"{JsInteropConfig.BaseJsFolder}{JsInteropConfig.EventedFile}";
        private const string _onCallbackJsFanction = "onCallback";

        public EventedJsInterop(IJSRuntime jsRuntime) : base(jsRuntime, _jsFilePath)
        {

        }

        public async ValueTask OnCallback(
            DotNetObjectReference<Evented> eventedClass,
            IJSObjectReference evented,
            string eventType)
        {
            IJSObjectReference module = await moduleTask.Value;
            await module.InvokeVoidAsync(_onCallbackJsFanction, eventedClass, evented, eventType);
        }
    }
}
