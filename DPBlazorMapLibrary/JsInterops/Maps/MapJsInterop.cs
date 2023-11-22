using DPBlazorMapLibrary.JsInterops.Base;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary.JsInterops.Maps
{
    public class MapJsInterop : BaseJsInterop, IMapJsInterop
    {
        private static readonly string _jsFilePath = $"{JsInteropConfig.BaseJsFolder}{JsInteropConfig.MapFile}";
        private const string _initializeJsFunction = "initialize";


        public MapJsInterop(IJSRuntime jsRuntime) : base(jsRuntime, _jsFilePath)
        {

        }

        public async ValueTask<IJSObjectReference> Initialize(string id, MapOptions mapOptions)
        {
            IJSObjectReference module = await moduleTask.Value;
            return await module.InvokeAsync<IJSObjectReference>(_initializeJsFunction, id, mapOptions);
        }
    }
}
