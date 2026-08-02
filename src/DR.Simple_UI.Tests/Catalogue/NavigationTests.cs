using System.Text.RegularExpressions;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Pages and the navigation agree in both directions, so neither an orphan page nor a dead entry survives.
/// </summary>
public class NavigationTests
{
    [Fact]
    public void The_catalogue_has_pages()
    {
        // Guards the other tests: a glob that silently matches nothing would make
        // every "for each page" assertion below pass vacuously.
        Assert.True(Assets.CataloguePages.Count() >= 10,
            $"Expected the full catalogue in {Assets.CatalogueDir}.");
    }

    [Fact]
    public void Every_page_is_reachable_from_the_navigation()
    {
        var nav = NavHrefs();
        var orphans = Assets.CataloguePages
            .Select(Path.GetFileName)
            .Where(f => f is not null && !nav.Contains(f!, StringComparer.Ordinal))
            .ToList();

        Assert.True(orphans.Count == 0,
            "These pages exist but nothing links to them — add them to CAT_PAGES in catalogue.js: " +
            string.Join(", ", orphans));
    }

    [Fact]
    public void Every_navigation_entry_points_at_a_real_page()
    {
        var missing = NavHrefs()
            .Where(h => !File.Exists(Path.Combine(Assets.CatalogueDir, h)))
            .ToList();

        Assert.True(missing.Count == 0,
            $"CAT_PAGES in catalogue.js links pages that do not exist: {string.Join(", ", missing)}");
    }

    /// <summary>The page list from CAT_PAGES in catalogue.js — the single source of the nav.</summary>
    private static ISet<string> NavHrefs()
    {
        var js = File.ReadAllText(Path.Combine(Assets.CatalogueDir, "catalogue.js"));
        var start = js.IndexOf("const CAT_PAGES", StringComparison.Ordinal);
        Assert.True(start >= 0, "CAT_PAGES not found in catalogue.js.");

        var end = js.IndexOf("];", start, StringComparison.Ordinal);
        Assert.True(end > start, "CAT_PAGES is not terminated in catalogue.js.");

        return Regex.Matches(js[start..end], @"href:\s*'(?<href>[^']+)'")
            .Select(m => m.Groups["href"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }
}
