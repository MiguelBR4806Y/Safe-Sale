#!/usr/bin/env bash
# Descarga MySQL Community portable a vendor/mysql/<rid> para empaquetarlo con Safe-Sale.
# Los binarios NO se commitean: cada plataforma se descarga antes de publicar.
#
# Uso:
#   ./scripts/fetch-mysql-vendor.sh
#   ./scripts/fetch-mysql-vendor.sh 8.4.5 osx-arm64
#   MYSQL_URL="https://dev.mysql.com/get/Downloads/MySQL-8.4/mysql-8.4.5-macos14-arm64.tar.gz" \
#       ./scripts/fetch-mysql-vendor.sh 8.4.5 osx-arm64
#
# Los nombres de los tarballs oficiales cambian por versión y sistema; si la URL
# por defecto no existe, usá MYSQL_URL con el link oficial correspondiente.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:-8.4.5}"
RID="${2:-}"
URL="${MYSQL_URL:-}"

if [ -z "$RID" ]; then
    case "$(uname -s)-$(uname -m)" in
        Darwin-arm64)       RID="osx-arm64" ;;
        Darwin-x86_64)      RID="osx-x64" ;;
        Linux-x86_64)       RID="linux-x64" ;;
        Linux-aarch64)      RID="linux-arm64" ;;
        *) echo "Arquitectura no soportada: $(uname -s)-$(uname -m)"; exit 1 ;;
    esac
fi

DEST="$ROOT/vendor/mysql/$RID"
if [ -x "$DEST/bin/mysqld" ]; then
    echo "Ya existe el motor en $DEST"
    exit 0
fi

if [ -z "$URL" ]; then
    echo "Definí MYSQL_URL con el link oficial del tarball para $RID."
    echo "Ejemplo (macOS arm64):"
    echo "  MYSQL_URL='https://dev.mysql.com/get/Downloads/MySQL-8.4/mysql-8.4.5-macos14-arm64.tar.gz' $0"
    echo "Ejemplo (Linux x64):"
    echo "  MYSQL_URL='https://dev.mysql.com/get/Downloads/MySQL-8.4/mysql-8.4.5-linux-glibc2.28-x86_64.tar.xz' $0"
    exit 1
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "Descargando MySQL Community $VERSION ($RID)"
echo "URL: $URL"
curl -fL "$URL" -o "$TMP/mysql-archive"

mkdir -p "$TMP/extract"
tar -xf "$TMP/mysql-archive" -C "$TMP/extract"

INNER="$(find "$TMP/extract" -mindepth 1 -maxdepth 1 -type d | head -1)"
if [ -z "$INNER" ]; then
    echo "El archivo no contiene una carpeta raíz."
    exit 1
fi

rm -rf "$DEST"
mkdir -p "$(dirname "$DEST")"
mv "$INNER" "$DEST"
chmod +x "$DEST/bin"/* 2>/dev/null || true

if [ ! -x "$DEST/bin/mysqld" ]; then
    echo "No se encontró bin/mysqld en el paquete descargado."
    exit 1
fi

echo "OK: $DEST"
