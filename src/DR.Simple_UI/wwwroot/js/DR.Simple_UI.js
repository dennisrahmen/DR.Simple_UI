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
window.drSimpleUi = (function () {

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

    // ── Theme / accessibility settings ──────────────────────────────────────
    // localStorage is the source of truth; the data-theme / data-cvd /
    // data-density attributes on <html> drive the CSS token layer. The boot
    // script applies them before first paint; save() keeps them applied.
    var settings = {
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

    // ── Hover hints (data-tip) ──────────────────────────────────────────────
    // One floating bubble, driven by [data-tip] through delegation on document —
    // so it covers content rendered after load (Blazor re-renders) with no
    // re-wiring. The bubble is appended to <body> and fixed-positioned, so a
    // card's or table's overflow never clips it (a pure-CSS ::after tooltip is).
    // Elements inside .sidebar are skipped: the collapsed rail has its own CSS
    // flyout, and both firing would double the tooltip.
    var tips = (function () {
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

    // ── Markdown editor ────────────────────────────────────────────────────
    // Toolbar + textarea + live preview inside one .md-editor root. Blazor owns
    // the value through the textarea's two-way @bind (@bind:event="oninput");
    // toolbar edits mutate the textarea and dispatch a bubbling 'input' event so
    // the binding picks them up — this code never calls back into .NET.
    // init() is idempotent per root, since Blazor re-renders its host.
    var md = {
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

            root.addEventListener('click', function (e) {
                var cmdBtn = e.target.closest('[data-md-cmd]');
                if (cmdBtn && root.contains(cmdBtn)) {
                    e.preventDefault();
                    self.apply(ta, cmdBtn.getAttribute('data-md-cmd'));
                    renderPreview();
                    return;
                }
                var tabBtn = e.target.closest('[data-md-tab]');
                if (tabBtn && root.contains(tabBtn)) {
                    e.preventDefault();
                    var tab = tabBtn.getAttribute('data-md-tab');
                    // Carry the height between panes (both are resize:vertical),
                    // reading the visible one before the flip. The preview then
                    // fills the same box and scrolls internally instead of
                    // ballooning its host on long text, and a manual resize in
                    // either pane sticks across the switch.
                    if (tab === 'preview') {
                        renderPreview();
                        if (preview) preview.style.height = ta.offsetHeight + 'px';
                    } else if (preview && preview.offsetHeight) {
                        ta.style.height = preview.offsetHeight + 'px';
                    }
                    root.setAttribute('data-md-view', tab);
                    root.querySelectorAll('[data-md-tab]').forEach(function (b) {
                        b.classList.toggle('md-tab--active', b === tabBtn);
                    });
                }
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
                return '  B' + (blocks.length - 1) + ' ';
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
                var ph = line.match(/^  B(\d+) $/);
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

    return {
        configure: function (opts) {
            if (!opts) return;
            Object.keys(opts).forEach(function (k) {
                if (k in config) config[k] = opts[k];
            });
            settings.apply();
        },

        settings: settings,
        tips: tips,
        md: md,

        // ── Small interop helpers ──────────────────────────────────────────
        openTab: function (url) {
            try { window.open(url, '_blank', 'noopener'); } catch (e) { /* ignore */ }
        },

        // Returns whether the copy succeeded, so the caller can toast either way.
        // Falls back to a hidden textarea where the async Clipboard API is
        // unavailable (older browsers, insecure origins).
        copyText: async function (text) {
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
        },

        viewportWidth: function () {
            return window.innerWidth || document.documentElement.clientWidth || 0;
        },

        getItem: function (k) { return readRaw(k); },
        setItem: function (k, value) {
            try { localStorage.setItem(k, value); } catch (e) { /* ignore */ }
        },

        requestNotify: function () {
            try {
                if ('Notification' in window && Notification.permission === 'default') {
                    Notification.requestPermission();
                }
            } catch (e) { /* notifications unavailable */ }
        },

        notify: function (title, body) {
            try {
                if ('Notification' in window && Notification.permission === 'granted') {
                    var opts = { body: body };
                    if (config.notifyIcon) opts.icon = config.notifyIcon;
                    new Notification(title, opts);
                }
            } catch (e) { /* ignore */ }
        },

        // Short two-tone ping via WebAudio — no audio asset to ship. The context
        // is created lazily; browsers only allow it after a user gesture anyway.
        ping: function () {
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
        }
    };
})();
