using System.Text.RegularExpressions;
using System.Xml.Linq;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Brand files wired into things that fail quietly if renamed: the package icon, the README hero, and the catalogue's own copies.
/// </summary>
public class BrandAssetTests
{
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
}
