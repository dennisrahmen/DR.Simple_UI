using System.Text.RegularExpressions;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// The landing page's figures are derived from the files they describe, not typed in.
/// </summary>
/// <remarks>
/// <para>
/// <c>build/catalogue-figures.sh</c> writes these numbers; this test derives them again,
/// independently, in C#. The duplication is deliberate. Every figure on that page has
/// been wrong at some point precisely because one implementation was checked against
/// itself: the token count sat at 176 (which counted theme remaps), the class count at
/// 317 (which counted the dotted names in <c>@layer dr.paint, …</c> as classes, after the
/// page had been updated to match a guard that did the same), and the icon count at 3,245
/// (which counted the 16 sizing utilities as icons).
/// </para>
/// <para>
/// So the definitions are fixed here, and each one means something:
/// </para>
/// <list type="bullet">
/// <item>design tokens — distinct <c>--names</c> declared anywhere in the sheet</item>
/// <item>CSS classes — distinct <c>.names</c> in a selector, excluding the layer names</item>
/// <item>bundled icons — distinct <c>.ri-*</c> classes that carry a glyph</item>
/// </list>
/// </remarks>
public class FigureTests
{
    [Fact]
    public void The_landing_page_figures_match_the_stylesheet()
    {
        var css = Assets.StripComments(Assets.Css);
        var tokens = Assets.DeclaredCustomProperties(css).Count;
        var classes = Assets.ClassSelectors(css).Count;
        var icons = Assets.IconGlyphClasses(File.ReadAllText(Assets.IconCssPath)).Count;

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
        Check("bundled icons", icons);

        Assert.True(problems.Count == 0,
            "Run build/catalogue-figures.sh to update the landing page: " + string.Join("; ", problems));
    }
}
