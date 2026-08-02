using Bunit;
using DR.Simple_UI.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DR.Simple_UI.Tests;

/// <summary>
/// NavItem owns its own active matching, which is why it exists rather than wrapping NavLink.
/// </summary>
public class NavItemTests : BunitContext
{
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
}
