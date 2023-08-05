namespace ClimateExplorer.Core;

public struct Colour
{
    public double R, G, B;

    public static Colour White { get { return new Colour { R = 255, G = 255, B = 255 }; } }

    public static Colour FromRgb(double r, double g, double b)
    {
        return new Colour { R = r, G = g, B = b };
    }

    public static Colour Blend(Colour colour1, Colour colour2, double amount)
    {
        byte r = (byte)(colour1.R * amount + colour2.R * (1 - amount));
        byte g = (byte)(colour1.G * amount + colour2.G * (1 - amount));
        byte b = (byte)(colour1.B * amount + colour2.B * (1 - amount));
        return Colour.FromRgb(r, g, b);
    }
}