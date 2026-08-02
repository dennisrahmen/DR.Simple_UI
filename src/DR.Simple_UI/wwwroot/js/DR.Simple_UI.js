/* ═══════════════════════════════════════════════════════════════════════════
   GENERATED FILE — DO NOT EDIT.

   Built by build/bundle-js.sh from src/DR.Simple_UI/js-parts/. Edit the part
   that owns the behaviour and re-run that script; a guard test fails the build
   if this file and the parts disagree. Adding a part needs no change here —
   the directory is the source of truth.

   Contents, in load order:
     00-core.js
     10-settings.js
     20-tips.js
     21-copy.js
     22-menu.js
     23-tabs.js
     24-palette.js
     30-markdown.js
     40-interop.js
     50-notify.js
     51-toast.js
     52-confirm.js
   ═══════════════════════════════════════════════════════════════════════════ */

/* ── 00-core.js ──────────────────────────────────────────────── */
/* DR.Simple_UI — shared browser behaviour.
   ───────────────────────────────────────────────────────────────────────────
   Load at the END of <body>:

     <script src="_content/DR.Simple_UI/js/DR.Simple_UI.js"></script>

   configure() is optional:

     <script>drSimpleUi.configure({ notifyIcon: '/images/logo.png' });</script>

   Everything here is generic UI behaviour. App-specific interop stays in the
   app's own script — do not grow this file with business logic.

   The global is `drSimpleUi` (the JS-identifier form of the package name; a
   single global cannot contain the dot).
   ─────────────────────────────────────────────────────────────────────────── */
window.drSimpleUi = window.drSimpleUi || {};

(function (ui) {

    var config = {
        // localStorage key prefix. localStorage is origin-scoped, so apps on
        // separate domains cannot collide and this needs no changing. Override it
        // only when two apps share one origin under different paths — and then set
        // the same value in data-prefix on the boot script.
        storagePrefix: 'drui.',
        // Icon used for desktop notifications. null = browser default.
        notifyIcon: null,
        // Also mirror the language into a "<prefix>lang" cookie, so a server-
        // rendered app can prerender in the chosen language.
        langCookie: false
    };

    function key(k) { return config.storagePrefix + k; }

    function readRaw(k) {
        try { return localStorage.getItem(k); } catch (e) { return null; }
    }

    // Shared internals for the other parts. The underscore means exactly one
    // thing: NOT part of the public contract. Nothing outside js-parts/ may read
    // it, and it may change in a patch release. Everything an app is allowed to
    // touch is a named member on `ui` itself.
    ui._ = { config: config, key: key, readRaw: readRaw };

    ui.configure = function (opts) {
        if (!opts) return;
        Object.keys(opts).forEach(function (k) {
            if (k in config) config[k] = opts[k];
        });
        // Guarded so core-plus-nothing still works for anyone using the parts
        // à la carte; in the shipped bundle settings is always present.
        if (ui.settings) ui.settings.apply();
    };

})(window.drSimpleUi);

/* ── 10-settings.js ──────────────────────────────────────────────── */
/* ── Theme / accessibility settings ──────────────────────────────────────────
   localStorage is the source of truth; the data-theme / data-cvd / data-density
   attributes on <html> drive the CSS token layer. The boot script applies them
   before first paint; save() keeps them applied.

   data-theme is ALWAYS written, `light` or `dark`, never absent — consuming apps
   brand the light palette with `:root[data-theme="light"]`, so that selector has
   to match whenever the light palette is in use. Any future support for the OS
   preference must resolve prefers-color-scheme into this attribute here, never
   express it as a @media block, or every app's light-theme branding stops
   applying with no app edit.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    var core = ui._;
    var config = core.config, key = core.key, readRaw = core.readRaw;

    ui.settings = {
        load: function () {
            var g = function (k) { return readRaw(key(k)); };
            return {
                lang:    g('lang') || (navigator.language || 'en').slice(0, 2).toLowerCase(),
                theme:   g('theme') === 'light' ? 'light' : 'dark',
                cvd:     g('cvd') === '1',
                compact: g('density') === 'compact'
            };
        },
        save: function (k, value) {
            try { localStorage.setItem(key(k), value); } catch (e) { /* ignore */ }
            if (k === 'lang') {
                if (config.langCookie) {
                    try {
                        document.cookie = key('lang') + '=' + value + ';path=/;max-age=31536000;SameSite=Lax';
                    } catch (e) { /* ignore */ }
                }
                document.documentElement.lang = value;
            }
            this.apply();
        },
        apply: function () {
            var g = function (k) { return readRaw(key(k)); };
            var root = document.documentElement;
            root.setAttribute('data-theme', g('theme') === 'light' ? 'light' : 'dark');
            if (g('cvd') === '1') root.setAttribute('data-cvd', '1');
            else root.removeAttribute('data-cvd');
            if (g('density') === 'compact') root.setAttribute('data-density', 'compact');
            else root.removeAttribute('data-density');
        }
    };

})(window.drSimpleUi);

