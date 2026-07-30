using System.Text.RegularExpressions;

namespace DR.Simple_UI.Tests.TestSupport;

/// <summary>
/// Locates the shipped assets on disk and provides the small amount of CSS
/// parsing the guard tests need.
/// </summary>
/// <remarks>
/// The tests read the real files rather than a copy: a test asserting things
/// about a duplicate of the stylesheet proves nothing about what ships.
/// </remarks>
internal static class Assets
{
    /// <summary>Repository root — the directory holding the solution file.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string ProjectDir => Path.Combine(RepoRoot, "src", "DR.Simple_UI");
    public static string CssPath => Path.Combine(ProjectDir, "wwwroot", "css", "DR.Simple_UI.css");
    public static string JsPath => Path.Combine(ProjectDir, "wwwroot", "js", "DR.Simple_UI.js");
    public static string BootJsPath => Path.Combine(ProjectDir, "wwwroot", "js", "DR.Simple_UI.boot.js");
    public static string CatalogueDir => Path.Combine(ProjectDir, "wwwroot", "catalogue");

    public static string Css => File.ReadAllText(CssPath);

    public static IEnumerable<string> CataloguePages =>
        Directory.EnumerateFiles(CatalogueDir, "*.html").OrderBy(p => p, StringComparer.Ordinal);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DR.Simple_UI.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find DR.Simple_UI.slnx above {AppContext.BaseDirectory}.");
    }

    /// <summary>
    /// Blanks out /* … */ comments so prose about colours is never scanned as
    /// CSS, keeping every newline so reported line numbers still match the file.
    /// </summary>
    public static string StripComments(string css) =>
        Regex.Replace(
            css,
            @"/\*.*?\*/",
            m => new string('\n', m.Value.Count(c => c == '\n')),
            RegexOptions.Singleline);

    /// <summary>
    /// A token block: a rule whose selector is <c>:root</c> plus zero or more
    /// attribute filters and nothing else — <c>:root</c>,
    /// <c>:root[data-theme="light"]</c>, <c>:root[data-theme="light"][data-cvd="1"]</c>.
    /// A selector with a descendant (<c>:root[data-density="compact"] .table</c>) is
    /// a normal rule, not a token block.
    /// </summary>
    private static readonly Regex TokenBlockPattern = new(
        @"(?<selector>:root(?:\[[^\]]*\])*)\s*\{(?<body>[^{}]*)\}",
        RegexOptions.Compiled);

    public static IEnumerable<(string Selector, string Body)> TokenBlocks(string css) =>
        TokenBlockPattern.Matches(css)
            .Select(m => (m.Groups["selector"].Value, m.Groups["body"].Value));

    /// <summary>
    /// Every line that is NOT part of a token block, with its real 1-based line
    /// number in the file — so a failure message points at somewhere that exists.
    /// </summary>
    public static IEnumerable<(string Line, int Number)> LinesOutsideTokenBlocks(string css)
    {
        var blocks = TokenBlockPattern.Matches(css)
            .Select(m => (Start: m.Index, End: m.Index + m.Length))
            .ToList();

        var offset = 0;
        var number = 0;
        foreach (var line in css.Split('\n'))
        {
            number++;
            var lineStart = offset;
            var lineEnd = offset + line.Length;
            offset = lineEnd + 1;   // + the '\n' we split on

            var insideBlock = blocks.Any(b => lineStart < b.End && lineEnd > b.Start);
            if (!insideBlock) yield return (line, number);
        }
    }

    /// <summary>Every custom property declared anywhere in the sheet.</summary>
    public static ISet<string> DeclaredCustomProperties(string css) =>
        Regex.Matches(css, @"(?<![\w-])(--[a-z0-9-]+)\s*:", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Every custom property referenced through var().</summary>
    public static ISet<string> ReferencedCustomProperties(string css) =>
        Regex.Matches(css, @"var\(\s*(--[a-z0-9-]+)", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
}
