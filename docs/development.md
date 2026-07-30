# Development

Requires the .NET 10 SDK, pinned in `global.json`.

```bash
dotnet build DR.Simple_UI.slnx
dotnet test  DR.Simple_UI.slnx
dotnet pack  src/DR.Simple_UI/DR.Simple_UI.csproj -c Release -o artifacts
bash build/verify-package.sh artifacts/DR.Simple_UI.0.1.0.nupkg
```

## Layout

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
docs/
```

## Tests

The tests are source scans that enforce the conventions:

| Guard | Enforces |
|---|---|
| No colour literals outside the token blocks | Apps can rebrand by redefining tokens |
| Every `var(--x)` is declared | An undeclared token resolves to nothing |
| Token blocks declare only custom properties | Token blocks define values, not styles |
| Theme blocks only remap tokens | CSS load order stays irrelevant |
| `font-family` rides a token | Apps can change typeface |
| No app-specific naming | Extracted app names stay out of the library |
| Every catalogue page links the shipped stylesheet | Examples match the shipped CSS |
| Every catalogue page is reachable from the nav | No orphaned pages |
| `catalogue.css` only styles `.cat-*` / `.ex-*` | Examples render as an app would get them |
| Shipped asset paths are pinned | Apps hard-code these paths |
| No changelog file | Release notes live on the Releases page |
| Brand assets exist; hero URL is absolute | The nuget.org listing renders |

New guards should be made to fail once before being relied on.

## Package verification

`build/verify-package.sh` unpacks the `.nupkg` and asserts:

- the DLL, stylesheet, both scripts, the package icon, README and LICENSE are present
- every catalogue page in the repo is in the package
- the packed stylesheet still contains the token layer
- the catalogue's relative CSS link resolves inside the package
- a scoped-CSS bundle is present if any `.razor.css` exists

Static web assets can be dropped from a package without failing the build, and the result is a runtime
404. CI runs this on every push.

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
