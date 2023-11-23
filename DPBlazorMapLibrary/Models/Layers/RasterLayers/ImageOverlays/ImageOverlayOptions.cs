namespace DPBlazorMapLibrary;

public class ImageOverlayOptions : InteractiveLayerOptions
{
    public ImageOverlayOptions()
    {
        Interactive = true;
    }
    /// <summary>
    /// The opacity of the image overlay.
    /// </summary>
    public double Opacity { get; set; } = 1d;

    /// <summary>
    /// Text for the alt attribute of the image (useful for accessibility).
    /// </summary>
    public string? Alt { get; set; } = null;

    /// <summary>
    /// Whether the crossOrigin attribute will be added to the image. If a String is provided, the image will have its crossOrigin attribute set to the String provided.
    /// This is needed if you want to access image pixel data. Refer to CORS Settings for valid String values.
    /// </summary>
    public bool? CrossOrigin { get; set; } = false;

    /// <summary>
    /// URL to the overlay image to show in place of the overlay that failed to load.
    /// </summary>
    public string? ErrorOverlayUrl { get; set; } = null;

    /// <summary>
    /// The explicit zIndex of the overlay layer.
    /// </summary>
    public int ZIndex { get; set; } = 1;

    /// <summary>
    /// A custom class name to assign to the image. Empty by default.
    /// </summary>
    public string? ClassName { get; set; } = null;
}
