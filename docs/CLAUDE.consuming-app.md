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

The layout frame — shell, sidebar, header, user widget — comes from the library, never from a copy kept
in this app. From the version that ships them, use the frame components; before that, the markup on the
catalogue's Frame page. Do not restyle it here and do not fork it: a copied frame stops receiving library
fixes, and it will silently miss later additions such as the responsive drawer and the skip link.

### Do not style a class name the library owns

Every class in the catalogue belongs to the library. If this app defines a rule for one of those names,
the two sets of declarations merge on upgrade and the appearance changes with no error and no diff in
this repo.

Before bumping the pinned version, read the class names the release adds and grep this app's stylesheets
for each one. Where a name collides, delete the local rule and use the library's, or rename the local one
into this app's own prefix.

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

### Overriding is now trivially easy, which is the reason not to

The library's stylesheet is entirely inside cascade layers. **This app's CSS is unlayered, so any rule
here beats any library rule, whatever the specificity.** You will never again need a longer selector to
win.

That makes discipline the only thing left protecting the shared design:

- **Redefine tokens, not rules.** A token override travels with the theme, the colour-blind palette and
  every future version. A rule override is a private fork of the design that nothing will tell you has
  gone stale.
- **Two consequences of the layering, if this app has old copied CSS:** a token set at bare `:root` here
  now also beats the library's `[data-theme="light"]` value for it, so set both blocks; and any
  unconditional rule here now beats a library rule that used to outrank it. The known case is a copied
  `.table th, .table td { padding: … }`, which now stops compact density from tightening this app's
  tables. **Delete copied library rules** — that was always the intention.
- **Never use `!important` against the library.** Layer order inverts for important declarations, so it
  is not needed and makes the result harder to reason about, not easier.

### Requesting a library change

`DR.Simple_UI` is a separate, versioned package. This app cannot release it.

- Open an issue or pull request at <https://github.com/dennisrahmen/DR.Simple_UI>.
- Describe the value or variant needed and where it is used, not just the CSS you would have written.
- Adding a token or variant is a minor release; renaming or removing one is major. A release can be
  major with nothing renamed or removed — changing an existing rule enough to move layout or colour, or
  making an override in this app stop working, both count. Read the notes, do not assume minor is safe.

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

The version is pinned and does not float. Before bumping it:

1. Read the release notes at <https://github.com/dennisrahmen/DR.Simple_UI/releases>.
2. Grep this app's stylesheets for every class name the release adds — see above.
3. Check the pages this app renders, in each theme (`data-theme`, `data-cvd`, `data-density`).
