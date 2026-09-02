# Script para ejecutar Safe-Sale con SQLite local
# Autor: Save-Sale Project

# Verificar si el proyecto existe
$projectPath = "src\SS\SS.csproj"

Write-Host "=== Safe-Sale - Iniciando proyecto ===" -ForegroundColor Green

if (Test-Path $projectPath) {
    Write-Host "Ejecutando: dotnet run --project $projectPath" -ForegroundColor Cyan
    
    # Ejecutar la aplicación
    # Este comando hará restore automáticamente e incluirá Microsoft.Data.Sqlite
    dotnet run --project $projectPath
    
    Write-Host ""
    Write-Host "=== Aplicación cerrada ===" -ForegroundColor Green
    Write-Host "La base de datos SQLite se creó automáticamente en la carpeta bin/Debug/net10.0/"
} else {
    Write-Host "Error: No se encontró el proyecto" -ForegroundColor Red
    Write-Host "Asegúrate de ejecutar este script desde la carpeta Safe-Sale"
}