/* ── 20-tips.js ──────────────────────────────────────────────── */
/* ── Hover hints (data-tip) ──────────────────────────────────────────────────
   One floating bubble, driven by [data-tip] through delegation on document — so
   it covers content rendered after load (Blazor re-renders) with no re-wiring.
   The bubble is appended to <body> and fixed-positioned, so a card's or table's
   overflow never clips it (a pure-CSS ::after tooltip is clipped).

   Elements inside .sidebar are skipped: the collapsed rail has its own CSS
   flyout, and both firing would double the tooltip.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    ui.tips = (function () {
        var tipEl = null, showTimer = null, current = null;
        var SHOW_DELAY = 130;   // ms — long enough not to flash on a passing cursor

        function ensureEl() {
            if (!tipEl) {
                tipEl = document.createElement('div');
                tipEl.className = 'dr-tip';
                tipEl.setAttribute('role', 'tooltip');
                document.body.appendChild(tipEl);
            }
            return tipEl;
        }

        function trigger(t) {
            if (!t || !t.closest) return null;
            var el = t.closest('[data-tip]');
            if (!el || el.closest('.sidebar')) return null;
            // Optional app gate — e.g. a guided tour suppressing hints outside
            // the live step. Set drSimpleUi.tips.gate = function (el) { … }.
            if (typeof api.gate === 'function' && !api.gate(el)) return null;
            return el;
        }

        function place(el) {
            var tip = el.getAttribute('data-tip');
            if (!tip) return hide();
            // Redundant when the trigger's own visible text already spells out
            // the whole hint (innerText respects CSS visibility, so a hidden
            // label correctly counts as absent).
            var vis = (el.innerText || '').trim();
            if (vis && vis.indexOf(tip) !== -1) return hide();

            var box = ensureEl();
            box.textContent = tip;
            // Measure at the origin with a settled width, then position.
            box.style.left = '0px';
            box.style.top = '0px';
            box.classList.add('dr-tip--visible');

            var r = el.getBoundingClientRect();
            var b = box.getBoundingClientRect();
            var pos = el.getAttribute('data-tip-pos') || 'top';
            var gap = 8, m = 6, vw = window.innerWidth, vh = window.innerHeight;

            // Vertical auto-flip when the preferred side has no room.
            if (pos === 'top' && r.top < b.height + gap + m) pos = 'bottom';
            else if (pos === 'bottom' && r.bottom + b.height + gap + m > vh) pos = 'top';

            var x, y;
            if (pos === 'left')        { x = r.left - b.width - gap; y = r.top + r.height / 2 - b.height / 2; }
            else if (pos === 'right')  { x = r.right + gap;          y = r.top + r.height / 2 - b.height / 2; }
            else if (pos === 'bottom') { x = r.left + r.width / 2 - b.width / 2; y = r.bottom + gap; }
            else                       { x = r.left + r.width / 2 - b.width / 2; y = r.top - b.height - gap; }

            // Keep the whole bubble inside the viewport.
            x = Math.max(m, Math.min(x, vw - b.width - m));
            y = Math.max(m, Math.min(y, vh - b.height - m));
            box.style.left = Math.round(x) + 'px';
            box.style.top = Math.round(y) + 'px';
        }

        function show(el) {
            if (current === el) return;   // already showing / queued for this one
            current = el;
            clearTimeout(showTimer);
            showTimer = setTimeout(function () { if (current === el) place(el); }, SHOW_DELAY);
        }

        function hide() {
            current = null;
            clearTimeout(showTimer);
            if (tipEl) tipEl.classList.remove('dr-tip--visible');
        }

        document.addEventListener('mouseover', function (e) {
            var el = trigger(e.target);
            if (el) show(el);
        });
        document.addEventListener('mouseout', function (e) {
            var el = trigger(e.target);
            if (!el || el !== current) return;
            // Ignore moves that stay inside the same trigger (e.g. onto its icon).
            if (e.relatedTarget && el.contains(e.relatedTarget)) return;
            hide();
        });
        document.addEventListener('focusin', function (e) {
            var el = trigger(e.target);
            if (el) { current = el; place(el); }   // no delay for keyboard focus
        });
        document.addEventListener('focusout', hide);
        document.addEventListener('mousedown', hide);    // a click dismisses its own hint
        window.addEventListener('scroll', hide, true);   // capture: any scroll container
        window.addEventListener('resize', hide);

        var api = { gate: null, hide: hide };
        return api;
    })();

})(window.drSimpleUi);

/* ── 21-copy.js ──────────────────────────────────────────────── */
/* ── Declarative copy-to-clipboard ───────────────────────────────────────────
   Put data-copy on a button and the click is handled for you:

     data-copy="literal text"      copies that text
     data-copy-target="#sel"       copies that element's textContent
     data-copy-target              (empty) copies the nearest .code-block's <pre>

   Delegated from document, so a button rendered by a later Blazor render works with
   no wiring — and nothing has to be re-bound on every render, which is how per-
   element handlers leak.

   The confirmation is swapped into the button and put back after 1.4s. The original
   HTML is stashed on the element rather than in a closure, so two rapid clicks
   cannot restore a "Copied" label as if it were the original.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    var RESTORE_MS = 1400;

    function textFor(btn) {
        if (btn.hasAttribute('data-copy')) return btn.getAttribute('data-copy') || '';

        var sel = btn.getAttribute('data-copy-target');
        var node = sel
            ? document.querySelector(sel)
            // The <pre> of the code block this button belongs to.
            : (btn.closest('.code-block') || document).querySelector('pre');

        return node ? (node.innerText || node.textContent || '') : '';
    }

    function flash(btn, ok) {
        // Only stash on the first click; a second click mid-flash must not stash
        // the confirmation as the thing to restore.
        if (btn.dataset.copyOriginal === undefined) {
            btn.dataset.copyOriginal = btn.innerHTML;
        }
        clearTimeout(+btn.dataset.copyTimer || 0);

        btn.innerHTML = ok
            ? '<i class="ri-check-line"></i><span>Copied</span>'
            : '<i class="ri-error-warning-line"></i><span>Copy failed</span>';

        btn.dataset.copyTimer = setTimeout(function () {
            btn.innerHTML = btn.dataset.copyOriginal;
            delete btn.dataset.copyOriginal;
            delete btn.dataset.copyTimer;
        }, RESTORE_MS);
    }

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-copy], [data-copy-target]');
        if (!btn) return;

        var text = textFor(btn);
        if (!text) return;

        e.preventDefault();
        // copyText resolves false rather than throwing, so the button always
        // reports what actually happened.
        ui.copyText(text).then(function (ok) { flash(btn, ok); });
    });

})(window.drSimpleUi);

/* ── 22-menu.js ──────────────────────────────────────────────── */
/* ── Menus, delegated ────────────────────────────────────────────────────────
   Opt-in wiring for the .menu panel, so a plain HTML page (or a Razor page that
   would rather not hold the state) gets a working dropdown:

     <div class="menu-anchor">
       <button data-menu-toggle aria-expanded="false">Actions</button>
       <div class="menu" hidden>…</div>
     </div>

   The `hidden` attribute is the closed state, not a class: a panel that is
   `hidden` is out of the accessibility tree and out of the tab order, which a
   `display:none` class also achieves but an `opacity:0` one does not.

   Delegated from document, so a menu rendered by a later Blazor render works with
   nothing re-bound. <UserWidget> does NOT use this — it holds its own state in C#,
   because the frame has to work with scripting blocked.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    function panelOf(toggle) {
        var anchor = toggle.closest('.menu-anchor');
        return anchor ? anchor.querySelector('.menu') : null;
    }

    function setOpen(toggle, panel, open) {
        panel.hidden = !open;
        toggle.setAttribute('aria-expanded', String(open));
    }

    function closeAll(except) {
        var toggles = document.querySelectorAll('[data-menu-toggle][aria-expanded="true"]');
        for (var i = 0; i < toggles.length; i++) {
            if (toggles[i] === except) continue;
            var panel = panelOf(toggles[i]);
            if (panel) setOpen(toggles[i], panel, false);
        }
    }

    ui.menu = {
        // Closes every open menu. An app calls this after navigating, so a menu does
        // not survive into a back-navigation from the browser cache.
        closeAll: function () { closeAll(null); }
    };

    document.addEventListener('click', function (e) {
        var toggle = e.target.closest('[data-menu-toggle]');

        if (toggle) {
            var panel = panelOf(toggle);
            if (!panel) return;
            e.preventDefault();
            var open = toggle.getAttribute('aria-expanded') === 'true';
            // Only one menu open at a time: two panels overlapping is never wanted,
            // and the second one silently covers the first.
            closeAll(toggle);
            setOpen(toggle, panel, !open);
            return;
        }

        // A click inside a panel that is not on an item leaves it open; a click on
        // an item closes it, because the item did something.
        var item = e.target.closest('.menu-item');
        if (item) { closeAll(null); return; }
        if (e.target.closest('.menu')) return;

        closeAll(null);
    });

    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        var open = document.querySelector('[data-menu-toggle][aria-expanded="true"]');
        if (!open) return;
        closeAll(null);
        // Focus goes back to the control that opened it. Without this, focus is left
        // on a node that has just been hidden and the next Tab starts from the top
        // of the document.
        try { open.focus(); } catch (err) { /* detached */ }
    });

})(window.drSimpleUi);

