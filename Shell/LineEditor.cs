using System.Text;
using System.Linq;

namespace Wish.Shell;

public sealed class LineEditor
{
    private readonly HistoryStore _history;
    private int _historyIndex = -1;
    private string _historyStash = "";
    private ShellConfig? _cfgForRender;
    private WishConfig? _wishForRender;

    public LineEditor(HistoryStore history) => _history = history;

    public string? ReadLine(PromptInfo prompt, ShellConfig cfg, WishConfig wish)
    {
        _cfgForRender = cfg;
        _wishForRender = wish;

        var buffer = new StringBuilder();
        var cursor = 0;

        _historyIndex = -1;
        _historyStash = "";

        Ansi.Write(prompt.PrePrompt);
        if (cfg.Options.AnimatePrompt && cfg.Options.PromptTrailMs > 0)
            AnimateTrail(cfg);

        Ansi.Write(prompt.InputPrefix);
        var prefixWidth = Ansi.PrintableWidth(prompt.InputPrefix);

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            var currentText = buffer.ToString();
            var suggestion = Suggest(currentText);

            if (key.Key == ConsoleKey.Z && key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                Ansi.WriteLine("");
                return null;
            }

            if (key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                // Cancel current line (like fish): clear buffer, print newline.
                buffer.Clear();
                cursor = 0;
                Ansi.WriteLine("");
                return "";
            }

            if (key.Key == ConsoleKey.Enter)
            {
                Ansi.WriteLine("");
                return buffer.ToString();
            }

            switch (key.Key)
            {
                case ConsoleKey.Backspace:
                    if (cursor > 0)
                    {
                        buffer.Remove(cursor - 1, 1);
                        cursor--;
                    }
                    break;
                case ConsoleKey.Delete:
                    if (cursor < buffer.Length)
                        buffer.Remove(cursor, 1);
                    break;
                case ConsoleKey.LeftArrow:
                    if (cursor > 0) cursor--;
                    break;
                case ConsoleKey.RightArrow:
                    if (cursor < buffer.Length)
                    {
                        cursor++;
                    }
                    else if (suggestion is not null && cursor == buffer.Length && suggestion.Length > buffer.Length)
                    {
                        // Fish-style accept one character from autosuggestion.
                        buffer.Append(suggestion[buffer.Length]);
                        cursor = buffer.Length;
                    }
                    break;
                case ConsoleKey.Home:
                    cursor = 0;
                    break;
                case ConsoleKey.End:
                    if (suggestion is not null && cursor == buffer.Length && suggestion.Length > buffer.Length)
                    {
                        // Accept full autosuggestion.
                        buffer.Clear();
                        buffer.Append(suggestion);
                        cursor = buffer.Length;
                    }
                    else
                    {
                        cursor = buffer.Length;
                    }
                    break;
                case ConsoleKey.UpArrow:
                    RecallHistory(older: true, buffer, ref cursor);
                    break;
                case ConsoleKey.DownArrow:
                    RecallHistory(older: false, buffer, ref cursor);
                    break;
                case ConsoleKey.Tab:
                    HandleCompletion(prompt, cfg, wish, buffer, ref cursor);
                    break;
                case ConsoleKey.R when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                    {
                        var picked = HistorySearch.Run(prompt, cfg, _history);
                        if (!string.IsNullOrEmpty(picked))
                        {
                            buffer.Clear();
                            buffer.Append(picked);
                            cursor = buffer.Length;
                        }
                    }
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        buffer.Insert(cursor, key.KeyChar);
                        cursor++;
                    }
                    break;
            }

