# Script de ejecución para Safe-Sale (Windows)
# Uso: .\run.ps1

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $Root "src\SS\SS.csproj"

Write-Host "=== Safe-Sale - Iniciando proyecto ===" -ForegroundColor Green

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: 'dotnet' no está instalado o no está en el PATH." -ForegroundColor Red
    Write-Host "Instalá el SDK de .NET 10: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $ProjectPath)) {
    Write-Host "Error: no se encontró el proyecto en $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Ejecutando: dotnet run --project $ProjectPath" -ForegroundColor Cyan
dotnet run --project $ProjectPath

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "La aplicación terminó con errores (código $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Aplicación cerrada correctamente ===" -ForegroundColor Green
Write-Host "La base de datos MySQL local se crea y configura automáticamente en el primer arranque"
