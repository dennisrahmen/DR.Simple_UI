using System.Text.RegularExpressions;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Mechanically enforces the token contract, so it holds because the build says
/// so and not because a document asks nicely.
/// </summary>
public class CssTokenContractTests
{
    // Colour literals. `transparent` and `currentColor` are allowed: neither
    // pins a value that a theme would need to change.
    private static readonly Regex HexColour = new(@"#[0-9a-fA-F]{3,8}\b", RegexOptions.Compiled);
    private static readonly Regex ColourFunction = new(
        @"\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color)\s*\(", RegexOptions.Compiled);
    // The lookarounds reject hyphens as well as word characters, so `white-space`,
    // `--badge-cyan-bg` and `border-color` are not mistaken for colour keywords.
    private static readonly Regex NamedColour = new(
        @"(?<![\w-])(?:white|black|red|green|blue|gray|grey|orange|yellow|purple|cyan|teal|" +
        @"magenta|silver|navy|olive|lime|maroon|aqua|fuchsia|pink|brown|gold|beige|" +
        @"ivory|khaki|salmon|tan|violet|indigo|crimson|tomato)(?![\w-])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Fact]
    public void No_hard_coded_colours_outside_the_token_blocks()
    {
        var css = Assets.StripComments(Assets.Css);

        var offenders = new List<string>();
        foreach (var (line, number) in Assets.LinesOutsideTokenBlocks(css))
        {
            if (HexColour.IsMatch(line)) offenders.Add($"line {number}: hex — {line.Trim()}");
            else if (ColourFunction.IsMatch(line)) offenders.Add($"line {number}: colour function — {line.Trim()}");
            else if (NamedColour.IsMatch(line)) offenders.Add($"line {number}: named colour — {line.Trim()}");
        }

        Assert.True(offenders.Count == 0,
            "Every colour in the library must resolve through a token, or an app cannot rebrand " +
            "by redefining tokens. Move these into the :root blocks and reference them with " +
            $"var(--…):{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void Token_blocks_declare_only_custom_properties()
    {
        var css = Assets.StripComments(Assets.Css);

        var offenders = new List<string>();
        foreach (var (selector, blockBody) in Assets.TokenBlocks(css))
        {
            var declarations = blockBody
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(d => d.Length > 0);

            offenders.AddRange(
                declarations
                    .Where(d => !d.StartsWith("--", StringComparison.Ordinal))
                    .Select(d => $"{selector} {{ {d} }}"));
        }

        Assert.True(offenders.Count == 0,
            "A token block defines values, it does not style anything. Move these declarations to " +
            $"a real selector:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void Every_referenced_token_is_declared()
    {
        var css = Assets.StripComments(Assets.Css);

        var declared = Assets.DeclaredCustomProperties(css);
        var referenced = Assets.ReferencedCustomProperties(css);
        var missing = referenced
            .Except(declared, StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            "These tokens are used but never declared, so they silently resolve to nothing: " +
            string.Join(", ", missing));
    }

    [Fact]
    public void Theme_blocks_only_remap_tokens_and_never_override_selectors()
    {
        var css = Assets.StripComments(Assets.Css);

        // A rule scoped to data-theme or data-cvd that also targets a descendant
        // means a value escaped the token layer. Density is exempt: it changes
        // geometry (table padding), which is not a colour and not a token.
        var offenders = Regex.Matches(
                css,
                @":root(?:\[[^\]]*\])*\[data-(?:theme|cvd)=[^\]]*\](?:\[[^\]]*\])*\s+[^{,]+\{",
                RegexOptions.Compiled)
            .Select(m => m.Value.Trim())
            .ToList();

        Assert.True(offenders.Count == 0,
            "The light and colour-blind themes must be pure token remapping — that is what keeps " +
            "load order from being load-bearing. Express these as tokens instead: " +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void The_documented_override_tokens_exist()
    {
        // The tokens an app is documented to redefine — the README, the catalogue
        // overview and the consuming-app CLAUDE.md template all show this exact
        // list. Renaming one silently breaks every app's brand file, which makes
        // it a MAJOR version change. This test is the tripwire.
        string[] required =
        [
            "--brand", "--brand-hover", "--brand-active", "--brand-soft", "--brand-text",
            "--brand-tint", "--brand-ring", "--brand-ring-soft", "--brand-ring-check",
            "--brand-glow", "--accent", "--sidebar-active"
        ];

        var declared = Assets.DeclaredCustomProperties(Assets.StripComments(Assets.Css));
        var missing = required.Where(t => !declared.Contains(t)).ToList();

        Assert.True(missing.Count == 0,
            "Renaming or removing a documented override token breaks every consuming app's brand " +
            $"file. Missing: {string.Join(", ", missing)}");
    }

    [Fact]
    public void No_application_specific_naming_leaked_into_the_library()
    {
        // The library was extracted from one app's stylesheet. These names are
        // that app's; if one reappears in a selector, something app-specific came
        // along with it. Comments are stripped first — describing where a rule
        // came from is fine, shipping the app's classes is not.
        string[] forbidden =
        [
            "athene", "zbx", "sn-journal", "queue-grid", "topics-grid", "calls-grid",
            "queue-group-header", "guide-", "chooser-", "tour-pop", "tour-spot", "claim-overlay",
            "gsearch"
        ];

        var css = Assets.StripComments(Assets.Css);
        var found = forbidden
            .Where(f => css.Contains(f, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(found.Count == 0,
            "App-specific naming does not belong in the shared library — these stay in the app " +
            $"that owns them: {string.Join(", ", found)}");
    }

    [Fact]
    public void Fonts_ride_tokens_so_an_app_can_change_typeface()
    {
        var css = Assets.StripComments(Assets.Css);

        // font-family: inherit is fine — a control adopting its host's font.
        var offenders = Assets.LinesOutsideTokenBlocks(css)
            .Where(x => Regex.IsMatch(x.Line, @"font-family\s*:"))
            .Where(x => !x.Line.Contains("var(--font-", StringComparison.Ordinal))
            .Where(x => !Regex.IsMatch(x.Line, @"font-family\s*:\s*inherit"))
            .Select(x => $"line {x.Number}: {x.Line.Trim()}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "Use var(--font-sans) / var(--font-mono) so a consuming app can change typeface: " +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }
}
