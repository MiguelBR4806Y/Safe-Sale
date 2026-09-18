#!/bin/bash
# Safe-Sale - Script de Publicación (macOS)
# Requiere: dotnet, vpk (dotnet tool install --global vpk), gh (brew install gh)

set -e

PROJECT_PATH="./src/SS/SS.csproj"
PUBLISH_DIR="./publish"
RELEASE_DIR="./Releases"

# Leer versión del .csproj
VERSION=$(grep -o '<Version>[^<]*</Version>' "$PROJECT_PATH" | head -1 | sed 's/<Version>//g;s/<\/Version>//g' | tr -d '[:space:]')

if [ -z "$VERSION" ]; then
    echo "Error: No se pudo leer la versión del .csproj"
    exit 1
fi

# Generar tag único con timestamp (v1.0.0-20260918-105500)
TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
TAG="v${VERSION}-${TIMESTAMP}"

echo "=== Safe-Sale - Publicación macOS ==="
echo "Versión: $VERSION"
echo "Tag: $TAG"

# 1. Publicar para Apple Silicon (M1/M2/M3/M4)
echo ""
echo "[1/5] Publicando para Apple Silicon (arm64)..."
dotnet publish "$PROJECT_PATH" -c Release -r osx-arm64 --self-contained -o "$PUBLISH_DIR/osx-arm64"

# 2. Publicar para Intel
echo ""
echo "[2/5] Publicando para Intel (x64)..."
dotnet publish "$PROJECT_PATH" -c Release -r osx-x64 --self-contained -o "$PUBLISH_DIR/osx-x64"

# 3. Empaquetar con Velopack
echo ""
echo "[3/5] Empaquetando instaladores..."
vpk pack --packId SafeSale --packVersion "$VERSION" --packDir "$PUBLISH_DIR/osx-arm64" --mainExe SafeSale -y
vpk pack --packId SafeSale --packVersion "$VERSION" --packDir "$PUBLISH_DIR/osx-x64" --mainExe SafeSale -y

# Renombrar instaladores
for pkg in "$RELEASE_DIR"/*.pkg; do
    if [ -f "$pkg" ]; then
        dir=$(dirname "$pkg")
        mv "$pkg" "$dir/SS-Installer.pkg"
        echo "Renombrado: $(basename "$pkg") → SS-Installer.pkg"
    fi
done

# 4. Crear Release en GitHub
echo ""
echo "[4/5] Creando Release en GitHub..."
gh release create "$TAG" --title "Safe-Sale v$VERSION" --notes "Versión $VERSION" --draft

# 5. Subir archivos y publicar
echo ""
echo "[5/5] Subiendo archivos al Release..."
for file in "$RELEASE_DIR"/*.pkg "$RELEASE_DIR"/*.nupkg; do
    if [ -f "$file" ]; then
        gh release upload "$TAG" "$file" --clobber
        echo "Subido: $(basename "$file")"
    fi
done

gh release edit "$TAG" --draft=false

echo ""
echo "=== ¡Publicación completada! ==="
echo "Tag: $TAG"
echo "URL: https://github.com/MiguelBR4806Y/Safe-Sale/releases/tag/$TAG"
