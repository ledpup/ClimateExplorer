namespace DPBlazorMapLibrary;

public class MouseEvent : Event
{
    /// <summary>
    /// The geographical point where the mouse event occurred.
    /// </summary>
    public LatLng? LatLng { get; set; }

    /// <summary>
    /// Pixel coordinates of the point where the mouse event occurred relative to the map layer.
    /// </summary>
    public Point? LayerPoint { get; set; }

    /// <summary>
    /// Pixel coordinates of the point where the mouse event occurred relative to the map сontainer.
    /// </summary>
    public Point? ContainerPoint { get; set; }
}
