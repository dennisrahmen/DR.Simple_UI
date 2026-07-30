/* Catalogue plumbing.
   ───────────────────────────────────────────────────────────────────────────
   The site chrome is built from the library's own frame classes — .layout,
   .sidebar, .topbar, .page, .nav-link, .topbar-btn — so these pages are an
   application built with DR.Simple_UI, not a site that merely describes it. A
   regression in the frame is visible here immediately.

   Three jobs:
   1. SHELL   — header and sidebar are rendered from CAT_PAGES, so a new page is
                added in one place and cannot go unlinked.
   2. EXAMPLES— each example's markup is authored once inside a <template>. This
                clones it into the live demo AND prints those same nodes as the
                code block, so the two cannot drift apart.
   3. STATE   — theme / colour-blind / density toggles, and the mobile drawer.

   Icons are Remix Icon (`ri-*`), bundled in the package. It is the only icon set. */

const CAT_PAGES = [
    {
        group: 'Start',
        items: [
            { href: 'index.html', label: 'Overview', icon: 'ri-home-4-line',
              blurb: 'What the library is, how to install it, and how to rebrand an app.' },
            { href: 'tokens.html', label: 'Tokens', icon: 'ri-palette-line',
              blurb: 'Every colour, font and shadow token, read live from the loaded stylesheet.' }
        ]
    },
    {
        group: 'Content',
        items: [
            { href: 'button.html', label: 'Buttons', icon: 'ri-cursor-line',
              blurb: 'Six variants, chosen by meaning rather than appearance.' },
            { href: 'badge.html', label: 'Badges', icon: 'ri-price-tag-3-line',
              blurb: 'Semantic pills plus three categorical hues.' },
            { href: 'card.html', label: 'Cards', icon: 'ri-square-line',
              blurb: 'Head, body and key/value rows. Put whatever markup you need inside.' },
            { href: 'table.html', label: 'Tables', icon: 'ri-table-line',
              blurb: 'One class on the table. You write the rows.' },
            { href: 'form.html', label: 'Forms', icon: 'ri-edit-box-line',
              blurb: 'Fields, checkboxes, read-only values and two-up rows.' },
            { href: 'toolbar.html', label: 'Toolbar', icon: 'ri-filter-3-line',
              blurb: 'The filter bar that sits above a table or list.' },
            { href: 'modal.html', label: 'Modal', icon: 'ri-window-2-line',
              blurb: 'Backdrop, panel, header, body and footer.' },
            { href: 'alert.html', label: 'Alerts', icon: 'ri-error-warning-line',
              blurb: 'Inline banners for a state that persists while the page is open.' },
            { href: 'grid.html', label: 'Grids', icon: 'ri-layout-grid-line',
              blurb: 'Two breakpoint-free layout primitives.' },
            { href: 'markdown.html', label: 'Markdown', icon: 'ri-markdown-line',
              blurb: 'Rendered Markdown, and an editor with a live preview.' }
        ]
    },
    {
        group: 'Frame',
        items: [
            { href: 'frame.html', label: 'Shell & nav', icon: 'ri-side-bar-line',
              blurb: 'Layout, sidebar, topbar and user widget — the chrome this site is built from.' }
        ]
    }
];

const CAT_LINKS = {
    repo: 'https://github.com/dennisrahmen/DR.Simple_UI',
    nuget: 'https://www.nuget.org/packages/DR.Simple_UI/',
    releases: 'https://github.com/dennisrahmen/DR.Simple_UI/releases',
    icons: 'https://remixicon.com'
};

const CURRENT_PAGE = location.pathname.split('/').pop() || 'index.html';

function el(tag, className, html) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (html !== undefined) node.innerHTML = html;
    return node;
}

/* ── Shell ─────────────────────────────────────────────────────────────── */

