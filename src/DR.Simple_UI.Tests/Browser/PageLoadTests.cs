using DR.Simple_UI.Tests.TestSupport;
using Microsoft.Playwright;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Every catalogue page loads and builds its demos with no console error.
/// </summary>
public class PageLoadTests : CatalogueBrowserTestBase
{
    [Fact]
    public async Task Every_catalogue_page_builds_its_demos_without_a_console_error()
    {
        if (NoBrowser) return;

        var problems = new List<string>();

        foreach (var path in Assets.CataloguePages)
        {
            var name = Path.GetFileName(path);
            var (page, errors) = await Open(name);

            // Every .cat-ex must produce a code block, and every one that is not
            // data-code-only must also produce a live demo. A page whose JS threw
            // half way through would otherwise look merely short.
            var counts = await page.EvaluateAsync<int[]>(@"() => [
                document.querySelectorAll('.cat-ex').length,
                document.querySelectorAll('.cat-ex .ex-code').length,
                document.querySelectorAll('.cat-ex:not([data-code-only])').length,
                document.querySelectorAll('.cat-ex:not([data-code-only]) .ex-demo').length,
                document.querySelectorAll('.nav-link').length
            ]");

            if (counts[0] != counts[1])
                problems.Add($"{name}: {counts[0]} examples but {counts[1]} code blocks");
            if (counts[2] != counts[3])
                problems.Add($"{name}: {counts[2]} live examples but {counts[3]} demos");
            if (counts[4] == 0)
                problems.Add($"{name}: the navigation did not render");

            problems.AddRange(errors);
            await page.CloseAsync();
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
