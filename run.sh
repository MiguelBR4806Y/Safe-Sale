#!/bin/bash
# Script multiplataforma para Safe-Sale (Windows, macOS, Linux)
# Autor: Save-Sale Project

# Detectar el sistema operativo
OS=$(uname -s)
CASE_LOWER=$(echo "$OS" | tr '[:upper:]' '[:lower:]')

echo "=== Safe-Sale - Iniciando proyecto ==="
echo "Sistema operativo detectado: $OS"

# Verificar si el proyecto existe
projectPath="src/SS/SS.csproj"

if [ -f "$projectPath" ]; then
    echo "Ejecutando: dotnet run --project $projectPath"
    
    # Ejecutar la aplicación
    # Este comando hará restore automáticamente e incluirá Microsoft.Data.Sqlite
    dotnet run --project "$projectPath"
    
    echo ""
    echo "=== Aplicación cerrada ==="
    echo "La base de datos SQLite se creó automáticamente"
    
    # Información adicional por plataforma
    case "$CASE_LOWER" in
        darwin*)
            echo "🍏 Ejecutado en macOS - Base de datos SQLite en: ./src/SS/bin/Debug/net10.0/"
            ;;
        linux*)
            echo "🐧 Ejecutado en Linux - Base de datos SQLite en: ./src/SS/bin/Debug/net10.0/"
            ;;
        cygwin*|msys*|win32*)
            echo "🪟 Ejecutado en Windows - Base de datos SQLite en: ./src/SS/bin/Debug/net10.0/"
            ;;
    esac
else
    echo "❌ Error: No se encontró el proyecto $projectPath"
    echo "Asegúrate de ejecutar este script desde la carpeta Safe-Sale"
    exit 1
fi