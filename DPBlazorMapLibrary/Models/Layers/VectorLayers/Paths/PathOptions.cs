namespace DPBlazorMapLibrary;

public class PathOptions : InteractiveLayerOptions
{
    public PathOptions()
    {
        BubblingMouseEvents = true;
    }

    /// <summary>
    /// Whether to draw stroke along the path. Set it to false to disable borders on polygons or circles.
    /// </summary>
    public bool? Stroke { get; init; } = true;

    /// <summary>
    /// Stroke color
    /// </summary>
    public string? Color { get; init; } = "#3388ff";

    /// <summary>
    /// Stroke width in pixels
    /// </summary>
    public int? Weight { get; init; } = 3;

    /// <summary>
    /// Stroke opacity
    /// </summary>
    public double? Opacity { get; init; } = 1d;

    /// <summary>
    /// A string that defines shape to be used at the end of the stroke.
    /// </summary>
    public string? LineCap { get; init; } = "round";

    /// <summary>
    /// A string that defines shape to be used at the corners of the stroke.
    /// </summary>
    public string? LineJoin { get; init; } = "round";

    /// <summary>
    /// A string that defines the stroke dash pattern. Doesn't work on Canvas-powered layers in some old browsers.
    /// </summary>
    public string? DashArray { get; init; } = null;

    /// <summary>
    /// A string that defines the distance into the dash pattern to start the dash. Doesn't work on Canvas-powered layers in some old browsers.
    /// </summary>
    public string? DashOffset { get; init; }

    /// <summary>
    /// Whether to fill the path with color. Set it to false to disable filling on polygons or circles.
    /// </summary>
    public bool? Fill { get; init; } = false;

    /// <summary>
    /// Fill color. Defaults to the value of the color option
    /// </summary>
    public string? FillColor { get; init; } = null;

    /// <summary>
    /// Fill opacity.
    /// </summary>
    public double? FillOpacity { get; init; } = 0.2d;

    /// <summary>
    /// A string that defines how the inside of a shape is determined.
    /// </summary>
    public string? FillRule { get; init; } = "evenodd";


    /// <summary>
    /// Custom class name set on an element. Only for SVG renderer.
    /// </summary>
    public string? ClassName { get; init; } = null;
}
