#!/usr/bin/env bash
#
# Verifies that a built .nupkg actually contains the CSS, the JS and the whole
# catalogue.
#
# This exists because static web assets have historically been dropped from
# packages under some build configurations, and the failure is silent: the
# package restores, the app builds, and every stylesheet link 404s at runtime.
# So we do not assume — we unpack and assert.
#
# Usage:  build/verify-package.sh artifacts/DR.Simple_UI.*.nupkg
#
set -euo pipefail

PACKAGE="${1:?usage: verify-package.sh <path-to-nupkg>}"

# Only $1 is read, so a glob matching several packages would silently verify one
# of them — most likely a stale build left in artifacts/ — and report success for
# a package nobody is shipping. Refuse instead.
if [[ $# -gt 1 ]]; then
    echo "::error::Expected exactly one package, got $#: $*"
    echo "Clear artifacts/ so the glob matches only the build under test."
    exit 1
fi

if [[ ! -f "$PACKAGE" ]]; then
    echo "::error::Package not found: $PACKAGE"
    exit 1
fi

echo "Verifying $(basename "$PACKAGE")"
echo

CONTENTS="$(unzip -Z1 "$PACKAGE")"

# Static web assets are packed under staticwebassets/ and served to a consuming
# app from _content/DR.Simple_UI/ at runtime.
REQUIRED=(
    "lib/net10.0/DR.Simple_UI.dll"
    "staticwebassets/css/DR.Simple_UI.css"
    "staticwebassets/js/DR.Simple_UI.js"
    "staticwebassets/js/DR.Simple_UI.boot.js"
    # The token export. A design tool reads this path out of the restored package,
    # so it is as much a shipped contract as the stylesheet.
    "staticwebassets/tokens/DR.Simple_UI.tokens.json"
    "staticwebassets/catalogue/index.html"
    "staticwebassets/catalogue/catalogue.css"
    "staticwebassets/catalogue/catalogue.js"
    "staticwebassets/catalogue/tokens.html"
    "staticwebassets/catalogue/button.html"
    "staticwebassets/catalogue/badge.html"
    "staticwebassets/catalogue/card.html"
    "staticwebassets/catalogue/table.html"
    "staticwebassets/catalogue/form.html"
    "staticwebassets/catalogue/toolbar.html"
    "staticwebassets/catalogue/modal.html"
    "staticwebassets/catalogue/alert.html"
    "staticwebassets/catalogue/grid.html"
    "staticwebassets/catalogue/markdown.html"
    "staticwebassets/catalogue/frame.html"
    "staticwebassets/catalogue/favicon.ico"
    "staticwebassets/catalogue/logo.png"
    "staticwebassets/lib/remixicon/remixicon.css"
    "staticwebassets/lib/remixicon/remixicon.woff2"
    "staticwebassets/lib/remixicon/LICENSE"
    "README.md"
    "LICENSE"
    "THIRD-PARTY-NOTICES.md"
    "icon.png"
)

failed=0
for entry in "${REQUIRED[@]}"; do
    if grep -Fxq "$entry" <<<"$CONTENTS"; then
        printf '  ok      %s\n' "$entry"
    else
        printf '  MISSING %s\n' "$entry"
        failed=1
    fi
done

echo

# Every catalogue page in the repo must have made it in — a new page that is not
# packed would be documentation nobody installing the package can read.
repo_pages=$(find src/DR.Simple_UI/wwwroot/catalogue -name '*.html' | wc -l | tr -d ' ')
packed_pages=$(grep -c '^staticwebassets/catalogue/.*\.html$' <<<"$CONTENTS" || true)
if [[ "$repo_pages" != "$packed_pages" ]]; then
    echo "::error::Catalogue page count mismatch — $repo_pages in the repo, $packed_pages in the package."
    failed=1
else
    echo "  ok      all $packed_pages catalogue pages packed"
fi

# The stylesheet must be the real one, not an empty placeholder, and must still
# carry the token block the whole contract rests on.
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
unzip -q -o "$PACKAGE" 'staticwebassets/css/DR.Simple_UI.css' -d "$tmp"
css="$tmp/staticwebassets/css/DR.Simple_UI.css"

if [[ ! -s "$css" ]]; then
    echo "::error::The packed stylesheet is empty."
    failed=1
elif ! grep -q -- '--brand:' "$css"; then
    echo "::error::The packed stylesheet has no --brand token — the token layer did not ship."
    failed=1
else
    echo "  ok      packed stylesheet carries the token layer ($(wc -c <"$css" | tr -d ' ') bytes)"
fi

# The catalogue's link must resolve INSIDE the package: staticwebassets/catalogue/
# → ../css/ → staticwebassets/css/. If someone "tidies" that href, the packaged
# docs render unstyled.
unzip -q -o "$PACKAGE" 'staticwebassets/catalogue/button.html' -d "$tmp"
if grep -q '\.\./css/DR\.Simple_UI\.css' "$tmp/staticwebassets/catalogue/button.html"; then
    echo "  ok      catalogue links ../css/DR.Simple_UI.css (resolves within the package)"
else
    echo "::error::A catalogue page does not link ../css/DR.Simple_UI.css."
    failed=1
fi

# Blazor CSS isolation: no scoped CSS exists yet (0.1.0 ships no components), but
# once it does its bundle must be packed too, or component styles vanish.
if grep -q '^staticwebassets/DR\.Simple_UI\.bundle\.scp\.css$' <<<"$CONTENTS"; then
    echo "  ok      scoped-CSS bundle packed"
elif grep -Erq '\.razor\.css$' <<<"$(find src/DR.Simple_UI -name '*.razor.css' 2>/dev/null || true)"; then
    echo "::error::Scoped CSS files exist in the project but no .bundle.scp.css was packed."
    failed=1
else
    echo "  ok      no scoped CSS in use, none expected"
fi

echo
if [[ "$failed" -ne 0 ]]; then
    echo "Package verification FAILED."
    exit 1
fi
echo "Package verification passed."
