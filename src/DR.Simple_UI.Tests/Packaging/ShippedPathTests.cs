using System.Text.RegularExpressions;
using System.Xml.Linq;
using DR.Simple_UI.Tests.TestSupport;

namespace DR.Simple_UI.Tests;

/// <summary>
/// Asset paths a consuming app hard-codes. A rename here is a silent 404 there.
/// </summary>
public class ShippedPathTests
{
    [Theory]
    [InlineData("wwwroot/css/DR.Simple_UI.css")]
    [InlineData("wwwroot/js/DR.Simple_UI.js")]
    [InlineData("wwwroot/js/DR.Simple_UI.boot.js")]
    [InlineData("wwwroot/catalogue/index.html")]
    [InlineData("wwwroot/catalogue/catalogue.css")]
    [InlineData("wwwroot/catalogue/catalogue.js")]
    [InlineData("wwwroot/catalogue/favicon.ico")]
    [InlineData("wwwroot/catalogue/logo.png")]
    [InlineData("wwwroot/lib/remixicon/remixicon.css")]
    [InlineData("wwwroot/lib/remixicon/remixicon.woff2")]
    [InlineData("wwwroot/lib/remixicon/LICENSE")]
    public void Shipped_asset_exists_at_its_documented_path(string relativePath)
    {
        // Consuming apps write these paths out by hand as
        // _content/DR.Simple_UI/<path-under-wwwroot>. A rename here is a silent
        // 404 there, so the names are part of the public contract.
        var full = Path.Combine(Assets.ProjectDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(full), $"Missing shipped asset: {relativePath}");
    }
}
