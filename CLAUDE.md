# DR.Simple_UI — conventions

Shared UI layer for Blazor apps. A UI fix is made here once, not re-copied into each app.

Stack: **.NET 10 / Blazor / server-side Razor**. `net10.0`, `LangVersion latest`, `Nullable enable`,
`ImplicitUsings enable`, `TreatWarningsAsErrors true`, xUnit.

## Two tiers

**Tier 1 — the frame.** Razor components from `0.2.0`, CSS-only in `0.1.0`. Shell, sidebar and nav,
header, user widget, toasts, modal shell. Pixel-identical in every app, never restyled per project.

**Tier 2 — the paint.** Tables, forms, cards, badges, buttons, panels, alerts. **CSS classes only, never
components.** Pages write plain HTML and apply the classes.

### New content UI is a CSS class, not a component

Do not add a `<DataTable>` or any other wrapper around page content. Add classes and a catalogue page.

Which tier: if anyone needs to adjust the inside of it, it is a class. If not, it is a component.

Most of this library is CSS.

## No hard-coded colours

Every colour, tint and shadow in `wwwroot/css/DR.Simple_UI.css` resolves through a token declared in one
of the `:root` blocks. No hex, no `rgb()`, no colour keyword anywhere else in the file.

`CssTokenContractTests` also enforces that:

- every `var(--x)` used is declared;
- token blocks declare only custom properties;
- the light and colour-blind blocks only remap tokens and never override a selector, which is what keeps
  CSS load order irrelevant;
- `font-family` rides `var(--font-sans)` / `var(--font-mono)`;
- no app-specific naming (`athene`, `zbx`, `gsearch`, `guide-`, …) appears.

Adding a token is a minor version. Renaming or removing one is major.

## Out of scope

App-specific business UI stays in the app that owns it: approval panels, SLA badges, tour overlays, demo
choosers, first-run guides, claim overlays, ServiceNow journal styling, page-specific grids.

Permanently out of scope:

- MudBlazor, Syncfusion, Radzen, Tailwind.
- Wrapping tables, forms or page content in components.
- Loading anything from a remote URL at runtime. Everything the package needs ships inside it, so no
  host outage can affect a customer site.
- An MCP server. The in-package catalogue covers discovery.
- A second icon set. Remix Icon is bundled and is the only one.

## The catalogue

`src/DR.Simple_UI/wwwroot/catalogue/` — one page per tier-2 class family, with copy-pasteable markup.
Add catalogue pages as classes are added.

Two rules, both test-enforced:

1. **Single source of CSS.** Every page links `../css/DR.Simple_UI.css`, never a copy.
2. **The catalogue ships in the package.** It lives under `wwwroot/`, so it travels as a static web asset:
   same version as the CSS, browsable at `_content/DR.Simple_UI/catalogue/index.html` in a consuming app,
   and readable in the restored package.

A hosted copy is published from `main` by `pages.yml` to <https://github.dennisrahmen.de/>. The
in-package copy remains the source of truth for any AI agent, since the site can be ahead of a released
version. Hosted pages render a notice saying so (`catHostedNotice` in `catalogue.js`), hidden when the
path contains `/_content/`. Keep that notice working.

Write each example once, inside a `<template>`. `catalogue.js` clones it into the demo and prints the same
nodes as the code block. Do not hand-write a `<pre>` next to a demo. Use `data-code-only` for a template
that is not live HTML for that page.

Adding a page: create the `.html`, then add it to `CAT_PAGES` in `catalogue.js`. Tests fail on an orphaned
page and on a nav entry with no page.

`catalogue.css` is the docs' own chrome and may only style `.cat-*` / `.ex-*`.

## Structure

```
src/
  DR.Simple_UI/
    wwwroot/css/DR.Simple_UI.css      tokens, tier-2 classes, frame CSS
    wwwroot/js/DR.Simple_UI.js        shared behaviour, global drSimpleUi
    wwwroot/js/DR.Simple_UI.boot.js   pre-paint theme, loaded in <head>
    wwwroot/catalogue/                the catalogue, ships in the package
    Components/                       tier 1 only, from 0.2.0
  DR.Simple_UI.Tests/                 xUnit; bUnit is added with the components
assets/brand/                         icon, logo, favicon, social preview
build/verify-package.sh               unpacks the .nupkg and asserts its contents
docs/                                 long-form documentation
```

Both projects live under `src/`. Tests sit beside the project they test.

Keep `README.md` short — hero, badges, the two-tier summary, versioning, licence, links into `docs/`.
Detail belongs in `docs/`.

Documentation is written as documentation: state what to do and what the rules are. Do not narrate design
rationale or explain why a choice was made unless the reason changes what a reader should do.

### README.md is also the nuget.org readme

Two constraints, both of which look correct on GitHub and fail on the package page:

- **No raw HTML.** nuget.org renders a subset of Markdown and ignores HTML.
- **Images and links must be absolute.** Relative paths do not resolve on nuget.org, and only
  allow-listed image hosts render — use `https://raw.githubusercontent.com/…`. A test asserts the hero
  image URL is absolute.

## Brand assets

`assets/brand/` holds the icon (SVG, PNG 16→1024), horizontal logo (light and dark), 1280×640 social
preview, and a multi-resolution favicon, in the library's default tokens (`#111827`, `#1F2937`, `#2563EB`,
`#60A5FA`, `#F3F4F6`).

