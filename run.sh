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
    # Este comando hará restore automáticamente e incluirá MySqlConnector
    dotnet run --project "$projectPath"
    
    echo ""
    echo "=== Aplicación cerrada ==="
    echo "La base de datos MySQL local se crea y configura automáticamente en el primer arranque"
    
    # Información adicional por plataforma
    case "$CASE_LOWER" in
        darwin*)
            echo "🍏 Ejecutado en macOS - Base de datos MySQL local en: ~/Library/Application Support/Safe-Sale/"
            ;;
        linux*)
            echo "🐧 Ejecutado en Linux - Base de datos MySQL local en: ~/.local/share/Safe-Sale/"
            ;;
        cygwin*|msys*|win32*)
            echo "🪟 Ejecutado en Windows - Base de datos MySQL local en: %LOCALAPPDATA%\\Safe-Sale\\"
            ;;
    esac
else
    echo "❌ Error: No se encontró el proyecto $projectPath"
    echo "Asegúrate de ejecutar este script desde la carpeta Safe-Sale"
    exit 1
fi