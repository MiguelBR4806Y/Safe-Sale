#!/bin/bash
# Safe-Sale - Script de Publicación (macOS)
# Requiere: dotnet, vpk (dotnet tool install --global vpk), gh (brew install gh)

set -e

VERSION="1.0.0"
PUBLISH_DIR="./publish"
PROJECT_PATH="./src/SS/SS.csproj"
RELEASE_DIR="./Releases"

echo "=== Safe-Sale - Publicación macOS ==="
echo "Versión: $VERSION"

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
vpk pack --packId SafeSale --packVersion "$VERSION" --packDir "$PUBLISH_DIR/osx-arm64" --mainExe SafeSale
vpk pack --packId SafeSale --packVersion "$VERSION" --packDir "$PUBLISH_DIR/osx-x64" --mainExe SafeSale

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
gh release create "v$VERSION" --title "Safe-Sale v$VERSION" --notes "Versión $VERSION" --draft

# 5. Subir archivos y publicar
echo ""
echo "[5/5] Subiendo archivos al Release..."
for file in "$RELEASE_DIR"/*.pkg "$RELEASE_DIR"/*.nupkg; do
    if [ -f "$file" ]; then
        gh release upload "v$VERSION" "$file" --clobber
        echo "Subido: $(basename "$file")"
    fi
done

gh release edit "v$VERSION" --draft=false

echo ""
echo "=== ¡Publicación completada! ==="
echo "Los usuarios recibirán la actualización automáticamente."
echo "URL: https://github.com/MiguelBR4806Y/Safe-Sale/releases/tag/v$VERSION"