Three are wired into things that fail quietly if renamed, and a test pins them:

- `dr-simple-ui-icon-128.png` → packed as `icon.png`, the NuGet package icon. nuget.org requires a raster
  image of 128×128 or smaller.
- `dr-simple-ui-social-preview.png` → the README hero.
- `wwwroot/catalogue/favicon.ico` and `logo.png` — copies inside the catalogue folder, so they ship as
  static web assets with the pages that use them.

## Icons

Remix Icon is bundled at `wwwroot/lib/remixicon/` and is the only icon set. Add or update it with
`build/vendor-remixicon.sh`, which pins a version, trims the `@font-face` `src` to woff2, preserves the
upstream copyright header, and copies the licence. The output is committed, so the build needs no network.

After changing the version, update `THIRD-PARTY-NOTICES.md` and the version stated in the docs. A test
compares the notice against the version in the vendored CSS header and fails on drift.

The icons stay under the **Remix Icon License v1.0**, not this repo's Apache-2.0. Section 9 of that
licence permits the combination; Sections 2.3 and 3.1 permit bundling them in a UI kit where they are a
minor component. Two restrictions bind this repo directly: the icons may not be redistributed as a
standalone icon pack, and none of them may be used as a logo or app icon — which is why the brand assets
in `assets/brand/` are bespoke rather than built from an icon.

## Naming

- CSS classes: semantic, lowercase-kebab, no app or vendor prefix. Library-owned utilities that need a
  namespace use `dr-` (`.dr-scroll`, `.dr-tip`); everything else is plain (`.card`, `.btn-go`).
- Modifiers: `--` suffix on the block (`.nav-status-card--ok`, `.md-tab--active`).
- Semantic families across buttons, badges and alerts: `go` (sends outward), `warn` (control changes),
  `danger`, `info`, `secret`, plus `cyan` / `orange` / `teal` as categorical hues with no meaning.
- Assets are named after the package. `ShippedAssetsTests` pins the paths; consuming apps hard-code them.
- The JS global is `drSimpleUi`.

## JavaScript

`DR.Simple_UI.js` holds generic UI behaviour only: hover hints, theme settings, clipboard, notifications,
the Markdown editor. App-specific interop stays in the app's own script.

- The hover-hint engine skips elements inside `.sidebar`; the collapsed rail has a CSS flyout, and both
  firing produces a double tooltip.
- An app suppresses hints by setting `drSimpleUi.tips.gate = el => …`. The library has no knowledge of
  what is suppressing them.
- Settings are stored under the `drui.` prefix, which apps do not need to configure — `localStorage` is
  origin-scoped, so apps on separate domains cannot collide. The prefix exists to namespace against other
  code on the same origin, for apps sharing one origin under different paths, and for the language
  cookie, which is not origin-scoped. If an app does override it, `data-prefix` and `storagePrefix` must
  match; a test asserts the two defaults agree.

## Z-order

topbar 60 < modal backdrop 500 < spotlight 510 < popover 550 < toast 600 < hover hints and reconnect
banner 1000. Use one of these values for a new overlay.

## Releasing

The git tag is the version — `v1.2.3` publishes `1.2.3`. No file in the repo records it.

**There is no `CHANGELOG.md` and one must not be added.** Release notes come from the annotated tag
message; the GitHub Releases page is the changelog. A test asserts no changelog file exists.

### When asked to release

A published nuget.org version cannot be replaced, reused or withdrawn. Do not tag without confirming the
version first.

1. `git describe --tags --abbrev=0` for the last released version.
2. Read `git log <last-tag>..HEAD` **and the diff**. A commit subject can hide a contract change.
3. Classify against the rules below, taking the highest applicable level.
4. State the proposed version, its level, and the reason, then wait for confirmation — e.g.
   "0.2.0 (minor): adds `--brand-glow` and `.badge-teal`, nothing renamed or removed."
5. Draft the release notes, confirm those too, then tag:

   ```bash
   git tag -a v0.2.0 -F notes.md
   git push origin v0.2.0
   ```

   Write `notes.md` outside the repo. The first line becomes the release title suffix; the rest becomes
   the body.

`release.yml` builds, tests, packs, verifies the package contents, publishes to nuget.org, and creates the
GitHub release.

### Version rules

Judged by what a consuming app sees, not by the size of the diff.

**Major** — an app breaks or changes appearance without editing anything:

- renaming or removing a token, class or modifier
- changing a tier-1 component's markup, parameters or emitted classes
- renaming a shipped asset path or the JS global
- changing an existing rule's values enough to move layout or colour
- a change that makes an existing app override stop working

**Minor** — additive and backwards compatible:

- a new token, class, variant, component or catalogue page
- a new optional parameter or JS function

**Patch** — no contract change:

- correcting a wrong value
- docs, tests, CI, comments

When a change is arguable, use the higher level and say so. Note the implied level while making an
ordinary change, so step 3 does not become archaeology.

### Trusted publishing

The release job exchanges its GitHub OIDC token (`permissions: id-token: write`) for a single-use NuGet
key valid one hour. No long-lived API key exists in this repo.

It requires a nuget.org policy matching owner `dennisrahmen`, repository `DR.Simple_UI` and workflow file
`release.yml`. The policy matches on the file **name** — renaming the workflow breaks publishing until the
policy is updated. The only secret is `NUGET_USER`, the nuget.org profile name.
