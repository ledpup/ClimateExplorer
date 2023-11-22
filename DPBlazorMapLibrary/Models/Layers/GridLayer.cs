using System.Drawing;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// Generic class for handling a tiled grid of HTML elements.
    /// This is the base class for all tile layers and replaces TileLayer.Canvas.
    /// GridLayer can be extended to create a tiled grid of HTML elements like <canvas>, <img> or <div>.
    /// GridLayer will handle creating and animating these DOM elements for you.
    /// </summary>
    public abstract class GridLayer : Layer
    {
       
    }
}
