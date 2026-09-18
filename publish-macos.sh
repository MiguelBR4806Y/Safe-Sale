#!/bin/bash
# Safe-Sale - Script de Publicación (macOS)
# Requiere: dotnet, Velopack (dotnet tool install --global Velopack)

set -e

VERSION="1.0.0"
PUBLISH_DIR="./publish"
PROJECT_PATH="./src/SS/SS.csproj"

echo "=== Safe-Sale - Publicación macOS ==="
echo "Versión: $VERSION"

# 1. Publicar para Apple Silicon (M1/M2/M3/M4)
echo ""
echo "[1/4] Publicando para Apple Silicon (arm64)..."
dotnet publish "$PROJECT_PATH" -c Release -r osx-arm64 --self-contained -o "$PUBLISH_DIR/osx-arm64"

# 2. Publicar para Intel
echo ""
echo "[2/4] Publicando para Intel (x64)..."
dotnet publish "$PROJECT_PATH" -c Release -r osx-x64 --self-contained -o "$PUBLISH_DIR/osx-x64"

# 3. Empaquetar con Velopack
echo ""
echo "[3/4] Empaquetando instaladores..."
vpk pack --packId SafeSale --packVersion "$VERSION" --packDir "$PUBLISH_DIR/osx-arm64" --mainExe SafeSale
vpk pack --packId SafeSale --packVersion "$VERSION" --packDir "$PUBLISH_DIR/osx-x64" --mainExe SafeSale

# 4. Publicar en GitHub Releases
echo ""
echo "[4/4] Publicando en GitHub Releases..."
vpk publish --repoUrl "https://github.com/MiguelBR4806Y/Safe-Sale" --tag "v$VERSION"

echo ""
echo "=== ¡Publicación completada! ==="
echo "Los usuarios recibirán la actualización automáticamente."
