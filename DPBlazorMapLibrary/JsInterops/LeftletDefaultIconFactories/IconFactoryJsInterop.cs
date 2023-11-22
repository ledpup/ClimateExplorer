using DPBlazorMapLibrary.JsInterops.Base;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary.JsInterops.LeftletDefaultIconFactories
{
    public class IconFactoryJsInterop : BaseJsInterop, IIconFactoryJsInterop
    {
        private static readonly string _jsFilePath = $"{JsInteropConfig.BaseJsFolder}{JsInteropConfig.IconFactoryFile}";
        private const string _createDefaultIconJsFunction = "createDefaultIcon";

        public IconFactoryJsInterop(IJSRuntime jsRuntime) : base(jsRuntime, _jsFilePath)
        {

        }

        public async ValueTask<IJSObjectReference> CreateDefaultIcon()
        {
            IJSObjectReference module = await moduleTask.Value;
            return await module.InvokeAsync<IJSObjectReference>(_createDefaultIconJsFunction);
        }
    }
}
