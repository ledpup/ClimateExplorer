namespace ClimateExplorer.Web.UiLogic;

using System.Globalization;

/// <summary>
/// Derives the colour of a trend series from the colour of the series it was fitted to.
/// </summary>
/// <remarks>
/// <para>
/// The transform keeps the parent's hue - so a trend reads as belonging to its series - while
/// desaturating and pushing lightness away from mid, which separates the two lines wherever they
/// sit on the chart:
/// </para>
/// <code>
/// h' = h
/// s' = s * 0.55
/// l' = l &lt;= 0.5 ? min(l + 0.22, 0.92) : max(l - 0.22, 0.08)
/// </code>
/// <para>
/// Scaling saturation rather than setting it means an achromatic parent (black, grey) stays
/// achromatic instead of acquiring an arbitrary hue from an undefined one. Colour is not the only
/// cue either: the trend dataset is drawn dashed and thinner than its parent, so the distinction
/// survives greyscale printing and colour-vision deficiency.
/// </para>
/// </remarks>
public static class TrendSeriesColour
{
    private const double SaturationScale = 0.55;
    private const double LightnessShift = 0.22;
    private const double MaximumLightness = 0.92;
    private const double MinimumLightness = 0.08;

    /// <summary>
    /// Returns the trend colour for a parent series colour given as "#rrggbb". A value that cannot
    /// be parsed is returned unchanged, so an unexpected colour format degrades to "same colour as
    /// the parent" rather than throwing mid-render.
    /// </summary>
    public static string Derive(string parentHtmlColourCode)
    {
        if (!TryParseHtmlColourCode(parentHtmlColourCode, out var red, out var green, out var blue))
        {
            return parentHtmlColourCode;
        }

        var (hue, saturation, lightness) = RgbToHsl(red, green, blue);

        var shiftedLightness = lightness <= 0.5
            ? Math.Min(lightness + LightnessShift, MaximumLightness)
            : Math.Max(lightness - LightnessShift, MinimumLightness);

        var (r, g, b) = HslToRgb(hue, saturation * SaturationScale, shiftedLightness);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static bool TryParseHtmlColourCode(string? value, out byte red, out byte green, out byte blue)
    {
        red = green = blue = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim().TrimStart('#');

        if (text.Length != 6)
        {
            return false;
        }

        return byte.TryParse(text.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out red)
            && byte.TryParse(text.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out green)
            && byte.TryParse(text.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out blue);
    }

    private static (double Hue, double Saturation, double Lightness) RgbToHsl(byte red, byte green, byte blue)
    {
        var r = red / 255.0;
        var g = green / 255.0;
        var b = blue / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var lightness = (max + min) / 2;

        if (max == min)
        {
            // Achromatic: hue is undefined, and zero saturation keeps it that way through the transform.
            return (0, 0, lightness);
        }

        var delta = max - min;
        var saturation = lightness > 0.5
            ? delta / (2 - max - min)
            : delta / (max + min);

        double hue;
        if (max == r)
        {
            hue = ((g - b) / delta) + (g < b ? 6 : 0);
        }
        else if (max == g)
        {
            hue = ((b - r) / delta) + 2;
        }
        else
        {
            hue = ((r - g) / delta) + 4;
        }

        return (hue / 6, saturation, lightness);
    }

    private static (byte Red, byte Green, byte Blue) HslToRgb(double hue, double saturation, double lightness)
    {
        if (saturation == 0)
        {
            var grey = ToByte(lightness);
            return (grey, grey, grey);
        }

        var q = lightness < 0.5
            ? lightness * (1 + saturation)
            : lightness + saturation - (lightness * saturation);
        var p = (2 * lightness) - q;

        return (
            ToByte(HueToChannel(p, q, hue + (1.0 / 3))),
            ToByte(HueToChannel(p, q, hue)),
            ToByte(HueToChannel(p, q, hue - (1.0 / 3))));
    }

    private static double HueToChannel(double p, double q, double t)
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

    private static byte ToByte(double channel)
    {
        return (byte)Math.Clamp(Math.Round(channel * 255), 0, 255);
    }
}
