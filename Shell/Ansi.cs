namespace Wish.Shell;

public static class Ansi
{
    public const string Reset = "\u001b[0m";
    public const string Bold = "\u001b[1m";
    public const string Dim = "\u001b[2m";

    public static string Fg(Rgb rgb) => $"\u001b[38;2;{rgb.R};{rgb.G};{rgb.B}m";
    public static string Bg(Rgb rgb) => $"\u001b[48;2;{rgb.R};{rgb.G};{rgb.B}m";

    public static void Write(string s) => Console.Write(s);
    public static void WriteLine(string s) => Console.WriteLine(s);

    public static int PrintableWidth(string s)
    {
        // Very small ANSI stripper: counts printable chars, skips CSI sequences.
        var width = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '\u001b' && i + 1 < s.Length && s[i + 1] == '[')
            {
                i += 2;
                while (i < s.Length && s[i] != 'm') i++;
                continue;
            }

            width++;
        }
        return width;
    }
}
