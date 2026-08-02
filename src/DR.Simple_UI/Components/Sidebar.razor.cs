using Microsoft.AspNetCore.Components;

namespace DR.Simple_UI.Components;

/// <summary>
/// The sidebar: a brand block, a scrolling list of navigation items, and an
/// optional pinned tools footer.
/// </summary>
/// <remarks>
/// <para>
/// Renders <c>aside.sidebar</c> containing <c>.brand</c> and
/// <c>nav.nav &gt; .nav-scroll</c>. Put <see cref="NavItem"/> elements in the
/// content; anything the library does not cover can be written as plain markup
/// with the documented classes, which is what the catalogue's Shell &amp; nav page
/// lists.
/// </para>
/// <para>
/// <see cref="Collapsed"/> adds <c>.collapsed</c> and nothing else — the rail is
/// entirely CSS, so no markup changes when it collapses. In the rail each item's
/// <c>data-tip</c> becomes a CSS flyout; the JavaScript hover-hint engine skips
/// <c>.sidebar</c> for exactly that reason, or both would fire and draw two
/// tooltips.
/// </para>
/// </remarks>
public partial class Sidebar : ComponentBase
{
    /// <summary>
    /// Collapses the sidebar to the 56px icon rail. The app owns this state,
    /// because it also owns where the toggle button lives.
    /// </summary>
    [Parameter] public bool Collapsed { get; set; }

    /// <summary>
    /// The brand block, replacing the default one built from <see cref="Title"/>,
    /// <see cref="Subtitle"/> and <see cref="LogoSrc"/>. Render
    /// <c>.brand</c> yourself when you supply this.
    /// </summary>
    [Parameter] public RenderFragment? Brand { get; set; }

    /// <summary>
    /// The application name. Nothing is rendered for the brand block unless this
    /// or <see cref="Brand"/> is set.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The smaller line under the title — a tenant, a company, a stage.</summary>
    [Parameter] public string? Subtitle { get; set; }

    /// <summary>
    /// Logo image source. Ships nothing itself: the app's own logo is an app asset.
    /// </summary>
    [Parameter] public string? LogoSrc { get; set; }

    /// <summary>
    /// Where the brand block links to, usually the app root. When null the brand is
    /// rendered as a plain element rather than an unreachable link.
    /// </summary>
    [Parameter] public string? BrandHref { get; set; }

    /// <summary>The navigation items, rendered inside <c>.nav-scroll</c>.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Pinned to the bottom in <c>.nav-tools</c> and excluded from the scroll — for
    /// links out of the app: documentation, the repository, a status page.
    /// </summary>
    [Parameter] public RenderFragment? Tools { get; set; }

    /// <summary>
    /// Extra classes, appended after <c>.sidebar</c>. A plain <c>class="…"</c> at the
    /// call site binds here too, so it can never replace the frame class.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Anything else is written onto the <c>aside.sidebar</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string SidebarCssClass =>
        AppShell.Compose(Collapsed ? "sidebar collapsed" : "sidebar", Class);
}
