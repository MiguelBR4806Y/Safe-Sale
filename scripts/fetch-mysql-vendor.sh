#!/usr/bin/env bash
# Descarga MySQL Community portable a vendor/mysql/<rid> para empaquetarlo con Safe-Sale.
# Los binarios NO se commitean: cada plataforma se descarga antes de publicar.
#
# Uso:
#   ./scripts/fetch-mysql-vendor.sh
#   ./scripts/fetch-mysql-vendor.sh 8.4.13 osx-arm64
#   MYSQL_URL="https://cdn.mysql.com/Downloads/MySQL-8.4/mysql-8.4.13-macos15-arm64.tar.gz" \
#       ./scripts/fetch-mysql-vendor.sh 8.4.13 osx-arm64
#
# Si no se pasa MYSQL_URL, se arma automáticamente la URL oficial según el RID.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:-8.4.13}"
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

trim_vendor() {
    local dest="$1"
    find "$dest" -type f \( -name '*.pdb' -o -name '*.lib' \) -delete
    rm -rf "$dest/include" "$dest/docs" "$dest/lib/mecab" "$dest/lib/plugin/debug"
    if [ -d "$dest/bin" ]; then
        find "$dest/bin" -maxdepth 1 -type f -name '*.exe' ! -name 'mysqld.exe' -delete
    fi
    find "$dest" -type f -name '*debug*.dll' -delete
}

if [ -x "$DEST/bin/mysqld" ]; then
    echo "Ya existe el motor en $DEST. Recortando archivos innecesarios..."
    trim_vendor "$DEST"
    exit 0
fi

if [ -z "$URL" ]; then
    case "$RID" in
        osx-arm64)
            URL="https://cdn.mysql.com/Downloads/MySQL-8.4/mysql-${VERSION}-macos15-arm64.tar.gz" ;;
        osx-x64)
            echo "MySQL 8.4 no publica tarball oficial para macOS Intel."
            echo "Pasá MYSQL_URL con una build compatible para osx-x64."
            exit 1 ;;
        linux-x64)
            URL="https://cdn.mysql.com/Downloads/MySQL-8.4/mysql-${VERSION}-linux-glibc2.28-x86_64.tar.xz" ;;
        linux-arm64)
            URL="https://cdn.mysql.com/Downloads/MySQL-8.4/mysql-${VERSION}-linux-glibc2.28-aarch64.tar.xz" ;;
        *)
            echo "RID no soportado por este script: $RID"
            echo "Para Windows usá scripts/fetch-mysql-vendor.ps1 o definí MYSQL_URL manualmente."
            exit 1 ;;
    esac
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

echo "Recortando archivos innecesarios del motor..."
trim_vendor "$DEST"

if [ ! -x "$DEST/bin/mysqld" ]; then
    echo "No se encontró bin/mysqld en el paquete descargado."
    exit 1
fi

echo "OK: $DEST"
