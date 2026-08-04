using Microsoft.JSInterop;

namespace Sedna.UI;

/// <summary>
/// The default <see cref="ISednaUi"/>, calling the <c>drSimpleUi</c> global
/// through <see cref="IJSRuntime"/>.
/// </summary>
/// <remarks>
/// Registered by <c>AddSednaUi</c>. Every method is a thin, untyped call onto
/// the shipped script: this class holds no state and no logic beyond mapping
/// arguments, so the browser stays the single implementation of every behaviour.
/// </remarks>
public sealed class SednaUi : ISednaUi
{
    private readonly IJSRuntime _js;
    private readonly SednaUiOptions _options;

    /// <summary>Creates the service.</summary>
    /// <param name="jsRuntime">The app's JavaScript runtime.</param>
    /// <param name="options">The options to push in <see cref="ConfigureAsync"/>.</param>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    public SednaUi(IJSRuntime jsRuntime, SednaUiOptions options)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(options);

        _js = jsRuntime;
        _options = options;
    }

    /// <inheritdoc />
    public Task ToastAsync(
        string message,
        ToastKind kind = ToastKind.Info,
        string? title = null,
        int timeoutMs = 4000,
        bool dismissible = true)
        => _js.InvokeVoidAsync(
            "drSimpleUi.toast",
            message,
            new { kind = Name(kind), title, timeout = timeoutMs, dismissible }).AsTask();

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(
        string title,
        string? message = null,
        string confirmLabel = "Confirm",
        string cancelLabel = "Cancel",
        bool danger = false)
        => _js.InvokeAsync<bool>(
            "drSimpleUi.confirm",
            new { title, message, confirm = confirmLabel, cancel = cancelLabel, danger }).AsTask();

    /// <inheritdoc />
    public Task<bool> CopyTextAsync(string text)
        => _js.InvokeAsync<bool>("drSimpleUi.copyText", text).AsTask();

    /// <inheritdoc />
    public Task InitMarkdownAsync()
        => _js.InvokeVoidAsync("drSimpleUi.md.init").AsTask();

    /// <inheritdoc />
    public Task<SednaUiSettings> LoadSettingsAsync()
        => _js.InvokeAsync<SednaUiSettings>("drSimpleUi.settings.load").AsTask();

    /// <inheritdoc />
    public Task SaveSettingAsync(string key, string value)
        => _js.InvokeVoidAsync("drSimpleUi.settings.save", key, value).AsTask();

    /// <inheritdoc />
    public Task ConfigureAsync()
        => _js.InvokeVoidAsync(
            "drSimpleUi.configure",
            new
            {
                storagePrefix = _options.StoragePrefix,
                notifyIcon = _options.NotifyIcon,
                langCookie = _options.LangCookie,
            }).AsTask();

    /// <inheritdoc />
    public Task RegisterCommandsAsync(IReadOnlyList<PaletteCommand> commands)
        => _js.InvokeVoidAsync("drSimpleUi.palette.register", commands).AsTask();

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaletteCommand>> RankCommandsAsync(string query)
        => await _js.InvokeAsync<PaletteCommand[]>("drSimpleUi.palette.rank", query)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task OpenPaletteAsync() => _js.InvokeVoidAsync("drSimpleUi.palette.open").AsTask();

    /// <inheritdoc />
    public Task ClosePaletteAsync() => _js.InvokeVoidAsync("drSimpleUi.palette.close").AsTask();

    /// <inheritdoc />
    public Task RegisterSearchAsync(IReadOnlyList<SearchItem> items)
        => _js.InvokeVoidAsync("drSimpleUi.search.register", items).AsTask();

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchItem>> RankSearchAsync(string query)
        => await _js.InvokeAsync<SearchItem[]>("drSimpleUi.search.rank", query)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task CloseSearchAsync() => _js.InvokeVoidAsync("drSimpleUi.search.close").AsTask();

    /// <inheritdoc />
    public Task HideTipsAsync() => _js.InvokeVoidAsync("drSimpleUi.tips.hide").AsTask();

    /// <inheritdoc />
    public Task CloseMenusAsync() => _js.InvokeVoidAsync("drSimpleUi.menu.closeAll").AsTask();

    /// <inheritdoc />
    public Task ScrollPageTopAsync() => _js.InvokeVoidAsync("drSimpleUi.scrollPageTop").AsTask();

    /// <inheritdoc />
    public Task SelectTabAsync(string tabOrPanelId)
        => _js.InvokeVoidAsync("drSimpleUi.tabs.select", tabOrPanelId).AsTask();

    /// <inheritdoc />
    public Task OpenTabAsync(string url) => _js.InvokeVoidAsync("drSimpleUi.openTab", url).AsTask();

    /// <inheritdoc />
    public Task<int> ViewportWidthAsync()
        => _js.InvokeAsync<int>("drSimpleUi.viewportWidth").AsTask();

    /// <inheritdoc />
    public Task<string?> GetItemAsync(string key)
        => _js.InvokeAsync<string?>("drSimpleUi.getItem", key).AsTask();

    /// <inheritdoc />
    public Task SetItemAsync(string key, string value)
        => _js.InvokeVoidAsync("drSimpleUi.setItem", key, value).AsTask();

    /// <inheritdoc />
    public Task<bool> RequestNotifyAsync()
        => _js.InvokeAsync<bool>("drSimpleUi.requestNotify").AsTask();

    /// <inheritdoc />
    public Task NotifyAsync(string title, string? body = null)
        => _js.InvokeVoidAsync("drSimpleUi.notify", title, body).AsTask();

    /// <inheritdoc />
    public Task PingAsync() => _js.InvokeVoidAsync("drSimpleUi.ping").AsTask();

    // The script's own vocabulary, which is also the CSS modifier suffix
    // (.toast-go, .toast-danger). Mapped explicitly rather than lower-casing the
    // enum name, so renaming a member here cannot silently change a CSS class.
    private static string Name(ToastKind kind) => kind switch
    {
        ToastKind.Go => "go",
        ToastKind.Warn => "warn",
        ToastKind.Danger => "danger",
        _ => "info",
    };
}
