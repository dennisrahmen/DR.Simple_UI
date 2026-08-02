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
    public void The_package_takes_no_third_party_dependency()
    {
        // "Loading anything from a remote URL at runtime" is permanently out of
        // scope, and a third-party package is the same exposure moved to build time:
        // a supply-chain risk, a licence to audit, and a transitive version conflict
        // in every consuming app. Everything the package needs ships inside it.
        //
        // Microsoft.AspNetCore.Components.Web is the one allowed reference, and it is
        // unavoidable: ComponentBase, RenderFragment and NavigationManager live
        // there. It is not a FrameworkReference because that would stop a Blazor
        // WebAssembly app consuming this library.
        var project = XDocument.Load(Path.Combine(Assets.ProjectDir, "DR.Simple_UI.csproj"));

        var thirdParty = project
            .Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(id => !id.StartsWith("Microsoft.", StringComparison.Ordinal)
                      && !id.StartsWith("System.", StringComparison.Ordinal))
            .ToList();

        Assert.True(thirdParty.Count == 0,
            "The shipped package must not depend on a third-party package. Found: "
            + string.Join(", ", thirdParty));
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
    public void The_project_template_references_a_version_that_has_the_components()
    {
        // The template's generated app uses AppShell, Sidebar, NavItem, AppHeader and
        // UserWidget. Those do not exist in 0.1.0, which shipped CSS only — so a
        // template defaulting to 0.1.0 generates a project that does not compile, and
        // the first thing a new user sees is five CS0246 errors. That happened.
        var templateDir = Path.Combine(Assets.RepoRoot, "templates", "content", "dr-blazor");
        var config = Path.Combine(templateDir, ".template.config", "template.json");
        Assert.True(File.Exists(config), $"No template config at {config}");

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(config));
        var symbols = doc.RootElement.GetProperty("symbols");

        var version = symbols.GetProperty("DrSimpleUiVersion").GetProperty("defaultValue").GetString();
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.True(Version.Parse(version!) >= new Version(0, 2, 0),
            $"The template defaults to DR.Simple_UI {version}, which predates the frame components it "
            + "uses. The generated project would not compile.");

        // The layout must spell out <ChildContent>. Razor rejects loose child content
        // as soon as a component has one named RenderFragment — AppShell has
        // Navigation and Header — and fails with RZ9996. Easy to regress by
        // "simplifying" the template.
        var layout = File.ReadAllText(Path.Combine(templateDir, "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("<ChildContent>", layout, StringComparison.Ordinal);

        // The host page's asset order is the whole point of the template.
        var host = File.ReadAllText(Path.Combine(templateDir, "Components", "App.razor"));
        var boot = host.IndexOf("DR.Simple_UI.boot.js", StringComparison.Ordinal);
        var sheet = host.IndexOf("css/DR.Simple_UI.css", StringComparison.Ordinal);
        var brand = host.IndexOf("css/brand.css", StringComparison.Ordinal);
        var main = host.IndexOf("js/DR.Simple_UI.js", StringComparison.Ordinal);
        var headEnd = host.IndexOf("</head>", StringComparison.Ordinal);

        Assert.True(boot > 0 && boot < headEnd, "boot.js must be in <head>, or the theme flashes.");
        Assert.True(sheet > boot, "The stylesheet must come after boot.js.");
        Assert.True(brand > sheet, "brand.css must come after the library stylesheet.");
        Assert.True(main > headEnd, "The main script belongs at the end of <body>.");

        // All three reconnect rows, since the missing one renders as an empty bar.
        foreach (var state in new[] { "reconnect-attempting", "reconnect-failed", "reconnect-rejected" })
            Assert.Contains(state, host, StringComparison.Ordinal);
    }

    [Fact]
    public void The_token_export_matches_the_stylesheet()
    {
        // wwwroot/tokens/DR.Simple_UI.tokens.json is the token contract for consumers
        // that are not CSS — a Figma import, a report generator picking chart colours,
        // a contrast audit. Generated by build/export-tokens.sh, committed like the
        // other two generated assets, and therefore able to drift.
        //
        // Checked as invariants rather than by re-running the generator in C#: every
        // token declared anywhere in the sheet appears in the export with the same
        // value, and the export invents nothing.
        var exportPath = Path.Combine(Assets.ProjectDir, "wwwroot", "tokens", "DR.Simple_UI.tokens.json");
        Assert.True(File.Exists(exportPath),
            $"No token export at {exportPath}. Run build/export-tokens.sh");

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(exportPath));

        var exported = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var block in doc.RootElement.GetProperty("blocks").EnumerateArray())
            foreach (var token in block.GetProperty("tokens").EnumerateObject())
            {
                if (!exported.TryGetValue(token.Name, out var values))
                    exported[token.Name] = values = new HashSet<string>(StringComparer.Ordinal);
                values.Add(Squash(token.Value.GetString() ?? string.Empty));
            }

        var css = Assets.StripComments(Assets.Css);

        // Every declaration in a token block, as name → the set of values it takes
        // across the themes. Compared as sets, because a token legitimately has one
        // value per theme.
        var declared = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var (_, body) in Assets.TokenBlocks(css))
            foreach (var declaration in body.Split(';', StringSplitOptions.TrimEntries))
            {
                if (!declaration.StartsWith("--", StringComparison.Ordinal)) continue;
                var colon = declaration.IndexOf(':', StringComparison.Ordinal);
                if (colon <= 0) continue;

                var name = declaration[..colon].Trim();
                var value = Squash(declaration[(colon + 1)..].Trim());
                if (!declared.TryGetValue(name, out var values))
                    declared[name] = values = new HashSet<string>(StringComparer.Ordinal);
                values.Add(value);
            }

        var problems = new List<string>();

        foreach (var (name, values) in declared.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!exported.TryGetValue(name, out var got))
            {
                problems.Add($"{name} is in the stylesheet but not in the export");
                continue;
            }

            foreach (var missing in values.Except(got, StringComparer.Ordinal))
                problems.Add($"{name} = \"{missing}\" is in the stylesheet but not in the export");
        }

        foreach (var name in exported.Keys.Except(declared.Keys, StringComparer.Ordinal)
                     .OrderBy(n => n, StringComparer.Ordinal))
            problems.Add($"{name} is in the export but declared nowhere in the stylesheet");

        Assert.True(problems.Count == 0,
            $"Run build/export-tokens.sh:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    /// <summary>Collapses runs of whitespace, so a wrapped declaration compares equal.</summary>
    private static string Squash(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    [Fact]
    public void The_shipped_script_matches_its_parts()
        => AssertBundleMatchesParts("js-parts", "*.js", Assets.JsPath, "build/bundle-js.sh");

    [Fact]
    public void The_shipped_stylesheet_matches_its_parts()
        => AssertBundleMatchesParts("css-parts", "*.css", Assets.CssPath, "build/bundle-css.sh");

    /// <summary>
    /// Both shipped assets are generated by concatenating a directory of parts. The
    /// checks are invariants rather than a re-implementation of the generator's
    /// header formatting — two copies of the same formatting would drift against
    /// each other and fail for cosmetic reasons.
    /// </summary>
    private static void AssertBundleMatchesParts(
        string partsDirName, string pattern, string bundlePath, string script)
    {
        // Both assets are committed in generated form so a plain checkout, the
        // catalogue and the package all work with no build step. That makes drift
        // possible, which is what this guards: edit a part, forget the script, and the
        // two disagree silently — every consuming app would keep the old behaviour.
        //
        // The .NET SDK cannot generate either for us. Its only CSS bundling is scoped
        // .razor.css, which rewrites selectors and would scope tier-2 classes to markup
        // the library renders; it does nothing at all for a global script.
        //
        // Ordinal ordering matches LC_ALL=C in the scripts, so the same bytes come out
        // on Windows and on the Linux CI runner.
        var partsDir = Path.Combine(Assets.ProjectDir, partsDirName);
        Assert.True(Directory.Exists(partsDir), $"No {partsDirName} directory at {partsDir}");

        var parts = Directory.GetFiles(partsDir, pattern)
            .OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal)
            .ToList();

        Assert.True(parts.Count > 0, $"No parts found in {partsDir}");

        var bundle = File.ReadAllText(bundlePath);
        var problems = new List<string>();

        // Checked as invariants rather than by re-implementing the generator's header
        // in C#: two copies of the same formatting would drift against each other and
        // the test would start failing for cosmetic reasons.

        // 1. Ordering is the filename order, so every part needs a numeric prefix.
        foreach (var name in parts.Select(Path.GetFileName))
            if (name is null || !Regex.IsMatch(name, @"^\d\d-"))
                problems.Add($"{name} has no NN- prefix, so its position in the build is undefined");

        // 2. Every part's exact text is present, and in the same order — that is what
        //    makes the single file equivalent to the parts.
        var cursor = 0;
        foreach (var part in parts)
        {
            var text = File.ReadAllText(part);
            var at = bundle.IndexOf(text, cursor, StringComparison.Ordinal);
            if (at < 0)
            {
                problems.Add($"{Path.GetFileName(part)} is missing from the bundle, or appears out of order");
                break;
            }

            cursor = at + text.Length;
        }

        // 3. The generated contents block lists exactly the directory, so a part can
        //    never be silently absent from the build.
        var ext = Regex.Escape(Path.GetExtension(pattern));
        var listed = Regex.Matches(bundle, @"^\s{5}(\d\d-[a-z0-9-]+" + ext + @")\s*$", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value)
            .ToList();
        var expectedNames = parts.Select(Path.GetFileName).ToList();
        if (!listed.SequenceEqual(expectedNames, StringComparer.Ordinal))
            problems.Add(
                $"the generated contents block lists [{string.Join(", ", listed)}] but {partsDirName}/ holds " +
                $"[{string.Join(", ", expectedNames)}]");

        Assert.True(problems.Count == 0,
            $"{Path.GetFileName(bundlePath)} is out of step with {partsDirName}/. Run {script}:" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    [Fact]
    public void The_catalogues_brand_copies_match_their_source_in_assets_brand()
    {
        // assets/brand/ is the documented home of the brand, but the catalogue needs
        // its favicon and nav logo as static web assets so they travel with the pages
        // that use them. That makes them copies, and a copy drifts silently: update
        // the icon in its documented home and the catalogue keeps shipping the old
        // one, on the hosted site and inside every restored package.
        (string Source, string Copy)[] pairs =
        [
            (Path.Combine("assets", "brand", "favicon.ico"),
             Path.Combine("src", "DR.Simple_UI", "wwwroot", "catalogue", "favicon.ico")),
            (Path.Combine("assets", "brand", "dr-simple-ui-icon-64.png"),
             Path.Combine("src", "DR.Simple_UI", "wwwroot", "catalogue", "logo.png")),
        ];

        var offenders = new List<string>();
        foreach (var (source, copy) in pairs)
        {
            var sourcePath = Path.Combine(Assets.RepoRoot, source);
            var copyPath = Path.Combine(Assets.RepoRoot, copy);

            if (!File.Exists(sourcePath)) { offenders.Add($"{source} is missing"); continue; }
            if (!File.Exists(copyPath)) { offenders.Add($"{copy} is missing"); continue; }

            if (!File.ReadAllBytes(sourcePath).SequenceEqual(File.ReadAllBytes(copyPath)))
                offenders.Add($"{copy} differs from {source}");
        }

        Assert.True(offenders.Count == 0,
            "Re-copy the brand asset so the catalogue ships the current one: " +
            string.Join("; ", offenders));
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