/* ── 23-tabs.js ──────────────────────────────────────────────── */
/* ── Tabs, delegated ─────────────────────────────────────────────────────────
   Opt-in wiring for .tabs, and the reason it exists is the keyboard: the CSS can
   colour a selected tab, but arrow-key movement between tabs and the single-stop
   tab order are behaviour, and a tablist without them is a tablist in name only.

   Add data-tabs to the .tabs container. Each tab needs role="tab",
   aria-controls="<panel id>" and aria-selected; each panel needs role="tabpanel"
   and a matching id.

     <div class="tabs" role="tablist" data-tabs>
       <button class="tab" role="tab" aria-selected="true"  aria-controls="p1">Open</button>
       <button class="tab" role="tab" aria-selected="false" aria-controls="p2">All</button>
     </div>
     <div class="tab-panel" role="tabpanel" id="p1">…</div>
     <div class="tab-panel" role="tabpanel" id="p2" hidden>…</div>

   An app that drives the tabs from C# should NOT add data-tabs — it would then have
   two things setting aria-selected. Wire the arrow keys in the component instead.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    function tabsIn(list) {
        return Array.prototype.filter.call(
            list.querySelectorAll('[role="tab"]'),
            function (t) { return !t.disabled && t.getAttribute('aria-disabled') !== 'true'; });
    }

    function select(list, tab) {
        var all = list.querySelectorAll('[role="tab"]');
        for (var i = 0; i < all.length; i++) {
            var isIt = all[i] === tab;
            all[i].setAttribute('aria-selected', String(isIt));
            // Roving tabindex: only the selected tab is a tab stop, so Tab moves past
            // the whole tablist rather than through every tab in it.
            all[i].tabIndex = isIt ? 0 : -1;

            var panel = document.getElementById(all[i].getAttribute('aria-controls') || '');
            if (panel) panel.hidden = !isIt;
        }
    }

    ui.tabs = {
        // Selects a tab programmatically, by element or by its aria-controls id.
        select: function (tabOrPanelId) {
            var tab = typeof tabOrPanelId === 'string'
                ? document.querySelector('[role="tab"][aria-controls="' + tabOrPanelId + '"]')
                : tabOrPanelId;
            var list = tab && tab.closest('[data-tabs]');
            if (list) select(list, tab);
        }
    };

    document.addEventListener('click', function (e) {
        var tab = e.target.closest('[data-tabs] [role="tab"]');
        if (!tab || tab.disabled) return;
        e.preventDefault();
        select(tab.closest('[data-tabs]'), tab);
    });

    document.addEventListener('keydown', function (e) {
        var tab = e.target.closest('[data-tabs] [role="tab"]');
        if (!tab) return;

        var list = tab.closest('[data-tabs]');
        var tabs = tabsIn(list);
        var at = tabs.indexOf(tab);
        if (at < 0) return;

        // Home/End as well as the arrows: with a dozen tabs, holding an arrow key to
        // reach the last one is the kind of thing that makes people use a mouse.
        var to = -1;
        if (e.key === 'ArrowRight' || e.key === 'ArrowDown') to = (at + 1) % tabs.length;
        else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') to = (at - 1 + tabs.length) % tabs.length;
        else if (e.key === 'Home') to = 0;
        else if (e.key === 'End') to = tabs.length - 1;
        else return;

        e.preventDefault();
        select(list, tabs[to]);
        tabs[to].focus();
    });

})(window.drSimpleUi);

/* ── 24-palette.js ──────────────────────────────────────────────── */
/* ── Command palette ─────────────────────────────────────────────────────────
   drSimpleUi.palette.register([{ label, icon, group, note, run, keywords }])
   drSimpleUi.palette.open()      — or Ctrl/Cmd-K, which is wired for you

   The scorer is a hand-rolled subsequence matcher, about thirty lines. No fuse.js:
   this package loads nothing at runtime, and a fuzzy matcher good enough for a
   command list is smaller than the argument for taking a dependency.

   Ranking, in the order it matters:
     1. a prefix match on the label            — you typed the start of the name
     2. a word-start match                     — "ai" finds "Approve Item"
     3. a contiguous run inside the label
     4. any subsequence, penalised by how spread out it is
   Matches in `keywords` score below the same match in the label, so a command is
   never outranked by one that merely mentions the word.

   Everything is built from real elements the catalogue documents, so a palette
   opened by this looks like the one on the Overlays page.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    var commands = [];
    var dialog = null;
    var input = null;
    var list = null;
    var shown = [];      // the currently visible commands, in ranked order
    var at = 0;          // index into shown

    /* Returns a score, or -1 for no match. Higher is better. */
    function score(needle, haystack, penalty) {
        if (!needle) return 1;

        var n = needle.toLowerCase();
        var h = haystack.toLowerCase();

        var idx = h.indexOf(n);
        if (idx === 0) return 1000 - penalty;                       // prefix
        if (idx > 0) {
            // A run that starts a word beats one buried inside it.
            var wordStart = idx === 0 || /[\s\-_/]/.test(h[idx - 1]);
            return (wordStart ? 800 : 600) - idx - penalty;
        }

        // Subsequence. Track the span it occupies: a match spread across the whole
        // string is a worse match than a tight one, which is what stops "ae"
        // ranking "Approve … escalate" above "Archive entry".
        var first = -1, last = -1, hi = 0;
        for (var ni = 0; ni < n.length; ni++) {
            var found = h.indexOf(n[ni], hi);
            if (found < 0) return -1;
            if (first < 0) first = found;
            last = found;
            hi = found + 1;
        }
        var span = last - first + 1;
        return 400 - (span - n.length) - first - penalty;
    }

    function rank(query) {
        var out = [];
        for (var i = 0; i < commands.length; i++) {
            var c = commands[i];
            var best = score(query, c.label, 0);
            if (c.keywords) {
                // 200 keeps a keyword hit strictly below the same hit in a label.
                var k = score(query, c.keywords, 200);
                if (k > best) best = k;
            }
            if (best >= 0) out.push({ c: c, s: best, i: i });
        }
        // Stable: equal scores keep registration order, so the list does not
        // reshuffle between keystrokes that do not change the ranking.
        out.sort(function (a, b) { return b.s - a.s || a.i - b.i; });
        return out.map(function (r) { return r.c; });
    }

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function render(query) {
        shown = rank(query);
        at = 0;
        list.textContent = '';

        if (!shown.length) {
            var empty = el('li');
            empty.setAttribute('role', 'presentation');
            // Says what was searched, not just "no results" — the reader needs to
            // know the query was what they thought it was.
            empty.appendChild(el('div', 'palette-empty',
                query ? 'Nothing matches “' + query + '”.' : 'No commands registered.'));
            list.appendChild(empty);
            input.removeAttribute('aria-activedescendant');
            return;
        }

        var lastGroup = null;
        shown.forEach(function (c, i) {
            // Groups are only meaningful in registration order, so they are dropped
            // once a query has reordered the list — a heading over unrelated results
            // is worse than no heading.
            if (!query && c.group && c.group !== lastGroup) {
                // role="presentation" is load-bearing: a listbox may only own
                // options, and a bare <li> here breaks aria-required-children — while
                // also ceasing to be a listitem, because the <ul> is no longer a list.
                // Presentation makes the <li> transparent to both rules.
                var head = el('li');
                head.setAttribute('role', 'presentation');
                head.appendChild(el('div', 'palette-group', c.group));
                list.appendChild(head);
                lastGroup = c.group;
            }

            // role="option" on a <div>, not a <button>: an option is not a button,
            // and being one inside a listbox is what makes aria-selected and
            // aria-activedescendant legal. Activated by click and by the input's
            // Enter handler, never by receiving focus.
            var li = el('li');
            li.setAttribute('role', 'presentation');
            var btn = el('div', 'palette-item');
            btn.setAttribute('role', 'option');
            btn.id = 'dr-palette-' + i;
            btn.setAttribute('aria-selected', String(i === 0));

            if (c.icon) {
                var icon = el('i', c.icon);
                icon.setAttribute('aria-hidden', 'true');
                btn.appendChild(icon);
            }
            btn.appendChild(document.createTextNode(c.label));
            if (c.note) btn.appendChild(el('span', 'palette-item-note', c.note));

            btn.addEventListener('click', function () { run(i); });
            li.appendChild(btn);
            list.appendChild(li);
        });

        input.setAttribute('aria-activedescendant', 'dr-palette-0');
    }

    function items() { return list.querySelectorAll('.palette-item'); }

    function highlight(next) {
        var all = items();
        if (!all.length) return;

        at = (next + all.length) % all.length;
        for (var i = 0; i < all.length; i++) {
            all[i].setAttribute('aria-selected', String(i === at));
        }
        input.setAttribute('aria-activedescendant', all[at].id);
        // Keeps the highlight in view without moving focus, which stays in the input
        // so typing continues to work — that is the whole reason for
        // aria-activedescendant rather than moving focus down the list.
        all[at].scrollIntoView({ block: 'nearest' });
    }

    function run(i) {
        var c = shown[i];
        close();
        // After close(), so a command that opens a modal is not fighting a dialog
        // that is still shutting.
        if (c && typeof c.run === 'function') c.run();
    }

    function build() {
        dialog = document.createElement('dialog');
        dialog.className = 'palette';

        input = el('input', 'palette-input');
        input.type = 'text';
        input.placeholder = 'Search commands…';
        input.setAttribute('role', 'combobox');
        input.setAttribute('aria-expanded', 'true');
        input.setAttribute('aria-controls', 'dr-palette-list');
        input.setAttribute('aria-label', 'Search commands');
        input.setAttribute('autocomplete', 'off');

        list = el('ul', 'palette-list');
        list.id = 'dr-palette-list';
        // A real listbox owned by the combobox input. Claimed because the keyboard
        // contract behind it is implemented in full below: arrows, Home/End, Enter,
        // and the highlight moving while focus stays in the input. axe rejects
        // aria-selected on a plain button, correctly — the attribute means nothing
        // without the role.
        list.setAttribute('role', 'listbox');
        list.setAttribute('aria-label', 'Commands');

        var foot = el('div', 'palette-footer');
        foot.innerHTML =
            '<span><span class="kbd">&uarr;</span> <span class="kbd">&darr;</span> to move</span>' +
            '<span><span class="kbd">Enter</span> to run</span>' +
            '<span><span class="kbd">Esc</span> to close</span>';

        dialog.append(input, list, foot);
        document.body.appendChild(dialog);

        input.addEventListener('input', function () { render(input.value); });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown') { e.preventDefault(); highlight(at + 1); }
            else if (e.key === 'ArrowUp') { e.preventDefault(); highlight(at - 1); }
            else if (e.key === 'Home') { e.preventDefault(); highlight(0); }
            else if (e.key === 'End') { e.preventDefault(); highlight(items().length - 1); }
            else if (e.key === 'Enter') { e.preventDefault(); if (items().length) run(at); }
        });

        // Clicking the backdrop closes it. The dialog fills only part of the top
        // layer, so a click whose target IS the dialog element landed outside the
        // panel's own children.
        dialog.addEventListener('click', function (e) { if (e.target === dialog) close(); });
    }

    function close() {
        if (dialog && dialog.open) dialog.close();
    }

    ui.palette = {
        /* Replaces the whole command list. An app calls this whenever what is
           available changes — after a permission check, or on navigation. */
        register: function (list_) {
            commands = Array.isArray(list_) ? list_.slice() : [];
        },

        open: function () {
            if (!dialog) build();
            if (dialog.open) return true;

            input.value = '';
            render('');
            dialog.showModal();
            input.focus();
            return true;
        },

        close: close,

        /* Exposed for tests and for an app that wants the same ranking in its own
           UI. Returns the matching commands, best first. */
        rank: rank
    };

    document.addEventListener('keydown', function (e) {
        // metaKey for macOS, ctrlKey everywhere else. Checking both rather than
        // sniffing the platform: a Mac user on an external PC keyboard uses Ctrl.
        if (e.key !== 'k' && e.key !== 'K') return;
        if (!e.ctrlKey && !e.metaKey) return;
        if (!commands.length) return;      // nothing registered: leave the browser's own binding alone

        e.preventDefault();
        ui.palette.open();
    });

})(window.drSimpleUi);

