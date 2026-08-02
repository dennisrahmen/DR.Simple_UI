using Microsoft.Playwright;

namespace DR.Simple_UI.Tests.TestSupport;

/// <summary>
/// Opens the shipped catalogue pages in a browser, for the tests that assert what a CSS
/// engine actually computes.
/// </summary>
/// <remarks>
/// The source-scanning guards cannot see the one failure mode this library really
/// suffers: a rule that parses fine, is never reported by anything, and silently does
/// nothing because a more specific rule already set the property. Three of those shipped
/// and were caught this way — <c>.col-num</c> losing <c>text-align</c> to
/// <c>.table td</c>, the zebra stripe outranking the hover highlight, and an even
/// striped row outranking <c>aria-selected</c>.
/// </remarks>
public abstract class CatalogueBrowserTestBase : BrowserTestBase
{
    /// <summary>The catalogue is loaded over file:// — it links only relative assets.</summary>
    protected static string PageUrl(string name) =>
        new Uri(Path.Combine(Assets.CatalogueDir, name)).AbsoluteUri;

    /// <summary>
    /// Opens a catalogue page, collects its console errors, and returns both.
    /// </summary>
    protected async Task<(IPage Page, List<string> Errors)> Open(string name)
    {
        var page = await Browser!.NewPageAsync();
        var errors = new List<string>();

        page.Console += (_, msg) => { if (msg.Type == "error") errors.Add($"{name}: {msg.Text}"); };
        page.PageError += (_, err) => errors.Add($"{name}: uncaught {err}");

        await page.GotoAsync(PageUrl(name));

        // Wait on the navigation, not on the examples: tokens.html has no <template>
        // examples at all — it renders swatches read out of the loaded stylesheet — so
        // waiting for .ex-code would hang there for ever. The sidebar is the one thing
        // catalogue.js builds on every page.
        //
        // catalogue.js runs at the end of the body and builds synchronously, so this is
        // normally already true on arrival. On failure the page's own state is the only
        // useful diagnosis: a bare "timeout after 30s" does not distinguish "the script
        // 404'd" from "the script threw" from "this page has none of that".
        try
        {
            await page.WaitForFunctionAsync(
                "() => document.querySelectorAll('.nav-link').length > 0",
                null, new() { Timeout = 10_000 });
        }
        catch (TimeoutException)
        {
            var diagnosis = await page.EvaluateAsync<string>(@"() => JSON.stringify({
                url: location.href,
                scripts: [...document.scripts].map(s => s.src || '(inline)'),
                globalPresent: typeof CAT_PAGES !== 'undefined',
                libraryPresent: typeof window.drSimpleUi !== 'undefined',
                examples: document.querySelectorAll('.cat-ex').length,
                templates: document.querySelectorAll('.cat-ex template').length,
                built: document.querySelectorAll('.ex-code').length
            })");

            throw new InvalidOperationException(
                $"{name}: catalogue.js did not build the page shell. Page state: {diagnosis}. "
                + $"Console: {string.Join(" | ", errors)}");
        }

        return (page, errors);
    }
}
