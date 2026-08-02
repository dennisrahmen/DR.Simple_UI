using Bunit;
using DR.Simple_UI.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DR.Simple_UI.Tests;

/// <summary>
/// The markup the tier-1 components emit.
/// </summary>
/// <remarks>
/// These assert element names and class names rather than appearance, because that
/// is what a consuming app is coupled to: a changed class or a changed nesting
/// order silently restyles four apps, and the release rules call it Major. The CSS
/// itself is covered by <see cref="CssTokenContractTests"/>; here we only care that
/// the components ask for the classes the stylesheet actually defines — which
/// <c>Every_class_the_components_emit_exists_in_the_stylesheet</c> checks directly.
/// </remarks>
public class FrameComponentTests : BunitContext
{
    // ── AppShell ────────────────────────────────────────────────────────────

    [Fact]
    public void AppShell_nests_layout_content_and_page()
    {
        var cut = Render<AppShell>(p => p
            .Add(c => c.Navigation, "<aside class=\"sidebar\"></aside>")
            .Add(c => c.Header, "<header class=\"topbar\"></header>")
            .AddChildContent("<h1>Queue</h1>"));

        // .page must be inside .content, which must be inside .layout, and the
        // sidebar must be a sibling of .content — not of .page.
        Assert.NotNull(cut.Find(".layout > .content > .page"));
        Assert.NotNull(cut.Find(".layout > aside.sidebar"));
        Assert.NotNull(cut.Find(".content > header.topbar"));
        Assert.Equal("Queue", cut.Find(".page h1").TextContent);
    }

    [Fact]
    public void AppShell_bare_drops_the_sidebar_and_the_content_column()
    {
        var cut = Render<AppShell>(p => p
            .Add(c => c.Bare, true)
            .Add(c => c.Navigation, "<aside class=\"sidebar\"></aside>")
            .Add(c => c.Header, "<header class=\"topbar\"></header>")
            .AddChildContent("<p>No access</p>"));

        Assert.NotNull(cut.Find(".bare-layout > header.topbar"));
        Assert.NotNull(cut.Find(".bare-layout > .page"));
        Assert.Empty(cut.FindAll(".layout"));
        Assert.Empty(cut.FindAll(".content"));
        // Navigation is ignored rather than rendered somewhere unstyled.
        Assert.Empty(cut.FindAll(".sidebar"));
    }

    [Fact]
    public void AppShell_page_class_is_appended_and_never_replaces_page()
    {
        var cut = Render<AppShell>(p => p.Add(c => c.PageClass, "queue-grid"));

        var page = cut.Find("div.page");
        Assert.Equal("page queue-grid", page.GetAttribute("class"));
    }

    // ── Sidebar ─────────────────────────────────────────────────────────────

    [Fact]
    public void Sidebar_emits_the_aside_nav_and_scroll_container()
    {
        var cut = Render<Sidebar>(p => p
            .Add(c => c.Title, "Approval Console")
            .Add(c => c.Subtitle, "Netpoint")
            .AddChildContent("<a class=\"nav-link\" href=\"#\">Queue</a>"));

        Assert.NotNull(cut.Find("aside.sidebar > nav.nav > .nav-scroll > .nav-link"));
        Assert.Equal("Approval Console", cut.Find(".brand .brand-text strong").TextContent);
        Assert.Equal("Netpoint", cut.Find(".brand .brand-sub").TextContent);
        // No tools slot supplied: no empty footer that would still draw its border
        // and its shadow.
        Assert.Empty(cut.FindAll(".nav-tools"));
    }

    [Fact]
    public void Sidebar_collapsed_adds_the_rail_class_and_nothing_else()
    {
        var expanded = Render<Sidebar>(p => p.Add(c => c.Title, "X"));
        var collapsed = Render<Sidebar>(p => p
            .Add(c => c.Title, "X")
            .Add(c => c.Collapsed, true));

        Assert.Equal("sidebar", expanded.Find("aside").GetAttribute("class"));
        Assert.Equal("sidebar collapsed", collapsed.Find("aside").GetAttribute("class"));

        // The rail is pure CSS: the markup inside must be identical, or the
        // collapse animation has something to reflow.
        Assert.Equal(
            expanded.Find("nav.nav").InnerHtml,
            collapsed.Find("nav.nav").InnerHtml);
    }

