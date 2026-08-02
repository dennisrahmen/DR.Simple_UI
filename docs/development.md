# Development

Requires the .NET 10 SDK, pinned in `global.json`.

```bash
dotnet build DR.Simple_UI.slnx
dotnet test  DR.Simple_UI.slnx
dotnet pack  src/DR.Simple_UI/DR.Simple_UI.csproj -c Release -o artifacts
bash build/verify-package.sh artifacts/DR.Simple_UI.*.nupkg
```

## Layout

```
src/
  DR.Simple_UI/
    css-parts/                        the stylesheet, authored in 57 files
    js-parts/                         the script, authored in 12 files
    wwwroot/css/DR.Simple_UI.css      GENERATED from css-parts/ — do not edit
    wwwroot/js/DR.Simple_UI.js        GENERATED from js-parts/ — do not edit
    wwwroot/js/DR.Simple_UI.boot.js   pre-paint theme, loaded in <head>; standalone
    wwwroot/catalogue/                the catalogue, ships in the package
    wwwroot/tokens/…tokens.json       GENERATED from the token parts — do not edit
    Components/                       tier 1 only — markup in .razor, API in .razor.cs
  DR.Simple_UI.Tests/                 xUnit + bUnit
assets/brand/                         icon, logo, favicon, social preview
templates/                            the `dotnet new dr-blazor` template, a separate package
build/verify-package.sh               unpacks the .nupkg and asserts its contents
build/verify-template.sh              generates a project from the template and builds it
docs/
```

The part counts above are hand-maintained and only orient a reader — `bundle-css.sh --check` and
`bundle-js.sh --check` are what actually hold the bundles to the directory.

## Tests

The tests are source scans that enforce the conventions:

| Guard | Enforces |
|---|---|
| No colour literals outside the token blocks | Apps can rebrand by redefining tokens |
| Every `var(--x)` is declared | An undeclared token resolves to nothing |
| Token blocks declare only custom properties | Token blocks define values, not styles |
| Theme blocks only remap tokens | CSS load order stays irrelevant |
| Appearance media queries only remap tokens | Same, for `prefers-color-scheme` / `prefers-contrast` / `forced-colors` |
| Layout media queries only change geometry | A colour set inside a breakpoint is invisible to a rebrand and to a theme remap. Token-valued properties are allowed |
| The responsive rail mirrors the collapsed rail | CSS cannot alias a selector, so the two are duplicated; this catches the drift |
| Every class a component emits exists in the stylesheet | A misspelt class name renders unstyled with no error |
| Components and `catalogue/frame.html` agree | Hand-written frame markup and the components must describe the same frame |
| The package takes no third-party dependency | `Microsoft.AspNetCore.Components.Web` is the one allowed reference |
| Spacing, type and motion ride their scales | A literal is invisible to the token layer, so one hard-coded `14px` means "make this app denser" cannot reach that rule |
| No physical direction properties without a justification | The layout mirrors from `dir="rtl"` on its own; exceptions carry an `/* rtl-ok: why */` marker |
| Every rule is inside a cascade layer | An unlayered library rule outranks the whole library *and* is unreachable from an app |
| The layer order is declared up front | An undeclared layer sorts after every declared one |
| The token export matches the stylesheet | The JSON is generated and committed, so it can drift |
| Every class is shown somewhere in the catalogue | "A class nobody can find is a class nobody uses" — this had no enforcement and found 97 |
| The template references a version that has the components | It once defaulted to 0.1.0, and the generated app failed with five CS0246 errors |
| No `!important` | An app can always win an override |
| Nothing is loaded or inlined | No runtime fetch, and no `data:` URI smuggling a colour past the colour guard |
| Every `z-index` is on the documented scale | Overlay ordering stays reviewable |
| `font-family` rides a token | Apps can change typeface |
| No app-specific naming | Extracted app names stay out of the library |
| Every catalogue page links the shipped stylesheet | Examples match the shipped CSS |
| Every catalogue page is reachable from the nav | No orphaned pages |
| `catalogue.css` only styles `.cat-*` / `.ex-*` | Examples render as an app would get them |
| Shipped asset paths are pinned | Apps hard-code these paths |
| No changelog file | Release notes live on the Releases page |
| Brand assets exist; hero URL is absolute | The nuget.org listing renders |

