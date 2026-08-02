using System.Text.RegularExpressions;
using System.Xml.Linq;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// The script's global name, its storage prefix, and the two things it must not do.
/// </summary>
public class ScriptContractTests
{
    [Fact]
    public void The_javascript_global_is_drSimpleUi()
    {
        var js = File.ReadAllText(Assets.JsPath);
        Assert.Contains("window.drSimpleUi", js, StringComparison.Ordinal);
        Assert.Contains("configure", js, StringComparison.Ordinal);
    }

    [Fact]
    public void The_boot_script_and_the_main_script_share_a_default_storage_prefix()
    {
        // They read and write the same localStorage keys. If the defaults ever
        // disagree, an app that does not configure a prefix loses its theme on
        // every reload — the boot script would stamp one set of values and the
        // main script another.
        // Comments are stripped first — both files document a `storagePrefix:
        // 'myapp.'` usage example, which would otherwise match before the default.
        var boot = StripJsComments(File.ReadAllText(Assets.BootJsPath));
        var main = StripJsComments(File.ReadAllText(Assets.JsPath));

        var bootPrefix = Regex.Match(boot, @"dataset\.prefix\)\s*\|\|\s*'(?<p>[^']+)'").Groups["p"].Value;
        var mainPrefix = Regex.Match(main, @"storagePrefix:\s*'(?<p>[^']+)'").Groups["p"].Value;

        Assert.False(string.IsNullOrEmpty(bootPrefix), "Could not find the boot script's default prefix.");
        Assert.Equal(bootPrefix, mainPrefix);
    }

    /// <summary>
    /// Removes JS comments. The line-comment rule skips a <c>//</c> preceded by a
    /// colon so URLs inside string literals survive.
    /// </summary>
    private static string StripJsComments(string js)
    {
        js = Regex.Replace(js, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(js, @"(?<!:)//[^\n]*", string.Empty);
    }

    [Fact]
    public void The_scripts_carry_no_application_specific_naming()
    {
        string[] forbidden = ["atheneConsole", "athene.", "netpoint", "servicenow"];

        foreach (var path in new[] { Assets.JsPath, Assets.BootJsPath })
        {
            var js = File.ReadAllText(path);
            var found = forbidden.Where(f => js.Contains(f, StringComparison.OrdinalIgnoreCase)).ToList();

            Assert.True(found.Count == 0,
                $"{Path.GetFileName(path)} carries app-specific naming: {string.Join(", ", found)}");
        }
    }

    [Fact]
    public void The_tip_engine_leaves_the_sidebar_to_its_own_css_flyout()
    {
        // Both firing produces a double tooltip on the collapsed rail. The CSS
        // side is `.sidebar.collapsed [data-tip]:hover::after`; this is the other
        // half of that contract.
        var js = File.ReadAllText(Assets.JsPath);
        Assert.Contains("closest('.sidebar')", js, StringComparison.Ordinal);
    }
}