    [Fact]
    public void Sidebar_brand_is_a_link_only_when_it_has_somewhere_to_go()
    {
        var plain = Render<Sidebar>(p => p.Add(c => c.Title, "X"));
        var linked = Render<Sidebar>(p => p
            .Add(c => c.Title, "X")
            .Add(c => c.BrandHref, "/"));

        Assert.Equal("DIV", plain.Find(".brand").NodeName);
        Assert.Equal("A", linked.Find(".brand").NodeName);
        Assert.Equal("/", linked.Find("a.brand").GetAttribute("href"));
    }

    [Fact]
    public void Sidebar_renders_no_brand_block_when_it_has_no_content()
    {
        var cut = Render<Sidebar>();
        Assert.Empty(cut.FindAll(".brand"));
    }

    // ── AppHeader ───────────────────────────────────────────────────────────

    [Fact]
    public void AppHeader_always_puts_the_spacer_between_the_two_sides()
    {
        var cut = Render<AppHeader>(p => p
            .Add(c => c.Start, "<button class=\"topbar-btn topbar-btn--start\">L</button>")
            .AddChildContent("<button class=\"topbar-btn\">R</button>"));

        var children = cut.Find("header.topbar").Children;
        Assert.Equal(3, children.Length);
        Assert.Equal("L", children[0].TextContent);
        Assert.Equal("topbar-spacer", children[1].GetAttribute("class"));
        Assert.Equal("R", children[2].TextContent);
    }

    [Fact]
    public void AppHeader_emits_the_spacer_even_with_no_content()
    {
        // Without it a header holding only trailing controls would left-align them.
        var cut = Render<AppHeader>();
        Assert.NotNull(cut.Find("header.topbar > .topbar-spacer"));
    }

    // ── NavItem ─────────────────────────────────────────────────────────────

    [Fact]
    public void NavItem_emits_the_icon_label_and_count()
    {
        var cut = Render<NavItem>(p => p
            .Add(c => c.Href, "queue")
            .Add(c => c.Label, "Queue")
            .Add(c => c.Icon, "ri-inbox-line")
            .Add(c => c.Count, 3));

        Assert.NotNull(cut.Find("a.nav-link > i.ri-inbox-line"));
        Assert.Equal("Queue", cut.Find("a.nav-link > span:not(.nav-count)").TextContent);
        Assert.Equal("3", cut.Find(".nav-count").TextContent);
    }

    [Fact]
    public void NavItem_renders_no_count_pill_when_the_count_is_null()
    {
        var cut = Render<NavItem>(p => p.Add(c => c.Href, "queue").Add(c => c.Label, "Queue"));
        Assert.Empty(cut.FindAll(".nav-count"));
    }

    [Theory]
    // Exact match, and a trailing slash on either side, are one address.
    [InlineData("http://localhost/queue", "queue", NavLinkMatch.All, true)]
    [InlineData("http://localhost/queue/", "queue", NavLinkMatch.All, true)]
    [InlineData("http://localhost/queue", "queue/", NavLinkMatch.All, true)]
    // A query string or a fragment does not change which page you are on.
    [InlineData("http://localhost/queue?page=2", "queue", NavLinkMatch.All, true)]
    [InlineData("http://localhost/queue#top", "queue", NavLinkMatch.All, true)]
    // All matches only the page itself; Prefix also matches below it.
    [InlineData("http://localhost/queue/42", "queue", NavLinkMatch.All, false)]
    [InlineData("http://localhost/queue/42", "queue", NavLinkMatch.Prefix, true)]
    // The boundary check: a prefix must end on a path segment.
    [InlineData("http://localhost/queue-archive", "queue", NavLinkMatch.Prefix, false)]
    [InlineData("http://localhost/queued", "queue", NavLinkMatch.Prefix, false)]
    // The root link with the default Prefix is active everywhere, which is why it
    // needs Match="All" — the same trap the framework's NavLink has.
    [InlineData("http://localhost/queue", "", NavLinkMatch.Prefix, true)]
    [InlineData("http://localhost/queue", "", NavLinkMatch.All, false)]
    [InlineData("http://localhost/", "", NavLinkMatch.All, true)]
    public void NavItem_active_state_follows_the_address(
        string current, string href, NavLinkMatch match, bool expected)
    {
        Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>()
            .NavigateTo(current);

        var cut = Render<NavItem>(p => p
            .Add(c => c.Href, href)
            .Add(c => c.Label, "X")
            .Add(c => c.Match, match));

        var link = cut.Find("a");
        Assert.Equal(expected, link.ClassList.Contains("active"));
        // The class alone only colours the item; aria-current is what is announced.
        Assert.Equal(expected ? "page" : null, link.GetAttribute("aria-current"));
    }

