using System.Text.RegularExpressions;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Every page is styled by the one stylesheet that ships — never a copy, never a remote host.
/// </summary>
public class StylesheetSourceTests
{

    private static readonly Regex StylesheetLink = new(
        @"<link\b[^>]*rel\s*=\s*""stylesheet""[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HrefValue = new(
        @"href\s*=\s*""(?<href>[^""]*)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Fact]
    public void Every_page_links_the_stylesheet_that_ships()
    {
        var offenders = new List<string>();

        foreach (var page in Assets.CataloguePages)
        {
            var hrefs = StylesheetHrefs(File.ReadAllText(page)).ToList();
            if (!hrefs.Contains(Assets.ShippedCssHref, StringComparer.Ordinal))
                offenders.Add($"{Path.GetFileName(page)} — links [{string.Join(", ", hrefs)}]");
        }

        Assert.True(offenders.Count == 0,
            $"Every catalogue page must link \"{Assets.ShippedCssHref}\" — the exact file in the package, so " +
            "the examples and the shipped CSS are the same version by definition: " +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void No_page_links_a_copy_of_the_design_system()
    {
        var allowed = new[] { Assets.ShippedCssHref, Assets.CatalogueCssFile, Assets.IconCssHref };
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
        var resolved = Path.GetFullPath(Path.Combine(Assets.CatalogueDir, Assets.ShippedCssHref));
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
            .Where(p => !StylesheetHrefs(File.ReadAllText(p)).Contains(Assets.IconCssHref, StringComparer.Ordinal))
            .Select(p => Path.GetFileName(p)!)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"These pages do not link \"{Assets.IconCssHref}\": {string.Join(", ", offenders)}");
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
}
