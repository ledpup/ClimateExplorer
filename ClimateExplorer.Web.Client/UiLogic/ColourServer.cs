namespace ClimateExplorer.Web.UiLogic;

public class ColourServer
{
    private readonly List<Colours> reservedColours;
    private readonly List<Colours> availableColours;

    private readonly Dictionary<Colours, string> colours = new Dictionary<Colours, string>
    {
        { Colours.Red, "#FF2D2D" },
        { Colours.Blue, "#36A2EB" },
        { Colours.Green, "#4DAF4A" },
        { Colours.Purple, "#9966FF" },
        { Colours.Orange, "#FF9532" },
        { Colours.Black, "#000000" },
        { Colours.Yellow, "#FFCD56" },
        { Colours.Brown, "#a65628" },
        { Colours.Pink, "#f781bf" },
        { Colours.Grey, "#666666" },
    };

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
        return colours[nextColour];
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