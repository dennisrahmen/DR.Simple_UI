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
