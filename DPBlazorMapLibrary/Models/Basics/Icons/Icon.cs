using Microsoft.JSInterop;

namespace DPBlazorMapLibrary
{
    public class Icon : JsReferenceBase
    {
        public Icon(IJSObjectReference jsReference)
        {
            JsReference = jsReference;
        }
    }
}
