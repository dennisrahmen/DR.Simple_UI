using DR.Simple_UI.Catalogue.Tests.TestSupport;
using Microsoft.Playwright;

namespace DR.Simple_UI.Catalogue.Tests;

/// <summary>
/// Every page loads, and loads clean.
/// </summary>
/// <remarks>
/// A console error on a documentation site is worse than on an app: the reader
/// assumes the library is broken. Errors are collected before navigating, because a
/// listener attached afterwards misses everything the parse produced.
/// </remarks>
[Collection(CatalogueAppCollection.Name)]
public class PageLoadTests(CatalogueAppFixture app)
{
    [Fact]
    public async Task Every_page_loads_without_a_console_error()
    {
        if (app.NoBrowser) return;

        var problems = new List<string>();

        foreach (var route in RoutedPages.All)
        {
            var page = await app.Browser!.NewPageAsync();
            page.Console += (_, message) =>
            {
                if (message.Type == "error") problems.Add($"{route}: {message.Text}");
            };
            page.PageError += (_, error) => problems.Add($"{route}: {error}");

            var response = await page.GotoAsync(app.Url(route),
                new() { WaitUntil = WaitUntilState.NetworkIdle });

            if (response is null || !response.Ok)
                problems.Add($"{route}: returned {response?.Status.ToString() ?? "nothing"}");

            await page.CloseAsync();
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public async Task Every_page_renders_its_examples_and_its_navigation()
    {
        if (app.NoBrowser) return;

        var problems = new List<string>();

        foreach (var route in RoutedPages.All)
        {
            var page = await app.OpenAsync(route);

            // Scoped to .cat-drawer, which is the layout's own sidebar. A bare
            // ".sidebar .nav-link" also matches the demos on /frame and /layouts,
            // which render sidebars of their own — complete with an active link,
            // because that is what they are demonstrating.
            var sidebar = page.Locator(".cat-drawer .sidebar");

            // The sidebar is built from the registry, so a page that renders none of
            // it has lost its layout rather than its content.
            var links = await sidebar.Locator(".nav-link").CountAsync();
            if (links < 10) problems.Add($"{route}: only {links} nav links rendered");

            var active = await sidebar.Locator(".nav-link.active").CountAsync();
            if (active != 1) problems.Add($"{route}: {active} nav links are active, expected 1");

            // Server-rendered, so this holds before the circuit connects.
            var blocks = await page.Locator(".code-block pre code").AllTextContentsAsync();
            if (blocks.Any(string.IsNullOrWhiteSpace))
                problems.Add($"{route}: a code block rendered empty");

            await page.CloseAsync();
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Desktop, a phone, and the mirrored layout. A spill is a width problem, so the
    /// narrow viewport is where it shows, and a logical property written as a
    /// physical one only shows under <c>dir="rtl"</c>.
    /// </summary>
    public static TheoryData<int, string> Viewports() => new()
    {
        { 1280, "ltr" },
        { 375, "ltr" },
        { 1280, "rtl" },
    };

    [Theory]
    [MemberData(nameof(Viewports))]
    public async Task No_demo_spills_out_of_its_box(int width, string dir)
    {
        if (app.NoBrowser) return;

        // The defect this pins is the one a reader reports first: a demo whose
        // content hangs off the side of the bordered box it is in. It has three
        // causes and they all look the same — a dropdown anchored to its trailing
        // edge next to a narrow trigger, a control wider than the column, and a
        // fixed element inside a demo that ignores the box entirely. A source scan
        // cannot see any of them; only a layout engine can.
        var problems = new List<string>();

        // What is measured is each element's VISIBLE box: its own rect clipped by
        // every scrolling or hidden ancestor between it and the demo. Without that,
        // an animation that overshoots its own clipped track — the indeterminate
        // progress bar does — reads as a spill nobody can see.
        //
        // Two pixels of tolerance, because a subpixel box measured against a
        // subpixel parent differs in the last decimal on any fractional layout.
        const string measure =
            """
            () => Array.from(document.querySelectorAll('.ex-demo')).flatMap(demo => {
                const box = demo.getBoundingClientRect();
                return Array.from(demo.querySelectorAll('*')).filter(el => {
                    const style = getComputedStyle(el);
                    if (style.display === 'none' || style.visibility === 'hidden') return false;
                    if (style.position === 'fixed') return false;

                    const r = el.getBoundingClientRect();
                    if (r.width === 0 && r.height === 0) return false;

                    let left = r.left, right = r.right;
                    for (let a = el.parentElement; a && a !== demo; a = a.parentElement) {
                        if (getComputedStyle(a).overflowX === 'visible') continue;
                        const ar = a.getBoundingClientRect();
                        left = Math.max(left, ar.left);
                        right = Math.min(right, ar.right);
                    }
                    if (right <= left) return false;

                    return left < box.left - 2 || right > box.right + 2;
                }).map(el => (el.className || el.tagName) + '');
            })
            """;

        foreach (var route in RoutedPages.All)
        {
            var page = await app.OpenAsync(route);
            await page.SetViewportSizeAsync(width, 900);
            await page.EvaluateAsync("d => document.documentElement.setAttribute('dir', d)", dir);

            var spills = new List<string>(await page.EvaluateAsync<string[]>(measure));

            // Then once per delegated menu, because a closed panel is `hidden` and
            // the static render therefore cannot show the commonest cause of a
            // spill: a 220px panel anchored to the trailing edge of a narrow
            // trigger. One at a time — an open panel covers the next trigger, and
            // `data-menu-toggle` is handled by the library's own script, which runs
            // without the circuit.
            var toggles = await page.Locator(".ex-demo [data-menu-toggle]").CountAsync();
            for (var i = 0; i < toggles; i++)
            {
                var toggle = page.Locator(".ex-demo [data-menu-toggle]").Nth(i);
                await toggle.ClickAsync();
                spills.AddRange(await page.EvaluateAsync<string[]>(measure));
                await page.Keyboard.PressAsync("Escape");
            }

            foreach (var spill in spills.Distinct(StringComparer.Ordinal))
                problems.Add($"{route} at {width}px {dir}: '{spill}' extends outside its .ex-demo box");

            await page.CloseAsync();
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public async Task The_active_link_follows_navigation_without_a_reload()
    {
        if (app.NoBrowser) return;

        // The regression this pins: the sidebar survives navigation, so nothing
        // re-renders it unless it subscribes to LocationChanged ITSELF. Subscribing
        // in the layout instead compiles, runs, and does nothing — Blazor skips a
        // child component whose parameters are unchanged, so the links kept
        // reporting the address of the page the reader arrived on and only caught up
        // when some other interaction happened to change a parameter.
        var page = await app.OpenInteractiveAsync("/");
        var sidebar = page.Locator(".cat-drawer .sidebar");

        foreach (var route in (string[])["/badge", "/card", "/table", "/"])
        {
            await sidebar.Locator($".nav-link[href='{route}']").ClickAsync();

            // A Playwright timeout here names the selector, which is the whole
            // diagnosis: the link for the page we are on never became active.
            await sidebar.Locator($".nav-link.active[href='{route}']")
                .WaitForAsync(new() { Timeout = 10_000 });

            var active = await sidebar.Locator(".nav-link.active").AllAsync();
            var hrefs = await Task.WhenAll(active.Select(a => a.GetAttributeAsync("href")));

            Assert.Equal([route], hrefs.Select(h => h ?? "«no href»").ToList());
            Assert.Equal(route, await sidebar.Locator("[aria-current='page']").GetAttributeAsync("href"));
        }

        await page.CloseAsync();
    }
}
