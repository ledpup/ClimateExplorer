namespace ClimateExplorer.Web.UiLogic;

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

public class ColourServer
{
    private static readonly Dictionary<Colours, string> HtmlColourCodesByColour = new Dictionary<Colours, string>
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

    private readonly List<Colours> reservedColours;
    private readonly List<Colours> availableColours;
    private readonly Dictionary<Colours, string> colours = HtmlColourCodesByColour;

    public ColourServer()
    {
        availableColours = [];
        reservedColours = [];
        SetupAvailableColours();
    }

    /// <summary>
    /// Looks up the fixed HTML colour code for a specific <see cref="Colours"/> value, independent of
    /// the per-chart allocation tracked by an instance's <see cref="GetNextColour"/>. Used where a
    /// colour is requested explicitly rather than auto-assigned - e.g. a preset's positive/negative
    /// value colours for a bar chart.
    /// </summary>
    public static string GetHtmlColourCode(Colours colour)
    {
        if (!HtmlColourCodesByColour.TryGetValue(colour, out var htmlColourCode))
        {
            throw new ArgumentOutOfRangeException(nameof(colour), colour, "No HTML colour code is defined for this colour.");
        }

        return htmlColourCode;
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