# Architecture

## Two tiers

**Tier 1 — the frame.** Shell, sidebar and nav, header, user widget. Layout chrome that is
pixel-identical in every app and is not restyled per project. Shipped as CSS classes and, from `0.2.0`,
as Razor components that emit exactly those classes.

**Tier 2 — the paint.** Tables, forms, cards, badges, buttons, alerts. Shipped as semantic CSS classes.
Pages write plain HTML and apply the classes.

Content UI is always a class, never a component. There is no `<DataTable>` and there will not be one.

Which tier a thing belongs in: if anyone needs to adjust the inside of it, it is a class. If not, it is a
component.

## The frame components

| Component | Emits | Notes |
|---|---|---|
| `AppShell` | `.layout > .content > .page`, or `.bare-layout > .page` with `Bare` | `.page` is the only scroll container; do not wrap it in another |
| `Sidebar` | `aside.sidebar`, `.brand`, `nav.nav > .nav-scroll`, `.nav-tools` | `Collapsed` adds `.collapsed` and nothing else — the rail is pure CSS |
| `NavItem` | `a.nav-link`, `.nav-link-tool`, `.nav-count`, `.nav-link-ext` | Tracks the address and sets `aria-current="page"` as well as `active` |
| `AppHeader` | `header.topbar` with `.topbar-spacer` between `Start` and the child content | The spacer is always emitted, so it cannot be forgotten |
| `UserWidget` | `.user-widget`, `.user-trigger`, `.user-avatar`, `.user-info`, `.user-signout`, `.user-menu` | Trigger is a `<button>` only when there is a `Menu` to open |

The components and the hand-written markup on the catalogue's *Shell & nav* page are interchangeable,
and `ComponentClassContractTests` fails if they diverge or if a component names a class the stylesheet
does not define.

Three things that decide how these are written:

- **`NavItem`, never `NavLink`.** `Microsoft.AspNetCore.Components.Routing.NavLink` is in scope in
  every Blazor app. A component called `NavLink` here would become an ambiguous reference the moment an
  app added `@using DR.Simple_UI.Components`, breaking every existing `<NavLink>` in it.
- **Every component declares a `Class` parameter.** Blazor matches parameters case-insensitively, so
  `Class` also captures a plain `class="…"` written at the call site and appends it. Without it,
  `<Sidebar class="x">` would land in `AdditionalAttributes` and replace `.sidebar`, breaking the
  layout with no error.
- **No component requires JavaScript.** The user widget's dropdown is opened by Blazor state and
  dismissed by a scrim element and an `@onkeydown`, not by `DR.Simple_UI.js`.

The only package dependency is `Microsoft.AspNetCore.Components.Web`, which is where `ComponentBase`
and `NavigationManager` live. It is a `PackageReference` rather than a `FrameworkReference` because the
shared framework is not available to a Blazor WebAssembly consumer. A test fails on any third-party
package reference.

## Responsive frame

`.layout--responsive` on `.layout` turns the sidebar into the icon rail below 900px and trims the user
widget's text at 900px and 560px. **Opt-in**: applying it automatically would change how every released
app looks on a narrow screen with no app edit.

It is also the fix for the reverse hazard. An app that sets `.sidebar { width: 260px }` unconditionally
would beat a bare `@media .sidebar { width: 56px }` rule on source order; `.layout--responsive .sidebar`
is `(0,2,0)` and outranks it.

Two rules hold for any layout media query added later:

- **Geometry only**, or a value that comes entirely from a token. A colour set inside a breakpoint is
  invisible to both a rebrand and a theme remap, and reappears at one window size.
  `Layout_media_queries_only_change_geometry` enforces it.
- The responsive rail **duplicates** `12-frame-collapsed-rail.css`, because CSS cannot alias a selector
  — there is no way to say "also apply the rail when this media query matches".
  `The_responsive_frame_mirrors_the_collapsed_rail` fails when the two drift.

## Cascade layers

The shipped stylesheet is entirely inside cascade layers, declared up front:

```css
@layer dr.tokens, dr.base, dr.frame, dr.paint, dr.utilities, dr.overrides;
```

**Your stylesheet is unlayered, so it beats all of them, whatever the specificity.** That is the point:
overriding the library no longer needs a longer selector than the library's, and there is nothing to
out-specify.

| Layer | Parts | Holds |
|---|---|---|
| `dr.tokens` | `00`–`04` | tokens and the theme remap blocks |
| `dr.base` | `05`–`09` | bare element styles |
| `dr.frame` | `10`–`29` | tier 1 |
| `dr.paint` | `30`–`79` | tier 2, then RTL, forced colours, print |
| `dr.utilities` | `80`–`89` | single-purpose classes |
| `dr.overrides` | `90`–`99` | density, reduced motion |

