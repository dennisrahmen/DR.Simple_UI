using System.Text.RegularExpressions;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// The component examples in the documentation have to compile in a consuming app.
/// </summary>
/// <remarks>
/// <para>
/// Both traps below were found by generating a project from a template and building it.
/// <b>There is no template any more, so nothing compiles a documented example.</b> This is
/// a source scan standing in for that compile step: it catches these two shapes and
/// cannot catch a third Razor rule nobody has hit yet, so compile a documented snippet by
/// hand when changing one.
/// </para>
/// <para>
/// It matters because the examples are what an app copies. Both of these were live in
/// <c>frame.html</c> and <c>getting-started.md</c> at one point, meaning the documented
/// markup would not have built.
/// </para>
/// </remarks>
public class DocumentedExampleTests
{
    /// <summary>Components with at least one named RenderFragment, and so RZ9996-prone.</summary>
    private static readonly string[] WithNamedSlots = ["AppShell", "Sidebar", "AppHeader"];

    private static IEnumerable<(string Name, string Text)> DocumentedSources()
    {
        yield return ("catalogue/frame.html",
            File.ReadAllText(Path.Combine(Assets.CatalogueDir, "frame.html")));

        foreach (var doc in new[] { "getting-started.md", "architecture.md", "accessibility.md" })
        {
            var path = Path.Combine(Assets.RepoRoot, "docs", doc);
            if (File.Exists(path)) yield return ($"docs/{doc}", File.ReadAllText(path));
        }

        // Components/CLAUDE.md is deliberately NOT scanned: it documents these two traps
        // and has to quote the broken form to explain it. Scanning it makes the guard fire
        // on its own rule, which is what happened on the first run.
    }

    [Fact]
    public void Documented_component_examples_avoid_the_two_Razor_traps()
    {
        var problems = new List<string>();

        foreach (var (name, text) in DocumentedSources())
        {
            // RZ9996: a component with any named RenderFragment stops accepting loose
            // child content, so <ChildContent> has to be spelled out. Only checked on an
            // example that actually has child content — a self-closing tag is fine.
            foreach (var component in WithNamedSlots)
            {
                foreach (var open in Regex.Matches(text, $@"<{component}\b[^>]*?(?<!/)>").Cast<Match>())
                {
                    var close = text.IndexOf($"</{component}>", open.Index, StringComparison.Ordinal);
                    if (close < 0) continue;

                    var inner = text[(open.Index + open.Length)..close];
                    var hasNamedSlot = Regex.IsMatch(inner, @"<(Navigation|Header|Tools|Start)\b");
                    var spellsOutDefault = inner.Contains("<ChildContent>", StringComparison.Ordinal);
                    var hasLooseContent = Regex.IsMatch(
                        Regex.Replace(inner, @"<(Navigation|Header|Tools|Start)\b.*?</\1>", string.Empty,
                            RegexOptions.Singleline),
                        @"\S");

                    if (hasNamedSlot && hasLooseContent && !spellsOutDefault)
                        problems.Add($"{name}: <{component}> mixes a named slot with loose child "
                            + "content and no <ChildContent> — RZ9996, the app fails to build.");
                }
            }

            // RZ9986: text containing @ cannot go straight into an attribute — it is
            // parsed as a C# expression at the @. An e-mail address in an example is the
            // way this happens.
            foreach (var attr in Regex.Matches(text, @"\b[A-Z]\w*\s*=\s*""([^""]*@[^""]*)""").Cast<Match>())
            {
                var value = attr.Groups[1].Value;
                // A Razor expression (@field, @(expr)) is legitimate; a literal @ is not.
                if (value.StartsWith('@')) continue;
                problems.Add($"{name}: attribute value \"{value}\" contains a literal @ — "
                    + "RZ9986. Bind it to a field, which is what a real app does anyway.");
            }
        }

        Assert.True(problems.Count == 0,
            "A documented component example would not compile in a consuming app:"
            + Environment.NewLine + string.Join(Environment.NewLine, problems));
    }
}
