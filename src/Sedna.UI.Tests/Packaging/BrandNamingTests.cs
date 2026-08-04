using System.Text.RegularExpressions;
using Sedna.UI.Tests.TestSupport;

namespace Sedna.UI.Tests;

/// <summary>
/// No trace of the pre-Sedna brand survives in anything that ships.
/// </summary>
/// <remarks>
/// The rename moved a class name, a keyframes name, a DOM id, a cascade layer and a
/// storage prefix. Every one of those fails SILENTLY when only one side of a pair
/// moves: an unmatched keyframes name disables an animation, a stale utility class
/// renders unstyled, a stale storage prefix loses a stored setting. This is the cheap
/// check that stops one returning through a copy-pasted part.
///
/// Deliberately not repository-wide: docs/migrating-from-dr-simple-ui.md exists in
/// order to name the old brand.
/// </remarks>
public class BrandNamingTests
{
    /// <summary>
    /// The old brand's markers. <c>dr-</c> carries a word boundary so
    /// <c>--dialog-backdrop</c> and friends are not false positives.
    /// </summary>
    /// <remarks>
    /// <c>DR\\?\.Simple_UI</c> tolerates an optional literal backslash before the dot,
    /// so it catches both <c>DR.Simple_UI</c> and the escaped spelling
    /// (<c>DR\.Simple_UI</c>) that a grep, <c>perl -pi</c> or C# regex literal writes
    /// when it needs the dot to be literal — exactly the shape of a real leftover
    /// this rebrand hit once already (an escaped verification pattern that survived a
    /// sweep). Without this, that spelling reappearing in a catalogue example or a
    /// shipped comment would pass silently.
    /// </remarks>
    internal static readonly Regex OldBrand = new(
        @"\bdr-|DR\\?\.Simple_UI|DrSimpleUi|drSimpleUi|drui\.|@layer\s+dr\.|\bdr\.(tokens|base|frame|paint|utilities|overrides)\b",
        RegexOptions.Compiled);

    public static IEnumerable<object[]> ShippedAssets() =>
    [
        [Assets.CssPath], [Assets.JsPath], [Assets.BootJsPath], [Assets.TokensPath]
    ];

    [Theory]
    [MemberData(nameof(ShippedAssets))]
    public void No_shipped_asset_carries_the_old_brand(string path)
    {
        var found = OldBrand.Matches(File.ReadAllText(path))
            .Select(m => m.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(found.Count == 0,
            $"{Path.GetFileName(path)} still carries: {string.Join(", ", found)}");
    }
}
