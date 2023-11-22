using DPBlazorMapLibrary.JsInterops.LeftletDefaultIconFactories;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    public class IconFactory : IIconFactory
    {
        private const string _createIconJsFunction = "L.icon";

        private readonly IIconFactoryJsInterop _iconFactoryJsInterop;
        private readonly IJSRuntime _jsRuntime;
        public IconFactory(
            IJSRuntime jsRuntime,
            IIconFactoryJsInterop iconFactoryJsInterop)
        {
            _jsRuntime = jsRuntime;
            _iconFactoryJsInterop = iconFactoryJsInterop;
        }

        #region Icon
        public async Task<Icon> Create(IconOptions options)
        {
            IJSObjectReference jsReference = await _jsRuntime.InvokeAsync<IJSObjectReference>(_createIconJsFunction, options);
            return new Icon(jsReference);
        }

        public async Task<Icon> CreateDefault()
        {
            IJSObjectReference jsReference = await _iconFactoryJsInterop.CreateDefaultIcon();
            return new Icon(jsReference);
        }
        #endregion
    }
}
