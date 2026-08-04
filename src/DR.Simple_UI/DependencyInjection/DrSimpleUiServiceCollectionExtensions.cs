using DR.Simple_UI;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers DR.Simple_UI's services.
/// </summary>
/// <remarks>
/// In the <c>Microsoft.Extensions.DependencyInjection</c> namespace by convention,
/// so <c>AddDrSimpleUi()</c> is reachable in <c>Program.cs</c> without a using
/// directive.
/// </remarks>
public static class DrSimpleUiServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IDrSimpleUi"/>, the typed wrapper over the browser API.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional: the options to push with <see cref="IDrSimpleUi.ConfigureAsync"/>.</param>
    /// <returns>The same collection, so calls chain.</returns>
    /// <remarks>
    /// <para>
    /// The service is <b>scoped</b>, because <see cref="Microsoft.JSInterop.IJSRuntime"/>
    /// is: in Blazor Server one scope is one circuit, and a singleton would call
    /// into whichever browser connected first.
    /// </para>
    /// <para>
    /// Calling this more than once is harmless — the registration is idempotent, so
    /// a library that also calls it does not produce a second, differently
    /// configured service.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    public static IServiceCollection AddDrSimpleUi(
        this IServiceCollection services,
        Action<DrSimpleUiOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DrSimpleUiOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddScoped<IDrSimpleUi, DrSimpleUi>();

        return services;
    }
}
