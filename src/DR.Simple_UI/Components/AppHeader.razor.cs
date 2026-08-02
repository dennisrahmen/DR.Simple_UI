using Microsoft.AspNetCore.Components;

namespace DR.Simple_UI.Components;

/// <summary>
/// The header row above the page: a fixed-height bar with a leading area, a
/// spacer, and a trailing area.
/// </summary>
/// <remarks>
/// <para>
/// Renders <c>header.topbar</c> with a <c>.topbar-spacer</c> between
/// <see cref="Start"/> and the child content. The spacer is what pushes the
/// trailing controls to the far edge, so it is always emitted — an app never
/// writes it.
/// </para>
/// <para>
/// The trailing side is the child content because that is where nearly everything
/// goes: the status indicator, the icon buttons, the user widget. Put the sidebar
/// toggle in <see cref="Start"/> with <c>.topbar-btn.topbar-btn--start</c>, whose
/// divider is on its right.
/// </para>
/// <para>
/// <c>.topbar</c> creates a stacking context at z-index 60. A panel nested inside
/// the header is therefore ordered <em>within</em> it and cannot be lifted above
/// the modal backdrop by z-index alone — see the z-order table in
/// <c>docs/architecture.md</c>.
/// </para>
/// </remarks>
public partial class AppHeader : ComponentBase
{
    /// <summary>
    /// The leading area, before the spacer — conventionally the sidebar toggle and
    /// a page or breadcrumb title.
    /// </summary>
    [Parameter] public RenderFragment? Start { get; set; }

    /// <summary>
    /// The trailing area, after the spacer: status, icon buttons, and the
    /// <see cref="UserWidget"/> last.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Extra classes, appended after <c>.topbar</c>. A plain <c>class="…"</c> at the
    /// call site binds here too, so it can never replace the frame class.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Anything else is written onto the <c>header.topbar</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass => AppShell.Compose("topbar", Class);
}