    [Fact]
    public void NavItem_reacts_to_navigation_after_the_first_render()
    {
        var nav = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
        nav.NavigateTo("http://localhost/topics");

        var cut = Render<NavItem>(p => p
            .Add(c => c.Href, "queue")
            .Add(c => c.Label, "Queue")
            .Add(c => c.Match, NavLinkMatch.All));

        Assert.DoesNotContain("active", cut.Find("a").ClassList);

        cut.InvokeAsync(() => nav.NavigateTo("http://localhost/queue"));

        Assert.Contains("active", cut.Find("a").ClassList);
        Assert.Equal("page", cut.Find("a").GetAttribute("aria-current"));
    }

    [Fact]
    public void NavItem_active_parameter_overrides_address_matching()
    {
        Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>()
            .NavigateTo("http://localhost/queue");

        var forcedOff = Render<NavItem>(p => p
            .Add(c => c.Href, "queue").Add(c => c.Label, "Q").Add(c => c.Active, false));
        var forcedOn = Render<NavItem>(p => p
            .Add(c => c.Href, "elsewhere").Add(c => c.Label, "E").Add(c => c.Active, true));

        Assert.DoesNotContain("active", forcedOff.Find("a").ClassList);
        Assert.Contains("active", forcedOn.Find("a").ClassList);
    }

    [Fact]
    public void NavItem_with_no_href_is_never_active()
    {
        Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>()
            .NavigateTo("http://localhost/queue");

        var cut = Render<NavItem>(p => p.Add(c => c.Label, "Q"));
        Assert.DoesNotContain("active", cut.Find("a").ClassList);
    }

    [Fact]
    public void NavItem_tool_and_external_add_their_classes_and_link_safety()
    {
        var cut = Render<NavItem>(p => p
            .Add(c => c.Href, "https://example.invalid/docs")
            .Add(c => c.Label, "Documentation")
            .Add(c => c.Icon, "ri-book-2-line")
            .Add(c => c.Tool, true)
            .Add(c => c.External, true));

        var link = cut.Find("a");
        Assert.Contains("nav-link", link.ClassList);
        Assert.Contains("nav-link-tool", link.ClassList);
        Assert.Equal("_blank", link.GetAttribute("target"));
        // Without noopener the opened tab can reach back through window.opener.
        Assert.Equal("noopener", link.GetAttribute("rel"));
        Assert.NotNull(cut.Find("i.nav-link-ext"));
    }

    [Fact]
    public void NavItem_class_parameter_is_appended_never_substituted()
    {
        var cut = Render<NavItem>(p => p
            .Add(c => c.Href, "queue")
            .Add(c => c.Label, "Q")
            .Add(c => c.Class, "app-highlight"));

        Assert.Equal("nav-link app-highlight", cut.Find("a").GetAttribute("class"));
    }

    [Fact]
    public void A_class_attribute_written_at_the_call_site_is_appended_not_substituted()
    {
        // Blazor matches parameters case-insensitively, so a plain class="…" at the
        // call site binds to the Class parameter rather than landing in
        // AdditionalAttributes and overwriting the frame class. That is why every
        // component in this library declares Class: it turns the destructive spelling
        // into the additive one, and an app that writes class="x" out of habit gets
        // the frame class kept for free.
        var cut = Render<NavItem>(p => p
            .Add(c => c.Href, "queue")
            .Add(c => c.Label, "Q")
            .AddUnmatched("class", "app-highlight"));

        Assert.Equal("nav-link app-highlight", cut.Find("a").GetAttribute("class"));
    }

