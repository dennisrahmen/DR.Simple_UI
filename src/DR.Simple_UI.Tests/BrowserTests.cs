using DR.Simple_UI.Tests.TestSupport;
using Microsoft.Playwright;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Loads the catalogue in a real browser and asserts what a CSS engine actually
/// computes.
/// </summary>
/// <remarks>
/// <para>
/// The source-scanning guards cannot see the one failure mode this library really
/// suffers: a rule that parses fine, is never reported by anything, and silently
/// does nothing because a more specific rule already set the property. Three of
/// those shipped into 0.3.0 and were caught here —
/// <c>.col-num</c> losing <c>text-align</c> to <c>.table td</c>, the zebra stripe
/// outranking the hover highlight, and an even striped row outranking
/// <c>aria-selected</c>.
/// </para>
/// <para>
/// Deliberately <b>not</b> pixel baselines. Screenshots taken on Windows do not
/// match the Linux CI runner — font rasterisation and scrollbar metrics differ — so
/// the suite would either fail constantly or need a tolerance wide enough to miss
/// real changes. Computed values are exact on every platform.
/// </para>
/// <para>
/// The browser binaries are not restored with the package, and a test that passes
/// without asserting anything is worse than one that fails — so a missing browser is
/// a <b>failure by default</b>. Install it once:
/// </para>
/// <code>pwsh bin/Debug/net10.0/playwright.ps1 install chromium</code>
/// <para>
/// <c>DR_UI_BROWSER_TESTS=0</c> opts out, for the rare case of running the source
/// scans on a machine that genuinely cannot host a browser. It has to be set
/// deliberately, which is the point: the quiet path is the honest one.
/// </para>
/// </remarks>
public class BrowserTests : IAsyncLifetime
{
    private const string OptOutEnvVar = "DR_UI_BROWSER_TESTS";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _unavailable;

    /// <summary>The catalogue is loaded over file:// — it links only relative assets.</summary>
    private static string PageUrl(string name) =>
        new Uri(Path.Combine(Assets.CatalogueDir, name)).AbsoluteUri;

