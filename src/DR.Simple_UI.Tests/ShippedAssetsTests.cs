using System.Text.RegularExpressions;
using System.Xml.Linq;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Asserts the things a consuming app's markup hard-codes: asset file names, the
/// JS global, and the packaging properties that decide what lands in the
/// <c>.nupkg</c>. Any of these changing silently breaks every app at runtime
/// rather than at build time, which is why they are pinned by a test.
/// </summary>
public class ShippedAssetsTests
{
    [Theory]
    [InlineData("wwwroot/css/DR.Simple_UI.css")]
    [InlineData("wwwroot/js/DR.Simple_UI.js")]
    [InlineData("wwwroot/js/DR.Simple_UI.boot.js")]
    [InlineData("wwwroot/catalogue/index.html")]
    [InlineData("wwwroot/catalogue/catalogue.css")]
    [InlineData("wwwroot/catalogue/catalogue.js")]
    [InlineData("wwwroot/catalogue/favicon.ico")]
    [InlineData("wwwroot/catalogue/logo.png")]
    [InlineData("wwwroot/lib/remixicon/remixicon.css")]
    [InlineData("wwwroot/lib/remixicon/remixicon.woff2")]
    [InlineData("wwwroot/lib/remixicon/LICENSE")]
    public void Shipped_asset_exists_at_its_documented_path(string relativePath)
    {
        // Consuming apps write these paths out by hand as
        // _content/DR.Simple_UI/<path-under-wwwroot>. A rename here is a silent
        // 404 there, so the names are part of the public contract.
        var full = Path.Combine(Assets.ProjectDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(full), $"Missing shipped asset: {relativePath}");
    }

    [Fact]
    public void The_javascript_global_is_drSimpleUi()
    {
        var js = File.ReadAllText(Assets.JsPath);
        Assert.Contains("window.drSimpleUi", js, StringComparison.Ordinal);
        Assert.Contains("configure", js, StringComparison.Ordinal);
    }

    [Fact]
    public void The_boot_script_and_the_main_script_share_a_default_storage_prefix()
    {
        // They read and write the same localStorage keys. If the defaults ever
        // disagree, an app that does not configure a prefix loses its theme on
        // every reload — the boot script would stamp one set of values and the
        // main script another.
        // Comments are stripped first — both files document a `storagePrefix:
        // 'myapp.'` usage example, which would otherwise match before the default.
        var boot = StripJsComments(File.ReadAllText(Assets.BootJsPath));
        var main = StripJsComments(File.ReadAllText(Assets.JsPath));

        var bootPrefix = Regex.Match(boot, @"dataset\.prefix\)\s*\|\|\s*'(?<p>[^']+)'").Groups["p"].Value;
        var mainPrefix = Regex.Match(main, @"storagePrefix:\s*'(?<p>[^']+)'").Groups["p"].Value;

        Assert.False(string.IsNullOrEmpty(bootPrefix), "Could not find the boot script's default prefix.");
        Assert.Equal(bootPrefix, mainPrefix);
    }