    [Theory]
    [InlineData("layout")]
    [InlineData("sidebar")]
    [InlineData("topbar")]
    [InlineData("user-widget")]
    public void No_component_lets_a_call_site_class_replace_its_frame_class(string frameClass)
    {
        var markup = frameClass switch
        {
            "layout" => Render<AppShell>(p => p.AddUnmatched("class", "app-x")).Markup,
            "sidebar" => Render<Sidebar>(p => p.AddUnmatched("class", "app-x")).Markup,
            "topbar" => Render<AppHeader>(p => p.AddUnmatched("class", "app-x")).Markup,
            _ => Render<UserWidget>(p => p.AddUnmatched("class", "app-x")).Markup
        };

        Assert.Contains($"class=\"{frameClass} app-x\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void NavItem_tip_becomes_the_data_tip_attribute()
    {
        var cut = Render<NavItem>(p => p
            .Add(c => c.Href, "queue").Add(c => c.Label, "Q")
            .Add(c => c.Tip, "Items waiting on a decision."));

        Assert.Equal("Items waiting on a decision.", cut.Find("a").GetAttribute("data-tip"));
        // Absent, not empty: an empty data-tip would make the engine open a blank
        // bubble.
        var without = Render<NavItem>(p => p.Add(c => c.Href, "q").Add(c => c.Label, "Q"));
        Assert.False(without.Find("a").HasAttribute("data-tip"));
    }

    // ── UserWidget ──────────────────────────────────────────────────────────

    [Fact]
    public void UserWidget_trigger_is_not_a_button_when_there_is_nothing_to_open()
    {
        var cut = Render<UserWidget>(p => p
            .Add(c => c.Name, "Dennis Rahmen")
            .Add(c => c.Secondary, "rahmen@netpoint.de"));

        Assert.Equal("DIV", cut.Find(".user-trigger").NodeName);
        Assert.Equal("Dennis Rahmen", cut.Find(".user-name").TextContent);
        Assert.Equal("rahmen@netpoint.de", cut.Find(".user-email").TextContent);
        Assert.NotNull(cut.Find(".user-avatar i.ri-user-line"));
        Assert.Empty(cut.FindAll(".user-signout"));
    }

    [Fact]
    public void UserWidget_avatar_falls_back_from_image_to_initials_to_the_icon()
    {
        var image = Render<UserWidget>(p => p
            .Add(c => c.AvatarSrc, "/me.png").Add(c => c.Initials, "DR"));
        var initials = Render<UserWidget>(p => p.Add(c => c.Initials, "DR"));
        var icon = Render<UserWidget>(p => p.Add(c => c.Name, "X"));

        Assert.Equal("/me.png", image.Find(".user-avatar img").GetAttribute("src"));
        Assert.Equal("DR", initials.Find(".user-avatar span").TextContent);
        Assert.NotNull(icon.Find(".user-avatar i.ri-user-line"));
    }

    [Fact]
    public void UserWidget_menu_opens_on_click_and_closes_on_the_scrim()
    {
        var opened = new List<bool>();
        var cut = Render<UserWidget>(p => p
            .Add(c => c.Name, "Dennis Rahmen")
            .Add(c => c.Menu, "<a class=\"menu-item\" href=\"/settings\">Settings</a>")
            .Add(c => c.OpenChanged, opened.Add));

        var trigger = cut.Find("button.user-trigger");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll(".menu"));

        trigger.Click();

        Assert.Equal("true", cut.Find("button.user-trigger").GetAttribute("aria-expanded"));
        Assert.NotNull(cut.Find(".menu .menu-item"));

        cut.Find(".menu-scrim").Click();

        Assert.Empty(cut.FindAll(".menu"));
        Assert.Equal([true, false], opened);
    }

    [Fact]
    public void UserWidget_menu_closes_on_escape()
    {
        var cut = Render<UserWidget>(p => p
            .Add(c => c.Menu, "<button class=\"menu-item\">Settings</button>"));

        cut.Find("button.user-trigger").Click();
        Assert.NotNull(cut.Find(".menu"));

        cut.Find(".user-widget").KeyDown(Key.Escape);

        Assert.Empty(cut.FindAll(".menu"));
    }

    [Fact]
    public void UserWidget_scrim_is_ordered_before_the_panel()
    {
        // The panel paints above the scrim on source order plus its own z-index. If
        // the scrim came second it would cover the panel and eat the first click on
        // every menu item.
        var cut = Render<UserWidget>(p => p
            .Add(c => c.Menu, "<button class=\"menu-item\">S</button>"));
        cut.Find("button.user-trigger").Click();

        var children = cut.Find(".user-widget").Children.Select(c => c.GetAttribute("class")).ToArray();
        Assert.Equal(
            Array.IndexOf(children, "menu-scrim") + 1,
            Array.IndexOf(children, "menu"));
    }

    [Fact]
    public void UserWidget_sign_out_is_an_icon_only_link_with_a_name()
    {
        var cut = Render<UserWidget>(p => p
            .Add(c => c.SignOutHref, "/signout")
            .Add(c => c.SignOutTip, "Sign out of the console."));

        var link = cut.Find("a.user-signout");
        Assert.Equal("/signout", link.GetAttribute("href"));
        Assert.Equal("Sign out", link.GetAttribute("aria-label"));
        Assert.Equal("Sign out of the console.", link.GetAttribute("data-tip"));
    }
}