function catSidebar() {
    const host = document.getElementById('cat-sidebar');
    if (!host) return;

    const brand = el('a', 'brand');
    brand.href = 'index.html';
    brand.innerHTML =
        '<img class="brand-logo" src="logo.png" width="30" height="30" alt="" />' +
        '<span class="brand-text"><strong>DR.Simple_UI</strong>' +
        '<span class="brand-sub">catalogue</span></span>';

    const nav = el('nav', 'nav');
    const scroll = el('div', 'nav-scroll');

    for (const section of CAT_PAGES) {
        const wrap = el('div', 'nav-section');
        wrap.appendChild(el('span', 'nav-section-label', section.group));

        for (const item of section.items) {
            const a = el('a', 'nav-link' + (item.href === CURRENT_PAGE ? ' active' : ''));
            a.href = item.href;
            a.innerHTML = `<i class="${item.icon}"></i><span>${item.label}</span>`;
            if (item.href === CURRENT_PAGE) a.setAttribute('aria-current', 'page');
            wrap.appendChild(a);
        }
        scroll.appendChild(wrap);
    }

    const tools = el('div', 'nav-tools');
    tools.innerHTML =
        `<a class="nav-link nav-link-tool" href="${CAT_LINKS.repo}">` +
            '<i class="ri-github-fill"></i><span>Repository</span>' +
            '<i class="ri-external-link-line nav-link-ext"></i></a>' +
        `<a class="nav-link nav-link-tool" href="${CAT_LINKS.nuget}">` +
            '<i class="ri-box-3-line"></i><span>NuGet</span>' +
            '<i class="ri-external-link-line nav-link-ext"></i></a>' +
        `<a class="nav-link nav-link-tool" href="${CAT_LINKS.icons}">` +
            '<i class="ri-brush-line"></i><span>Remix Icon</span>' +
            '<i class="ri-external-link-line nav-link-ext"></i></a>';

    nav.append(scroll, tools);
    host.append(brand, nav);
}

/* Toggles live in the topbar so they are reachable on every page and on a
   phone. Each is a .topbar-btn carrying a data-tip, which also exercises the
   library's hover-hint engine on every page load. */
const CAT_TOGGLES = [
    { attr: 'data-theme', on: 'light', off: 'dark', icon: 'ri-sun-line', offIcon: 'ri-moon-line',
      label: 'Light theme', tip: 'Switch between the light and dark theme.' },
    { attr: 'data-cvd', on: '1', off: null, icon: 'ri-contrast-2-line',
      label: 'Colour-blind palette', tip: 'Deuteranopia-safe palette: the go family turns blue.' },
    { attr: 'data-density', on: 'compact', off: null, icon: 'ri-list-check-2',
      label: 'Compact density', tip: 'Tighten table rows.' }
];

function catTopbar() {
    const host = document.getElementById('cat-topbar');
    if (!host) return;
    const root = document.documentElement;

    const burger = el('button', 'topbar-btn topbar-btn--start cat-burger',
        '<i class="ri-menu-line"></i>');
    burger.type = 'button';
    burger.setAttribute('aria-label', 'Open navigation');
    burger.addEventListener('click', () => catDrawer(true));

    // Desktop: collapse the sidebar to the icon rail. This is the library's own
    // .sidebar.collapsed behaviour, documented on the Shell & nav page.
    const collapse = el('button', 'topbar-btn topbar-btn--start cat-collapse',
        '<i class="ri-side-bar-line"></i>');
    collapse.type = 'button';
    collapse.setAttribute('aria-label', 'Collapse navigation');
    collapse.setAttribute('data-tip', 'Collapse the sidebar to an icon rail.');
    collapse.addEventListener('click', () => {
        const bar = document.getElementById('cat-sidebar');
        if (bar) bar.classList.toggle('collapsed');
    });

    const brand = el('a', 'cat-topbrand',
        '<img src="logo.png" width="24" height="24" alt="" /><span>DR.Simple_UI</span>');
    brand.href = 'index.html';

    const spacer = el('div', 'topbar-spacer');

    host.append(burger, collapse, brand, spacer);

    for (const def of CAT_TOGGLES) {
        const active = root.getAttribute(def.attr) === def.on;
        const btn = el('button', 'topbar-btn cat-toggle',
            `<i class="${active && def.offIcon ? def.offIcon : def.icon}"></i>`);
        btn.type = 'button';
        btn.setAttribute('aria-label', def.label);
        btn.setAttribute('aria-pressed', String(active));
        btn.setAttribute('data-tip', def.tip);

        btn.addEventListener('click', () => {
            const isOn = root.getAttribute(def.attr) === def.on;
            if (isOn) {
                if (def.off === null) root.removeAttribute(def.attr);
                else root.setAttribute(def.attr, def.off);
            } else {
                root.setAttribute(def.attr, def.on);
            }
            btn.setAttribute('aria-pressed', String(!isOn));
            if (def.offIcon) {
                btn.innerHTML = `<i class="${!isOn ? def.offIcon : def.icon}"></i>`;
            }
        });
        host.appendChild(btn);
    }

    const repo = el('a', 'topbar-btn cat-ext', '<i class="ri-github-fill"></i>');
    repo.href = CAT_LINKS.repo;
    repo.setAttribute('aria-label', 'Repository on GitHub');
    repo.setAttribute('data-tip', 'Source, issues and releases on GitHub.');
    host.appendChild(repo);
}

