using System.Text.RegularExpressions;
using Bunit;
using DR.Simple_UI.Components;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// The seam between the components and the stylesheet.
/// </summary>
/// <remarks>
/// A component asks for a class by writing a string. Nothing in the compiler
/// notices when that string is misspelt, or when the class it names is renamed in
/// <c>css-parts/</c> — the app simply renders unstyled and looks broken. This
/// closes that gap from both directions.
/// </remarks>
public class ComponentClassContractTests : BunitContext
{
    /// <summary>
    /// Slot content deliberately carries no class of its own, so everything the
    /// scan finds was emitted by the component under test rather than by the test.
    /// </summary>
    private const string BareSlot = "<span data-slot=\"1\"></span>";

    private static readonly Regex ClassAttribute =
        new(@"class=""(?<value>[^""]*)""", RegexOptions.Compiled);

    /// <summary>Every component, rendered with every feature turned on.</summary>
    private List<string> RenderEverything()
    {
        var markup = new List<string>
        {
            Render<AppShell>(p => p
                .Add(c => c.Navigation, BareSlot)
                .Add(c => c.Header, BareSlot)
                .AddChildContent(BareSlot)).Markup,

            Render<AppShell>(p => p
                .Add(c => c.Bare, true)
                .Add(c => c.Header, BareSlot)
                .AddChildContent(BareSlot)).Markup,

            Render<Sidebar>(p => p
                .Add(c => c.Title, "Console")
                .Add(c => c.Subtitle, "Netpoint")
                .Add(c => c.LogoSrc, "/logo.png")
                .Add(c => c.BrandHref, "/")
                .Add(c => c.Collapsed, true)
                .AddChildContent(BareSlot)
                .Add(c => c.Tools, BareSlot)).Markup,

            Render<AppHeader>(p => p
                .Add(c => c.Start, BareSlot)
                .AddChildContent(BareSlot)).Markup,

            Render<NavItem>(p => p
                .Add(c => c.Href, "queue")
                .Add(c => c.Label, "Queue")
                .Add(c => c.Icon, "ri-inbox-line")
                .Add(c => c.Count, 3)
                .Add(c => c.Tool, true)
                .Add(c => c.External, true)
                .Add(c => c.Active, true)).Markup
        };

        var widget = Render<UserWidget>(p => p
            .Add(c => c.Name, "Dennis Rahmen")
            .Add(c => c.Secondary, "rahmen@netpoint.de")
            .Add(c => c.Initials, "DR")
            .Add(c => c.SignOutHref, "/signout")
            .Add(c => c.Menu, BareSlot));
        widget.Find("button.user-trigger").Click();     // the panel and its scrim
        markup.Add(widget.Markup);

        markup.Add(Render<UserWidget>(p => p.Add(c => c.AvatarSrc, "/me.png")).Markup);

        return markup;
    }

    private static IEnumerable<string> ClassesIn(IEnumerable<string> markup) =>
        markup
            .SelectMany(m => ClassAttribute.Matches(m).Cast<Match>())
            .SelectMany(m => m.Groups["value"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            // Remix Icon classes are defined in the vendored icon font's own
            // stylesheet, not in ours.
            .Where(c => !c.StartsWith("ri-", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal);

    [Fact]
    public void Every_class_the_components_emit_exists_in_the_stylesheet()
    {
        var css = Assets.Css;
        // No lookbehind before the dot: `active` and `collapsed` are only ever
        // written compounded, as `.nav-link.active` and `.sidebar.collapsed`, and a
        // lookbehind rejecting a preceding word character would miss both. A class
        // name always starts with a letter here, so a decimal like `1.5em` cannot be
        // mistaken for one.
        var missing = ClassesIn(RenderEverything())
            .Where(c => !Regex.IsMatch(css, $@"\.{Regex.Escape(c)}(?![\w-])"))
            .ToList();

        Assert.True(missing.Count == 0,
            "These classes are emitted by a tier-1 component but defined nowhere in "
            + "wwwroot/css/DR.Simple_UI.css, so the markup renders unstyled: "
            + string.Join(", ", missing));
    }

    [Fact]
    public void The_components_emit_the_same_frame_classes_the_catalogue_documents()
    {
        // frame.html is what a reader copies when they write the markup by hand
        // instead of using the components. The two must not describe different
        // frames.
        //
        // Anywhere on the page counts, not just inside a class attribute: one class
        // cannot be demonstrated live. .user-menu-scrim is a fixed, full-viewport
        // element, so a working demo of it would swallow every click on the page. It
        // is described in prose there instead, which is still documentation.
        var framePage = File.ReadAllText(Path.Combine(Assets.CatalogueDir, "frame.html"));

        var undocumented = ClassesIn(RenderEverything())
            .Where(c => !Regex.IsMatch(framePage, $@"(?<![\w-]){Regex.Escape(c)}(?![\w-])"))
            .ToList();

        Assert.True(undocumented.Count == 0,
            "A tier-1 component emits these classes, but catalogue/frame.html never "
            + "shows them, so hand-written markup and the components disagree: "
            + string.Join(", ", undocumented));
    }
}
