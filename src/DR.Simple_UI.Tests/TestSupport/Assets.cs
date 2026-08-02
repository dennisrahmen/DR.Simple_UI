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

    // AllDirectories: a page filed in a subfolder would otherwise be invisible to
    // every catalogue guard at once, and each of them would still pass.
    public static IEnumerable<string> CataloguePages =>
        Directory.EnumerateFiles(CatalogueDir, "*.html", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal);

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
    /// Every line with the token blocks masked out, and its real 1-based line number
    /// so a failure message points at somewhere that exists.
    /// </summary>
    /// <remarks>
    /// Masks by character range rather than skipping whole lines. Skipping the line
    /// exempted anything sharing a line with a token block — most reachably the
    /// closing brace, where `} .btn { color: red }` would never be scanned. Lengths
    /// and newlines are preserved so line numbers still match the file.
    /// </remarks>
    public static IEnumerable<(string Line, int Number)> LinesOutsideTokenBlocks(string css)
    {
        var masked = new System.Text.StringBuilder(css);
        foreach (var block in TokenBlockPattern.Matches(css).Cast<Match>())
            for (var i = block.Index; i < block.Index + block.Length; i++)
                if (masked[i] != '\n') masked[i] = ' ';

        var number = 0;
        foreach (var line in masked.ToString().Split('\n'))
        {
            number++;
            if (line.Trim().Length > 0) yield return (line, number);
        }
    }

    /// <summary>
    /// Every <c>@media</c> block as (condition, body), matched by counting braces
    /// rather than by regex, so a nested rule inside the block does not truncate it.
    /// </summary>
    public static IEnumerable<(string Condition, string Body)> MediaBlocks(string css)
    {
        foreach (var open in Regex.Matches(css, @"@media([^{]*)\{", RegexOptions.Compiled).Cast<Match>())
        {
            var depth = 1;
            var i = open.Index + open.Length;
            while (i < css.Length && depth > 0)
            {
                if (css[i] == '{') depth++;
                else if (css[i] == '}') depth--;
                i++;
            }

            // depth 0 means we found the matching brace; i is one past it.
            if (depth == 0)
            {
                var bodyStart = open.Index + open.Length;
                yield return (open.Groups[1].Value.Trim(), css[bodyStart..(i - 1)]);
            }
        }
    }

    /// <summary>
    /// Rules opening directly inside a block body as (selector, body), ignoring
    /// anything nested deeper. Used to ask "does this block style anything, or only
    /// remap tokens on :root?".
    /// </summary>
    public static IEnumerable<(string Selector, string Body)> TopLevelRules(string blockBody)
    {
        var depth = 0;
        var start = 0;
        var selector = string.Empty;
        var bodyStart = 0;

        for (var i = 0; i < blockBody.Length; i++)
        {
            if (blockBody[i] == '{')
            {
                if (depth == 0)
                {
                    selector = blockBody[start..i].Trim();
                    bodyStart = i + 1;
                }

                depth++;
            }
            else if (blockBody[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    yield return (selector, blockBody[bodyStart..i]);
                    start = i + 1;
                }
            }
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
