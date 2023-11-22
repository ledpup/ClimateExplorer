namespace DPBlazorMapLibrary
{
    public class InteractiveLayerOptions : LayerOptions
    {
        /// <summary>
        /// If false, the layer will not emit mouse events and will act as a part of the underlying map.
        /// </summary>
        public bool Interactive { get; set; } = true;

        /// <summary>
        /// When true, a mouse event on this layer will trigger the same event on the map (unless L.DomEvent.stopPropagation is used).
        /// </summary>
        public bool BubblingMouseEvents { get; set; } = true;
    }
}
