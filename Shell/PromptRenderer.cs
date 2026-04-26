namespace Wish.Shell;

public static class PromptRenderer
{
    public static PromptInfo Render(ShellConfig cfg, int lastExitCode)
    {
        var md3 = cfg.Md3;
        var theme = cfg.Prompt;
        var opts = cfg.Options;

        var sym = PromptSymbols.For(opts);
        var sep = PromptSeparators.For(opts);

        var user = $"{sym.User} {Environment.UserName}".Trim();
        var cwd = Environment.CurrentDirectory;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home) && cwd.StartsWith(home, StringComparison.OrdinalIgnoreCase))
        {
            cwd = "~" + cwd[home.Length..];
        }

        var shellName = $"{sym.Shell} wish".Trim();
        cwd = $"{sym.Folder} {cwd}".Trim();

        var kinds = ProjectDetector.Detect(Environment.CurrentDirectory);
        var toolSegments = DetectToolSegments(kinds, sym);

        GitStatus? gitStatus = null;
        if (GitInfo.TryGetStatus(Environment.CurrentDirectory, out var gs))
            gitStatus = gs;

        var error = lastExitCode == 0 ? null : $"exit {lastExitCode}";

        // Powerline-ish first line like the screenshot:
        // [user]▶[shell]▶[path]▶[tool]
        var pre = new List<Segment>
        {
            new(theme.UserBg, theme.UserFg, user),
            new(theme.ShellBg, theme.ShellFg, shellName),
            new(theme.PathBg, theme.PathFg, cwd),
        };
        pre.AddRange(toolSegments);

        // Git panel should be on the top line, aligned to the right of the other segments.
        var inRepo = GitRepo.IsInRepo(Environment.CurrentDirectory);
        var gitPanel = inRepo ? RenderGitPanel(theme, gitStatus, sym, sep) : "";
        var errorSeg = string.IsNullOrEmpty(error) ? "" : RenderError(theme, error);
        var right = (gitPanel + errorSeg).TrimEnd();

        var left = RenderPowerline(pre);
        var topLine = RenderLeftRightLine(left, right);

        // Input line should be empty (no arrow, no prefix).
        var prePrompt = topLine + Environment.NewLine;
        var inputPrefix = "";

        return new PromptInfo(prePrompt, inputPrefix);
    }

    private static string RenderPowerline(IReadOnlyList<Segment> segments)
    {
        const string sep = "";
        const string leftCap = "";
        const string rightCap = "";

        var s = "";
        if (segments.Count > 0)
            s += $"{Ansi.Fg(segments[0].Bg)}{leftCap}{Ansi.Reset}";

        for (var i = 0; i < segments.Count; i++)
        {
            var cur = segments[i];
            var nextBg = i + 1 < segments.Count ? segments[i + 1].Bg : (Rgb?)null;

            s += $"{Ansi.Bg(cur.Bg)}{Ansi.Fg(cur.Fg)} {cur.Text} {Ansi.Reset}";
            if (nextBg is not null)
            {
                // Separator in next bg with current bg as fg to make the triangle blend.
                s += $"{Ansi.Fg(cur.Bg)}{Ansi.Bg(nextBg.Value)}{sep}{Ansi.Reset}";
            }
        }
        if (segments.Count > 0)
            s += $"{Ansi.Fg(segments[^1].Bg)}{rightCap}{Ansi.Reset}";
        return s;
    }

    private static string RenderGitPanel(PromptTheme theme, GitStatus? status, PromptSymbols sym, PromptSeparators sep)
    {
        if (status is null)
        {
            var text = $"{sym.Git} git";
            return RenderRounded(theme.GitBg, theme.GitFg, text.Trim(), sep);
        }

        var st = status.Value;
        var dirty = st.Staged + st.Changed + st.Untracked > 0;
        var stats = new List<string>();
        if (dirty) stats.Add("≠");
        if (st.Ahead > 0) stats.Add($"↑{st.Ahead}");
        if (st.Behind > 0) stats.Add($"↓{st.Behind}");
        if (st.Staged > 0) stats.Add($"+{st.Staged}");
        if (st.Changed > 0) stats.Add($"~{st.Changed}");
        if (st.Untracked > 0) stats.Add($"?{st.Untracked}");

        var branchText = $"{sym.Git} {st.Branch}".Trim();
        var statsText = stats.Count == 0 ? "clean" : string.Join(' ', stats);

        // Two connected rounded segments: branch (active) + stats.
        return RenderConnectedRounded(
            new Segment(theme.GitBranchBg, theme.GitBranchFg, branchText),
            new Segment(theme.GitBg, theme.GitFg, statsText),
            sep);
    }

    private static string RenderError(PromptTheme theme, string text)
        => " " + RenderRounded(theme.ErrorBg, theme.ErrorFg, text, PromptSeparators.DefaultPowerline());

    private static string RenderRounded(Rgb bg, Rgb fg, string text, PromptSeparators sep)
    {
        return $"{Ansi.Fg(bg)}{sep.LeftCap}{Ansi.Reset}{Ansi.Bg(bg)}{Ansi.Fg(fg)} {text} {Ansi.Reset}{Ansi.Fg(bg)}{sep.RightCap}{Ansi.Reset}";
    }

    private static string RenderConnectedRounded(Segment left, Segment right, PromptSeparators sep)
    {
        // Left rounded start
        var s = $"{Ansi.Fg(left.Bg)}{sep.LeftCap}{Ansi.Reset}{Ansi.Bg(left.Bg)}{Ansi.Fg(left.Fg)} {left.Text} {Ansi.Reset}";
        // Join triangle using left bg as fg and right bg as bg
        s += $"{Ansi.Fg(left.Bg)}{Ansi.Bg(right.Bg)}{sep.Join}{Ansi.Reset}";
        // Right body + rounded end
        s += $"{Ansi.Bg(right.Bg)}{Ansi.Fg(right.Fg)} {right.Text} {Ansi.Reset}{Ansi.Fg(right.Bg)}{sep.RightCap}{Ansi.Reset}";
        return s;
    }

    private static IEnumerable<Segment> DetectToolSegments(ProjectKinds kinds, PromptSymbols sym)
    {
        // Stable pastel colors per language/tool (no accidental duplicates).
        Segment Seg(string text, string bg, string fg = "#111827")
            => new Segment(Rgb.ParseHex(bg), Rgb.ParseHex(fg), text);

        var segments = new List<Segment>();

        if (kinds.HasFlag(ProjectKinds.Node) && ToolInfo.TryGetNodeVersion(out var node))
            segments.Add(Seg($"{sym.Node} {node}".Trim(), "#86EFAC", "#05210E")); // green

        if (kinds.HasFlag(ProjectKinds.Python) && ToolInfo.TryGetPythonVersion(out var py))
            segments.Add(Seg($"{sym.Python} {py}".Trim(), "#FDE68A")); // yellow

        if (kinds.HasFlag(ProjectKinds.Php) && ToolInfo.TryGetPhpVersion(out var php))
            segments.Add(Seg($"{sym.Php} {php}".Trim(), "#F0ABFC")); // pink

        if (kinds.HasFlag(ProjectKinds.Ruby) && ToolInfo.TryGetRubyVersion(out var rb))
            segments.Add(Seg($"{sym.Ruby} {rb}".Trim(), "#C4B5FD")); // violet

        if (kinds.HasFlag(ProjectKinds.DotNet) && ToolInfo.TryGetDotnetVersion(out var dn))
            segments.Add(Seg($"{sym.DotNet} {dn}".Trim(), "#FDE68A")); // yellow

        if (kinds.HasFlag(ProjectKinds.Rust))
        {
            if (ToolInfo.TryGetCargoVersion(out var cargo))
                segments.Add(Seg($"{sym.Rust} {cargo}".Trim(), "#FDBA74")); // orange
            else if (ToolInfo.TryGetRustcVersion(out var rustc))
                segments.Add(Seg($"{sym.Rust} {rustc}".Trim(), "#FDBA74")); // orange
            else
                segments.Add(Seg($"{sym.Rust} rust".Trim(), "#FDBA74")); // orange
        }

        if (kinds.HasFlag(ProjectKinds.Go) && ToolInfo.TryGetGoVersion(out var go))
            segments.Add(Seg($"{sym.Go} {go}".Trim(), "#67E8F9", "#0B1B33")); // cyan

        if (kinds.HasFlag(ProjectKinds.Cpp))
            segments.Add(Seg($"{sym.Cpp} C/C++".Trim(), "#FCA5A5")); // red

        if (kinds.HasFlag(ProjectKinds.Asm))
            segments.Add(Seg($"{sym.Asm} asm".Trim(), "#FECACA")); // lighter red

        if (kinds.HasFlag(ProjectKinds.Java) && ToolInfo.TryGetJavaVersion(out var java))
            segments.Add(Seg($"{sym.Java} {java}".Trim(), "#A7F3D0", "#05210E")); // mint

        return segments;
    }

    private static string RenderLeftRightLine(string left, string right)
    {
        if (string.IsNullOrEmpty(right)) return left;

        var width = 0;
        try { width = Console.BufferWidth; } catch { width = 0; }
        if (width <= 0) return left + " " + right;

        var leftW = Ansi.PrintableWidth(left);
        var rightW = Ansi.PrintableWidth(right);
        var spaces = width - leftW - rightW;
        if (spaces < 1) return left + " " + right;
        return left + new string(' ', spaces) + right;
    }
}

