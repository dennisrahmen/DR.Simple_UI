# Migrating from DR.Simple_UI

`DR.Simple_UI` is now `Sedna.UI`. The package ID, the namespace, the asset paths,
the JavaScript global, the CSS utility prefix, the cascade layers and the
`localStorage` prefix all change. There are no aliases and no compatibility
shims: a shim is a second code path nobody tests, and it outlives the migration
it was written for.

## 1. The package

```bash
dotnet remove package DR.Simple_UI
dotnet add package Sedna.UI
```

Pin the version. Do not use a floating version range.

## 2. The host page

Every `_content/…` path is keyed by assembly name, so it moves with the package. Replace the old five
lines:

```html
<script src="_content/DR.Simple_UI/js/DR.Simple_UI.boot.js"></script>

<link rel="stylesheet" href="_content/DR.Simple_UI/lib/remixicon/remixicon.css" />
<link rel="stylesheet" href="_content/DR.Simple_UI/css/DR.Simple_UI.css" />
<link rel="stylesheet" href="css/brand.css" />
```

```html
<script src="_content/DR.Simple_UI/js/DR.Simple_UI.js"></script>
```

with the new five:

```html
<script src="_content/Sedna.UI/js/Sedna.UI.boot.js"></script>

<link rel="stylesheet" href="_content/Sedna.UI/lib/remixicon/remixicon.css" />
<link rel="stylesheet" href="_content/Sedna.UI/css/Sedna.UI.css" />
<link rel="stylesheet" href="css/brand.css" />
```

```html
<script src="_content/Sedna.UI/js/Sedna.UI.js"></script>
```

`css/brand.css` is your own file and its path does not change.

## 3. Identifiers

| Old (`DR.Simple_UI`) | New (`Sedna.UI`) |
|---|---|
| Package / namespace | `DR.Simple_UI` → `Sedna.UI` |
| Interop class | `DrSimpleUi` → `SednaUi` |
| Interface | `IDrSimpleUi` → `ISednaUi` |
| Options | `DrSimpleUiOptions` → `SednaUiOptions` |
| Settings record | `DrSimpleUiSettings` → `SednaUiSettings` |
| DI registration | `AddDrSimpleUi()` → `AddSednaUi()` |
| JavaScript global | `window.drSimpleUi` → `window.sednaUi` |
| Cascade layers | `dr.tokens, dr.base, dr.frame, dr.paint, dr.utilities, dr.overrides` → `sedna.tokens, sedna.base, sedna.frame, sedna.paint, sedna.utilities, sedna.overrides` |

`Program.cs` changes from:

```csharp
builder.Services.AddDrSimpleUi();
```

to:

```csharp
builder.Services.AddSednaUi();
```

Every other member on the interface — `ToastAsync`, `ConfirmAsync`, `CopyTextAsync`,
`SaveSettingAsync`, `LoadSettingsAsync` and the rest — keeps its name; only the type and the
registration call move.

## 4. CSS classes

Only the library's own namespaced utilities move. Semantic class names — `.card`, `.badge-go`,
`.btn`, `.modal` and the rest — are unchanged and need no edit.

| Old | New |
|---|---|
| `.dr-scroll` | `.sedna-scroll` |
| `.dr-tip` | `.sedna-tip` |
| `.dr-tip--visible` | `.sedna-tip--visible` |

Grep your own stylesheets and markup for `dr-scroll`, `dr-tip` and `dr-tip--visible` before
upgrading — an app that already defines one of those names sees no error, only a changed rule once
the library's version wins the cascade.

## 5. Stored settings are lost — read this

`localStorage` keys move from a `drui.` prefix to a `sedna.` prefix:

| Old key | New key | Holds |
|---|---|---|
| `drui.theme` | `sedna.theme` | `"dark"` or `"light"` |
| `drui.cvd` | `sedna.cvd` | Whether the colour-blind palette is on |
| `drui.density` | `sedna.density` | Whether compact density is on |
| `drui.dir` | `sedna.dir` | `"ltr"` or `"rtl"` |
| `drui.lang` | `sedna.lang` | The two-letter language code |

Reading the old `drui.*` keys and migrating their values into the new `sedna.*` ones is deliberately
not implemented. Every user who has visited before sees the library's defaults once, the same as a
first-time visitor: dark theme, no colour-blind palette, comfortable density, and `<html dir>` /
`<html lang>` left exactly as the host page wrote them. Nothing is destructive — the old `drui.*`
keys stay in `localStorage`, unread and harmless, until the browser evicts them on its own.

If your app calls `configure({ storagePrefix: … })` or passes `data-prefix` on the boot script, update
both to a value with the new prefix — the two have to match, or the theme is not found on reload.

## 6. Cascade layers

Only relevant if your app's own stylesheet addresses one of the library's layers by name — for
example an `@layer` statement that orders against it, or a debugging rule written directly into one
of them.

The shipped stylesheet still declares the same six layers, in the same order, with the same purpose;
only the layer names themselves change, from the `dr.*` family to the `sedna.*` family:

```css
@layer sedna.tokens, sedna.base, sedna.frame, sedna.paint, sedna.utilities, sedna.overrides;
```

An app that does not name a layer explicitly needs no change here — the layer boundaries and their
ordering are exactly what they were.
