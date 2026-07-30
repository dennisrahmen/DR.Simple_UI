/* DR.Simple_UI — pre-paint theme boot.
   ───────────────────────────────────────────────────────────────────────────
   Load this in <head>, BEFORE the first paint, so a light-theme or compact-
   density user never sees a dark flash:

     <script src="_content/DR.Simple_UI/js/DR.Simple_UI.boot.js"></script>

   Both attributes below are optional.

   It only stamps data-theme / data-cvd / data-density / lang on <html> from
   localStorage. The main DR.Simple_UI.js (end of <body>) keeps them current
   afterwards, and both default to the same prefix, so neither needs configuring
   unless two apps share one origin.

   It ships as a file rather than a snippet to copy into every app on purpose:
   a copied snippet is drift waiting to happen.

   data-prefix       localStorage key prefix. Default "drui.". Only needed when
                     two apps share an origin; must match storagePrefix.
   data-lang-cookie  "true" to also write a "<prefix>lang" cookie, so a
                     server-rendered app can prerender in the chosen language
                     instead of flashing the default one. */
(function () {
    var el = document.currentScript;
    var prefix = (el && el.dataset.prefix) || 'drui.';
    var wantCookie = !!(el && el.dataset.langCookie === 'true');

    try {
        var get = function (k) { return localStorage.getItem(prefix + k); };
        var root = document.documentElement;

        root.setAttribute('data-theme', get('theme') === 'light' ? 'light' : 'dark');
        if (get('cvd') === '1') root.setAttribute('data-cvd', '1');
        if (get('density') === 'compact') root.setAttribute('data-density', 'compact');

        var lang = get('lang') || (navigator.language || 'en').slice(0, 2).toLowerCase();
        root.lang = lang;
        if (wantCookie) {
            document.cookie = prefix + 'lang=' + lang + ';path=/;max-age=31536000;SameSite=Lax';
        }
    } catch (e) {
        /* storage blocked — first paint falls back to the dark default */
    }
})();
