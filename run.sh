#!/usr/bin/env bash
# Script de ejecución multiplataforma para Safe-Sale
# Uso: ./run.sh

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/src/SS/SS.csproj"

echo "=== Safe-Sale - Iniciando proyecto ==="
echo "Sistema operativo detectado: $(uname -s)"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Error: 'dotnet' no está instalado o no está en el PATH."
    echo "Instalá el SDK de .NET 10: https://dotnet.microsoft.com/download"
    exit 1
fi

if [ ! -f "$PROJECT" ]; then
    echo "Error: no se encontró el proyecto en $PROJECT"
    exit 1
fi

echo "Ejecutando: dotnet run --project $PROJECT"
set +e
dotnet run --project "$PROJECT"
EXIT_CODE=$?
set -e

if [ "$EXIT_CODE" -ne 0 ]; then
    echo ""
    echo "La aplicación terminó con errores (código $EXIT_CODE)."
    exit "$EXIT_CODE"
fi

echo ""
echo "=== Aplicación cerrada correctamente ==="
echo "La base de datos MySQL local se crea y configura automáticamente en el primer arranque"

case "$(uname -s)" in
    Darwin*)
        echo "Base de datos local: ~/Library/Application Support/Safe-Sale/"
        ;;
    Linux*)
        echo "Base de datos local: ~/.local/share/Safe-Sale/"
        ;;
    CYGWIN*|MINGW*|MSYS*)
        echo "Base de datos local: %LOCALAPPDATA%\\Safe-Sale\\"
        ;;
esac
