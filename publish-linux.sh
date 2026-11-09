#!/usr/bin/env bash
# Safe-Sale - Publicación (Linux x64)
# Requiere: dotnet, vpk (dotnet tool install --global vpk), gh (https://github.com/cli/cli)
#
# Velopack genera un AppImage (no un instalador tradicional). El usuario debe:
#   chmod +x SS-Installer.AppImage && ./SS-Installer.AppImage

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/src/SS/SS.csproj"
PUBLISH="$ROOT/publish"
RELEASES="$ROOT/Releases"
ICON="$ROOT/src/SS/Assets/SS-ico.png"
PACK_ID="SafeSale"
MAIN_EXE="SafeSale"
RUNTIME="linux-x64"
CHANNEL="linux-x64"

VERSION="$(grep -o '<Version>[^<]*</Version>' "$PROJECT" | head -1 | sed 's/<Version>//g; s#</Version>##g' | tr -d '[:space:]')"
if [ -z "$VERSION" ]; then
    echo "Error: No se pudo leer <Version> del .csproj"
    exit 1
fi

if [ ! -f "$ICON" ]; then
    echo "Error: falta el ícono PNG requerido para AppImage: $ICON"
    exit 1
fi

TAG="v${VERSION}-$(date +%Y%m%d-%H%M%S)"
OUT="$RELEASES/$RUNTIME"

echo "=== Safe-Sale - Publicación Linux ==="
echo "Versión: $VERSION"
echo "Canal:   $CHANNEL"
echo "Runtime: $RUNTIME"
echo "Tag:     $TAG"

if [ ! -x "$ROOT/vendor/mysql/$RUNTIME/bin/mysqld" ]; then
    echo "Error: falta el motor MySQL local para $RUNTIME."
    echo "Ejecutá: ./scripts/fetch-mysql-vendor.sh $VERSION $RUNTIME"
    exit 1
fi

echo ""
echo "[1/4] Publicando aplicación..."
dotnet publish "$PROJECT" -c Release -r "$RUNTIME" --self-contained true -o "$PUBLISH/$RUNTIME"

echo ""
echo "[2/4] Empaquetando AppImage con Velopack..."
rm -rf "$OUT"
vpk pack --packId "$PACK_ID" --packVersion "$VERSION" \
    --packDir "$PUBLISH/$RUNTIME" --mainExe "$MAIN_EXE" \
    --outputDir "$OUT" --channel "$CHANNEL" \
    --packAuthors "Safe-Sale Team" \
    --icon "$ICON" --categories "Utility;Office;" --yes

for img in "$OUT"/*.AppImage; do
    [ -e "$img" ] || continue
    mv "$img" "$OUT/SS-Installer.AppImage"
    chmod +x "$OUT/SS-Installer.AppImage"
    echo "AppImage: SS-Installer.AppImage"
    break
done

echo ""
echo "[3/4] Creando Release en GitHub..."
gh release create "$TAG" \
    --title "Safe-Sale v$VERSION (Linux)" \
    --notes "Versión $VERSION para Linux x64 (AppImage). Ejecutar con: chmod +x SS-Installer.AppImage && ./SS-Installer.AppImage" \
    --draft

echo ""
echo "[4/4] Subiendo artefactos..."
for file in "$OUT"/*; do
    [ -f "$file" ] || continue
    gh release upload "$TAG" "$file" --clobber
    echo "Subido: $(basename "$file")"
done

gh release edit "$TAG" --draft=false

echo ""
echo "=== ¡Publicación completada! ==="
echo "URL: https://github.com/MiguelBR4806Y/Safe-Sale/releases/tag/$TAG"
