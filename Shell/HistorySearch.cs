namespace Wish.Shell;

public static class HistorySearch
{
    public static string? Run(PromptInfo prompt, ShellConfig cfg, HistoryStore history)
    {
        // Very small fzf-like UI:
        // - Type to filter
        // - Up/Down to select
        // - Enter to accept
        // - Esc to cancel
        var query = "";
        var selected = 0;

        while (true)
        {
            var matches = FindMatches(history, query).Take(10).ToList();
            selected = Math.Clamp(selected, 0, Math.Max(0, matches.Count - 1));

            Render(prompt, cfg, query, matches, selected);

            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Escape)
            {
                ClearSearch(prompt);
                return null;
            }
            if (key.Key == ConsoleKey.Enter)
            {
                ClearSearch(prompt);
                return matches.Count == 0 ? null : matches[selected];
            }
            if (key.Key == ConsoleKey.UpArrow)
            {
                selected = Math.Max(0, selected - 1);
                continue;
            }
            if (key.Key == ConsoleKey.DownArrow)
            {
                selected = Math.Min(Math.Max(0, matches.Count - 1), selected + 1);
                continue;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (query.Length > 0) query = query[..^1];
                selected = 0;
                continue;
            }
            if (!char.IsControl(key.KeyChar))
            {
                query += key.KeyChar;
                selected = 0;
            }
        }
    }

    private static IEnumerable<string> FindMatches(HistoryStore history, string query)
    {
        // Prefer most recent, simple contains match.
        for (var i = history.Items.Count - 1; i >= 0; i--)
        {
            var item = history.Items[i];
            if (query.Length == 0 || item.Contains(query, StringComparison.OrdinalIgnoreCase))
                yield return item;
        }
    }

    private static void Render(PromptInfo prompt, ShellConfig cfg, string query, List<string> matches, int selected)
    {
        // Draw over current input line and below (no attempt to preserve scrollback perfectly).
        // We clear line, print search line, then a small list, then reprint prompt line.
        Ansi.Write("\r\u001b[0K");
        Ansi.Write($"{Ansi.Dim}search{Ansi.Reset} {Ansi.Fg(cfg.Md3.Primary)}{query}{Ansi.Reset}");
        Ansi.WriteLine("");

        for (var i = 0; i < matches.Count; i++)
        {
            var isSel = i == selected;
            var line = matches[i];
            if (isSel)
                Ansi.WriteLine($"{Ansi.Bg(cfg.Md3.SurfaceVariant)}{Ansi.Fg(cfg.Md3.OnSurface)} {line} {Ansi.Reset}");
            else
                Ansi.WriteLine($"{Ansi.Dim}{line}{Ansi.Reset}");
        }

        Ansi.Write(prompt.PrePrompt);
        Ansi.Write(prompt.InputPrefix);
    }

    private static void ClearSearch(PromptInfo prompt)
    {
        // Just move to new line before returning control.
        Ansi.WriteLine("");
        Ansi.Write(prompt.PrePrompt);
        Ansi.Write(prompt.InputPrefix);
    }
}