    public async Task InitializeAsync()
    {
        try
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        }
        catch (Exception ex)
        {
            // Almost always "Executable doesn't exist" — the binaries were never
            // downloaded. Recorded rather than thrown so the rest of the suite runs.
            _unavailable = ex.Message.Split('\n')[0];
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    /// <summary>True when the caller has deliberately opted out of the browser tests.</summary>
    private static bool OptedOut =>
        Environment.GetEnvironmentVariable(OptOutEnvVar) == "0";

    [Fact]
    public void A_browser_is_available()
    {
        if (OptedOut) return;

        Assert.True(_unavailable is null,
            "No browser could be launched, so every browser test would have asserted nothing. "
            + "Run `pwsh src/DR.Simple_UI.Tests/bin/Debug/net10.0/playwright.ps1 install chromium`, "
            + $"or set {OptOutEnvVar}=0 to run only the source scans. Playwright said: {_unavailable}");
    }

    /// <summary>
    /// Opens a catalogue page, fails on any console error, and returns the page.
    /// </summary>
    private async Task<(IPage Page, List<string> Errors)> Open(string name)
    {
        var page = await _browser!.NewPageAsync();
        var errors = new List<string>();

        page.Console += (_, msg) => { if (msg.Type == "error") errors.Add($"{name}: {msg.Text}"); };
        page.PageError += (_, err) => errors.Add($"{name}: uncaught {err}");

        await page.GotoAsync(PageUrl(name));

        // Wait on the navigation, not on the examples: tokens.html has no
        // <template> examples at all — it renders swatches read out of the loaded
        // stylesheet — so waiting for .ex-code would hang there for ever. The
        // sidebar is the one thing catalogue.js builds on every page.
        //
        // catalogue.js runs at the end of the body and builds synchronously, so this
        // is normally already true on arrival. On failure the page's own state is the
        // only useful diagnosis: a bare "timeout after 30s" does not distinguish "the
        // script 404'd" from "the script threw" from "this page has none of that".
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

    [Fact]
    public async Task Every_catalogue_page_builds_its_demos_without_a_console_error()
    {
        if (_unavailable is not null) return;

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

    [Fact]
    public async Task Single_purpose_classes_are_not_outranked_by_the_rules_they_sit_beside()
    {
        if (_unavailable is not null) return;

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

    [Fact]
    public async Task Interactive_states_repaint_when_the_state_changes()
    {
        if (OptedOut) return;

        // A state that only paints correctly on first render is the worst failure
        // shape there is — the control looks operable and is not — so it is worth a
        // real browser.
        //
        // The trap this test exists to avoid, having fallen into it: nearly everything
        // interactive in this library has a `transition`, and getComputedStyle read
        // immediately after a class or checkedness change returns the value at t=0,
        // which is the OLD one. That reads exactly like a broken selector. It nearly
        // caused a redesign of the segmented control away from :has(), and the tell
        // was the control below: a plain `.sidebar.collapsed` toggle appearing not to
        // work, which no browser has got wrong since 1998.
        //
        // So transitions are switched off first, and the sentinel stays in as the
        // thing that catches the mistake next time.
        var (page, errors) = await Open("tabs.html");
        Assert.Empty(errors);

        var report = await page.EvaluateAsync<string[]>(@"() => {
            const problems = [];

            // Kill every transition, or each reading below is the value at t=0 — the
            // one being transitioned AWAY from. `* !important` is fine here: it is a
            // test harness, not the library, which uses none.
            const freeze = document.createElement('style');
            freeze.textContent = '*, *::before, *::after { transition: none !important; ' +
                                 'animation: none !important; }';
            document.head.appendChild(freeze);

            const host = document.createElement('div');
            host.innerHTML = `
                <aside class='sidebar' id='sb'></aside>
                <div class='segmented'>
                  <label class='segmented-option' id='s1'><input type='radio' name='v' checked> A</label>
                  <label class='segmented-option' id='s2'><input type='radio' name='v'> B</label>
                </div>
                <label class='form-check'><input type='checkbox' id='cb'><span>t</span></label>`;
            document.body.appendChild(host);
            const q = id => host.querySelector('#' + id);
            const bg = el => getComputedStyle(el).backgroundColor;

            // Control: if a plain class toggle does not take effect, the environment is
            // broken and nothing below can be trusted — so say that, rather than
            // reporting a library fault.
            const sb = q('sb');
            const w0 = getComputedStyle(sb).width;
            sb.classList.add('collapsed');
            if (getComputedStyle(sb).width === w0)
                problems.push('ENVIRONMENT: a .sidebar.collapsed class toggle did not repaint');

            // The chosen segmented option must move with the radio. The LABEL is what
            // is painted — :has(input:checked) on it — because the label is also the
            // whole hit area.
            const s1 = q('s1'), s2 = q('s2');
            const before = [bg(s1), bg(s2)];
            s2.querySelector('input').click();
            const after = [bg(s1), bg(s2)];
            if (after[0] === before[0] || after[1] === before[1])
                problems.push('segmented: chosen option did not move — ' +
                    JSON.stringify({ before, after }));

            // The checkbox fills with the brand colour when checked.
            const cb = q('cb');
            const cbBefore = bg(cb);
            cb.click();
            if (bg(cb) === cbBefore)
                problems.push('checkbox: :checked did not repaint — stayed ' + cbBefore);

            host.remove();
            freeze.remove();
            return problems;
        }");

        Assert.True(report.Length == 0, string.Join(Environment.NewLine, report));
        await page.CloseAsync();
    }

    [Fact]
    public async Task The_theme_and_density_toggles_only_change_token_values()
    {
        if (_unavailable is not null) return;

        var (page, errors) = await Open("index.html");
        Assert.Empty(errors);

        // The whole override model rests on this: if flipping the theme changed a
        // rule rather than a token, CSS load order would become load-bearing again
        // and an app's own stylesheet would start winning by accident.
        var report = await page.EvaluateAsync<string[]>(@"() => {
            const probe = document.createElement('div');
            probe.style.cssText = 'position:absolute;left:-9999px;top:0';
            probe.innerHTML = `<div class='card'><div class='card-body'>
                <button class='btn btn-primary'>b</button>
                <table class='table'><tr><th>h</th><td>d</td></tr></table>
            </div></div>`;
            document.body.appendChild(probe);

            const watch = ['.card', '.btn-primary', '.table th', '.table td'];
            const geometry = ['padding', 'margin', 'borderWidth', 'borderRadius', 'fontSize',
                              'fontWeight', 'display', 'minHeight', 'gap'];
            const snap = () => watch.map(s => {
                const el = probe.querySelector(s), cs = getComputedStyle(el);
                return s + '|' + geometry.map(p => p + ':' + cs[p]).join(';');
            });

            const root = document.documentElement;
            const dark = snap();
            root.setAttribute('data-theme', 'light');
            const light = snap();
            root.setAttribute('data-cvd', '1');
            const cvd = snap();
            root.removeAttribute('data-theme'); root.removeAttribute('data-cvd');
            probe.remove();

            const out = [];
            for (let i = 0; i < dark.length; i++) {
                if (light[i] !== dark[i]) out.push('theme changed geometry: ' + dark[i] + ' -> ' + light[i]);
                if (cvd[i] !== light[i]) out.push('cvd changed geometry: ' + light[i] + ' -> ' + cvd[i]);
            }
            return out;
        }");

        Assert.True(report.Length == 0,
            "A theme or palette toggle moved geometry, not just colour:"
            + Environment.NewLine + string.Join(Environment.NewLine, report));

        await page.CloseAsync();
    }
}
