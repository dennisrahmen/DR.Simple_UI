using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace DR.Simple_UI.Components;

/// <summary>
/// The signed-in identity at the trailing end of the header: avatar, name, a
/// second line, an optional dropdown panel and an optional sign-out button.
/// </summary>
/// <remarks>
/// <para>
/// Renders <c>.user-widget</c> containing <c>.user-trigger</c> and, when
/// <see cref="SignOutHref"/> is set, <c>.user-signout</c>. The trigger is a
/// <c>&lt;button&gt;</c> only when there is a <see cref="Menu"/> to open —
/// otherwise it is a plain element, because a button that does nothing is
/// announced as a control and reached by keyboard for no reason.
/// </para>
/// <para>
/// The panel is dismissed by clicking outside it or pressing Escape. It is a
/// disclosure rather than an ARIA menu; see the comment in the markup.
/// </para>
/// <para>
/// <c>.user-widget</c> creates a stacking context at z-index 200. Anything nested
/// inside it is ordered within that context and cannot be lifted above the modal
/// backdrop by z-index alone.
/// </para>
/// </remarks>
public partial class UserWidget : ComponentBase
{
    private bool _open;

    /// <summary>The display name, on the first line.</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>
    /// The smaller second line — an e-mail address, a role, a tenant. Nothing is
    /// rendered for it when empty.
    /// </summary>
    [Parameter] public string? Secondary { get; set; }

    /// <summary>
    /// Initials for the avatar circle, used when no <see cref="AvatarSrc"/> or
    /// <see cref="Avatar"/> is given. Falls back to a person icon.
    /// </summary>
    [Parameter] public string? Initials { get; set; }

    /// <summary>An image for the avatar circle. Takes precedence over <see cref="Initials"/>.</summary>
    [Parameter] public string? AvatarSrc { get; set; }

    /// <summary>Replaces the contents of the avatar circle entirely.</summary>
    [Parameter] public RenderFragment? Avatar { get; set; }

    /// <summary>
    /// The dropdown panel's contents. Supplying this turns the trigger into a
    /// button. The panel is the library's one dropdown style, so use
    /// <c>.menu-item</c>, <c>.menu-label</c> and <c>.menu-sep</c> — see the
    /// catalogue's Menus page.
    /// </summary>
    [Parameter] public RenderFragment? Menu { get; set; }

    /// <summary>
    /// Where the sign-out button goes. Nothing is rendered when this is empty, for
    /// apps that sign out from inside the <see cref="Menu"/> instead.
    /// </summary>
    [Parameter] public string? SignOutHref { get; set; }

    /// <summary>Accessible name for the sign-out button, which is icon-only.</summary>
    [Parameter] public string SignOutLabel { get; set; } = "Sign out";

    /// <summary>Hover-hint text for the sign-out button.</summary>
    [Parameter] public string? SignOutTip { get; set; }

    /// <summary>Raised when the dropdown panel opens or closes.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    /// Extra classes, appended after <c>.user-widget</c>. A plain <c>class="…"</c> at
    /// the call site binds here too, so it can never replace the frame class.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Anything else is written onto the <c>.user-widget</c> element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass => AppShell.Compose("user-widget", Class);

    /// <summary>Closes the dropdown panel from outside the component.</summary>
    public void Close() => SetOpen(false);

    private void Toggle() => SetOpen(!_open);

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (_open && e.Key == "Escape") SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        if (_open == open) return;
        _open = open;
        StateHasChanged();
        if (OpenChanged.HasDelegate) OpenChanged.InvokeAsync(open);
    }
}
