namespace ClimateExplorer.Web.UiLogic;

public class ColourServer
{
    private readonly List<Colours> reservedColours;
    private readonly List<Colours> availableColours;

    private readonly Dictionary<Colours, string> set1Modified = new Dictionary<Colours, string>
    {
        { Colours.Red, "#e41a1c" },
        { Colours.Blue, "#377eb8" },
        { Colours.Green, "#4daf4a" },
        { Colours.Purple, "#984ea3" },
        { Colours.Black, "#000000" },
        { Colours.Orange, "#ff7f00" },
        { Colours.Yellow, "#e3bd0d" },
        { Colours.Brown, "#a65628" },
        { Colours.Pink, "#f781bf" },
        { Colours.Grey, "#999999" },
    };

    // https://observablehq.com/@d3/color-schemes
    private readonly string[] category10 = ["#4e79a7", "#f28e2c", "#e15759", "#76b7b2", "#59a14f", "#edc949", "#af7aa1", "#ff9da7", "#9c755f", "#bab0ab"];
    private readonly string[] tableau10 = ["#4e79a7", "#f28e2c", "#e15759", "#76b7b2", "#59a14f", "#edc949", "#af7aa1", "#ff9da7", "#9c755f", "#bab0ab"];
    private readonly string[] set1 = ["#e41a1c", "#377eb8", "#4daf4a", "#984ea3", "#ff7f00", "#ffff33", "#a65628", "#f781bf", "#999999"];
    private readonly string[] paired = ["#a6cee3", "#1f78b4", "#b2df8a", "#33a02c", "#fb9a99", "#e31a1c", "#fdbf6f", "#ff7f00", "#cab2d6", "#6a3d9a", "#ffff99", "#b15928"];

    public ColourServer()
    {
        availableColours = new List<Colours>();
        reservedColours = new List<Colours>();
        SetupAvailableColours();
    }

    public string GetNextColour(Colours requestedColour, List<Colours> requestedColours)
    {
        requestedColours.Remove(requestedColour);
        if (requestedColour != Colours.AutoAssigned)
        {
            if (!reservedColours.Contains(requestedColour))
            {
                reservedColours.Add(requestedColour);
            }
            else
            {
                requestedColour = Colours.AutoAssigned;
            }
        }

        if (availableColours.Count == 0)
        {
            // Reset the list because we've run out of colours
            SetupAvailableColours();
        }

        var nextColour = availableColours.First(x => (requestedColour == Colours.AutoAssigned && !requestedColours.Contains(x)) || x == requestedColour);
        availableColours.Remove(nextColour);
        return set1Modified[nextColour];
    }

    private void SetupAvailableColours()
    {
        foreach (var colour in (Colours[])Enum.GetValues(typeof(Colours)))
        {
            if (colour == Colours.AutoAssigned)
            {
                continue;
            }

            availableColours.Add(colour);
        }
    }
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