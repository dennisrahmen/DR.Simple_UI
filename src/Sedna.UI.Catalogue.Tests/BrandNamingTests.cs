using System.Text.RegularExpressions;
using Sedna.UI.Catalogue.Tests.TestSupport;
using Sedna.UI.Tests.TestSupport;

namespace Sedna.UI.Catalogue.Tests;

/// <summary>
/// No catalogue example, page or registry entry carries the pre-Sedna brand.
/// </summary>
/// <remarks>
/// An example using a class the stylesheet no longer defines renders UNSTYLED, with
/// no error anywhere. CoverageTests catches the same mistake from the other
/// direction — a sedna-* class no example mentions fails as undocumented — but only
/// while no other example happens to use it.
/// </remarks>
public class BrandNamingTests
{
    /// <summary>
    /// A second copy of the regex in <c>Sedna.UI.Tests.BrandNamingTests.OldBrand</c>.
    /// That field is internal to the library's test project, and this project has no
    /// reference to that project's assembly — only a single linked source file
    /// (<c>Assets.cs</c>) — so the type does not resolve here. Adding an
    /// InternalsVisibleTo or a project reference would couple the two suites, which is
    /// exactly the split CLAUDE.md requires staying honest (the library suite must
    /// pass with the catalogue project deleted). Two copies of one regex is the
    /// lesser evil.
    /// </summary>
    /// <remarks>
    /// <c>DR\\?\.Simple_UI</c> tolerates an optional literal backslash before the dot
    /// so it also catches the escaped spelling (<c>DR\.Simple_UI</c>) a grep,
    /// <c>perl -pi</c> or C# regex literal writes when it needs the dot to be
    /// literal — see the matching remark on the other copy of this regex for why.
    /// </remarks>
    private static readonly Regex OldBrand = new(
        @"\bdr-|DR\\?\.Simple_UI|DrSimpleUi|drSimpleUi|drui\.|@layer\s+dr\.|\bdr\.(tokens|base|frame|paint|utilities|overrides)\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Files that name the old brand on purpose, pending a later, already-planned
    /// task, and so are not a regression.
    /// </summary>
    /// <remarks>
    /// <c>Examples/Mcp/ClaudeCode.txt</c> and <c>Examples/Mcp/Config.txt</c> register
    /// an MCP server named <c>dr-simple-ui</c> against the still-current domain.
    /// <c>docs/superpowers/plans/2026-08-04-sedna-ui-rebrand.md</c> Task 5's sweep
    /// deliberately shields this exact string with a negative lookahead
    /// (<c>\bdr-(?!simple-ui)</c>, rule R11), and Task 8 Step 1a renames both files
    /// alongside the domain swap — not this task, whose own file list is only the two
    /// BrandNamingTests files. This mirrors <c>CatalogueAssets.ContentFiles()</c>
    /// already excluding <c>wwwroot/catalogue.css</c> for the same kind of reason: a
    /// known, tracked exception is not a use of the old brand this guard exists to
    /// catch. Remove this exception once Task 8 lands — at that point it excludes
    /// nothing, which is the point.
    /// </remarks>
    private static readonly string[] PendingRenameFiles =
    [
        Path.Combine("Examples", "Mcp", "ClaudeCode.txt"),
        Path.Combine("Examples", "Mcp", "Config.txt"),
    ];

    [Fact]
    public void No_catalogue_content_file_carries_the_old_brand()
    {
        var offenders = new List<string>();

        foreach (var file in CatalogueAssets.ContentFiles())
        {
            if (PendingRenameFiles.Any(pending => file.EndsWith(pending, StringComparison.Ordinal)))
                continue;

            var found = OldBrand
                .Matches(File.ReadAllText(file))
                .Select(m => m.Value)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (found.Count > 0)
                offenders.Add(
                    $"{Path.GetRelativePath(Assets.RepoRoot, file)}: {string.Join(", ", found)}");
        }

        Assert.True(offenders.Count == 0,
            "The old brand survives in: " + string.Join("; ", offenders));
    }
}
