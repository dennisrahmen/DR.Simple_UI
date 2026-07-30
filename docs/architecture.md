# Architecture

## Two tiers

**Tier 1 — the frame.** Shell, sidebar and nav, header, user widget. Layout chrome that is
pixel-identical in every app and is not restyled per project. Shipped as CSS in `0.1.0`; Razor components
follow in `0.2.0`.

**Tier 2 — the paint.** Tables, forms, cards, badges, buttons, alerts. Shipped as semantic CSS classes.
Pages write plain HTML and apply the classes.

Content UI is always a class, never a component. There is no `<DataTable>` and there will not be one.

Which tier a thing belongs in: if anyone needs to adjust the inside of it, it is a class. If not, it is a
component.

## The token contract

- Tokens are declared in the library's `:root`, plus the light and colour-blind blocks.
- Every class references tokens. The library CSS contains no colour literals outside the token
  declarations.
- An app redefines tokens in its own stylesheet, loaded after the library. Normally the `--brand*` family,
  `--accent` and `--sidebar-active`.
- Apps must not declare new token names. Request the token instead — see
  [CONTRIBUTING.md](../CONTRIBUTING.md) — and use an app-prefixed variable (`--myapp-…`) in the meantime.
  A bare new name risks colliding with a future library token.

Theme differences are expressed only as token values, so the light and colour-blind blocks contain no
selector overrides and CSS load order does not affect them.

The full token list is on the [Tokens](https://github.dennisrahmen.de/catalogue/tokens.html) catalogue
page, which reads its values from the live stylesheet.

## Theming

`data-theme="light"`, `data-cvd="1"` and `data-density="compact"` are set on `<html>`.

- `DR.Simple_UI.boot.js` applies them from `localStorage` before first paint.
- `drSimpleUi.settings.save('theme', 'light')` updates them at runtime.

The colour-blind palette (`data-cvd="1"`) remaps only the `go` family to blue, so go and danger read as
blue against red. Amber and the brand colour are unchanged.

`data-density="compact"` tightens `.table` padding. Apps tighten their own page-specific components.

## Semantic families

Used consistently across buttons, badges and alerts:

| Family | Meaning |
|---|---|
| `go` | Sends something outward — approve, apply, send |
| `warn` | Changes who is in control — take over |
| `danger` | Destructive or failed |
| `info` | Informational |
| `secret` | Sensitive values |
| `cyan`, `orange`, `teal` | Categorical only, no meaning |

A panel's primary action is a filled button in its semantic colour.

## Z-order

| Layer | z-index |
|---|---|
| Topbar | 60 |
| Modal backdrop | 500 |
| Spotlight | 510 |
| Popover | 550 |
| Toast | 600 |
| Hover hints, reconnect banner | 1000 |

New overlays use one of these values.

## JavaScript

`DR.Simple_UI.js` exposes the global `drSimpleUi`:

| Member | Purpose |
|---|---|
| `configure(options)` | Storage prefix, notification icon, language cookie |
| `settings` | `load()`, `save(key, value)`, `apply()` |
| `tips` | Hover-hint engine. Set `tips.gate = el => bool` to suppress hints conditionally |
| `md` | Markdown editor: `init(root)`, `apply(textarea, cmd)`, `render(src)` |
| `copyText`, `openTab`, `viewportWidth` | Interop helpers |
| `getItem`, `setItem` | `localStorage` access |
| `requestNotify`, `notify`, `ping` | Desktop notifications and an audio ping |

Hover hints are delegated from `document`, so content rendered after load is covered without re-wiring.
Elements inside `.sidebar` are skipped — the collapsed rail has a CSS flyout instead.

`drSimpleUi.md.render()` escapes HTML before re-introducing a fixed set of Markdown constructs, and
restricts link hrefs to `http:`, `https:`, `mailto:` and root-relative paths. Sanitise untrusted input
server-side as well.

## Icons

[Remix Icon](https://remixicon.com) 4.9.1 is bundled at `_content/DR.Simple_UI/lib/remixicon/`. It is the
only icon set; the library styles `i` elements (`.btn i`, `.nav-link i`) and the icon classes come from
this font.

Only `woff2` is shipped. Upstream also carries eot, woff, ttf and svg for IE and iOS 4, which no browser
running Blazor Server needs.

The icons remain under the Remix Icon License v1.0, not Apache-2.0. See
[THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md).

## Out of scope

- App-specific business UI — approval panels, SLA badges, tour overlays, page-specific grids.
- MudBlazor, Syncfusion, Radzen, Tailwind.
- Wrapping tables, forms or page content in components.
- Loading anything from a remote URL at runtime. Everything the package needs, including the icon font,
  ships inside it.
