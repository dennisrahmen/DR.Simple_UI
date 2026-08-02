using Bunit;
using DR.Simple_UI.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Sidebar's structure, the collapsed rail, and the optional brand block.
/// </summary>
public class SidebarTests : BunitContext
{
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
}
