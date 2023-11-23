using Microsoft.JSInterop;
using System.Text.Json.Serialization;

namespace DPBlazorMapLibrary;

public class MarkerOptions : InteractiveLayerOptions
{
    public MarkerOptions()
    {
        BubblingMouseEvents = false;
        Pane = "markerPane";
    }

    private Icon? _iconRef;

    [JsonIgnore]
    public Icon IconRef
    {
        get => _iconRef!;
        init
        {
            _iconRef = value;
            if (value != null)
            {
                Icon = _iconRef.JsReference;
            }
            else
            {
                Icon = null;
            }
        }
    }

    /// <summary>
    /// Icon instance to use for rendering the marker. See Icon documentation for details on how to customize the marker icon. If not specified, a common instance of L.Icon.Default is used.
    /// </summary>
    public IJSObjectReference? Icon { get; init; }

    /// <summary>
    /// Whether the marker can be tabbed to with a keyboard and clicked by pressing enter.
    /// </summary>
    public bool? Keyboard { get; init; } = true;

    /// <summary>
    /// Text for the browser tooltip that appear on marker hover (no tooltip by default).
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Text for the alt attribute of the icon image (useful for accessibility).
    /// </summary>
    public string? Alt { get; init; }

    /// <summary>
    /// By default, marker images zIndex is set automatically based on its latitude. Use this option if you want to put the marker on top of all others (or below), specifying a high value like 1000 (or high negative value, respectively).
    /// </summary>
    public int? ZIndexOffset { get; init; } = 0;

    /// <summary>
    /// The opacity of the marker.
    /// </summary>
    public double? Opacity { get; init; } = 1d;

    /// <summary>
    /// If true, the marker will get on top of others when you hover the mouse over it.
    /// </summary>
    public bool? RiseOnHover { get; init; } = false;

    /// <summary>
    /// The z-index offset used for the riseOnHover feature.
    /// </summary>
    public int? RiseOffset { get; init; } = 250;

    /// <summary>
    /// Map pane where the markers shadow will be added.
    /// </summary>
    public string? ShadowPane { get; init; } = "shadowPane";


    #region Draggable marker options

    /// <summary>
    /// Whether the marker is draggable with mouse/touch or not.
    /// </summary>
    public bool? Draggable { get; init; }

    /// <summary>
    /// Whether to pan the map when dragging this marker near its edge or not.
    /// </summary>
    public bool? AutoPan { get; init; } = false;

    /// <summary>
    /// Distance (in pixels to the left/right and to the top/bottom) of the map edge to start panning the map.
    /// </summary>
    public Point? AutoPanPadding { get; init; } = new Point(50, 50);

    /// <summary>
    /// Number of pixels the map should pan by.
    /// </summary>
    public int? AutoPanSpeed { get; init; } = 10;

    #endregion
}
