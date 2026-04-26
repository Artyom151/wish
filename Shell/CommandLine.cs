namespace Wish.Shell;

public static class CommandLine
{
    public sealed record Token(string Text, int Start, int Length, bool WasQuoted);

    public static List<Token> Tokenize(string line)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i >= line.Length) break;

            var start = i;
            var wasQuoted = false;
            var text = "";
            char? quote = null;

            if (line[i] is '"' or '\'')
            {
                quote = line[i];
                wasQuoted = true;
                i++;
            }

            while (i < line.Length)
            {
                var ch = line[i];
                if (quote is null)
                {
                    if (char.IsWhiteSpace(ch)) break;
                    if (ch == '\\' && i + 1 < line.Length)
                    {
                        // Simple escaping in unquoted token.
                        text += line[i + 1];
                        i += 2;
                        continue;
                    }
                    text += ch;
                    i++;
                }
                else if (quote == '"')
                {
                    if (ch == '"')
                    {
                        i++;
                        quote = null;
                        break;
                    }
                    if (ch == '\\' && i + 1 < line.Length)
                    {
                        text += line[i + 1];
                        i += 2;
                        continue;
                    }
                    text += ch;
                    i++;
                }
                else // single quote: literal until next '
                {
                    if (ch == '\'')
                    {
                        i++;
                        quote = null;
                        break;
                    }
                    text += ch;
                    i++;
                }
            }

            // If we ended quoted token and immediately have non-space, keep consuming until whitespace.
            while (i < line.Length && !char.IsWhiteSpace(line[i]))
            {
                text += line[i];
                i++;
            }

            var end = i;
            tokens.Add(new Token(text, start, end - start, wasQuoted));
        }
        return tokens;
    }

    public static (int TokenStart, int TokenLength, string RawToken)? GetTokenAtCursor(string line, int cursor)
    {
        cursor = Math.Clamp(cursor, 0, line.Length);
        // Cursor can be in whitespace => token is empty at cursor.
        var left = cursor;
        while (left > 0 && !char.IsWhiteSpace(line[left - 1])) left--;
        var right = cursor;
        while (right < line.Length && !char.IsWhiteSpace(line[right])) right++;
        var raw = line[left..right];
        return (left, right - left, raw);
    }

    public static string ReplaceToken(string line, int tokenStart, int tokenLength, string replacement, out int newCursor)
    {
        var before = line[..tokenStart];
        var after = line[(tokenStart + tokenLength)..];
        newCursor = (before + replacement).Length;
        return before + replacement + after;
    }
}

