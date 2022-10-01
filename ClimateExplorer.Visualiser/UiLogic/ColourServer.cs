using Blazorise;
using Microsoft.VisualBasic;

namespace ClimateExplorer.Visualiser.UiLogic;

public class ColourServer
{
    List<Colours> ReservedColours;
    List<Colours> AvailableColours;

    public ColourServer()
    {
        AvailableColours = new List<Colours>();
        ReservedColours = new List<Colours>();
        SetupAvailableColours();
    }

    private void SetupAvailableColours()
    {
        foreach (var colour in (Colours[])Enum.GetValues(typeof(Colours)))
        {
            if (colour == Colours.AutoAssigned)
            {
                continue;
            }
            AvailableColours.Add(colour);
        }
    }

    public string GetNextColour(Colours requestedColour, List<Colours> requestedColours)
    {
        requestedColours.Remove(requestedColour);
        if (requestedColour != Colours.AutoAssigned)
        {
            if (!ReservedColours.Contains(requestedColour))
            {
                ReservedColours.Add(requestedColour);
            }
            else
            {
                requestedColour = Colours.AutoAssigned;
            }
        }

        if (AvailableColours.Count == 0)
        {
            // Reset the list because we've run out of colours
            SetupAvailableColours();
        }

        var nextColour = AvailableColours.First(x => (requestedColour == Colours.AutoAssigned && !requestedColours.Contains(x)) || x == requestedColour);
        AvailableColours.Remove(nextColour);
        return set1Modified[nextColour];
    }

    Dictionary<Colours, string> set1Modified = new Dictionary<Colours, string>
    {
        { Colours.Red, "#e41a1c" },
        { Colours.Blue, "#377eb8" },
        { Colours.Green, "#4daf4a" },
        { Colours.Purple, "#984ea3" },
        { Colours.Black, "#000000" },
        { Colours.Orange, "#ff7f00" },
        { Colours.Yellow, "#ffff33" },
        { Colours.Brown, "#a65628" },
        { Colours.Pink, "#f781bf" },
        { Colours.Grey, "#999999" },
    };

    // https://observablehq.com/@d3/color-schemes
    string[] category10 = new[] { "#4e79a7", "#f28e2c", "#e15759", "#76b7b2", "#59a14f", "#edc949", "#af7aa1", "#ff9da7", "#9c755f", "#bab0ab" };
    string[] tableau10 = new[] { "#4e79a7", "#f28e2c", "#e15759", "#76b7b2", "#59a14f", "#edc949", "#af7aa1", "#ff9da7", "#9c755f", "#bab0ab" };
    string[] set1 = new[] { "#e41a1c", "#377eb8", "#4daf4a", "#984ea3", "#ff7f00", "#ffff33", "#a65628", "#f781bf", "#999999" };
    string[] paired = new[] { "#a6cee3", "#1f78b4", "#b2df8a", "#33a02c", "#fb9a99", "#e31a1c", "#fdbf6f", "#ff7f00", "#cab2d6", "#6a3d9a", "#ffff99", "#b15928" };
}

public enum Colours
{
    AutoAssigned,
    Red,
    Blue,
    Green,
    Purple,
    Black,
    Orange,
    Yellow,
    Brown,
    Pink,
    Grey,
}