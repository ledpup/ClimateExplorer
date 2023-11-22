namespace DPBlazorMapLibrary
{
    public class MapOptions
    {
        /// <summary>
        /// Whether Paths should be rendered on a Canvas renderer. By default, all Paths are rendered in a SVG renderer.
        /// </summary>
        public bool PreferCanvas { get; set; } = false;

        #region Control options

        /// <summary>
        /// Whether a attribution control is added to the map by default.
        /// </summary>
        public bool AttributionControl { get; set; } = true;

        /// <summary>
        /// Whether a zoom control is added to the map by default.
        /// </summary>
        public bool ZoomControl { get; set; } = true;

        #endregion

        #region Interaction Options

        /// <summary>
        /// Set it to false if you don't want popups to close when user clicks the map.
        /// </summary>
        public bool ClosePopupOnClick { get; set; } = true;

        /// <summary>
        /// Forces the map's zoom level to always be a multiple of this, particularly right after a fitBounds() or a pinch-zoom. By default, the zoom level snaps to the nearest integer; lower values (e.g. 0.5 or 0.1) allow for greater granularity. A value of 0 means the zoom level will not be snapped after fitBounds or a pinch-zoom.
        /// </summary>
        public int ZoomSnap { get; set; } = 1;

        /// <summary>
        /// Controls how much the map's zoom level will change after a zoomIn(), zoomOut(), pressing + or - on the keyboard, or using the zoom controls. Values smaller than 1 (e.g. 0.5) allow for greater granularity.
        /// </summary>
        public int ZoomDelta { get; set; } = 1;

        /// <summary>
        /// Whether the map automatically handles browser window resize to update itself.
        /// </summary>
        public bool TrackResize { get; set; } = true;

        /// <summary>
        /// Whether the map can be zoomed to a rectangular area specified by dragging the mouse while pressing the shift key.
        /// </summary>
        public bool BoxZoom { get; set; } = true;


        //TODO: doubleClickZoom

        /// <summary>
        /// Whether the map be draggable with mouse/touch or not.
        /// </summary>
        public bool Dragging { get; set; } = true;
        #endregion

        #region Map State Options


        //TODO: add CRS

        /// <summary>
        /// Initial geographic center of the map
        /// </summary>
        public LatLng Center { get; set; } = new LatLng(0, 0);

        /// <summary>
        /// Initial map zoom level
        /// </summary>
        public int Zoom { get; set; } = 15;

        /// <summary>
        /// Array of layers that will be added to the map initially
        /// </summary>
        public Layer[] Layers { get; set; } = Array.Empty<Layer>();

        #endregion

        //TODO: add Animation Options
        //TODO: add Panning Inertia Options
        //TODO: add Keyboard Navigation Options
        //TODO: add Mouse wheel options
        //TODO: add Touch interaction options
    }
}
