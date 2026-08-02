using Bunit;
using DR.Simple_UI.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Blazor matches parameters case-insensitively, so a plain class="x" at a call site binds to the Class parameter. Every component appends it rather than replacing its own frame class.
/// </summary>
public class CallSiteClassTests : BunitContext
{
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
}
