using System.Text.RegularExpressions;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Every page carries a copy-pasteable example, and the docs' own chrome never styles a library class.
/// </summary>
public class ExampleTests
{
    [Fact]
    public void Every_page_renders_at_least_one_example()
    {
        // A page with no example is a page that documents nothing.
        var offenders = Assets.CataloguePages
            .Where(p => !Path.GetFileName(p)!.Equals("tokens.html", StringComparison.Ordinal))
            .Where(p => !File.ReadAllText(p).Contains("data-example", StringComparison.Ordinal))
            .Select(p => Path.GetFileName(p)!)
            .ToList();

        Assert.True(offenders.Count == 0,
            "These catalogue pages carry no copy-pasteable example: " + string.Join(", ", offenders));
    }

    [Fact]
    public void The_catalogue_chrome_never_styles_a_library_class()
    {
        // catalogue.css exists to lay out the docs. The moment it touches a
        // library class, the examples stop showing what an app would actually get.
        var css = Assets.StripComments(File.ReadAllText(Path.Combine(Assets.CatalogueDir, Assets.CatalogueCssFile)));

        var offenders = Regex.Matches(css, @"\.(?<name>[A-Za-z][\w-]*)", RegexOptions.Compiled)
            .Select(m => m.Groups["name"].Value)
            .Where(n => !n.StartsWith("cat-", StringComparison.Ordinal)
                     && !n.StartsWith("ex-", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            "catalogue.css may only style its own .cat-* / .ex-* chrome. These are someone else's " +
            $"classes: {string.Join(", ", offenders)}");
    }
}
