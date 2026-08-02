using DR.Blazor.App.Components;

var builder = WebApplication.CreateBuilder(args);

// InteractiveServer: the frame's components hold state (the collapsed rail, the user
// menu) and need a circuit to do it. Everything in DR.Simple_UI still renders
// statically — the CSS applies with no scripting at all — so a prerendered page is
// never unstyled.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// MapStaticAssets fingerprints and compresses the library's CSS and JS along with
// your own, which is where the stylesheet's gzipped size comes from.
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
