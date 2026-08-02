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
    // color-mix and light-dark are listed explicitly: `color` alone does not match
    // them, because the hyphen sits where this pattern expects the paren. Both are
    // legitimate inside a token block and must not leak outside one.
    private static readonly Regex ColourFunction = new(
        @"\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color-mix|light-dark|color)\s*\(",
        RegexOptions.Compiled);
    // The lookarounds reject hyphens as well as word characters, so `white-space`,
    // `--badge-cyan-bg` and `border-color` are not mistaken for colour keywords.
    private static readonly Regex NamedColour = new(
        @"(?<![\w-])(?:white|black|red|green|blue|gray|grey|orange|yellow|purple|cyan|teal|" +
        @"magenta|silver|navy|olive|lime|maroon|aqua|fuchsia|pink|brown|gold|beige|" +
        @"ivory|khaki|salmon|tan|violet|indigo|crimson|tomato)(?![\w-])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // CSS system colours. Legal ONLY as a token remap inside
    // @media (forced-colors: active) — which lands inside a :root block and is
    // therefore masked out before this scan. Anywhere else they pin a value a theme
    // cannot change, exactly like a hex.
    //
    // Matched case-sensitively on the conventional CamelCase spelling, deliberately.
    // CSS keywords are case-insensitive, but matching case-insensitively would make
    // `mark { … }` and `field-sizing` collide with Mark and Field. The authoritative
    // check on placement is Appearance_media_queries_only_remap_tokens; this is the
    // second line of defence.
    private static readonly Regex SystemColour = new(
        @"(?<![\w-])(?:Canvas|CanvasText|ButtonFace|ButtonText|ButtonBorder|LinkText|" +
        @"VisitedText|ActiveText|GrayText|Highlight|HighlightText|SelectedItem|" +
        @"SelectedItemText|AccentColor|AccentColorText)(?![\w-])",
        RegexOptions.Compiled);

    [Fact]
    public void No_hard_coded_colours_outside_the_token_blocks()
    {
        var css = Assets.StripComments(Assets.Css);

        // Inside a forced-colors block the system palette IS the token layer: the
        // browser has already replaced every colour with the user's choice, and
        // `CanvasText` is the name of one of them. A focus outline there cannot go
        // through a var() — box-shadow is not painted in that mode, so the ring has to
        // be a real outline in a real system colour. Everywhere else the keywords stay
        // banned, which is what the line-range check preserves.
        var forcedColourLines = ForcedColourLineNumbers(css);

        var offenders = new List<string>();
        foreach (var (line, number) in Assets.LinesOutsideTokenBlocks(css))
        {
            if (HexColour.IsMatch(line)) offenders.Add($"line {number}: hex — {line.Trim()}");
            else if (ColourFunction.IsMatch(line)) offenders.Add($"line {number}: colour function — {line.Trim()}");
            else if (NamedColour.IsMatch(line)) offenders.Add($"line {number}: named colour — {line.Trim()}");
            else if (SystemColour.IsMatch(line) && !forcedColourLines.Contains(number))
                offenders.Add($"line {number}: system colour — {line.Trim()}");
        }

        Assert.True(offenders.Count == 0,
            "Every colour in the library must resolve through a token, or an app cannot rebrand " +
            "by redefining tokens. Move these into the :root blocks and reference them with " +
            $"var(--…):{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    /// <summary>
    /// The 1-based line numbers that fall inside a <c>@media (forced-colors: active)</c>
    /// block, so the system-colour keywords may be recognised there and nowhere else.
    /// </summary>
    private static HashSet<int> ForcedColourLineNumbers(string css)
    {
        var inside = new HashSet<int>();

        foreach (var open in Regex.Matches(css, @"@media[^{]*forced-colors[^{]*\{").Cast<Match>())
        {
            var depth = 1;
            var i = open.Index + open.Length;
            var line = css.Take(open.Index).Count(c => c == '\n') + 1;

            while (i < css.Length && depth > 0)
            {
                if (css[i] == '{') depth++;
                else if (css[i] == '}') depth--;
                else if (css[i] == '\n') { line++; inside.Add(line); }
                i++;
            }
        }

        return inside;
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

        // A rule scoped to an appearance attribute that also targets a descendant
        // means a value escaped the token layer. Density is exempt: it changes
        // geometry (table padding), which is not a colour and not a token.
        //
        // The attribute list is an allowlist rather than `data-[a-z-]+`, so that
        // ordinary attribute selectors (`[data-tip]`) are not swept up. ADD ANY NEW
        // APPEARANCE ATTRIBUTE HERE — a theme this test does not know about is a
        // theme that may quietly override selectors. `:root` is optional because
        // `[data-theme="light"] .btn { }` is the same mistake written shorter.
        var offenders = Regex.Matches(
                css,
                @"(?::root)?(?:\[[^\]]*\])*\[data-(?:theme|cvd|contrast)=[^\]]*\](?:\[[^\]]*\])*\s+[^{,]+\{",
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
        // The tokens an app is documented to redefine. docs/getting-started.md's
        // rebrand recipe and the consuming-app CLAUDE.md template both name this
        // family; the README carries no token list. Renaming one silently breaks
        // every app's brand file, which makes it a MAJOR version change. This test
        // is the tripwire. The five --brand-*ring*/tint/glow names stay listed even
        // though they are now derived from --brand: an app may still pin them, so
        // removing the name would still be breaking.
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

    [Fact]
    public void The_stylesheet_neither_loads_nor_inlines_anything()
    {
        // Two rules in one. `url(` would either fetch at runtime — which no customer
        // site may depend on — or point at a file that has to be packed and version-
        // matched. `data:` is the sneakier half: an inlined SVG is still a shipped
        // asset, and it smuggles a colour past the three colour patterns above,
        // because a percent-encoded `%23fff` carries no literal `#` and base64
        // carries nothing recognisable at all.
        var css = Assets.StripComments(Assets.Css);

        var offenders = css.Split('\n')
            .Select((line, index) => (Line: line.Trim(), Number: index + 1))
            .Where(x => x.Line.Contains("url(", StringComparison.OrdinalIgnoreCase)
                     || x.Line.Contains("data:", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"line {x.Number}: {x.Line}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "The stylesheet must reference no external file and inline no asset. Draw the mark in " +
            "CSS, or use a glyph from the bundled icon font on a pseudo-element. An inlined data: " +
            $"URI also hard-codes a colour the token contract forbids:{Environment.NewLine}" +
            string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void Every_root_rule_parses_as_a_token_block()
    {
        // The token-block parser requires a body with no braces. CSS nesting inside a
        // :root rule is valid CSS but stops matching, and the block then silently
        // leaves the token layer: its lines re-enter the colour scan and
        // Token_blocks_declare_only_custom_properties stops seeing it at all. Fail
        // loudly here instead, so adopting nesting means fixing the parser first.
        var css = Assets.StripComments(Assets.Css);

        var opened = Regex.Matches(css, @":root(?:\[[^\]]*\])*\s*\{", RegexOptions.Compiled).Count;
        var parsed = Assets.TokenBlocks(css).Count();

        Assert.True(opened == parsed,
            $"{opened} :root rules open but {parsed} parse as token blocks. A :root rule whose body " +
            "contains a nested rule or at-rule is no longer recognised as a token block and drops out " +
            "of every token guard silently. Make the parser brace-aware before nesting inside :root.");
    }

    [Fact]
    public void The_library_uses_no_important_declarations()
    {
        // !important defeats the override model twice. Unlayered, it beats an app's
        // ordinary override. Layered, it is worse: layer order inverts for important
        // declarations, so a layered !important outranks an app's own !important and
        // the app has no way left to win. Raise specificity instead.
        var css = Assets.StripComments(Assets.Css);

        var offenders = css.Split('\n')
            .Select((line, index) => (Line: line.Trim(), Number: index + 1))
            .Where(x => x.Line.Contains("!important", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"line {x.Number}: {x.Line}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "An app must always be able to override a library rule, so the library declares nothing " +
            "!important. Give the rule enough specificity to win on its own instead:" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void Every_z_index_comes_from_the_documented_scale()
    {
        // The documented overlay scale in docs/architecture.md and CLAUDE.md. A new
        // overlay picks one of these; it does not invent a value, because the only
        // way to reason about six overlay families is for the list to be closed.
        // 0 and 1 are allowed for local stacking inside a component (a sticky table
        // header above its own rows), which is not part of the overlay scale.
        int[] documented = [0, 1, 60, 200, 400, 480, 490, 500, 510, 550, 600, 1000];

        // catalogue.css is included: it is the only other stylesheet that ships, and
        // it draws on the same scale. Its drawer once sat on the spotlight rung with
        // its scrim on the modal-backdrop rung, which would have interleaved a drawer
        // with a real modal — precisely the mistake a shared scale exists to prevent.
        var sheets = new[]
        {
            (Name: "DR.Simple_UI.css", Css: Assets.StripComments(Assets.Css)),
            (Name: "catalogue/catalogue.css",
             Css: Assets.StripComments(File.ReadAllText(Path.Combine(Assets.CatalogueDir, "catalogue.css")))),
        };

        var offenders = sheets
            .SelectMany(s => Regex.Matches(s.Css, @"z-index\s*:\s*(-?\d+)", RegexOptions.Compiled)
                .Select(m => (s.Name, Value: int.Parse(m.Groups[1].Value))))
            .Where(x => !documented.Contains(x.Value))
            .Distinct()
            .OrderBy(x => x.Value)
            .Select(x => $"{x.Name}: {x.Value}")
            .ToList();

        Assert.True(offenders.Count == 0,
            "These z-index values are not on the documented scale. Either use a documented layer or " +
            "add the new layer to the scale in docs/architecture.md and CLAUDE.md first, so the " +
            $"ordering stays reviewable: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Appearance_media_queries_only_remap_tokens()
    {
        // The light and colour-blind themes are pure token remaps, and that is what
        // makes CSS load order irrelevant. A media query that varies *appearance* is
        // the same kind of block and carries the same obligation — with the sharper
        // edge that @media adds source order without adding specificity, which is
        // exactly how the five light-theme cascade bugs found migrating AI_Console
        // outranked the semantic rules above them.
        //
        // Layout media queries (min-width / max-width / orientation / print) are NOT
        // covered: a responsive frame has to move real selectors, and a print sheet
        // has to hide chrome. Only appearance is constrained.
        string[] appearance = ["prefers-color-scheme", "prefers-contrast", "forced-colors"];

        var css = Assets.StripComments(Assets.Css);

        var offenders = new List<string>();
        foreach (var (condition, body) in Assets.MediaBlocks(css))
        {
            if (!appearance.Any(f => condition.Contains(f, StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (var (selector, ruleBody) in Assets.TopLevelRules(body))
            {
                // A token block is `:root` plus attribute filters and NOTHING else.
                // StartsWith(":root") would wave through `:root .btn` and
                // `:root[data-theme="light"] .table`, which are the very overrides
                // this guard exists to catch.
                if (Regex.IsMatch(selector, @"^:root(?:\[[^\]]*\])*$")) continue;

                // forced-colors is the one appearance query with legitimate non-token
                // rules, and there are exactly two kinds.
                //
                // `forced-color-adjust` opts an element out of the forced palette,
                // for the few things whose actual colour IS the content — a status
                // dot is not a coloured label, it is the label.
                //
                // `outline` restates a focus ring. Every ring in this library is a
                // box-shadow, and forced colours does not paint box-shadow at all —
                // so without these rules the library becomes unusable by keyboard for
                // exactly the people most likely to be in that mode. An outline is
                // what the mode does paint, and it cannot be expressed as a token.
                string[] permitted = ["forced-color-adjust", "outline"];

                var declarations = ruleBody
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(d => d.Length > 0)
                    .ToList();

                if (declarations.Count > 0 && declarations.TrueForAll(
                        d => permitted.Any(p => d.StartsWith(p, StringComparison.Ordinal))))
                    continue;

                offenders.Add($"@media {condition} → {selector}");
            }
        }

        Assert.True(offenders.Count == 0,
            "An appearance media query must remap tokens on :root, not restyle selectors — otherwise " +
            "load order becomes load-bearing again. Express the difference as token values. The only " +
            "permitted exception is a rule whose declarations are all forced-color-adjust:" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    /// <summary>
    /// Properties a layout media query may set freely. Everything else has to come
    /// from a token, so appearance stays decided in one place.
    /// </summary>
    private static readonly HashSet<string> GeometryProperties = new(StringComparer.Ordinal)
    {
        "width", "min-width", "max-width", "height", "min-height", "max-height",
        "aspect-ratio", "object-fit", "visibility", "content", "pointer-events",
        "clip-path", "border-spacing", "border-collapse",
        // Print flow, and the outlines forced colours needs.
        "break-inside", "break-before", "break-after", "word-break", "overflow-wrap",
        "outline", "outline-offset", "table-layout",
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
        "padding-block", "padding-inline", "padding-inline-start", "padding-inline-end",
        "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
        "margin-block", "margin-inline", "margin-inline-start", "margin-inline-end",
        "display", "flex", "flex-direction", "flex-wrap", "flex-shrink", "flex-grow",
        "flex-basis", "align-items", "align-self", "justify-content", "order",
        "gap", "row-gap", "column-gap",
        "grid-template-columns", "grid-template-rows", "grid-column", "grid-row",
        "position", "top", "right", "bottom", "left",
        "inset", "inset-block", "inset-inline", "inset-block-start", "inset-block-end",
        "inset-inline-start", "inset-inline-end",
        "transform", "overflow", "overflow-x", "overflow-y", "z-index",
        "border-radius", "border-width", "border-style",
        "font-size", "font-weight", "line-height", "letter-spacing",
        "text-align", "text-transform", "white-space", "text-overflow",
        // Switching motion off is not an appearance decision.
        "transition", "animation"
    };

    [Fact]
    public void Layout_media_queries_only_change_geometry()
    {
        // A width query answers "how much room is there", never "what should this
        // look like". Appearance is decided once, in the token blocks, so that a
        // rebrand is a token edit and a theme is a token remap — a colour set inside
        // a breakpoint is invisible to both and reappears at one window size.
        //
        // A non-geometry property is still allowed when its value comes entirely from
        // tokens: the collapsed rail's flyout has to restate its surface, and doing so
        // through var() keeps the decision in the token block where it belongs.
        //
        // Scoped by an allowlist of conditions rather than by excluding appearance
        // ones, so a new kind of media query is not silently waved through. The
        // others are each somebody else's business: appearance queries have their own
        // stricter guard above, `prefers-reduced-motion` exists to turn motion off,
        // and a capability query (`hover`, `pointer`) legitimately reveals a control
        // that hover would otherwise have revealed.
        string[] layoutFeatures = ["width", "height", "orientation", "print"];

        var css = Assets.StripComments(Assets.Css);
        var offenders = new List<string>();

        foreach (var (condition, body) in Assets.MediaBlocks(css))
        {
            if (!layoutFeatures.Any(f => condition.Contains(f, StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (var (selector, ruleBody) in Assets.TopLevelRules(body))
            {
                foreach (var declaration in ruleBody.Split(';', StringSplitOptions.TrimEntries))
                {
                    if (declaration.Length == 0) continue;

                    var colon = declaration.IndexOf(':', StringComparison.Ordinal);
                    if (colon <= 0) continue;

                    var property = declaration[..colon].Trim();
                    var value = declaration[(colon + 1)..].Trim();

                    if (property.StartsWith("--", StringComparison.Ordinal)) continue;
                    if (GeometryProperties.Contains(property)) continue;
                    // Token-only values are fine: the decision still lives in :root.
                    if (value.Contains("var(--", StringComparison.Ordinal)) continue;
                    if (value is "transparent" or "currentColor" or "none" or "inherit") continue;

                    offenders.Add($"@media {condition} → {selector} → {property}: {value}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "A layout media query may only change geometry, or set a property whose value comes from a " +
            "token. Anything else decides appearance at one window size, where neither a rebrand nor a " +
            "theme remap can reach it:" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [Fact]
    public void The_responsive_frame_mirrors_the_collapsed_rail()
    {
        // 19-frame-responsive.css repeats 12-frame-collapsed-rail.css with a
        // different trigger, because CSS cannot alias a selector: there is no way to
        // say "also apply the rail when this media query matches". Duplication is
        // therefore forced, and duplication drifts — a rule added to the rail and not
        // to the responsive arm means the narrow-screen rail is subtly broken, on a
        // width nobody develops at.
        var rail = Assets.StripComments(ReadPart("12-frame-collapsed-rail.css"));
        var responsive = Assets.StripComments(ReadPart("19-frame-responsive.css"));

        // The rail part has no media wrapper; the responsive part is entirely inside
        // one, so its rules are read out of the block bodies.
        var railSelectors = Selectors(rail, ".sidebar.collapsed");

        var responsiveSelectors = Assets.MediaBlocks(responsive)
            .SelectMany(m => Selectors(m.Body, ".layout--responsive .sidebar"))
            .ToHashSet(StringComparer.Ordinal);

        var missing = railSelectors.Except(responsiveSelectors).ToList();
        var extra = responsiveSelectors.Except(railSelectors).ToList();

        Assert.True(missing.Count == 0 && extra.Count == 0,
            "12-frame-collapsed-rail.css and 19-frame-responsive.css must cover the same selectors, or "
            + "the forced rail below 900px behaves differently from the toggled one."
            + (missing.Count > 0 ? $"{Environment.NewLine}Only in the rail: {string.Join(", ", missing)}" : "")
            + (extra.Count > 0 ? $"{Environment.NewLine}Only in the responsive arm: {string.Join(", ", extra)}" : ""));
    }

    /// <summary>
    /// A directional property that has a logical equivalent. Matched on the property
    /// name at the start of a declaration, so `border-inline-start` and a value
    /// containing the word "left" are both untouched.
    /// </summary>
    private static readonly Regex PhysicalDirection = new(
        @"(?<![\w-])(?:"
        + @"margin-(?:left|right)|padding-(?:left|right)|"
        + @"border-(?:left|right)(?:-(?:color|width|style))?|"
        + @"border-(?:top|bottom)-(?:left|right)-radius|"
        + @"(?:left|right)\s*:|"
        + @"text-align\s*:\s*(?:left|right)"
        + @")",
        RegexOptions.Compiled);

    [Fact]
    public void Every_rule_in_the_stylesheet_is_inside_a_cascade_layer()
    {
        // The override model rests on this one invariant. An unlayered rule beats every
        // layered rule whatever its specificity — so a rule that escaped its layer
        // would outrank the entire rest of the library AND be unreachable from a
        // consuming app's own stylesheet, which is unlayered too. The app would have
        // no way to override it short of !important, which this library does not use.
        //
        // The generator wraps each part by its NN- prefix, so the only way to get here
        // is a hand edit of the generated file — which the drift guard also catches —
        // or a bug in layer_for().
        var css = Assets.StripComments(Assets.Css);

        var problems = new List<string>();
        var depth = 0;
        var line = 1;
        var atRuleStart = -1;

        for (var i = 0; i < css.Length; i++)
        {
            var c = css[i];
            if (c == '\n') { line++; continue; }

            if (c == '{')
            {
                if (depth == 0)
                {
                    var selector = css[(atRuleStart + 1)..i].Trim();
                    if (!selector.StartsWith("@layer", StringComparison.Ordinal))
                        problems.Add($"line {line}: {Squash(selector)}");
                }

                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0) atRuleStart = i;
            }
            else if (depth == 0 && c == ';')
            {
                // The `@layer a, b, c;` ordering statement, and nothing else.
                atRuleStart = i;
            }
        }

        Assert.True(problems.Count == 0,
            "Every rule must sit inside a @layer block, or it outranks the whole library and no "
            + "consuming app can override it without !important. These are at the top level:"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    [Fact]
    public void The_layer_order_is_declared_before_any_layer_is_used()
    {
        // Without the up-front `@layer a, b, c;` statement, layer order is the order in
        // which each layer first appears — which would make it depend on the numeric
        // prefix of whichever part happens to come first, and change silently when a
        // part is added. The statement pins it.
        var css = Assets.StripComments(Assets.Css);

        var statement = Regex.Match(css, @"@layer\s+(?<names>[a-z.\s,]+?)\s*;");
        Assert.True(statement.Success, "No `@layer a, b, c;` ordering statement in the stylesheet.");

        var declared = statement.Groups["names"].Value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        Assert.Equal(["dr.tokens", "dr.base", "dr.frame", "dr.paint", "dr.utilities", "dr.overrides"],
            declared);

        var firstBlock = css.IndexOf("@layer " + declared[0] + " {", StringComparison.Ordinal);
        Assert.True(firstBlock > statement.Index,
            "The ordering statement must come before the first @layer block.");

        // Every layer that is used must be declared, or it is appended after all the
        // declared ones and silently outranks them.
        var used = Regex.Matches(css, @"@layer\s+(dr\.[a-z]+)\s*\{")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var undeclared = used.Except(declared, StringComparer.Ordinal).ToList();
        Assert.True(undeclared.Count == 0,
            "These layers are used but not in the ordering statement, so they sort after every "
            + "declared layer: " + string.Join(", ", undeclared));
    }

    /// <summary>Collapses whitespace so a multi-line selector reports on one line.</summary>
    private static string Squash(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    [Fact]
    public void Spacing_type_and_motion_ride_their_scales()
    {
        // Same reasoning as the colour rule, applied to the other three things an app
        // may want to rescale: a literal is invisible to the token layer, so one
        // hard-coded 14px means "make this app denser" cannot reach that rule.
        //
        // Scoped to the properties that genuinely express space, size of type, and
        // duration. Widths, heights and offsets are excluded on purpose — a 28px close
        // button and a 34px switch track are dimensions of a thing, not space between
        // things, and forcing them onto a spacing ramp would let a density change break
        // the switch.
        //
        // 0 and 1px are always allowed: zero is not a scale step, and a 1px hairline is
        // a border, not spacing.
        const string spacingProps =
            @"padding|padding-top|padding-right|padding-bottom|padding-left|"
            + @"padding-block|padding-inline|padding-inline-start|padding-inline-end|"
            + @"margin|margin-top|margin-right|margin-bottom|margin-left|"
            + @"margin-block|margin-inline|margin-inline-start|margin-inline-end|"
            + @"gap|row-gap|column-gap";

        var css = Assets.StripComments(Assets.Css);
        var offenders = new List<string>();

        // The token block declares the scales, so it is the one place literals live.
        var tokenRanges = TokenBlockRanges(css);
        bool InTokenBlock(int index) => tokenRanges.Any(r => index >= r.Start && index < r.End);

        void Scan(string pattern, string what)
        {
            foreach (var m in Regex.Matches(css, pattern).Cast<Match>())
            {
                if (InTokenBlock(m.Index)) continue;

                var value = m.Groups["value"].Value;
                foreach (var lit in Regex.Matches(value, @"(?<![\w.-])(\d+)px(?![\w-])").Cast<Match>())
                {
                    if (lit.Groups[1].Value is "0" or "1") continue;
                    var line = css.Take(m.Index).Count(c => c == '\n') + 1;
                    offenders.Add($"line {line}: {what} — {Squash(m.Value)}");
                }
            }
        }

        Scan($@"(?<![\w-])(?:{spacingProps})\s*:(?<value>[^;}}]+)", "spacing");
        Scan(@"(?<![\w-])font-size\s*:(?<value>[^;}]+)", "type");

        // Durations: any bare `<n>s` outside the token block.
        foreach (var m in Regex.Matches(css, @"(?<![\w-])(?:transition|animation)\s*:(?<value>[^;}]+)").Cast<Match>())
        {
            if (InTokenBlock(m.Index)) continue;
            foreach (var _ in Regex.Matches(m.Groups["value"].Value, @"(?<![\w.])\d*\.?\d+s(?![\w])"))
            {
                var line = css.Take(m.Index).Count(c => c == '\n') + 1;
                offenders.Add($"line {line}: motion — {Squash(m.Value)}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Spacing, type sizes and durations must come from --space-*, --text-* and --motion-* / "
            + "--*-duration, or an app cannot rescale them. A literal here is invisible to the token "
            + "layer:" + $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    /// <summary>Character ranges covered by a token block, so its own literals are exempt.</summary>
    private static List<(int Start, int End)> TokenBlockRanges(string css) =>
        Regex.Matches(css, @"(?::root(?:\[[^\]]*\])*)\s*\{[^{}]*\}")
            .Select(m => (m.Index, m.Index + m.Length))
            .ToList();

    [Fact]
    public void No_physical_direction_properties_without_a_justification()
    {
        // Every directional property here is logical — margin-inline-start,
        // border-inline-end, inset-inline, text-align: start — which is what lets the
        // whole layout mirror from dir="rtl" with almost no rules at all. 70-rtl.css is
        // three-quarters comment for that reason.
        //
        // The exceptions are real gaps in CSS rather than shortcuts, so each one has to
        // say why on the line above it: an `/* rtl-ok: … */` marker. Two exist —
        // centring (where a translate does the offset, so direction is irrelevant) and
        // the hover hint, whose `left` is overwritten in pixels by the script anyway.
        //
        // The marker is required on the PRECEDING line rather than anywhere in the
        // file, so it cannot drift away from what it justifies.
        var offenders = new List<string>();

        foreach (var part in Directory.GetFiles(Path.Combine(Assets.ProjectDir, "css-parts"), "*.css")
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            var lines = Assets.StripComments(File.ReadAllText(part)).Split('\n');
            var raw = File.ReadAllText(part).Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                if (!PhysicalDirection.IsMatch(lines[i])) continue;

                // A [dir="rtl"] rule is the whole point of 70-rtl.css: it exists to
                // express what a logical property cannot.
                if (Path.GetFileName(part) == "70-rtl.css") continue;

                // The marker may sit on the same line or anywhere in the comment block
                // immediately above it — a one-line window would reject a two-line
                // justification, which is most of them.
                var justified = raw[i].Contains("rtl-ok:", StringComparison.Ordinal);
                for (var back = 1; back <= 3 && !justified && i - back >= 0; back++)
                    justified = raw[i - back].Contains("rtl-ok:", StringComparison.Ordinal);
                if (!justified)
                    offenders.Add($"{Path.GetFileName(part)}:{i + 1}: {lines[i].Trim()}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Use the logical property — margin-inline-start, border-inline-end, inset-inline, "
            + "text-align: start — so the layout mirrors from dir=\"rtl\" on its own. Where physical "
            + "really is correct, put an /* rtl-ok: why */ comment on the line above:"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static string ReadPart(string name) =>
        File.ReadAllText(Path.Combine(Assets.ProjectDir, "css-parts", name));

    /// <summary>
    /// Every individual selector in a block, with <paramref name="trigger"/> replaced
    /// by a placeholder so the two triggers compare equal. Comma-grouped selectors are
    /// split apart, so regrouping rules is not mistaken for a change. Selectors that
    /// never mention the trigger — the responsive arm also hides parts of the user
    /// widget, which the rail has no counterpart for — are dropped.
    /// </summary>
    private static HashSet<string> Selectors(string blockBody, string trigger) =>
        Assets.TopLevelRules(blockBody)
            .SelectMany(r => r.Selector.Split(','))
            .Select(s => Regex.Replace(s.Trim(), @"\s+", " "))
            .Where(s => s.StartsWith(trigger, StringComparison.Ordinal))
            .Select(s => "«rail»" + s[trigger.Length..])
            .ToHashSet(StringComparer.Ordinal);
}
