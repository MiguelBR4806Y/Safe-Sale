#!/usr/bin/env bash
# Safe-Sale - Publicación (macOS: Apple Silicon + Intel)
# Requiere: dotnet, vpk (dotnet tool install --global vpk), gh (brew install gh)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/src/SS/SS.csproj"
PUBLISH="$ROOT/publish"
RELEASES="$ROOT/Releases"
PACK_ID="SafeSale"
MAIN_EXE="SafeSale"

VERSION="$(grep -o '<Version>[^<]*</Version>' "$PROJECT" | head -1 | sed 's/<Version>//g; s#</Version>##g' | tr -d '[:space:]')"
if [ -z "$VERSION" ]; then
    echo "Error: No se pudo leer <Version> del .csproj"
    exit 1
fi

TAG="v${VERSION}-$(date +%Y%m%d-%H%M%S)"

echo "=== Safe-Sale - Publicación macOS ==="
echo "Versión: $VERSION"
echo "Tag:     $TAG"

for rid in osx-arm64 osx-x64; do
    if [ ! -x "$ROOT/vendor/mysql/$rid/bin/mysqld" ]; then
        echo "Error: falta el motor MySQL local para $rid."
        echo "Ejecutá: ./scripts/fetch-mysql-vendor.sh $VERSION $rid"
        exit 1
    fi
done

ICON_PNG="$ROOT/src/SS/Assets/SS-ico.png"
ICON_ICNS="$ROOT/src/SS/Assets/SS-ico.icns"

if [ -f "$ICON_PNG" ] && [ ! -f "$ICON_ICNS" ] && command -v iconutil >/dev/null 2>&1; then
    TMPICONSET="$(mktemp -d)/AppIcon.iconset"
    mkdir -p "$TMPICONSET"
    for size in 16 32 128 256 512; do
        sips -z "$size" "$size" "$ICON_PNG" --out "$TMPICONSET/icon_${size}x${size}.png" >/dev/null
        double=$((size * 2))
        sips -z "$double" "$double" "$ICON_PNG" --out "$TMPICONSET/icon_${size}x${size}@2x.png" >/dev/null
    done
    iconutil -c icns "$TMPICONSET" -o "$ICON_ICNS"
    rm -rf "$(dirname "$TMPICONSET")"
fi

pack_arch() {
    local rid="$1"
    local channel="$2"
    local installer="$3"
    local out="$RELEASES/$rid"

    echo ""
    echo "--- Publicando $rid (canal $channel) ---"

    dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained true -o "$PUBLISH/$rid"

    rm -rf "$out"
    ICON_ARGS=()
    if [ -f "$ICON_ICNS" ]; then
        ICON_ARGS=(--icon "$ICON_ICNS")
    fi

    vpk pack --packId "$PACK_ID" --packVersion "$VERSION" \
        --packDir "$PUBLISH/$rid" --mainExe "$MAIN_EXE" \
        --outputDir "$out" --channel "$channel" \
        "${ICON_ARGS[@]+"${ICON_ARGS[@]}"}" \
        --packAuthors "Safe-Sale Team" --yes

    for pkg in "$out"/*.pkg; do
        [ -e "$pkg" ] || continue
        mv "$pkg" "$out/$installer"
        echo "Instalador: $installer"
        break
    done
}

# Canales separados por arquitectura: evita que los feeds (.json) colisionen
# y que un Mac Intel descargue el paquete de Apple Silicon (o viceversa).
pack_arch "osx-arm64" "osx-arm64" "SS-Installer-arm64.pkg"
pack_arch "osx-x64"   "osx-x64"   "SS-Installer-Intel.pkg"

echo ""
echo "Creando Release en GitHub..."
gh release create "$TAG" \
    --title "Safe-Sale v$VERSION (macOS)" \
    --notes "Versión $VERSION para macOS (Apple Silicon e Intel)." \
    --draft

echo "Subiendo artefactos..."
for dir in "$RELEASES/osx-arm64" "$RELEASES/osx-x64"; do
    [ -d "$dir" ] || continue
    for file in "$dir"/*; do
        [ -f "$file" ] || continue
        gh release upload "$TAG" "$file" --clobber
        echo "Subido: $(basename "$file")"
    done
done

gh release edit "$TAG" --draft=false

echo ""
echo "=== ¡Publicación completada! ==="
echo "URL: https://github.com/MiguelBR4806Y/Safe-Sale/releases/tag/$TAG"