/* ── Mobile drawer ─────────────────────────────────────────────────────── */

function catDrawer(open) {
    document.body.classList.toggle('cat-nav-open', open);
    const scrim = document.getElementById('cat-scrim');
    if (scrim) scrim.hidden = !open;
    const burger = document.querySelector('.cat-burger');
    if (burger) burger.setAttribute('aria-expanded', String(open));
}

function catDrawerWiring() {
    const scrim = document.getElementById('cat-scrim');
    if (scrim) scrim.addEventListener('click', () => catDrawer(false));

    document.addEventListener('keydown', e => {
        if (e.key === 'Escape') catDrawer(false);
    });

    // Following a link inside the drawer navigates; close it so the state does
    // not survive into a back-navigation from the browser cache.
    const sidebar = document.getElementById('cat-sidebar');
    if (sidebar) {
        sidebar.addEventListener('click', e => {
            if (e.target.closest('a')) catDrawer(false);
        });
    }

    window.addEventListener('resize', () => {
        if (window.innerWidth > 900) catDrawer(false);
    });
}

/* ── Examples ──────────────────────────────────────────────────────────── */

/* Strip the shared leading indentation the <template> inherited from the HTML
   source, so the printed snippet is flush-left and paste-ready. */
function dedent(html) {
    const lines = html.replace(/\t/g, '    ').split('\n');
    while (lines.length && !lines[0].trim()) lines.shift();
    while (lines.length && !lines[lines.length - 1].trim()) lines.pop();
    const indents = lines.filter(l => l.trim()).map(l => l.match(/^ */)[0].length);
    const cut = indents.length ? Math.min(...indents) : 0;
    return lines.map(l => l.slice(cut)).join('\n');
}

function catExamples() {
    for (const block of document.querySelectorAll('[data-example]')) {
        const tpl = block.querySelector('template');
        if (!tpl) continue;

        // data-code-only: the template is not live HTML for this page — a CSS
        // override file, or host-page <script> tags. Cloning a <script> into the
        // document would execute it, since cloneNode drops the "already started"
        // flag.
        const codeOnly = 'codeOnly' in block.dataset;

        let demo = null;
        if (!codeOnly) {
            demo = el('div', 'ex-demo' + (block.dataset.demo ? ' ' + block.dataset.demo : ''));
            demo.appendChild(tpl.content.cloneNode(true));
        }

        const wrap = el('div', 'ex-codewrap' + (codeOnly ? ' ex-codewrap--only' : ''));
        const pre = el('pre', 'ex-code');
        const code = document.createElement('code');
        const src = dedent(tpl.innerHTML);
        code.textContent = src;
        pre.appendChild(code);

        const copy = el('button', 'ex-copy', '<i class="ri-file-copy-line"></i><span>Copy</span>');
        copy.type = 'button';
        copy.addEventListener('click', async () => {
            const ok = await (window.drSimpleUi
                ? drSimpleUi.copyText(src)
                : navigator.clipboard.writeText(src).then(() => true, () => false));
            copy.innerHTML = ok
                ? '<i class="ri-check-line"></i><span>Copied</span>'
                : '<span>Copy failed</span>';
            setTimeout(() => {
                copy.innerHTML = '<i class="ri-file-copy-line"></i><span>Copy</span>';
            }, 1400);
        });

        wrap.append(pre, copy);
        if (demo) block.append(demo, wrap);
        else block.append(wrap);
    }
}

/* Deep-linkable headings, so a section of a page can be pointed at directly. */
function catAnchors() {
    const used = new Set();

    for (const h of document.querySelectorAll('.cat-main h2')) {
        let id = h.id || (h.textContent || '')
            .toLowerCase()
            .replace(/[^\w\s-]/g, '')
            .trim()
            .replace(/\s+/g, '-');
        if (!id) continue;
        while (used.has(id)) id += '-x';
        used.add(id);
        h.id = id;

        const a = el('a', 'cat-anchor', '<i class="ri-links-line"></i>');
        a.href = '#' + id;
        a.setAttribute('aria-label', `Link to “${h.textContent.trim()}”`);
        h.appendChild(a);
    }
}

