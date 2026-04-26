using System.Globalization;

namespace Wish.Shell;

public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb ParseHex(string hex)
    {
        hex = hex.Trim();
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length != 6) throw new FormatException("Expected #RRGGBB.");

        var r = byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new Rgb(r, g, b);
    }
}