public readonly record struct PromptInfo(string PrePrompt, string InputPrefix);

internal readonly record struct Segment(Rgb Bg, Rgb Fg, string Text);

internal readonly record struct PromptSymbols(
    string User,
    string Shell,
    string Folder,
    string Node,
    string Python,
    string DotNet,
    string Rust,
    string Go,
    string Cpp,
    string Php,
    string Ruby,
    string Asm,
    string Java,
    string Git)
{
    public static PromptSymbols For(PromptOptions opts)
    {
        if (opts.UseNerdFontIcons)
        {
            return new PromptSymbols(
                User: "",
                Shell: "",
                Folder: "",
                Node: "󰎙",
                Python: "",
                DotNet: "",
                Rust: "",
                Go: "",
                Cpp: "",
                Php: "",
                Ruby: "",
                Asm: "󰘚",
                Java: "",
                Git: "");
        }

        return new PromptSymbols(
            User: "user:",
            Shell: "",
            Folder: "",
            Node: "node",
            Python: "py",
            DotNet: ".net",
            Rust: "rs",
            Go: "go",
            Cpp: "cc",
            Php: "php",
            Ruby: "rb",
            Asm: "asm",
            Java: "java",
            Git: "git");
    }
}

internal readonly record struct PromptSeparators(string LeftCap, string Join, string RightCap)
{
    public static PromptSeparators DefaultPowerline() => new("", "", "");

    public static PromptSeparators For(PromptOptions opts)
    {
        if (opts.UsePowerlineSeparators)
            return DefaultPowerline();
        return new PromptSeparators("[", "][", "]");
    }
}
