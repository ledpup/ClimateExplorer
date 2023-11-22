using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPBlazorMapLibrary
{
    /// <summary>
    /// Extended LayerGroup that makes it easier to do the same thing to all its member layers:
    /// </summary>
    public class FeatureGroup : LayerGroup
    {
        //TODO: setStyle
        //TODO: bringToFront
        //TODO: bringToBack
        //TODO: getBounds

        public FeatureGroup(IJSObjectReference jsReference) : base(jsReference)
        {
        }

    }
}
