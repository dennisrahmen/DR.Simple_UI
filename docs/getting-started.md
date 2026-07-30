# Getting started

## Install

```bash
dotnet add package DR.Simple_UI
```

Pin the version. Do not use a floating version range.

## Host page

Add two stylesheets and two scripts to `App.razor` (or `_Host.cshtml`). The override file must come
**after** the library stylesheet.

```html
<head>
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
    --brand:            #e41f16;
    --brand-hover:      #c8170f;
    --brand-active:     #a81209;
    --brand-soft:       #ff6f66;
    --brand-text:       #ff8f88;
    --brand-tint:       rgba(228, 31, 22, 0.14);
    --brand-ring:       rgba(228, 31, 22, 0.5);
    --brand-ring-soft:  rgba(228, 31, 22, 0.4);
    --brand-ring-check: rgba(228, 31, 22, 0.35);
    --brand-glow:       rgba(228, 31, 22, 0.18);
    --accent:           #ff6f66;
    --sidebar-active:   #e41f16;
}

:root[data-theme="light"] {
    --brand-soft: #e41f16;
    --brand-text: #c8170f;
    --brand-tint: rgba(228, 31, 22, 0.1);
    --accent:     #c8170f;
}
```

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

## Writing pages

Copy markup from the catalogue rather than writing it from scratch:

- In a running app: `_content/DR.Simple_UI/catalogue/index.html`
- On disk: `%USERPROFILE%\.nuget\packages\dr.simple_ui\<version>\staticwebassets\catalogue\index.html`
  (Linux/macOS: `~/.nuget/packages/dr.simple_ui/<version>/staticwebassets/catalogue/index.html`)
- Online: <https://github.dennisrahmen.de/>

The in-package copy matches the version you have installed. The online copy shows `main`.

## AI agents

Copy the block in [`CLAUDE.consuming-app.md`](CLAUDE.consuming-app.md) into your app's `CLAUDE.md`.
