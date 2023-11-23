using DPBlazorMapLibrary.JsInterops.Events;
using DPBlazorMapLibrary.JsInterops.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DPBlazorMapLibrary;

public partial class Map
{
    [Inject]
    public IJSRuntime? JsRuntime { get; set; }

    [Inject]
    public IMapJsInterop? MapJsInterop { get; set; }

    [Inject]
    public IEventedJsInterop? EventedJsInterop { get; set; }

    public MapEvented? MapEvented { get; set; }

    [Parameter]
    public string DivId { get; set; } = "mapId";

    [Parameter]
    public string CssClass { get; set; } = "mapClass";


    [Parameter]
    public MapOptions? MapOptions { get; set; }

    [Parameter]
    public EventCallback AfterRender { get; set; }

    public IJSObjectReference? MapReference { get; set; }

    private const string getCenter = "getCenter";
    private const string getZoom = "getZoom";
    private const string getMinZoom = "getMinZoom";
    private const string getMaxZoom = "getMaxZoom";
    private const string setView = "setView";
    private const string setZoom = "setZoom";
    private const string zoomIn = "zoomIn";
    private const string zoomOut = "zoomOut";
    private const string setZoomAround = "setZoomAround";
    private const string invalidateSize = "invalidateSize";
    private const string fitBounds = "fitBounds";
    private const string panTo = "panTo";
    private const string flyTo = "flyTo";
    private const string flyToBounds = "flyToBounds";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            MapReference = await MapJsInterop!.Initialize(DivId, MapOptions!);
            MapEvented = new MapEvented(MapReference, EventedJsInterop!);
            await AfterRender.InvokeAsync();
        }
    }

    public async Task<LatLng> GetCenter()
    {
        return await MapReference!.InvokeAsync<LatLng>(getCenter);
    }

    public async Task<int> GetZoom()
    {
        return await MapReference!.InvokeAsync<int>(getZoom);
    }

    public async Task<int> GetMinZoom()
    {
        return await MapReference!.InvokeAsync<int>(getMinZoom);
    }

    public async Task<int> GetMaxZoom()
    {
        return await MapReference!.InvokeAsync<int>(getMaxZoom);
    }

    /// <summary>
    /// Sets the view of the map (geographical center and zoom) with the given animation options.
    /// </summary>
    /// <param name="latLng"></param>
    /// <param name="zoom"></param>
    /// <returns></returns>
    public async Task SetView(LatLng latLng, int zoom)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(setView, latLng, zoom);
    }

    public async Task SetZoom(int zoom)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(setZoom, zoom);
    }

    public async Task ZoomIn(int zoomDelta)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(zoomIn, zoomDelta);
    }

    public async Task ZoomOut(int zoomDelta)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(zoomOut, zoomDelta);
    }

    public async Task SetZoomAround(LatLng latLng, int zoom)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(setZoomAround, latLng, zoom);
    }

    public async Task InvalidateSize()
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(invalidateSize);
    }

    /// <summary>
    /// Задает вид карты (географический центр и масштабирование), выполняя плавную анимацию панорамирования и масштабирования.
    /// https://leafletjs.com/reference.html#:~:text=of%20pixels%20(animated).-,flyTo,-(%3CLatLng%3E%20latlng%2C%20%3CNumber
    /// </summary>
    /// <param name="latLng">lat lng</param>
    /// <param name="zoom">уровень zoom</param>
    /// <returns></returns>
    public async Task FlyTo(LatLng latLng, int? zoom)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(flyTo, latLng, zoom);
    }

    /// <summary>
    /// Задает вид карты с плавной анимацией, так же как flyTo.
    /// https://leafletjs.com/reference.html#:~:text=pan-zoom%20animation.-,flyToBounds,-(%3CLatLngBounds%3E%20bounds%2C%20%3CfitBounds
    /// </summary>
    /// <param name="latLngBounds">область</param>
    /// <returns></returns>
    public async Task FlyToBounds(LatLngBounds latLngBounds)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(flyToBounds, latLngBounds.ToLatLng());
    }

    /// <summary>
    /// Задает вид карты, содержащий заданные географические границы, с максимально возможным уровнем масштабирования.
    /// https://leafletjs.com/reference.html#:~:text=left%20corner)%20stationary.-,fitBounds,-(%3CLatLngBounds%3E%20bounds%2C%20%3CfitBounds
    /// </summary>
    /// <param name="latLngBounds">область</param>
    public async Task FitBounds(LatLngBounds latLngBounds)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(fitBounds, latLngBounds.ToLatLng());
    }

    /// <summary>
    /// Перемещает карту в заданный центр.
    /// https://leafletjs.com/reference.html#:~:text=zoom%20level%20possible.-,panTo,-(%3CLatLng%3E%20latlng%2C%20%3CPan
    /// </summary>
    /// <param name="latLng">lat lng</param>
    /// <param name="animate">If true, panning will always be animated if possible. If false, it will not animate panning, either resetting the map view if panning more than a screen away, or just setting a new offset for the map pane (except for panBy which always does the latter).</param>
    /// <param name="duration">Duration of animated panning, in seconds.</param>
    /// <param name="easeLinearity">The curvature factor of panning animation easing (third parameter of the Cubic Bezier curve). 1.0 means linear animation, and the smaller this number, the more bowed the curve.</param>
    /// <param name="noMoveStart">If true, panning won't fire movestart event on start (used publicly for panning inertia).</param>
    /// <returns></returns>
    public async Task PanTo(LatLng latLng, bool animate = true, float duration = 0.25f, float easeLinearity = 0.25f, bool noMoveStart = false)
    {
        await MapReference!.InvokeAsync<IJSObjectReference>(panTo, latLng, animate, duration, easeLinearity, noMoveStart);
    }

    public async Task OnClick(Func<MouseEvent, Task> callback)
    {
        await MapEvented!.OnClick(callback);
    }

    public async Task OnDblClick(Func<MouseEvent, Task> callback)
    {
        await MapEvented!.OnDblClick(callback);
    }

    public async Task OnMouseDown(Func<MouseEvent, Task> callback)
    {
        await MapEvented!.OnMouseDown(callback);
    }

    public async Task OnMouseUp(Func<MouseEvent, Task> callback)
    {
        await MapEvented!.OnMouseUp(callback);
    }

    public async Task OnMouseOver(Func<MouseEvent, Task> callback)
    {
        await MapEvented!.OnMouseOver(callback);
    }

    public async Task OnMouseOut(Func<MouseEvent, Task> callback)
    {
        await MapEvented!.OnMouseOut(callback);
    }

    public async Task OnContextMenu(Func<MouseEvent, Task> callback)
    {
        await MapEvented!.OnContextMenu(callback);
    }

    public async Task Off(string eventType)
    {
        await MapEvented!.Off(eventType);
    }
}
