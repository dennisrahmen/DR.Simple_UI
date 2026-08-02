using Bunit;
using DR.Simple_UI.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DR.Simple_UI.Tests;

/// <summary>
/// AppHeader always emits the spacer that separates its two sides.
/// </summary>
public class AppHeaderTests : BunitContext
{
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
}