Each part's layer comes from its numeric prefix, so it cannot drift away from the source order. Two
tests hold the model up: one fails if any rule escapes a layer — an unlayered library rule would
outrank the whole library *and* be unreachable from your stylesheet — and one fails if a layer is used
without being in the ordering statement, since an undeclared layer sorts after every declared one.

### Three consequences worth knowing before upgrading

**A token you set at bare `:root` now also beats the library's `[data-theme="light"]` value for it.**
Before layers, the library's light block won on specificity. Set both blocks, as the rebrand recipe
shows — the recipe has always shown both, so a rebrand that follows it is unaffected.

**An unconditional rule of yours now beats a library rule that used to outrank it.** The clearest case
is compact density: an app still carrying its own `.table th, .table td { padding: 8px 12px }` from
before the extraction used to lose to `:root[data-density="compact"] .table td` at (0,3,1). It now
wins, and compact density stops tightening its tables. The fix is to delete the copied rule, which was
the intention anyway.

**`!important` is now actively harmful, not merely unnecessary.** Layer order *inverts* for important
declarations, so an `!important` inside `dr.paint` becomes harder for you to override than an ordinary
declaration would be. The library uses none, and a test enforces it.

## The token contract

- Tokens are declared in the library's `:root`, plus the light and colour-blind blocks.
- Every class references tokens. The library CSS contains no colour literals outside the token
  declarations.
- An app redefines tokens in its own stylesheet, loaded after the library. Normally the `--brand*` family,
  `--accent` and `--sidebar-active`.
- Some tokens are derived from others rather than restated. `--brand-tint`, `--brand-ring`,
  `--brand-ring-soft`, `--brand-ring-check` and `--brand-glow` are `color-mix()` of `--brand`, so
  redefining `--brand` carries all five. An app may still pin any of them to change the alpha.
  A derived token serialises through `getComputedStyle` as `color(srgb …)` rather than `rgba(…)`; the
  painted result is unchanged, but a tool that compares computed-style *strings* will report a difference
  where there is none.
- Apps must not declare new token names. Request the token instead — see
  [CONTRIBUTING.md](../CONTRIBUTING.md) — and use an app-prefixed variable (`--myapp-…`) in the meantime.
  A bare new name risks colliding with a future library token.

Theme differences are expressed only as token values, so the light and colour-blind blocks contain no
selector overrides and CSS load order does not affect them.

