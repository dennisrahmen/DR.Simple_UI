# `wwwroot/catalogue/` — how the catalogue is written

One page per tier-2 class family, with copy-pasteable markup. A class with no catalogue page is a class
nobody can find, so the page is part of adding the class, not a follow-up.

This folder is under `wwwroot/`, so it **ships in the package** as a static web asset: same version as
the CSS, browsable at `_content/DR.Simple_UI/catalogue/index.html` in a consuming app, and readable in
the restored package. That is the copy an agent should trust. A hosted copy is published from `main` to
<https://github.dennisrahmen.de/> and can be ahead of any released version, which is what the
`catHostedNotice` strip says.

## Adding a page

1. Create `<name>.html`. Copy the head, shell and `<noscript>` block from an existing page verbatim —
   they are identical on every page and two tests depend on them.
2. Add an entry to `CAT_PAGES` in `catalogue.js`, with a `label`, a Remix Icon name and a one-sentence
   `blurb`. The sidebar, the landing-page tiles and the nav are all built from it.
3. `dotnet test`. A page missing from `CAT_PAGES` and a `CAT_PAGES` entry with no page both fail.

## Write each example exactly once

Put the markup in a `<template>`. `catalogue.js` clones it into the live demo **and** prints those same
nodes as the code block, so the demo and the snippet cannot drift apart.

```html
<section class="cat-ex" data-example data-demo="ex-demo--block">
    <h2>Section title</h2>
    <p>One or two sentences: when to use this, and what the reader would otherwise get wrong.</p>
    <template>
        <span class="badge badge-go">Approved</span>
    </template>
</section>
```

**Never hand-write a `<pre>` beside a demo.** That is the one thing this arrangement exists to prevent.

- `data-demo="ex-demo--block"` lays the demo out as a block instead of a centred row. Omit it for
  small inline things like a badge or a button.
- `data-code-only` on the `<section>` prints the snippet without rendering it. Use it when the template
  is not live HTML for that page — a CSS override, or host-page `<script>` tags. It also keeps a
  `<script>` from executing: `cloneNode` clears the "already started" flag, so a cloned script runs.
- `<h2>` gets an id and a link anchor automatically, so any section can be linked to directly.

## Rules

- **One source of CSS.** Link `../css/DR.Simple_UI.css`. Never copy the stylesheet or inline a rule
  from it — a test fails on both. The examples have to be what the shipped CSS actually does.
- **Nothing is loaded from a remote host**, in the page or in a `<template>`. The whole package works
  offline; a CDN link inside a template is a real dependency, because it gets cloned into the page.
- **`catalogue.css` may only style `.cat-*` and `.ex-*`.** It is the docs' own chrome. Styling anything
  else would make an example look better here than in the app that copies it. A test enforces it.
- **`z-index` comes from the documented scale** in `docs/architecture.md`, in `catalogue.css` too.
- **The site chrome is the library's own frame classes** — `.layout`, `.sidebar`, `.topbar`, `.page`,
  `.nav-link`. This site is an application built with DR.Simple_UI, not a site describing one, so a
  regression in the frame shows up here first.
- **The examples are the documentation.** Prefer realistic content — a real-sounding queue, an actual
  error message — over `Foo` and `Lorem ipsum`. Say what a class is *for* and which mistake it avoids;
  do not narrate the CSS, which the reader can read.

## Local preview

```bash
# .claude/launch.json defines this
python -m http.server 8129 --directory src/DR.Simple_UI/wwwroot
```

Open `http://localhost:8129/catalogue/`. **A `file://` page will not work** — it renders as an inert
snapshot, `catalogue.js` never runs, and every demo and code block is blank. That looks exactly like
the examples having gone missing.

When checking a CSS change in the browser, **cache-bust the stylesheet** before believing a computed
style: the preview pane will happily keep serving the previous copy.
