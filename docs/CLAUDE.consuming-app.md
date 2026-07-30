# Consuming-app `CLAUDE.md` block

Copy the section below into the `CLAUDE.md` of any app that uses `DR.Simple_UI`, so an AI agent working
in that app follows the library's rules instead of inventing its own UI.

Replace:

- `<VERSION>` — the pinned package version, e.g. `0.1.0`
- `<APP-PREFIX>` — a short CSS prefix for this app, e.g. `myapp`

---

## Shared UI — DR.Simple_UI

This project uses `DR.Simple_UI` version `<VERSION>` for all shared UI.

### Copy markup from the catalogue

The catalogue ships inside the package and matches the installed version:

- While the app is running: `_content/DR.Simple_UI/catalogue/index.html`
- On disk: `%USERPROFILE%\.nuget\packages\dr.simple_ui\<VERSION>\staticwebassets\catalogue\index.html`
  (Linux/macOS: `~/.nuget/packages/dr.simple_ui/<VERSION>/staticwebassets/catalogue/index.html`)

Every page carries copy-pasteable HTML for its class family.

Do not write shared-UI markup from memory, and do not copy it from the hosted docs site — that site shows
the library's `main` branch, which can differ from the installed version. The in-package catalogue is the
source of truth.

### Content UI is a CSS class, not a component

Tables, forms, cards, badges, buttons and panels are CSS classes. Write plain HTML in the `.razor` file
and apply them. Do not wrap page content in a component, and do not add MudBlazor, Syncfusion, Radzen or
Tailwind.

The layout frame — shell, sidebar, header, user widget — is used as the catalogue shows it. Do not
restyle it in this app.

### Icons

Remix Icon is bundled in the package. Link `_content/DR.Simple_UI/lib/remixicon/remixicon.css` and use
`ri-*` classes on an `<i>`. Do not add a second icon set and do not load icons from a CDN.

### CSS variables

The only file in this project that may contain `:root { --… }` is `wwwroot/css/brand.css`, and it may
only **redefine tokens the library already declares** — normally `--brand*`, `--accent` and
`--sidebar-active`.

**Never declare a new `--` name that the library does not define.** A future library version may
introduce that exact name with a different meaning, and this app would silently break on upgrade.

If a value you need is genuinely missing from the library:

1. Prefer requesting it upstream — see *Requesting a library change* below. A token added to the library
   benefits every app and survives upgrades.
2. If you need it before that lands, use an **app-scoped** variable with this app's own prefix
   (`--<APP-PREFIX>-…`) in an app stylesheet, not a bare name in the shared namespace. Prefixed names
   cannot collide with a future library token.

Do not restyle a library class to work around a missing value. Overriding `.btn` or `.card` in this app
means the next library upgrade fights you, which is the drift the shared library exists to prevent.

### Requesting a library change

`DR.Simple_UI` is a separate, versioned package. This app cannot release it.

- Open an issue or pull request at <https://github.com/dennisrahmen/DR.Simple_UI>.
- Describe the value or variant needed and where it is used, not just the CSS you would have written.
- Adding a token or variant is a minor release; renaming or removing one is major.

Once a version containing the change is published, bump the reference here.

### Load order

```html
<script src="_content/DR.Simple_UI/js/DR.Simple_UI.boot.js"></script>
<link rel="stylesheet" href="_content/DR.Simple_UI/lib/remixicon/remixicon.css" />
<link rel="stylesheet" href="_content/DR.Simple_UI/css/DR.Simple_UI.css" />
<link rel="stylesheet" href="css/brand.css" />
```

At the end of `<body>`:

```html
<script src="_content/DR.Simple_UI/js/DR.Simple_UI.js"></script>
```

`brand.css` must load after the library stylesheet.

Settings are stored under the default `drui.` prefix. Do not set `data-prefix` / `storagePrefix` unless
this app shares an origin with another app; if you do set one, both values must be identical.

### Upgrading

The version is pinned and does not float. Before bumping it, read the release notes at
<https://github.com/dennisrahmen/DR.Simple_UI/releases>, then check the pages this app renders.
