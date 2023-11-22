namespace DPBlazorMapLibrary
{
    public class VideoOverlayOptions : ImageOverlayOptions
    {
        /// <summary>
        /// Whether the video starts playing automatically when loaded.
        /// </summary>
        public bool Autoplay { get; set; } = true;

        /// <summary>
        /// Whether the video will loop back to the beginning when played.
        /// </summary>
        public bool Loop = true;

        /// <summary>
        /// Whether the video will save aspect ratio after the projection. Relevant for supported browsers. See browser compatibility
        /// </summary>
        public bool KeepAspectRatio = true;

        /// <summary>
        /// Whether the video starts on mute when loaded.
        /// </summary>
        public bool Muted { get; set; } = false;
    }
}
