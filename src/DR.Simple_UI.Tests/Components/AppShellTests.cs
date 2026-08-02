using Bunit;
using DR.Simple_UI.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DR.Simple_UI.Tests;

/// <summary>
/// AppShell's nesting, which every page sits inside.
/// </summary>
public class AppShellTests : BunitContext
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
}
