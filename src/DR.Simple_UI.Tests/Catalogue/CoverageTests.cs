using System.Text.RegularExpressions;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Every class the stylesheet defines must be findable in the catalogue.
/// </summary>
/// <remarks>
/// "A class with no catalogue page is a class nobody can find" is a rule in
/// <c>CLAUDE.md</c>, and until this test existed nothing enforced it: the catalogue
/// guards only checked that pages and <c>CAT_PAGES</c> agreed with each other, not
/// that the classes were covered. The first run found 97 undocumented classes; all
/// were documented, so there is no allowlist here and there should never be one — a
/// class is either shown or it is not shipped.
/// </remarks>
public class CatalogueCoverageTests
{
    /// <summary>
    /// Classes the library styles but does not own: Blazor sets them, and they are
    /// documented on the Shell &amp; nav page as the reconnect banner rather than by
    /// class name. The cascade layer names used to be listed here too — they are not
    /// classes at all, and <see cref="Assets.ClassSelectors"/> now drops them by
    /// stripping the <c>@layer</c> preludes, so adding a layer needs no edit here.
    /// </summary>
    private static readonly HashSet<string> NotOurClasses = new(StringComparer.Ordinal)
    {
        "components-reconnect-show", "components-reconnect-failed", "components-reconnect-rejected"
    };

    [Fact]
    public void Every_class_in_the_stylesheet_is_shown_somewhere_in_the_catalogue()
    {
        var css = Assets.StripComments(Assets.Css);
        var classes = Assets.ClassSelectors(css)
            .Where(c => !NotOurClasses.Contains(c))
            .ToHashSet(StringComparer.Ordinal);

        // Anywhere in the catalogue counts — a class attribute, a <code> mention in
        // prose, or a CAT_PAGES blurb. The point is that a reader can find it. One
        // class genuinely cannot be demonstrated live: .menu-scrim is a fixed,
        // full-viewport element, so a working demo would swallow every click on the
        // page. It is described in prose instead, which this still accepts.
        var catalogue = string.Concat(Assets.CataloguePages.Select(File.ReadAllText))
            + File.ReadAllText(Path.Combine(Assets.CatalogueDir, "catalogue.js"));

        var undocumented = classes
            .Where(c => !Regex.IsMatch(catalogue, $@"(?<![\w-]){Regex.Escape(c)}(?![\w-])"))
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        Assert.True(undocumented.Count == 0,
            "These classes are in the stylesheet but shown nowhere in the catalogue. Add an example — "
            + "a class nobody can find is a class nobody uses:"
            + $"{Environment.NewLine}  {string.Join(", ", undocumented)}");
    }
}