/* ── 30-markdown.js ──────────────────────────────────────────────── */
/* ── Markdown editor ─────────────────────────────────────────────────────────
   Toolbar + textarea + live preview inside one .md-editor root. Blazor owns the
   value through the textarea's two-way @bind (@bind:event="oninput"); toolbar
   edits mutate the textarea and dispatch a bubbling 'input' event so the binding
   picks them up — this code never calls back into .NET.

   init() is idempotent per root, since Blazor re-renders its host.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    ui.md = {
        /* Counter for the per-editor radio group name. Private. */
        _seq: 0,

        init: function (root) {
            if (!root || root.dataset.mdReady === '1') return;
            root.dataset.mdReady = '1';
            var self = this;
            var ta = root.querySelector('[data-md-input]');
            var preview = root.querySelector('[data-md-preview]');
            if (!ta) return;

            var renderPreview = function () {
                if (preview) preview.innerHTML = self.render(ta.value);
            };

            // The Write/Preview switch is a .segmented control, so it is a radio
            // group: the checked state comes from the platform and CSS draws it with
            // :has(input:checked). Nothing here toggles a class.
            //
            // The radios need a shared `name` to be one group, and it has to be unique
            // per editor or two editors on a page fight over one selection. Assigned
            // here rather than in the markup, because only this code knows how many
            // roots exist.
            var views = root.querySelectorAll('input[data-md-tab]');
            if (views.length) {
                var group = 'dr-md-view-' + (++ui.md._seq);
                views.forEach(function (r) { r.name = group; });
            }

            root.addEventListener('click', function (e) {
                var cmdBtn = e.target.closest('[data-md-cmd]');
                if (cmdBtn && root.contains(cmdBtn)) {
                    e.preventDefault();
                    self.apply(ta, cmdBtn.getAttribute('data-md-cmd'));
                    renderPreview();
                }
            });

            root.addEventListener('change', function (e) {
                var radio = e.target.closest('input[data-md-tab]');
                if (!radio || !root.contains(radio) || !radio.checked) return;

                var view = radio.getAttribute('data-md-tab');
                // Carry the height between panes (both are resize:vertical), reading
                // the visible one before the flip. The preview then fills the same box
                // and scrolls internally instead of ballooning its host on long text,
                // and a manual resize in either pane sticks across the switch.
                if (view === 'preview') {
                    renderPreview();
                    if (preview) preview.style.height = ta.offsetHeight + 'px';
                } else if (preview && preview.offsetHeight) {
                    ta.style.height = preview.offsetHeight + 'px';
                }
                root.setAttribute('data-md-view', view);
            });

            ta.addEventListener('input', renderPreview);
            renderPreview();
        },

        // Apply a toolbar command to the current selection, then fire the input
        // event so the binding captures the new value.
        apply: function (ta, cmd) {
            var v = ta.value, s = ta.selectionStart, e = ta.selectionEnd;
            var sel = v.slice(s, e);
            var wrap = function (before, after, ph) {
                var body = sel || ph;
                ta.value = v.slice(0, s) + before + body + after + v.slice(e);
                ta.selectionStart = s + before.length;
                ta.selectionEnd = s + before.length + body.length;
            };
            var linePrefix = function (prefix) {
                // Expand the selection to whole lines, then prefix each.
                var ls = v.lastIndexOf('\n', s - 1) + 1;
                var le = v.indexOf('\n', e); if (le === -1) le = v.length;
                var block = v.slice(ls, le) || prefix.trim();
                var prefixed = block.split('\n').map(function (line, i) {
                    return (cmd === 'ol' ? (i + 1) + '. ' : prefix) + line;
                }).join('\n');
                ta.value = v.slice(0, ls) + prefixed + v.slice(le);
                ta.selectionStart = ls;
                ta.selectionEnd = ls + prefixed.length;
            };
            switch (cmd) {
                case 'bold':   wrap('**', '**', 'bold text'); break;
                case 'italic': wrap('_', '_', 'italic text'); break;
                case 'code':   wrap('`', '`', 'code'); break;
                case 'h2':     linePrefix('## '); break;
                case 'ul':     linePrefix('- '); break;
                case 'ol':     linePrefix('1. '); break;
                case 'quote':  linePrefix('> '); break;
                case 'link':   wrap('[', '](https://)', 'link text'); break;
                default: return;
            }
            ta.dispatchEvent(new Event('input', { bubbles: true }));
            ta.focus();
        },

        // Minimal, self-contained Markdown → HTML. HTML is escaped FIRST; only a
        // fixed set of block/inline constructs is re-introduced, and link hrefs
        // are scheme-checked. Not a spec-complete parser — enough for authored
        // prose, and safe enough that its output can be injected.
        render: function (src) {
            if (!src) return '';
            var esc = function (s) {
                return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            };
            // Pull fenced code blocks out first so their contents are never formatted.
            var blocks = [];
            src = src.replace(/```([\s\S]*?)```/g, function (_, code) {
                blocks.push('<pre><code>' + esc(code.replace(/^\n/, '').replace(/\n$/, '')) + '</code></pre>');
                return '  B' + (blocks.length - 1) + ' ';
            });
            var inline = function (t) {
                t = esc(t);
                t = t.replace(/`([^`]+)`/g, '<code>$1</code>');
                t = t.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
                t = t.replace(/_([^_]+)_/g, '<em>$1</em>');
                t = t.replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, function (_, txt, url) {
                    var safe = /^(https?:|mailto:|\/)/i.test(url) ? url : '#';
                    return '<a href="' + esc(safe) + '" target="_blank" rel="noopener">' + txt + '</a>';
                });
                return t;
            };
            var out = [], list = null;
            var closeList = function () { if (list) { out.push('</' + list + '>'); list = null; } };
            src.split(/\r?\n/).forEach(function (line) {
                var ph = line.match(/^  B(\d+) $/);
                if (ph) { closeList(); out.push(blocks[+ph[1]]); return; }
                if (!line.trim()) { closeList(); return; }
                var m;
                if ((m = line.match(/^(#{1,6})\s+(.*)$/))) {
                    closeList();
                    var n = m[1].length;
                    out.push('<h' + n + '>' + inline(m[2]) + '</h' + n + '>');
                    return;
                }
                if (/^(---|\*\*\*|___)\s*$/.test(line)) { closeList(); out.push('<hr>'); return; }
                if ((m = line.match(/^>\s?(.*)$/))) {
                    closeList(); out.push('<blockquote>' + inline(m[1]) + '</blockquote>'); return;
                }
                if ((m = line.match(/^[-*]\s+(.*)$/))) {
                    if (list !== 'ul') { closeList(); out.push('<ul>'); list = 'ul'; }
                    out.push('<li>' + inline(m[1]) + '</li>'); return;
                }
                if ((m = line.match(/^\d+\.\s+(.*)$/))) {
                    if (list !== 'ol') { closeList(); out.push('<ol>'); list = 'ol'; }
                    out.push('<li>' + inline(m[1]) + '</li>'); return;
                }
                closeList(); out.push('<p>' + inline(line) + '</p>');
            });
            closeList();
            return out.join('');
        }
    };

})(window.drSimpleUi);

/* ── 40-interop.js ──────────────────────────────────────────────── */
/* ── Small interop helpers ───────────────────────────────────────────────────
   Generic browser calls a Blazor component cannot make on its own. Note that
   getItem / setItem take the RAW key and do not apply the storage prefix — they
   are a plain localStorage bridge for an app's own keys, not a view onto the
   library's settings, which live under the prefix and are reached through
   drSimpleUi.settings.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    var readRaw = ui._.readRaw;

    ui.openTab = function (url) {
        try { window.open(url, '_blank', 'noopener'); } catch (e) { /* ignore */ }
    };

    // Returns whether the copy succeeded, so the caller can toast either way.
    // Falls back to a hidden textarea where the async Clipboard API is
    // unavailable (older browsers, insecure origins).
    ui.copyText = async function (text) {
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return true;
            }
        } catch (e) { /* fall through to the legacy path */ }
        try {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.focus(); ta.select();
            var ok = document.execCommand('copy');
            document.body.removeChild(ta);
            return ok;
        } catch (e) { return false; }
    };

    ui.viewportWidth = function () {
        return window.innerWidth || document.documentElement.clientWidth || 0;
    };

    ui.getItem = function (k) { return readRaw(k); };

    ui.setItem = function (k, value) {
        try { localStorage.setItem(k, value); } catch (e) { /* ignore */ }
    };

})(window.drSimpleUi);

/* ── 50-notify.js ──────────────────────────────────────────────── */
/* ── Desktop notifications and the audio ping ────────────────────────────────
   Both are best-effort: a browser may refuse either, and the caller should stay
   working when it does. Neither ships an asset.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    var config = ui._.config;

    ui.requestNotify = function () {
        try {
            if ('Notification' in window && Notification.permission === 'default') {
                Notification.requestPermission();
            }
        } catch (e) { /* notifications unavailable */ }
    };

    ui.notify = function (title, body) {
        try {
            if ('Notification' in window && Notification.permission === 'granted') {
                var opts = { body: body };
                if (config.notifyIcon) opts.icon = config.notifyIcon;
                new Notification(title, opts);
            }
        } catch (e) { /* ignore */ }
    };

    // Short two-tone ping via WebAudio — no audio asset to ship. The context is
    // created lazily; browsers only allow it after a user gesture anyway. `this`
    // is the drSimpleUi object when called as drSimpleUi.ping(), so the context is
    // cached across calls on the global rather than rebuilt each time.
    ui.ping = function () {
        try {
            var ctx = this._audio ||
                (this._audio = new (window.AudioContext || window.webkitAudioContext)());
            if (ctx.state === 'suspended') ctx.resume();
            var t = ctx.currentTime;
            var osc = ctx.createOscillator(), gain = ctx.createGain();
            osc.type = 'sine';
            osc.frequency.setValueAtTime(880, t);
            osc.frequency.setValueAtTime(660, t + 0.12);
            gain.gain.setValueAtTime(0.0001, t);
            gain.gain.exponentialRampToValueAtTime(0.12, t + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.3);
            osc.connect(gain); gain.connect(ctx.destination);
            osc.start(t); osc.stop(t + 0.32);
        } catch (e) { /* audio unavailable — the visual notification still fires */ }
    };

})(window.drSimpleUi);

/* ── 51-toast.js ──────────────────────────────────────────────── */
/* ── Toasts ──────────────────────────────────────────────────────────────────
   drSimpleUi.toast('Approved INC0031209', { kind: 'go' })

   For confirming something that already happened. Anything the user must act on is
   an .alert, which stays until the state changes — a toast that carries a required
   action is an action nobody performs.

   The stack is created on first use and reused, so an app renders nothing and
   positions nothing.

   Announced through aria-live on the stack rather than by moving focus: stealing
   focus to say "saved" interrupts whatever the user is typing. `polite` for the
   ordinary kinds and `assertive` for danger, because a failure is worth cutting in
   for and a success is not.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    var ICONS = {
        go: 'ri-check-line',
        warn: 'ri-alert-line',
        danger: 'ri-error-warning-line',
        info: 'ri-information-line'
    };

    function stack() {
        var el = document.querySelector('.toast-stack');
        if (!el) {
            el = document.createElement('div');
            el.className = 'toast-stack';
            // The region is a status log, not a landmark to navigate to.
            el.setAttribute('role', 'status');
            el.setAttribute('aria-live', 'polite');
            document.body.appendChild(el);
        }
        return el;
    }

    /**
     * message  the line to show; a plain string, inserted as text
     * opts     { kind: 'go'|'warn'|'danger'|'info', title, timeout, dismissible }
     * returns  a function that removes this toast early
     */
    ui.toast = function (message, opts) {
        opts = opts || {};
        var kind = ICONS[opts.kind] ? opts.kind : 'info';
        var host = stack();

        // A failure interrupts; a confirmation waits its turn.
        host.setAttribute('aria-live', kind === 'danger' ? 'assertive' : 'polite');

        var el = document.createElement('div');
        el.className = 'toast toast-' + kind;

        var icon = document.createElement('i');
        icon.className = ICONS[kind];
        icon.setAttribute('aria-hidden', 'true');

        var body = document.createElement('div');
        body.className = 'toast-body';
        if (opts.title) {
            var strong = document.createElement('strong');
            strong.textContent = opts.title;
            body.appendChild(strong);
        }
        // textContent, never innerHTML: the message often contains a value from the
        // server, and this is the one place an app would hand us one.
        body.appendChild(document.createTextNode(message == null ? '' : String(message)));

        el.appendChild(icon);
        el.appendChild(body);

        var timer = 0;
        function remove() {
            clearTimeout(timer);
            if (el.parentNode) el.parentNode.removeChild(el);
            if (!host.children.length && host.parentNode) host.parentNode.removeChild(host);
        }

        if (opts.dismissible !== false) {
            var close = document.createElement('button');
            close.type = 'button';
            close.className = 'toast-close';
            close.setAttribute('aria-label', 'Dismiss');
            close.innerHTML = '<i class="ri-close-line" aria-hidden="true"></i>';
            close.addEventListener('click', remove);
            el.appendChild(close);
        }

        host.appendChild(el);

        // 0 means "stays until dismissed" — for a failure the user has to read.
        var ms = opts.timeout === undefined ? 4000 : opts.timeout;
        if (ms > 0) timer = setTimeout(remove, ms);

        return remove;
    };

})(window.drSimpleUi);

/* ── 52-confirm.js ──────────────────────────────────────────────── */
/* ── Confirmation dialog ─────────────────────────────────────────────────────
   await drSimpleUi.confirm({ title, message, confirm, cancel, danger })
     → true if confirmed, false if cancelled or dismissed.

   Built on <dialog>.showModal(), which is the whole reason this exists rather than
   an app hand-rolling a .modal-backdrop: the platform gives the top layer, a focus
   trap, Escape-to-close and inert content behind, and none of those are things a
   div-based overlay can do without a lot of code that is usually wrong.

   Replaces window.confirm(), which blocks the thread, cannot be styled, and in
   Blazor Server blocks the circuit while it is open.

   No fallback for a browser without <dialog>. The supported floor is Firefox ESR 140
   and current Chrome, Edge and Safari, all of which have had it for years — a
   fallback path would be untested code that only ever runs where the library is not
   supported anyway.
   ─────────────────────────────────────────────────────────────────────────── */
(function (ui) {

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    ui.confirm = function (opts) {
        opts = opts || {};
        var title = opts.title || 'Are you sure?';
        var message = opts.message || '';
        var confirmLabel = opts.confirm || 'Confirm';
        var cancelLabel = opts.cancel || 'Cancel';

        return new Promise(function (resolve) {
            var dialog = document.createElement('dialog');
            dialog.className = 'modal modal-sm';

            var header = el('div', 'modal-header');
            var h3 = el('h3', null, title);
            header.appendChild(h3);

            var body = el('div', 'modal-body');
            if (message) body.appendChild(el('p', null, message));

            var footer = el('div', 'modal-footer');
            var cancel = el('button', 'btn', cancelLabel);
            cancel.type = 'button';
            var ok = el('button', 'btn ' + (opts.danger ? 'btn-danger' : 'btn-primary'), confirmLabel);
            ok.type = 'button';
            footer.appendChild(cancel);
            footer.appendChild(ok);

            dialog.appendChild(header);
            if (message) dialog.appendChild(body);
            dialog.appendChild(footer);
            document.body.appendChild(dialog);

            // Settle on the ACTION, not only on the dialog's `close` event.
            //
            // Resolving purely from `close` gives the promise a single point of
            // failure: if that event does not arrive — and it does not, for instance,
            // in a background or non-compositing tab, where close() still takes
            // effect but the queued event is never dispatched — then `await confirm()`
            // never returns. In a Blazor handler that is an action that silently stops
            // working, with no error anywhere.
            //
            // So every route a user can take settles directly: both buttons, and the
            // `cancel` event that Escape fires. The `close` listener is a third line
            // only — it covers an app calling close() on the dialog itself, and it is
            // no more reliable than the event it hangs off, which is the point. Nothing
            // a user can do depends on it.
            //
            // settled makes the first route win and the rest no-ops, so the paths
            // cannot double-resolve or double-remove.
            var settled = false;
            function settle(value) {
                if (settled) return;
                settled = true;
                try { if (dialog.open) dialog.close(); } catch (e) { /* already closed */ }
                if (dialog.parentNode) dialog.parentNode.removeChild(dialog);
                resolve(value);
            }

            ok.addEventListener('click', function () { settle(true); });
            cancel.addEventListener('click', function () { settle(false); });
            dialog.addEventListener('cancel', function () { settle(false); });   // Escape
            dialog.addEventListener('close', function () { settle(false); });

            dialog.showModal();

            // Focus the SAFE choice. showModal() focuses the first focusable element,
            // which would be Cancel here by source order — but for a destructive
            // action that ordering is the point, so it is made explicit rather than
            // left to depend on the DOM order.
            (opts.danger ? cancel : ok).focus();
        });
    };

})(window.drSimpleUi);