New guards should be made to fail once before being relied on.

## Browser tests

A source scan cannot see the one failure mode this library really suffers: a rule that parses fine, is
reported by nothing, and silently does nothing because a more specific rule already set the property.
Three of those shipped into `0.3.0` and were caught by reading `getComputedStyle` in a real browser.

```bash
pwsh src/DR.Simple_UI.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

The browser binaries are not restored with the package, so **without that step the browser tests pass
without asserting anything**. `DR_UI_BROWSER_TESTS=1` turns a missing browser into a failure instead of
a silent skip; CI sets it, and you should too when you care about the result:

```bash
DR_UI_BROWSER_TESTS=1 dotnet test DR.Simple_UI.slnx
```

They assert computed values, never screenshots — see the decision note in
[`architecture.md`](architecture.md#decisions-with-a-measurement-behind-them).

`axe-core` runs over the same pages for the WCAG rules that only exist in the accessibility tree. The
catalogue's examples are the library's own markup, so a violation there is one every app inherits by
copying — which makes it the cheapest place in the project to catch one. Its first run found 16,
including one serious library defect: in the collapsed rail the labels were `display: none`, so every
navigation link lost its accessible name.

**One trap, learned the hard way.** Nearly everything interactive here has a `transition`, and
`getComputedStyle` read immediately after a class or checkedness change returns the value at t=0 — the
one being transitioned *away* from. That reads exactly like a broken selector and nearly caused a
redesign of the segmented control. `Interactive_states_repaint_when_the_state_changes` switches
transitions off first and keeps a `.sidebar.collapsed` sentinel, on the grounds that if a plain class
toggle appears not to work then the measurement is wrong, not the CSS.

## The project template

```bash
dotnet pack src/DR.Simple_UI/DR.Simple_UI.csproj -c Release -o artifacts
dotnet pack templates/DR.Simple_UI.Templates.csproj -c Release -o artifacts
bash build/verify-template.sh
```

`verify-template.sh` installs `dr-blazor`, generates a project against the package just built, and
compiles it with `-warnaserror`. A template is not verified by packing cleanly: the first version
packed, installed, generated — and produced an app that did not compile.

## Package verification

`build/verify-package.sh` unpacks the `.nupkg` and asserts:

- the DLL, stylesheet, both scripts, the package icon, README and LICENSE are present
- every catalogue page in the repo is in the package
- the packed stylesheet still contains the token layer
- the catalogue's relative CSS link resolves inside the package
- a scoped-CSS bundle is present if any `.razor.css` exists

Static web assets can be dropped from a package without failing the build, and the result is a runtime
404. CI runs this on every push.

## Editing the CSS and the JS

Both shipped assets are authored as a directory of small files and generated into the single file
that ships:

```bash
build/bundle-css.sh            # regenerate wwwroot/css/DR.Simple_UI.css from css-parts/
build/bundle-js.sh             # regenerate wwwroot/js/DR.Simple_UI.js  from js-parts/
build/export-tokens.sh         # regenerate wwwroot/tokens/DR.Simple_UI.tokens.json
build/bundle-css.sh --check    # fail if it is out of date
build/bundle-js.sh  --check
build/export-tokens.sh --check
```

The token export is the design contract for consumers that are not CSS — a Figma import, a report
generator picking chart colours, a contrast audit. It is an ordered array of blocks with their media
conditions rather than a map of themes, because `:root` appears twice (once at the top level and again
inside `prefers-contrast`) and a map would silently lose one.

`DR.Simple_UI.boot.js` is deliberately outside this: it is a standalone ~40-line file loaded in
`<head>` to apply the stored theme before first paint, and bundling it would defeat its purpose.

Edit the part, then run the script. `The_shipped_stylesheet_matches_its_parts` fails the build if the
two disagree, so a forgotten regeneration cannot ship.

**Parts are discovered, not listed.** The generator reads every `*.css` in the directory, so adding a
file is the whole job — there is no manifest to keep in step, which is the one thing a hand-kept index
is guaranteed to get wrong eventually. Cascade order is the byte-ordinal filename order, which is why
each part carries a numeric prefix and why the build fails on a part without one: `0x` tokens, themes
and base elements, `1x` the tier-1 frame, `3x`–`4x` tier-2 classes, `9x` density last.

The generated file opens with a contents block listing the parts in order and introduces each one with
a `── <file> ──` marker, so the single shipped stylesheet still reads as one section per component.

The JS works the same way, with one extra rule the generator enforces: every part must end with a
terminated IIFE (`})(window.drSimpleUi);`), because without the semicolon automatic semicolon insertion
can splice a part into the next one. Each part extends the one global and is therefore a valid script
on its own; `00-core.js` must come first, since it creates the global and the internals the others read.

Conventions live next to the files themselves, one `CLAUDE.md` per directory:
[`css-parts/`](../src/DR.Simple_UI/css-parts/CLAUDE.md),
[`js-parts/`](../src/DR.Simple_UI/js-parts/CLAUDE.md),
[`Components/`](../src/DR.Simple_UI/Components/CLAUDE.md) and
[`wwwroot/catalogue/`](../src/DR.Simple_UI/wwwroot/catalogue/CLAUDE.md).

Two things deliberately not done:

- **The parts are not static web assets.** They live outside `wwwroot`, so they are not in the
  package and there is exactly one supported stylesheet path. To reuse one part on its own, take the
  tokens part plus that part from the repo.
- **The parts are not loaded individually at runtime.** A JS loader would leave content unstyled
  until scripts run and make the CSS depend on JS; `@import` serialises the requests, because the
  browser cannot discover an import until the parent sheet has parsed. The .NET SDK cannot bundle
  this for us either — its only CSS bundling is scoped `.razor.css`, which rewrites selectors to add
  a `b-{hash}` attribute and would scope tier-2 classes to markup the library renders rather than
  markup the app writes.

## Updating the icon font

```bash
build/vendor-remixicon.sh          # the pinned version
build/vendor-remixicon.sh 4.9.2    # a specific version
```

Writes `wwwroot/lib/remixicon/`. The output is committed. Afterwards, update the version in
`THIRD-PARTY-NOTICES.md` and in `docs/getting-started.md` and `docs/architecture.md` — a test fails if
the notice and the vendored CSS header disagree.

## Adding a catalogue page

1. Create the `.html` in `src/DR.Simple_UI/wwwroot/catalogue/`.
2. Add it to `CAT_PAGES` in `catalogue.js`.

Write each example once, inside a `<template>`. `catalogue.js` clones it into the demo and prints the same
nodes as the code block. Do not hand-write a `<pre>` next to a demo.

Use `data-code-only` on an example whose template is not live HTML for that page — a CSS snippet, or host
page `<script>` tags.

## The hosted catalogue

`pages.yml` stages `wwwroot/css`, `wwwroot/js` and `wwwroot/catalogue` into `_site/`, keeping that
structure so the pages' `../css/DR.Simple_UI.css` link resolves. The root redirect and the `CNAME` are
generated into `_site` only; adding them to `wwwroot` would ship them in the package.

The site always shows `main`, so it can be ahead of a released version. Hosted pages render a notice
saying so, hidden when the path contains `/_content/`.

## Conventions

[`CLAUDE.md`](../CLAUDE.md) in the repo root is the full convention document.
