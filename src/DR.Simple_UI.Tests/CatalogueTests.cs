using System.Text.RegularExpressions;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Keeps the catalogue honest. Its whole value is that the examples cannot lie,
/// which only holds while every page is styled by the one stylesheet that
/// actually ships — never a copy, never a different version.
/// </summary>
public class CatalogueTests
{
    private const string ShippedCss = "../css/DR.Simple_UI.css";
    private const string CatalogueCss = "catalogue.css";
    private const string IconFontCss = "../lib/remixicon/remixicon.css";

    private static readonly Regex StylesheetLink = new(
        @"<link\b[^>]*rel\s*=\s*""stylesheet""[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HrefValue = new(
        @"href\s*=\s*""(?<href>[^""]*)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Fact]
    public void The_catalogue_has_pages()
    {
        // Guards the other tests: a glob that silently matches nothing would make
        // every "for each page" assertion below pass vacuously.
        Assert.True(Assets.CataloguePages.Count() >= 10,
            $"Expected the full catalogue in {Assets.CatalogueDir}.");
    }

    [Fact]
    public void Every_page_links_the_stylesheet_that_ships()
    {
        var offenders = new List<string>();

        foreach (var page in Assets.CataloguePages)
        {
            var hrefs = StylesheetHrefs(File.ReadAllText(page)).ToList();
            if (!hrefs.Contains(ShippedCss, StringComparer.Ordinal))
                offenders.Add($"{Path.GetFileName(page)} — links [{string.Join(", ", hrefs)}]");
        }

        Assert.True(offenders.Count == 0,
            $"Every catalogue page must link \"{ShippedCss}\" — the exact file in the package, so " +
            "the examples and the shipped CSS are the same version by definition: " +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void No_page_links_a_copy_of_the_design_system()
    {
        var allowed = new[] { ShippedCss, CatalogueCss, IconFontCss };
        var offenders = new List<string>();

        foreach (var page in Assets.CataloguePages)
        {
            offenders.AddRange(
                StylesheetHrefs(File.ReadAllText(page))
                    .Where(h => !allowed.Contains(h, StringComparer.Ordinal))
                    .Select(h => $"{Path.GetFileName(page)} → {h}"));
        }

        Assert.True(offenders.Count == 0,
            "A catalogue page may only load the shipped stylesheet, the catalogue's own chrome, and " +
            "the pinned icon font. Anything else can show a different version than the package: " +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void The_linked_stylesheet_resolves_on_disk()
    {
        // Catches the rename that leaves every href pointing at nothing — the
        // pages would still render, just unstyled and silently wrong.
        var resolved = Path.GetFullPath(Path.Combine(Assets.CatalogueDir, ShippedCss));
        Assert.True(File.Exists(resolved), $"The catalogue's stylesheet link resolves to {resolved}, which does not exist.");
        Assert.Equal(Path.GetFullPath(Assets.CssPath), resolved);
    }

    [Fact]
    public void No_page_loads_anything_from_a_remote_host()
    {
        // The icon font is bundled, so the package has no remote dependency at
        // all. The catalogue therefore renders identically from a file:// path,
        // out of the restored package, offline, and on the hosted site.
        // Subresources only — tags the browser fetches on load. An <a href> to
        // remixicon.com is a hyperlink the reader may click, not a dependency.
        var subresource = new Regex(
            @"<(?:link|script|img|iframe|source|audio|video|embed)\b[^>]*\b(?:href|src)\s*=\s*""(?<url>[^""]*)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var offenders = new List<string>();

        foreach (var page in Assets.CataloguePages)
        {
            // Templates are NOT stripped here, unlike in StylesheetHrefs. catalogue.js
            // clones each template into the live demo, so a remote URL inside one is
            // fetched for real — it is a dependency, not inert example markup.
            var html = File.ReadAllText(page);

            offenders.AddRange(
                subresource.Matches(html)
                    .Select(m => m.Groups["url"].Value)
                    .Where(u => u.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
                             || u.StartsWith("https:", StringComparison.OrdinalIgnoreCase)
                             || u.StartsWith("//", StringComparison.Ordinal))
                    .Select(u => $"{Path.GetFileName(page)} → {u}"));
        }

        Assert.True(offenders.Count == 0,
            "The catalogue must load nothing over the network — it has to work offline and out of the " +
            $"package:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void Every_page_loads_the_bundled_icon_font()
    {
        // The examples use icon classes; without the font they render as blank
        // boxes and the spacing of every button example is wrong.
        var offenders = Assets.CataloguePages
            .Where(p => !StylesheetHrefs(File.ReadAllText(p)).Contains(IconFontCss, StringComparer.Ordinal))
            .Select(p => Path.GetFileName(p)!)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"These pages do not link \"{IconFontCss}\": {string.Join(", ", offenders)}");
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
        var css = Assets.StripComments(File.ReadAllText(Path.Combine(Assets.CatalogueDir, CatalogueCss)));

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

    [Fact]
    public void The_landing_page_figures_match_the_stylesheet()
    {
        // index.html advertises counts in .cat-fact tiles. Hand-maintained numbers on
        // a page nobody re-reads go stale silently — the token count was still 176
        // (which counted theme remaps, not distinct tokens) long after it stopped
        // being right. The definitions are fixed here so the number has a meaning:
        //   design tokens = distinct --names declared anywhere in the sheet
        //   CSS classes   = distinct .names appearing in a selector
        var css = Assets.StripComments(Assets.Css);
        var tokens = Assets.DeclaredCustomProperties(css).Count;
        var classes = Regex.Matches(css, @"\.(-?[A-Za-z_][\w-]*)", RegexOptions.Compiled)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .Count();

        var html = File.ReadAllText(Path.Combine(Assets.CatalogueDir, "index.html"));
        var stated = Regex.Matches(html, @"<strong>([\d,]+)</strong><span>([^<]+)</span>", RegexOptions.Compiled)
            .ToDictionary(
                m => m.Groups[2].Value.Trim(),
                m => m.Groups[1].Value.Replace(",", "", StringComparison.Ordinal),
                StringComparer.Ordinal);

        var problems = new List<string>();

        void Check(string label, int actual)
        {
            if (!stated.TryGetValue(label, out var claim))
                problems.Add($"index.html no longer states a figure for \"{label}\"");
            else if (claim != actual.ToString())
                problems.Add($"\"{label}\": index.html says {claim}, the stylesheet has {actual}");
        }

        Check("design tokens", tokens);
        Check("CSS classes", classes);

        Assert.True(problems.Count == 0,
            "Update the .cat-fact figures on the catalogue landing page: " + string.Join("; ", problems));
    }

    private static readonly Regex TemplateBlock = new(
        @"<template\b.*?</template>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// The stylesheets a page actually loads. <c>&lt;template&gt;</c> content is
    /// removed first: it is inert example markup — the overview page documents the
    /// <c>_content/DR.Simple_UI/…</c> link a consuming app writes, and that must
    /// not be read as this page loading a second copy.
    /// </summary>
    private static IEnumerable<string> StylesheetHrefs(string html) =>
        StylesheetLink.Matches(TemplateBlock.Replace(html, string.Empty))
            .Select(m => HrefValue.Match(m.Value))
            .Where(m => m.Success)
            .Select(m => m.Groups["href"].Value);

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