/* ── Overview tiles ────────────────────────────────────────────────────── */

function catTiles() {
    const host = document.getElementById('cat-tiles');
    if (!host) return;

    for (const section of CAT_PAGES) {
        const pages = section.items.filter(i => i.href !== 'index.html');
        if (!pages.length) continue;

        host.appendChild(el('h2', null, section.group));

        const grid = el('div', 'cat-tiles');
        for (const item of pages) {
            const tile = el('a', 'card cat-tile');
            tile.href = item.href;
            tile.innerHTML =
                `<span class="cat-tile-ic"><i class="${item.icon}"></i></span>` +
                '<span class="cat-tile-text">' +
                    `<strong>${item.label}</strong>` +
                    `<span>${item.blurb}</span>` +
                '</span>' +
                '<i class="ri-arrow-right-line cat-tile-go"></i>';
            grid.appendChild(tile);
        }
        host.appendChild(grid);
    }
}

/* ── Hosted-site notice ────────────────────────────────────────────────── */

/* Served from a docs site rather than from inside a package, these pages show
   `main` and can be ahead of any released version. Served from within an app the
   path contains /_content/, where the notice is wrong and stays hidden. */
function catHostedNotice() {
    const isHosted = location.protocol.startsWith('http')
        && !location.pathname.includes('/_content/');
    if (!isHosted) return;

    // A strip between the header and the scrolling page, not a block inside the
    // content column — it is a statement about the site, not about the page.
    const page = document.querySelector('.page');
    const content = page && page.parentElement;
    if (!content) return;

    const bar = el('div', 'cat-hostedbar',
        '<i class="ri-git-branch-line"></i>' +
        '<span>Showing <strong>main</strong>. ' +
        '<span class="cat-hostedbar-long">The catalogue inside your installed package is the one that ' +
        'matches your version.</span> ' +
        `<a href="${CAT_LINKS.releases}">Releases</a></span>`);

    content.insertBefore(bar, page);
}

/* ── External links ────────────────────────────────────────────────────── */

/* Anything leaving this site opens in a new tab. Done centrally, after every
   other builder has run, so generated links are covered and a new page cannot
   forget it.

   Links inside a rendered demo are skipped: the demo has to match the snippet
   printed beside it, and a target the reader would not be copying does not
   belong there.

   Deliberately NOT in the library's JS. An app decides its own link targets;
   rewriting them from a UI package would be a surprise. */
function catExternalLinks() {
    for (const a of document.querySelectorAll('a[href]')) {
        if (a.closest('.ex-demo')) continue;

        let url;
        try { url = new URL(a.getAttribute('href'), location.href); }
        catch { continue; }

        if (url.protocol !== 'http:' && url.protocol !== 'https:') continue;
        // On a file:// page location.origin is "null", so every http(s) link is
        // correctly treated as leaving the site.
        if (url.origin === location.origin) continue;

        a.target = '_blank';
        if (!/\bnoopener\b/.test(a.rel)) a.rel = (a.rel ? a.rel + ' ' : '') + 'noopener';

        // Tell screen readers the tab changes. Skipped where the link is
        // icon-only and already carries an aria-label, which would otherwise be
        // announced twice.
        if (!a.hasAttribute('aria-label') && !a.querySelector('.cat-sr')) {
            a.appendChild(el('span', 'cat-sr', ' (opens in a new tab)'));
        }
    }
}

/* ── Footer ────────────────────────────────────────────────────────────── */

/* Rendered on every page from one definition, which is also where the Remix
   Icon attribution lives. */
function catFooter() {
    const main = document.querySelector('.cat-main');
    if (!main) return;

    main.appendChild(el('footer', 'cat-footer',
        `<p><strong>DR.Simple_UI</strong> — <a href="${CAT_LINKS.repo}">source</a> · ` +
        `<a href="${CAT_LINKS.nuget}">NuGet</a> · ` +
        `<a href="${CAT_LINKS.releases}">releases</a></p>` +
        '<p>Licensed under Apache-2.0. Icons from ' +
        `<a href="${CAT_LINKS.icons}">Remix Icon</a>, licensed under the Remix Icon License v1.0.</p>`));
}

catSidebar();
catTopbar();
catDrawerWiring();
catExamples();
catAnchors();
catTiles();
catHostedNotice();
catFooter();
catExternalLinks();   // last: every link in the document must exist by now
