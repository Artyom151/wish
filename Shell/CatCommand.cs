using System.Text;

namespace Wish.Shell;

public static class CatCommand
{
    public static int Run(string[] args)
    {
        var number = args.Any(a => a is "-n" or "--number");
        var files = args.Where(a => !a.StartsWith('-')).ToList();
        if (files.Count == 0)
        {
            Ansi.WriteLine("cat: missing operand");
            return 1;
        }

        var ec = 0;
        foreach (var f in files)
        {
            var path = Path.GetFullPath(f);
            if (!File.Exists(path))
            {
                Ansi.WriteLine($"cat: {f}: no such file");
                ec = 1;
                continue;
            }

            try
            {
                var lines = File.ReadAllLines(path, Encoding.UTF8);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (number)
                        Ansi.WriteLine($"{(i + 1).ToString().PadLeft(6)}  {lines[i]}");
                    else
                        Ansi.WriteLine(lines[i]);
                }
            }
            catch (Exception ex)
            {
                Ansi.WriteLine($"cat: {f}: {ex.Message}");
                ec = 1;
            }
        }

        return ec;
    }
}

