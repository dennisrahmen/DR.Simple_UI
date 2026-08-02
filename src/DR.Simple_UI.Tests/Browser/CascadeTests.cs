using DR.Simple_UI.Tests.TestSupport;
using Microsoft.Playwright;

namespace DR.Simple_UI.Tests;

/// <summary>
/// A rule that parses fine and silently loses to a more specific one is the failure mode no source scan can see. Three of these shipped before this existed.
/// </summary>
public class CascadeTests : CatalogueBrowserTestBase
{
    [Fact]
    public async Task Single_purpose_classes_are_not_outranked_by_the_rules_they_sit_beside()
    {
        if (NoBrowser) return;

        // Each case is a class whose whole job is one property, placed inside the
        // component that also sets it. This is the shape of every cascade bug this
        // library has had: nothing errors, the class simply loses.
        (string Page, string Markup, string Selector, string Property, string Expected)[] cases =
        [
            ("table.html",
             "<table class='table'><tr><td class='col-num'>1</td></tr></table>",
             // `end`, not `right`: the library uses logical text-align throughout so
             // the layout mirrors from dir="rtl" on its own.
             "td.col-num", "textAlign", "end"),

            ("table.html",
             "<table class='table'><tr><th class='col-num'>N</th></tr></table>",
             "th.col-num", "textAlign", "end"),

            // The selection rule must beat the zebra stripe: it is the more important
            // of the two signals, and it is the one on the even row that loses.
            ("table.html",
             "<table class='table table--zebra'><tbody>"
             + "<tr><td>a</td></tr><tr aria-selected='true'><td id='probe'>b</td></tr>"
             + "</tbody></table>",
             "#probe", "boxShadow", "rgb(37, 99, 235) 3px 0px 0px 0px inset"),

            // .form-select's caret survives .form-input's `background` shorthand only
            // on source order, so this fails the moment the part is renumbered.
            ("form.html",
             "<select class='form-input form-select'><option>a</option></select>",
             "select", "backgroundSize", "5px 5px, 5px 5px"),

            // The input group's inner control must give up its own border, or the
            // group draws two nested ones.
            ("form.html",
             "<div class='input-group'><input class='form-input' /></div>",
             ".input-group .form-input", "borderStyle", "none"),

            // .tab--active must not change the box height, or the label jumps.
            ("tabs.html",
             "<div class='tabs'><button class='tab'>a</button></div>",
             ".tab", "borderBottomWidth", "2px")
        ];

        var problems = new List<string>();

        foreach (var group in cases.GroupBy(c => c.Page))
        {
            var (page, errors) = await Open(group.Key);
            problems.AddRange(errors);

            foreach (var c in group)
            {
                var got = await page.EvaluateAsync<string>(
                    @"([markup, selector, property]) => {
                        const host = document.createElement('div');
                        host.style.cssText = 'position:absolute;left:-9999px;top:0';
                        host.innerHTML = markup;
                        document.body.appendChild(host);
                        const el = host.querySelector(selector);
                        const value = el ? getComputedStyle(el)[property] : '<no such element>';
                        host.remove();
                        return value;
                    }",
                    new[] { c.Markup, c.Selector, c.Property });

                if (got != c.Expected)
                    problems.Add(
                        $"{c.Selector} {c.Property}: expected \"{c.Expected}\", computed \"{got}\". "
                        + "A more specific rule is winning, so the class does nothing.");
            }

            await page.CloseAsync();
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