The full token list is on the [Tokens](https://github.dennisrahmen.de/catalogue/tokens.html) catalogue
page, which reads its values from the live stylesheet.

Every token also ships as JSON, for a design tool that needs the values without parsing CSS:

| Where | Path |
|---|---|
| In a running app | `_content/DR.Simple_UI/tokens/DR.Simple_UI.tokens.json` |
| In the repo, and in the restored package | `wwwroot/tokens/DR.Simple_UI.tokens.json` |
| Hosted | <https://github.dennisrahmen.de/tokens/DR.Simple_UI.tokens.json> |

`blocks` is an **ordered** array of `{ media, selector, tokens }` — merge them in order, applying a block
when its media condition matches and its selector matches the document root. It is not a map keyed by
theme: `:root` appears three times across the ten blocks (base, forced-colors, density), so a map would
silently lose two of them, and the media condition is part of the contract.

Generated by `build/export-tokens.sh` from the same parts as the stylesheet;
`The_token_export_matches_the_stylesheet` fails on drift, and `verify-package.sh` asserts the path ships.

## Theming

`data-theme="light"`, `data-cvd="1"` and `data-density="compact"` are set on `<html>`.

- `DR.Simple_UI.boot.js` applies them from `localStorage` before first paint.
- `drSimpleUi.settings.save('theme', 'light')` updates them at runtime.

`data-theme` is **always** present, set to `light` or `dark`, never absent. Consuming apps select on
`:root[data-theme="light"]` to brand the light palette, so that selector has to match whenever the light
palette is in use. Any future support for the operating system's preference must therefore resolve
`prefers-color-scheme` into this attribute in `boot.js` — expressing it as a `@media` block instead would
make the light palette reachable without the attribute, and every consuming app's light-theme branding
would silently stop applying.

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

| Layer | z-index | In the stylesheet |
|---|---|---|
| Local stacking inside a component | 0, 1 | not the overlay scale — a sticky table header above its own rows |
| Topbar | 60 | yes |
| User widget | 200 | yes |
| Collapsed-rail flyout | 400 | yes |
| Drawer scrim | 480 | the catalogue's own drawer |
| Drawer panel | 490 | the catalogue's own drawer |
| Modal backdrop | 500 | yes |
| Spotlight | 510 | yes — `.spotlight-hole`, `.spotlight-tip` |
| Popover, dropdown menu | 550 | yes — `.menu`, and the user widget's own panel. `.popover` is not shipped |
| Toast | 600 | yes — `.toast-stack` |
| Hover hints, reconnect banner | 1000 | yes |

A new overlay uses one of these values. `Every_z_index_comes_from_the_documented_scale` fails on any
other, so adding a layer means adding it to this table first.

Two things the flat list does not say:

- **`.topbar` (60) and `.user-widget` (200) create stacking contexts.** A panel nested inside either is
  ordered *within* that context, so its z-index is local and cannot lift it above a modal backdrop. A
  dropdown that must escape belongs in the top layer instead.
- **The top layer ignores z-index entirely.** An element promoted by `popover` or `dialog.showModal()`
  paints above every non-top-layer element regardless of this scale, and among top-layer elements the
  order is promotion order, not z-index. Once a family moves to the top layer, its row here describes
  the fallback path only.

## JavaScript

`DR.Simple_UI.js` exposes the global `drSimpleUi`:

| Member | Purpose |
|---|---|
| `configure(options)` | Storage prefix, notification icon, language cookie |
| `settings` | `load()`, `save(key, value)`, `apply()` |
| `tips` | Hover-hint engine. Set `tips.gate = el => bool` to suppress hints conditionally |
| `toast(message, options)` | Creates and reuses `.toast-stack`. Returns its own remover; `timeout: 0` stays until dismissed |
| `confirm(options)` | A `<dialog>.showModal()` confirmation. Returns a promise; `danger: true` reddens confirm and focuses cancel |
| `menu` | Delegated dropdowns. `closeAll()`, for after a navigation |
| `tabs` | Delegated tabs with the arrow/Home/End keyboard contract. `select(tabOrPanelId)` |
| `palette` | Command palette, opened by Ctrl/⌘-K once commands exist: `register(list)`, `open()`, `close()`, `rank(query)` |
| `md` | Markdown editor: `init(root)`, `apply(textarea, cmd)`, `render(src)` |
| `copyText`, `openTab`, `viewportWidth` | Interop helpers |
| `getItem`, `setItem` | `localStorage` access |
| `requestNotify`, `notify`, `ping` | Desktop notifications and an audio ping |

`ui._` also exists and is **private** — shared closure state the parts need. It may change in a patch.

Four behaviours are delegated from `document`, so content rendered after load is covered without
re-wiring: hover hints, `data-menu-toggle`, `data-tabs`, and `data-copy` / `data-copy-target`. The last
has no member on the global — the attribute is the whole API.
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

## Decisions with a measurement behind them

Recorded so they are not re-opened from intuition.

**No minification.** Measured on the shipped assets: minifying the CSS and JS saves **4,360 brotli
bytes**, about **2% of first load**. The .NET SDK already serves the stylesheet gzipped at 11,289
bytes. The cost would be a build step, a second artefact to keep in step with the parts, and a
stylesheet nobody can read in DevTools or in the restored package. Not worth 2%.

**No pixel baselines for visual regression.** Screenshot comparison is the obvious tool and the wrong
one here: baselines rendered on Windows do not match the Linux CI runner (font rasterisation and
scrollbar metrics differ), so the suite either fails constantly or gets a tolerance wide enough to
miss real changes. The regressions this library actually suffers are **cascade** regressions — a class
that loses a property to a more specific rule and silently does nothing. Three of those were found
during 0.3.0, and all three were found by reading `getComputedStyle`, not by looking. So the browser
tests assert computed values.

**No CSS anchor positioning.** Firefox ESR 140 has none of it, and it is the floor this library
supports. An anchored popover would work in Chrome and float in the middle of the viewport in Firefox
ESR — worse than not shipping it. `.menu-anchor` covers the anchored case with `position: relative`,
which needs nothing measured.

**Scroll-driven animations are avoided** for a sharper reason: they fail *incorrectly*. A browser that
drops `animation-timeline` leaves the rest of the `animation` shorthand running, so the animation plays
on a timer instead of not at all.

## Out of scope

- App-specific business UI — approval panels, SLA badges, tour overlays, page-specific grids.
- MudBlazor, Syncfusion, Radzen, Tailwind.
- Wrapping tables, forms or page content in components.
- Loading anything from a remote URL at runtime. Everything the package needs, including the icon font,
  ships inside it.
