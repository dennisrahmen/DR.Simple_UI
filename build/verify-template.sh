#!/usr/bin/env bash
#
# Installs the `dr-blazor` template from artifacts/, generates a project with it, and
# builds that project with warnings as errors.
#
# This exists because a template is not verified by packing cleanly. The first version
# packed, installed and generated a project that failed with five CS0246 errors — it
# defaulted to a DR.Simple_UI version that predated the components it uses — and two
# separate Razor rules were being broken in the generated layout:
#
#   * RZ9996: a component with any named RenderFragment stops accepting loose child
#     content, so <ChildContent> has to be spelled out.
#   * RZ9986: text containing @ cannot go straight into an attribute.
#
# Neither is visible in the template source; both need a compiler. Hence a script
# rather than a unit test.
#
# The generated project resolves DR.Simple_UI from artifacts/, so it is built against
# the package produced by the same run rather than whatever is on nuget.org.
#
# Usage:  build/verify-template.sh
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS="$ROOT/artifacts"

shopt -s nullglob
LIB=("$ARTIFACTS"/DR.Simple_UI.[0-9]*.nupkg)
TPL=("$ARTIFACTS"/DR.Simple_UI.Templates.*.nupkg)
shopt -u nullglob

if [[ ${#LIB[@]} -ne 1 ]]; then
    echo "::error::Expected exactly one library package in $ARTIFACTS, found ${#LIB[@]}."
    echo "Run: dotnet pack src/DR.Simple_UI/DR.Simple_UI.csproj -c Release -o artifacts"
    exit 1
fi
if [[ ${#TPL[@]} -ne 1 ]]; then
    echo "::error::Expected exactly one template package in $ARTIFACTS, found ${#TPL[@]}."
    echo "Run: dotnet pack templates/DR.Simple_UI.Templates.csproj -c Release -o artifacts"
    exit 1
fi

# 0.2.0 out of DR.Simple_UI.0.2.0.nupkg.
VERSION="$(basename "${LIB[0]}")"
VERSION="${VERSION#DR.Simple_UI.}"
VERSION="${VERSION%.nupkg}"

WORK="$(mktemp -d)"
# Leave nothing behind, and uninstall the template even on failure — a stale local
# template pointing at a deleted nupkg breaks `dotnet new` for everything afterwards.
cleanup() {
    dotnet new uninstall DR.Simple_UI.Templates >/dev/null 2>&1 || true
    rm -rf "$WORK"
}
trap cleanup EXIT

cat > "$WORK/nuget.config" <<XML
<configuration>
  <packageSources>
    <clear />
    <add key="dr-local" value="$ARTIFACTS" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
XML

echo "        installing $(basename "${TPL[0]}")"
dotnet new uninstall DR.Simple_UI.Templates >/dev/null 2>&1 || true
dotnet new install "${TPL[0]}" >/dev/null

echo "        generating a project against DR.Simple_UI $VERSION"
(
    cd "$WORK"
    dotnet new dr-blazor -n Ci.Sample \
        --DrSimpleUiVersion "$VERSION" \
        --AppTitle "CI Sample" \
        --Brand "#e41f16" \
        --ThemeDefault system >/dev/null
)

# -warnaserror: an RZ warning in the generated app is a defect in the template, and
# "Found markup element with unexpected name" is exactly the shape a broken slot takes.
echo "        building it with warnings as errors"
dotnet build "$WORK/Ci.Sample/Ci.Sample.csproj" -warnaserror --nologo

echo ""
echo "ok      the template generates a project that builds clean"