    /// <summary>
    /// Removes JS comments. The line-comment rule skips a <c>//</c> preceded by a
    /// colon so URLs inside string literals survive.
    /// </summary>
    private static string StripJsComments(string js)
    {
        js = Regex.Replace(js, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(js, @"(?<!:)//[^\n]*", string.Empty);
    }

    [Fact]
    public void The_scripts_carry_no_application_specific_naming()
    {
        string[] forbidden = ["atheneConsole", "athene.", "netpoint", "servicenow"];

        foreach (var path in new[] { Assets.JsPath, Assets.BootJsPath })
        {
            var js = File.ReadAllText(path);
            var found = forbidden.Where(f => js.Contains(f, StringComparison.OrdinalIgnoreCase)).ToList();

            Assert.True(found.Count == 0,
                $"{Path.GetFileName(path)} carries app-specific naming: {string.Join(", ", found)}");
        }
    }

    [Fact]
    public void The_tip_engine_leaves_the_sidebar_to_its_own_css_flyout()
    {
        // Both firing produces a double tooltip on the collapsed rail. The CSS
        // side is `.sidebar.collapsed [data-tip]:hover::after`; this is the other
        // half of that contract.
        var js = File.ReadAllText(Assets.JsPath);
        Assert.Contains("closest('.sidebar')", js, StringComparison.Ordinal);
    }

    [Fact]
    public void The_package_is_configured_the_way_the_release_workflow_expects()
    {
        var project = XDocument.Load(Path.Combine(Assets.ProjectDir, "DR.Simple_UI.csproj"));

        string? Property(string name) => project
            .Descendants(name)
            .Select(e => e.Value.Trim())
            .FirstOrDefault();

        Assert.Equal("DR.Simple_UI", Property("PackageId"));
        Assert.Equal("DR.Simple_UI", Property("AssemblyName"));
        Assert.Equal("net10.0", Property("TargetFramework"));
        Assert.Equal("true", Property("IsPackable"));
        Assert.Equal("README.md", Property("PackageReadmeFile"));
        Assert.Equal("icon.png", Property("PackageIcon"));
        Assert.False(string.IsNullOrWhiteSpace(Property("PackageLicenseExpression")),
            "nuget.org requires a license expression on a public package.");

        // The Razor SDK is what turns wwwroot into _content/DR.Simple_UI static
        // web assets. Plain Microsoft.NET.Sdk would build and pack fine and ship
        // no CSS at all.
        Assert.Equal("Microsoft.NET.Sdk.Razor", project.Root!.Attribute("Sdk")?.Value);
    }

    [Fact]
    public void The_readme_and_license_packed_into_the_nupkg_exist()
    {
        Assert.True(File.Exists(Path.Combine(Assets.RepoRoot, "README.md")));
        Assert.True(File.Exists(Path.Combine(Assets.RepoRoot, "LICENSE")));
    }

    [Fact]
    public void The_brand_assets_the_package_and_readme_depend_on_exist()
    {
        // The icon is packed as the NuGet package icon, and the social preview is
        // the README hero — which doubles as the nuget.org readme, loaded from a
        // raw.githubusercontent URL. Deleting or renaming either leaves a broken
        // image on the package listing, which nothing else would catch.
        string[] required =
        [
            Path.Combine("assets", "brand", "dr-simple-ui-icon-128.png"),
            Path.Combine("assets", "brand", "dr-simple-ui-social-preview.png"),
            Path.Combine("assets", "brand", "dr-simple-ui-icon.svg")
        ];

        var missing = required
            .Where(rel => !File.Exists(Path.Combine(Assets.RepoRoot, rel)))
            .ToList();

        Assert.True(missing.Count == 0, "Missing brand assets: " + string.Join(", ", missing));
    }

    [Fact]
    public void The_readme_hero_uses_an_absolute_image_url()
    {
        // README.md is also the nuget.org readme. Relative image paths do not
        // resolve there, and nuget.org only renders images from allow-listed
        // hosts — raw.githubusercontent.com being the usual one. A relative path
        // renders fine on GitHub and silently breaks on the package page.
        var readme = File.ReadAllText(Path.Combine(Assets.RepoRoot, "README.md"));
        var hero = readme.Split('\n').First(l => l.TrimStart().StartsWith("![", StringComparison.Ordinal));

        Assert.Contains("https://raw.githubusercontent.com/", hero, StringComparison.Ordinal);
    }

    [Fact]
    public void The_bundled_icon_font_ships_its_licence_and_copyright()
    {
        // Section 5 of the Remix Icon License: when redistributing the complete
        // library, retain the copyright notices and include a copy of the licence.
        // Both are conditions of being allowed to ship the font at all.
        var dir = Path.Combine(Assets.ProjectDir, "wwwroot", "lib", "remixicon");

        var licence = File.ReadAllText(Path.Combine(dir, "LICENSE"));
        Assert.Contains("Remix Icon License", licence, StringComparison.Ordinal);
        Assert.Contains("Remix Design", licence, StringComparison.Ordinal);

        // The upstream header must survive vendoring.
        var css = File.ReadAllText(Path.Combine(dir, "remixicon.css"));
        Assert.Contains("Copyright RemixIcon.com", css, StringComparison.Ordinal);
        Assert.Contains("remixicon.com", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_bundled_icon_font_references_only_the_woff2_that_ships()
    {
        // vendor-remixicon.sh trims the src list to woff2. If a full upstream CSS
        // is dropped in by hand, the browser requests .eot/.woff/.ttf/.svg files
        // that were never packed — four 404s per page load.
        var css = File.ReadAllText(Path.Combine(
            Assets.ProjectDir, "wwwroot", "lib", "remixicon", "remixicon.css"));

        var urls = Regex.Matches(css, @"url\(([^)]*)\)")
            .Select(m => m.Groups[1].Value.Trim('"', '\''))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["remixicon.woff2"], urls);
    }

    [Fact]
    public void The_third_party_notice_states_the_version_that_is_actually_vendored()
    {
        // Guards the drift that attribution documents always suffer: the font gets
        // updated and the notice keeps claiming the old version.
        var notices = File.ReadAllText(Path.Combine(Assets.RepoRoot, "THIRD-PARTY-NOTICES.md"));
        Assert.Contains("Remix Icon", notices, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(
            Assets.ProjectDir, "wwwroot", "lib", "remixicon", "remixicon.css"));

        var vendored = Regex.Match(css, @"Remix Icon v(?<v>\d+\.\d+\.\d+)").Groups["v"].Value;
        Assert.False(string.IsNullOrEmpty(vendored), "Could not read the version from the vendored CSS header.");

        Assert.True(notices.Contains(vendored, StringComparison.Ordinal),
            $"THIRD-PARTY-NOTICES.md does not mention the vendored version {vendored}. " +
            "Update it, and the version in the docs, after running build/vendor-remixicon.sh.");
    }

    [Fact]
    public void There_is_no_changelog_file()
    {
        // Release notes live on the GitHub Releases page, generated from each
        // annotated tag's message — written once at tagging time, alongside the
        // release they describe. A file in the working tree would be a second
        // place to keep in sync, and the one that silently goes stale.
        var stray = new[] { "CHANGELOG.md", "CHANGELOG", "CHANGES.md", "HISTORY.md", "RELEASES.md" }
            .Where(name => File.Exists(Path.Combine(Assets.RepoRoot, name)))
            .ToList();

        Assert.True(stray.Count == 0,
            "This repo deliberately has no changelog file — the Releases page is the changelog, and " +
            "notes come from the annotated tag message. See CLAUDE.md §Releasing. Found: " +
            string.Join(", ", stray));
    }
}
