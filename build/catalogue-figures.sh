#!/usr/bin/env bash
#
# Writes the figures on the catalogue landing page from the files they describe.
#
#   design tokens   distinct custom properties declared in the shipped stylesheet
#   CSS classes     distinct class names in its selectors
#   bundled icons   distinct .ri-* classes that set a glyph
#
# WHY GENERATED RATHER THAN COMPUTED IN THE BROWSER. Reading the numbers out of the
# CSSOM at load would need no second copy at all, and was the first plan. It does not
# survive the two ways this page is actually read: Chromium refuses `sheet.cssRules`
# for a stylesheet loaded over file://, which is how the catalogue is read inside a
# restored package, and `fetch` of the stylesheet is blocked there too. A computed
# figure would also be blank with scripting off. So the numbers are baked in, the same
# way the stylesheet and the token export are, and --check fails the build on drift.
#
# The numbers are ALSO derived independently in C# by
# The_landing_page_figures_match_the_stylesheet. That duplication is deliberate: two
# implementations agreeing is what caught the figure this script was written for. The
# page claimed 317 CSS classes against an actual 311, because the number had been
# updated to match a guard that counted the dotted names in `@layer dr.paint, …` as
# classes. A single implementation would have stayed self-consistently wrong.
#
# "Bundled icons" counts only the classes that carry a glyph. The previous figure,
# 3,245, included the 16 sizing utilities — .ri-lg, .ri-fw, .ri-2x and so on — which
# are not icons.
#
# Usage:  build/catalogue-figures.sh          # rewrite the figures
#         build/catalogue-figures.sh --check  # fail if any is out of date
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CSS="$ROOT/src/DR.Simple_UI/wwwroot/css/DR.Simple_UI.css"
ICONS="$ROOT/src/DR.Simple_UI/wwwroot/lib/remixicon/remixicon.css"
PAGE="$ROOT/src/DR.Simple_UI/wwwroot/catalogue/index.html"

CHECK=0
[[ "${1:-}" == "--check" ]] && CHECK=1

for f in "$CSS" "$ICONS" "$PAGE"; do
    [[ -f "$f" ]] || { echo "::error::Missing $f"; exit 1; }
done

# 1 234 567 -> 1,234,567
group() { sed ':a;s/\B[0-9]\{3\}\>/,&/;ta' <<<"$1"; }

TOKENS="$(bash "$ROOT/build/css-inventory.sh" --count "$CSS" tokens)"
CLASSES="$(bash "$ROOT/build/css-inventory.sh" --count "$CSS" classes)"

# A glyph-bearing icon class: `.ri-name:before { content: "\exxx" }`. Counted from the
# selector list rather than from `content` occurrences, because several icons share a
# rule and a codepoint count would not match the number of usable class names.
ICON_COUNT="$(grep -oE '\.ri-[a-z0-9-]+:before' "$ICONS" | sed 's/:before//' | sort -u | wc -l | tr -d ' ')"

declare -A FIGURES=(
    ["design tokens"]="$TOKENS"
    ["CSS classes"]="$CLASSES"
    ["bundled icons"]="$(group "$ICON_COUNT")"
)

status=0
for label in "${!FIGURES[@]}"; do
    want="${FIGURES[$label]}"

    # The tile is one line: <div class="cat-fact"><strong>N</strong><span>label</span></div>
    have="$(grep -oE "<strong>[0-9,]+</strong><span>$label</span>" "$PAGE" \
            | grep -oE '>[0-9,]+<' | tr -d '><' || true)"

    if [[ -z "$have" ]]; then
        echo "::error::index.html states no figure for \"$label\"."
        status=1
        continue
    fi

    if [[ "$have" == "$want" ]]; then
        printf '  ok      %-24s %s\n' "$label" "$want"
        continue
    fi

    if [[ "$CHECK" == "1" ]]; then
        printf '  STALE   %-24s page says %s, the source has %s\n' "$label" "$have" "$want"
        status=1
    else
        # The label anchors the replacement, so two tiles can never be swapped.
        sed -i "s|<strong>$have</strong><span>$label</span>|<strong>$want</strong><span>$label</span>|" "$PAGE"
        printf '  updated %-24s %s -> %s\n' "$label" "$have" "$want"
    fi
done

echo
if [[ "$status" != "0" ]]; then
    [[ "$CHECK" == "1" ]] && echo "Run build/catalogue-figures.sh to update the landing page."
    exit 1
fi

[[ "$CHECK" == "1" ]] && echo "ok      the landing page figures match their sources" \
                      || echo "ok      the landing page figures are up to date"
