using DPBlazorMapLibrary.JsInterops.Events;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary;

public class Evented : JsReferenceBase
{
    #region Js functions name
    private const string _clickJsFunction = "click";
    private const string _dblClickJsFunction = "dblclick";
    private const string _mouseDownJsFunction = "mousedown";
    private const string _mouseUpJsFunction = "mouseup";
    private const string _mouseOverJsFunction = "mouseover";
    private const string _mouseOutJsFunction = "mouseout";
    private const string _contextMenuJsFunction = "contextmenu";
    private const string _offJsFunction = "off";
    #endregion

    protected IEventedJsInterop? EventedJsInterop;
    private readonly IDictionary<string, Func<MouseEvent, Task>> MouseEvents = new Dictionary<string, Func<MouseEvent, Task>>();

    public async Task OnClick(Func<MouseEvent, Task> callback)
    {
        await On(_clickJsFunction, callback);
    }

    public async Task OnDblClick(Func<MouseEvent, Task> callback)
    {
        await On(_dblClickJsFunction, callback);
    }

    public async Task OnMouseDown(Func<MouseEvent, Task> callback)
    {
        await On(_mouseDownJsFunction, callback);
    }

    public async Task OnMouseUp(Func<MouseEvent, Task> callback)
    {
        await On(_mouseUpJsFunction, callback);
    }

    public async Task OnMouseOver(Func<MouseEvent, Task> callback)
    {
        await On(_mouseOverJsFunction, callback);
    }

    public async Task OnMouseOut(Func<MouseEvent, Task> callback)
    {
        await On(_mouseOutJsFunction, callback);
    }

    public async Task OnContextMenu(Func<MouseEvent, Task> callback)
    {
        await On(_contextMenuJsFunction, callback);
    }

    private async Task On(string eventType, Func<MouseEvent, Task> callback)
    {
        if (this.MouseEvents.ContainsKey(eventType))
        {
            return;
        }

        this.MouseEvents.Add(eventType, callback);
        await this.On(eventType);
    }

    private async Task On(string eventType)
    {
        DotNetObjectReference<Evented> eventedClass = DotNetObjectReference.Create(this);
        await this.EventedJsInterop!.OnCallback(eventedClass, this.JsReference, eventType);
    }

    public async Task Off(string eventType)
    {
        if (this.MouseEvents.ContainsKey(eventType))
        {
            this.MouseEvents.Remove(eventType);
            await this.JsReference.InvokeAsync<IJSObjectReference>(_offJsFunction, eventType);
        }
    }

    [JSInvokable]
    public async Task OnCallback(string eventType, MouseEvent mouseEvent)
    {
        bool isEvented = this.MouseEvents.TryGetValue(eventType, out Func<MouseEvent, Task>? callback);
        if (isEvented)
        {
            await callback!.Invoke(mouseEvent);
        }
    }
}
