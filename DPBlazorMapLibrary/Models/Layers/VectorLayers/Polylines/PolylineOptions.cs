namespace DPBlazorMapLibrary
{
    public class PolylineOptions : PathOptions
    {
        /// <summary>
        /// How much to simplify the polyline on each zoom level.
        /// More means better performance and smoother look, and less means more accurate representation.
        /// </summary>
        public double SmoothFactor { get; init; } = 1d;

        /// <summary>
        /// Disable polyline clipping.
        /// </summary>
        public bool NoClip { get; init; } = false;
    }
}
