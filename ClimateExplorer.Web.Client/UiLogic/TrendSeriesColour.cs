namespace ClimateExplorer.Web.UiLogic;

using System.Globalization;

/// <summary>
/// Derives the colour a chart series' second and third trend are drawn in, from the colour the
/// series itself (and its first trend) is drawn in. A series can carry up to three simultaneous
/// trends; drawing all of them in the parent's own colour (the single-trend design's choice) would
/// make them indistinguishable from each other, so the second and third are shifted in lightness -
/// darker for most colours, lighter for a black/near-black series, so the shift is always visible
/// regardless of how dark the parent already is.
/// </summary>
public static class TrendSeriesColour
{
    /// <summary>Lightness moved per tier, as a fraction of the 0-1 HSL lightness range.</summary>
    private const double LightnessStepPerTier = 0.25;

    /// <summary>At or below this lightness, a colour is treated as "black-like" and shifted lighter instead of darker.</summary>
    private const double NearBlackLightnessThreshold = 0.25;

    private const double MinLightness = 0.06;
    private const double MaxLightness = 0.94;

    /// <summary>
    /// Returns the colour to draw tier <paramref name="tier"/>'s trend in, derived from
    /// <paramref name="parentHexColour"/> (the series' own colour, i.e. tier 0's colour).
    /// </summary>
    /// <param name="parentHexColour">The parent series' colour, as a <c>#RRGGBB</c> hex string.</param>
    /// <param name="tier">
    /// 0 for the series' first trend (returns <paramref name="parentHexColour"/> unchanged - the
    /// existing single-trend rendering is untouched), 1 for the second trend, 2 for the third.
    /// </param>
    public static string Derive(string parentHexColour, int tier)
    {
        ArgumentNullException.ThrowIfNull(parentHexColour);

        if (tier == 0)
        {
            return parentHexColour;
        }

        var (h, s, l) = ToHsl(parentHexColour);

        var step = LightnessStepPerTier * tier;
        var newLightness = l <= NearBlackLightnessThreshold
            ? Math.Min(l + step, MaxLightness)
            : Math.Max(l - step, MinLightness);

        return ToHex(h, s, newLightness);
    }

    private static (double H, double S, double L) ToHsl(string hexColour)
    {
        var (r, g, b) = ToRgb(hexColour);

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;

        if (max == min)
        {
            // Achromatic (grey, including black and white) - hue and saturation are both zero.
            return (0, 0, l);
        }

        var delta = max - min;
        var s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);

        double h;
        if (max == r)
        {
            h = ((g - b) / delta) + (g < b ? 6 : 0);
        }
        else if (max == g)
        {
            h = ((b - r) / delta) + 2;
        }
        else
        {
            h = ((r - g) / delta) + 4;
        }

        h /= 6;

        return (h, s, l);
    }

    private static string ToHex(double h, double s, double l)
    {
        if (s == 0)
        {
            var grey = (int)Math.Round(l * 255);
            return FormatHex(grey, grey, grey);
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - (l * s);
        var p = (2 * l) - q;

        var r = HueToRgbChannel(p, q, h + (1.0 / 3));
        var g = HueToRgbChannel(p, q, h);
        var b = HueToRgbChannel(p, q, h - (1.0 / 3));

        return FormatHex(
            (int)Math.Round(r * 255),
            (int)Math.Round(g * 255),
            (int)Math.Round(b * 255));
    }

    private static double HueToRgbChannel(double p, double q, double t)
    {
        if (t < 0)
        {
            t += 1;
        }

        if (t > 1)
        {
            t -= 1;
        }

        if (t < 1.0 / 6)
        {
            return p + ((q - p) * 6 * t);
        }

        if (t < 1.0 / 2)
        {
            return q;
        }

        if (t < 2.0 / 3)
        {
            return p + ((q - p) * ((2.0 / 3) - t) * 6);
        }

        return p;
    }

    private static (double R, double G, double B) ToRgb(string hexColour)
    {
        var hex = hexColour.TrimStart('#');

        var r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return (r / 255.0, g / 255.0, b / 255.0);
    }

    private static string FormatHex(int r, int g, int b)
    {
        return $"#{Math.Clamp(r, 0, 255):X2}{Math.Clamp(g, 0, 255):X2}{Math.Clamp(b, 0, 255):X2}";
    }
}
