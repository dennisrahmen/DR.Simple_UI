using Microsoft.AspNetCore.Components;

namespace DR.Simple_UI.Components;

/// <summary>
/// The application shell: the flex row that fills the viewport, the column beside
/// the navigation, and the page scroll container.
/// </summary>
/// <remarks>
/// <para>
/// Renders <c>.layout &gt; .content &gt; .page</c>, with the navigation as the
/// <c>.layout</c>'s first child. <c>.page</c> is the only scroll container in the
/// frame; do not add another one around it, or the header scrolls away with the
/// content.
/// </para>
/// <para>
/// Set <see cref="Bare"/> for a page with no navigation to offer yet — sign-in,
/// access-denied, an error page. That renders <c>.bare-layout</c> instead and
/// ignores <see cref="Navigation"/>.
/// </para>
/// </remarks>
public partial class AppShell : ComponentBase
{
    /// <summary>
    /// The sidebar. Place a <see cref="Sidebar"/> here — or your own
    /// <c>&lt;aside class="sidebar"&gt;</c> if you need markup this component does
    /// not cover. Ignored when <see cref="Bare"/> is set.
    /// </summary>
    [Parameter] public RenderFragment? Navigation { get; set; }

    /// <summary>The header row. Place an <see cref="AppHeader"/> here.</summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>The page content, rendered inside <c>.page</c>.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Drops the sidebar and renders <c>.bare-layout</c> — header and body only.
    /// </summary>
    [Parameter] public bool Bare { get; set; }

    /// <summary>
    /// Extra classes for the <c>.page</c> element, for a page-specific layout the
    /// app owns. <c>.page</c> itself is always applied and cannot be replaced.
    /// </summary>
    [Parameter] public string? PageClass { get; set; }

    /// <summary>
    /// Extra classes for the outer element. Because Blazor matches parameters
    /// case-insensitively, a plain <c>class="…"</c> at the call site binds here too
    /// and is appended rather than replacing <c>.layout</c>.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Anything else is written onto the outer <c>.layout</c> /
    /// <c>.bare-layout</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string LayoutCssClass => Compose("layout", Class);
    private string BareCssClass => Compose("bare-layout", Class);
    private string PageCssClass => Compose("page", PageClass);

    internal static string Compose(string frameClass, string? extra) =>
        string.IsNullOrWhiteSpace(extra) ? frameClass : $"{frameClass} {extra.Trim()}";
}
