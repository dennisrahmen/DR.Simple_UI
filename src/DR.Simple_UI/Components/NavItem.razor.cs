using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace DR.Simple_UI.Components;

/// <summary>
/// One entry in the sidebar navigation: an icon, a label, an optional count pill,
/// and an active state that follows the current address.
/// </summary>
/// <remarks>
/// <para>
/// Named <c>NavItem</c> rather than <c>NavLink</c> on purpose.
/// <see cref="Microsoft.AspNetCore.Components.Routing.NavLink"/> is in scope in
/// every Blazor app through its <c>_Imports.razor</c>, so a component called
/// <c>NavLink</c> here would be an ambiguous reference the moment an app added
/// <c>@using DR.Simple_UI.Components</c> — and it would break every existing
/// <c>&lt;NavLink&gt;</c> in that app at the same time.
/// </para>
/// <para>
/// Unlike the framework's <c>NavLink</c> this also sets
/// <c>aria-current="page"</c> when active, so the current item is announced and
/// not merely coloured.
/// </para>
/// <para>
/// Matching drops the query string and the fragment, and treats a trailing slash
/// as insignificant, so <c>/queue</c>, <c>/queue/</c>, <c>/queue?page=2</c> and
/// <c>/queue#top</c> are one address. With the default
/// <see cref="NavLinkMatch.Prefix"/> the match must end on a path-segment
/// boundary, so <c>/queue</c> does not light up on <c>/queue-archive</c>. The link
/// to the app root needs <see cref="NavLinkMatch.All"/>, or it is active
/// everywhere.
/// </para>
/// </remarks>
public partial class NavItem : ComponentBase, IDisposable
{
    private bool _active;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Where the item goes. <c>""</c> is the app root. A null href renders a link
    /// that is never active.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>The visible label. Ignored when <see cref="ChildContent"/> is supplied.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Remix Icon class, for example <c>ri-inbox-line</c>. Remix Icon is bundled
    /// and is the only icon set.
    /// </summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>
    /// A count pill on the item — pending items, unread messages. Pass null to
    /// render no pill; <c>0</c> renders a pill reading zero, which is usually not
    /// what you want.
    /// </summary>
    [Parameter] public int? Count { get; set; }

    /// <summary>
    /// Hover-hint text: what the destination is, and its consequence. In the
    /// collapsed rail this becomes the CSS flyout that carries the label.
    /// </summary>
    [Parameter] public string? Tip { get; set; }

    /// <summary>
    /// How <see cref="Href"/> is compared with the current address. Defaults to
    /// <see cref="NavLinkMatch.Prefix"/>.
    /// </summary>
    [Parameter] public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    /// <summary>
    /// Forces the active state on or off, bypassing address matching — for
    /// navigation that does not correspond to a route, or a wizard step the app
    /// tracks itself. Leave null to match on the address.
    /// </summary>
    [Parameter] public bool? Active { get; set; }

    /// <summary>
    /// Renders the item as a tool link: smaller and dimmed, for the pinned
    /// <c>.nav-tools</c> footer rather than the main list.
    /// </summary>
    [Parameter] public bool Tool { get; set; }

    /// <summary>
    /// Marks the item as leaving the application: appends the outward arrow and
    /// opens in a new tab with <c>rel="noopener"</c>.
    /// </summary>
    [Parameter] public bool External { get; set; }

    /// <summary>
    /// Extra classes, appended after <c>.nav-link</c>. A plain <c>class="…"</c> at
    /// the call site binds here too — Blazor matches parameters
    /// case-insensitively — so it is appended rather than replacing the frame
    /// class.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Replaces the icon-plus-label content entirely.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Anything else is written onto the <c>&lt;a&gt;</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass
    {
        get
        {
            var css = "nav-link";
            if (Tool) css += " nav-link-tool";
            if (_active) css += " active";
            if (!string.IsNullOrWhiteSpace(Class)) css += " " + Class;
            return css;
        }
    }

    // Null values are not rendered as attributes, so these three vanish when off.
    private string? AriaCurrent => _active ? "page" : null;
    private string? Target => External ? "_blank" : null;
    private string? Rel => External ? "noopener" : null;

    /// <inheritdoc />
    protected override void OnInitialized() =>
        Navigation.LocationChanged += HandleLocationChanged;

    /// <inheritdoc />
    protected override void OnParametersSet() => _active = ComputeActive();

    /// <inheritdoc />
    public void Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
        GC.SuppressFinalize(this);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var was = _active;
        _active = ComputeActive();
        if (was != _active) StateHasChanged();
    }

    private bool ComputeActive()
    {
        if (Active.HasValue) return Active.Value;
        if (Href is null) return false;

        var current = Normalise(Navigation.Uri);
        var target = Normalise(Navigation.ToAbsoluteUri(Href).AbsoluteUri);

        if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase)) return true;
        if (Match == NavLinkMatch.All) return false;

        // The boundary check is the whole point: without it /queue also matches
        // /queue-archive, and two items light up at once.
        return current.Length > target.Length
            && current.StartsWith(target, StringComparison.OrdinalIgnoreCase)
            && current[target.Length] == '/';
    }

    private static string Normalise(string absoluteUri)
    {
        var cut = absoluteUri.IndexOfAny(['?', '#']);
        var path = cut < 0 ? absoluteUri : absoluteUri[..cut];
        return path.Length > 1 ? path.TrimEnd('/') : path;
    }
}