            Render(prompt, prefixWidth, buffer, cursor);
        }
    }

    private static void AnimateTrail(ShellConfig cfg)
    {
        var ms = Math.Clamp(cfg.Options.PromptTrailMs, 10, 400);
        var frames = new[] { ".", "o", "O", "@", "O", "o", "." };
        var per = Math.Max(10, ms / frames.Length);
        var color = Ansi.Fg(cfg.Md3.Primary);

        foreach (var f in frames)
        {
            Ansi.Write("\r\u001b[0K");
            Ansi.Write($"{color}{f}{Ansi.Reset}");
            Thread.Sleep(per);
        }

        Ansi.Write("\r\u001b[0K");
    }

    private void Render(PromptInfo prompt, int prefixWidth, StringBuilder buffer, int cursor)
    {
        // Suggestion: most recent history entry that starts with current buffer.
        var current = buffer.ToString();
        var suggestion = Suggest(current);
        var suffix = suggestion is null ? "" : $"{Ansi.Dim}{suggestion[current.Length..]}{Ansi.Reset}";
        var highlighted = (_cfgForRender is null || _wishForRender is null)
            ? current
            : SyntaxHighlighter.Highlight(current, _cfgForRender, _wishForRender);

        // Re-render current line: CR + clear to end + write buffer + suggestion, then place cursor.
        Ansi.Write("\r\u001b[0K");
        Ansi.Write(prompt.InputPrefix + highlighted + suffix);
        Ansi.Write("\u001b[0K");

        var targetCol = prefixWidth + cursor;
        Ansi.Write($"\r\u001b[{targetCol + 1}G");
    }

    private string? Suggest(string current)
    {
        if (string.IsNullOrEmpty(current)) return null;
        for (var i = _history.Items.Count - 1; i >= 0; i--)
        {
            var candidate = _history.Items[i];
            if (candidate.StartsWith(current, StringComparison.OrdinalIgnoreCase) && candidate.Length > current.Length)
                return candidate;
        }
        return null;
    }

    private void RecallHistory(bool older, StringBuilder buffer, ref int cursor)
    {
        if (_history.Items.Count == 0) return;

        if (_historyIndex == -1)
            _historyStash = buffer.ToString();

        if (older)
        {
            if (_historyIndex < _history.Items.Count - 1)
                _historyIndex++;
        }
        else
        {
            if (_historyIndex >= 0)
                _historyIndex--;
        }

        var next = _historyIndex == -1 ? _historyStash : _history.Items[^(_historyIndex + 1)];
        buffer.Clear();
        buffer.Append(next);
        cursor = buffer.Length;
    }

    private void HandleCompletion(PromptInfo prompt, ShellConfig cfg, WishConfig wish, StringBuilder buffer, ref int cursor)
    {
        var text = buffer.ToString();
        var candidates = CompletionEngine.GetCandidates(text, cursor, wish);
        if (candidates.Count == 0) return;

        if (candidates.Count == 1)
        {
            var completed = candidates[0];
            var newText = CompletionEngine.ApplySingleCompletion(text, cursor, completed, out var newCursor);
            buffer.Clear();
            buffer.Append(newText);
            cursor = newCursor;
            return;
        }

        // Menu: print candidates grid below, then redraw prompt + current input.
        Ansi.WriteLine("");
        PrintCandidateGrid(cfg, candidates);
        Ansi.Write(prompt.PrePrompt);
        Ansi.Write(prompt.InputPrefix);
    }

    private static void PrintCandidateGrid(ShellConfig cfg, IReadOnlyList<string> candidates)
    {
        var termWidth = Math.Max(40, Console.BufferWidth);
        var display = candidates.Take(60).Select(c => $"{Ansi.Fg(cfg.Md3.OnSurfaceVariant)}{c}{Ansi.Reset}").ToList();
        var widths = candidates.Take(60).Select(Ansi.PrintableWidth).ToList();
        var colWidth = Math.Min(widths.Max(), 30);
        var colPad = 2;
        var cols = Math.Max(1, (termWidth + colPad) / (colWidth + colPad));
        cols = Math.Min(cols, display.Count);
        var rows = (int)Math.Ceiling(display.Count / (double)cols);

        for (var r = 0; r < rows; r++)
        {
            var line = "";
            for (var c = 0; c < cols; c++)
            {
                var i = c * rows + r;
                if (i >= display.Count) continue;
                var w = widths[i];
                var pad = (colWidth - w) + colPad;
                line += display[i] + new string(' ', Math.Max(0, pad));
            }
            Ansi.WriteLine(line.TrimEnd());
        }
        if (candidates.Count > 60)
            Ansi.WriteLine($"{Ansi.Dim}… and {candidates.Count - 60} more{Ansi.Reset}");
    }
}

internal static class FuncExt
{
    public static TResult Let<T, TResult>(this T value, Func<T, TResult> f) => f(value);
}
