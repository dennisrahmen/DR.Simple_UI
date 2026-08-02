using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using DR.Simple_UI.Tests.TestSupport;
using Microsoft.Playwright;

namespace DR.Simple_UI.Tests;

/// <summary>
/// axe-core over every catalogue page.
/// </summary>
/// <remarks>
/// <para>
/// The catalogue is not a marketing site: every page is a rendered example of the
/// library's own markup, so an axe violation here is a violation an app will inherit
/// by copying the example. That makes this the cheapest place in the project to catch
/// one.
/// </para>
/// <para>
/// It complements rather than replaces the other two layers. The source scans check
/// the CSS contract, the browser tests check what a style engine computes, and this
/// checks the rules that only exist in the accessibility tree — a control with no
/// name, a label pointing at nothing, an ARIA attribute on an element that cannot
/// carry it.
/// </para>
/// <para>
/// Gated exactly like <see cref="BrowserTests"/>: a missing browser is a failure, not
/// a silent pass.
/// </para>
/// </remarks>
public class AccessibilityTests : IAsyncLifetime
{
    private const string OptOutEnvVar = "DR_UI_BROWSER_TESTS";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _unavailable;

    private static bool OptedOut => Environment.GetEnvironmentVariable(OptOutEnvVar) == "0";

    public async Task InitializeAsync()
    {
        try
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        }
        catch (Exception ex)
        {
            _unavailable = ex.Message.Split('\n')[0];
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task Every_catalogue_page_passes_axe()
    {
        if (OptedOut) return;
        Assert.True(_unavailable is null, $"No browser: {_unavailable}");

        var problems = new List<string>();

        foreach (var path in Assets.CataloguePages)
        {
            var name = Path.GetFileName(path);
            var page = await _browser!.NewPageAsync();
            await page.GotoAsync(new Uri(path).AbsoluteUri);
            await page.WaitForFunctionAsync(
                "() => document.querySelectorAll('.nav-link').length > 0",
                null, new() { Timeout = 10_000 });

            var results = await page.RunAxe(new AxeRunOptions
            {
                // WCAG 2.1 A and AA — the level docs/accessibility.md claims. Best
                // practice rules are excluded: they are opinions, and one of them
                // ("region": all content in a landmark) fails on the demo fragments
                // this catalogue is made of by design.
                RunOnly = new RunOnlyOptions
                {
                    Type = "tag",
                    Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]
                }
            });

            foreach (var violation in results.Violations)
            {
                var where = string.Join("; ", violation.Nodes
                    .Take(3)
                    .Select(n => n.Target.ToString()));

                problems.Add(
                    $"{name}: [{violation.Impact}] {violation.Id} — {violation.Help}"
                    + $"{Environment.NewLine}      {where}");
            }

            await page.CloseAsync();
        }

        Assert.True(problems.Count == 0,
            $"axe found WCAG 2.1 AA violations in the catalogue's own examples, which every app that "
            + $"copies them inherits:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }
}
