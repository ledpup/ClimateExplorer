namespace DPBlazorMapLibrary;

public class IconOptions
{
    /// <summary>
    /// (required) The URL to the icon image (absolute or relative to your script path).
    /// </summary>
    public string? IconUrl { get; init; } = null;

    /// <summary>
    /// The URL to a retina sized version of the icon image (absolute or relative to your script path). Used for Retina screen devices.
    /// </summary>
    public string? IconRetinaUrl { get; init; } = null;

    /// <summary>
    /// Size of the icon image in pixels.
    /// </summary>
    public Point? IconSize { get; init; } = null;

    /// <summary>
    /// The coordinates of the "tip" of the icon (relative to its top left corner). The icon will be aligned so that this point is at the marker's geographical location. Centered by default if size is specified, also can be set in CSS with negative margins.
    /// </summary>
    public Point? IconAnchor { get; init; } = null;

    /// <summary>
    /// The coordinates of the point from which popups will "open", relative to the icon anchor.
    /// </summary>
    public Point PopupAnchor { get; init; } = new Point(0, 0);
    /// <summary>
    /// The coordinates of the point from which tooltips will "open", relative to the icon anchor.
    /// </summary>
    public Point? TooltipAnchor { get; init; } = new Point(0, 0);
    /// <summary>
    /// The URL to the icon shadow image. If not specified, no shadow image will be created.
    /// </summary>
    public string? ShadowUrl { get; init; } = null;

    /// <summary>
    /// ?
    /// </summary>
    public string? ShadowRetinaUrl { get; init; } = null;

    /// <summary>
    /// Size of the shadow image in pixels.
    /// </summary>
    public Point? ShadowSize { get; init; } = null;

    /// <summary>
    /// The coordinates of the "tip" of the shadow (relative to its top left corner) (the same as iconAnchor if not specified).
    /// </summary>
    public Point? ShadowAnchor { get; init; } = null;

    /// <summary>
    /// A custom class name to assign to both icon and shadow images. Empty by default.
    /// </summary>
    public string? ClassName { get; init; } = null;
}
