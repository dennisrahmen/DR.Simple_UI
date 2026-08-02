# Getting started

## Install

```bash
dotnet add package DR.Simple_UI
```

Pin the version. Do not use a floating version range.

## Host page

Add three stylesheets and two scripts to `App.razor` (or `_Host.cshtml`). Your override file must come
**after** the library stylesheet.

```html
<head>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />

    <script src="_content/DR.Simple_UI/js/DR.Simple_UI.boot.js"></script>

    <link rel="stylesheet" href="_content/DR.Simple_UI/lib/remixicon/remixicon.css" />
    <link rel="stylesheet" href="_content/DR.Simple_UI/css/DR.Simple_UI.css" />
    <link rel="stylesheet" href="css/brand.css" />
</head>
<body>
    <!-- … -->
    <script src="_content/DR.Simple_UI/js/DR.Simple_UI.js"></script>
</body>
```

`DR.Simple_UI.boot.js` applies the stored theme before first paint. Load it in `<head>`.

No configuration is required. `drSimpleUi.configure()` is only needed for the options below.

### `configure()` options

| Option | Default | Purpose |
|---|---|---|
| `notifyIcon` | `null` | Icon for desktop notifications. |
| `langCookie` | `false` | Mirror the language into a cookie for server-side prerendering. |
| `storagePrefix` | `drui.` | See below. Rarely needed. |

### Storage keys

Settings are stored under `drui.theme`, `drui.cvd`, `drui.density` and `drui.lang`. `localStorage` is
scoped per origin, so apps on different domains never share state and the default prefix is fine.

Override it only when **two apps share one origin** — for example `example.com/app-a` and
`example.com/app-b` behind one reverse proxy, which is a single origin and therefore one `localStorage`.
It also matters for `langCookie`, since cookies are not origin-scoped the way `localStorage` is.

If you override it, set the same value in both places or the theme is not found on reload:

```html
<script src="_content/DR.Simple_UI/js/DR.Simple_UI.boot.js" data-prefix="app-a."></script>
<script>drSimpleUi.configure({ storagePrefix: 'app-a.' });</script>
```

## Branding

Create `wwwroot/css/brand.css` and redefine the brand tokens:

```css
:root {
    --brand:          #e41f16;
    --brand-hover:    #c8170f;
    --brand-active:   #a81209;
    --brand-soft:     #ff6f66;
    --brand-text:     #ff8f88;
    --accent:         #ff6f66;
    --sidebar-active: #e41f16;
}

:root[data-theme="light"] {
    --brand-soft: #e41f16;
    --brand-text: #c8170f;
    --accent:     #c8170f;
}
```

`--brand-tint`, `--brand-ring`, `--brand-ring-soft`, `--brand-ring-check` and `--brand-glow` are mixed
from `--brand` and follow it automatically, in both themes — set them only to override the alpha the
library chose. The five values above are hues, not opacities, which is why they are still stated.

Redefine only tokens the library already declares. **Never declare a new `--` name the library does not
define** — a future version may introduce that name with a different meaning and your app breaks on
upgrade.

If a value is missing, [request it](../CONTRIBUTING.md). Until it ships, use an app-prefixed variable
(`--myapp-…`) in your own stylesheet rather than a bare name in the shared namespace. Do not override
library classes to work around it. See [architecture](architecture.md#the-token-contract).

The full token list is on the [Tokens](https://github.dennisrahmen.de/catalogue/tokens.html) catalogue
page.

## Icons

[Remix Icon](https://remixicon.com) 4.9.1 is bundled in the package — 3,245 icons, no CDN and nothing
extra to install. Add the stylesheet:

```html
<link rel="stylesheet" href="_content/DR.Simple_UI/lib/remixicon/remixicon.css" />
```

Then use the classes on an `<i>`:

```html
<button class="btn btn-go"><i class="ri-check-line"></i> Approve</button>
<button class="btn" aria-label="Refresh"><i class="ri-refresh-line"></i></button>
```

Browse the full set at <https://remixicon.com>. Every catalogue example uses these class names.

The icons are licensed under the **Remix Icon License v1.0**, not this project's Apache-2.0. Displaying
them in an application requires nothing of you; see
[THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md) for the restrictions that do carry through — chiefly
that you may not redistribute them as a standalone icon pack, or use one as a logo.

## The frame

Add the namespace once, in your app's `_Imports.razor`:

```razor
@using DR.Simple_UI.Components
```

Then the shell is five components:

```razor
<AppShell>
    <Navigation>
        <Sidebar Title="Approval Console" Subtitle="Netpoint" LogoSrc="/logo.png" BrandHref="/">
            <ChildContent>
                <NavItem Href="" Match="NavLinkMatch.All" Icon="ri-home-4-line" Label="Overview" />
                <NavItem Href="queue" Icon="ri-inbox-line" Label="Queue" Count="@pending" />
            </ChildContent>
            <Tools>
                <NavItem Href="https://docs.example.com" Icon="ri-book-2-line"
                         Label="Documentation" Tool External />
            </Tools>
        </Sidebar>
    </Navigation>
    <Header>
        <AppHeader>
            <UserWidget Name="@user.Name" Secondary="@user.Email" SignOutHref="/signout" />
        </AppHeader>
    </Header>
    <ChildContent>
        @Body
    </ChildContent>
</AppShell>
```

**`<ChildContent>` is not optional here.** As soon as a component has one named
`RenderFragment` parameter, Razor stops accepting loose child content and fails the build with
`RZ9996`. `AppShell` has `Navigation` and `Header`, `Sidebar` has `Tools`, `AppHeader` has `Start` —
so all three need it spelled out. `AppHeader` above does not, because nothing named is used on it.

For the same family of reason, bind text that contains `@` to a field: an e-mail address written
straight into an attribute is parsed as a C# expression and fails with `RZ9986`.

- `Href=""` is the app root, and it needs `Match="NavLinkMatch.All"` — with the default `Prefix` it is
  active on every page.
- `<AppShell Bare>` drops the sidebar, for sign-in, access-denied and error pages.
- `<Sidebar Collapsed="true">` shows the 56px icon rail. Give each `NavItem` a `Tip`, which becomes the
  rail's flyout label.
- Add `Class="layout--responsive"` to `AppShell` to collapse to the rail automatically below 900px.

Everything the components emit can also be written by hand — the *Shell & nav* catalogue page shows the
markup, and a test keeps the two identical. Do that only where a component does not cover what you need,
and do not add local overrides for the frame: report frame problems against the library.

## Writing pages

Copy markup from the catalogue rather than writing it from scratch:

- In a running app: `_content/DR.Simple_UI/catalogue/index.html`
- On disk: `%USERPROFILE%\.nuget\packages\dr.simple_ui\<version>\staticwebassets\catalogue\index.html`
  (Linux/macOS: `~/.nuget/packages/dr.simple_ui/<version>/staticwebassets/catalogue/index.html`)
- Online: <https://github.dennisrahmen.de/>

The in-package copy matches the version you have installed. The online copy shows `main`.

## AI agents

Copy the block in [`CLAUDE.consuming-app.md`](CLAUDE.consuming-app.md) into your app's `CLAUDE.md`.
