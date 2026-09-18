# Safe-Sale - Script de Publicación (Windows)
# Requiere: dotnet, Velopack (dotnet tool install --global Velopack)

$ErrorActionPreference = "Stop"

$Version = "1.0.0"
$PublishDir = ".\publish"
$ProjectPath = ".\src\SS\SS.csproj"

Write-Host "=== Safe-Sale - Publicación Windows ===" -ForegroundColor Cyan
Write-Host "Versión: $Version" -ForegroundColor Yellow

# 1. Publicar la app
Write-Host "`n[1/3] Publicando aplicación..." -ForegroundColor Green
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained -o "$PublishDir\win-x64"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al publicar" -ForegroundColor Red
    exit 1
}

# 2. Empaquetar con Velopack
Write-Host "`n[2/3] Empaquetando instalador..." -ForegroundColor Green
vpk pack --packId SafeSale --packVersion $Version --packDir "$PublishDir\win-x64" --mainExe SafeSale.exe

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al empaquetar" -ForegroundColor Red
    exit 1
}

# 3. Publicar en GitHub Releases
Write-Host "`n[3/3] Publicando en GitHub Releases..." -ForegroundColor Green
vpk publish --repoUrl "https://github.com/MiguelBR4806Y/Safe-Sale" --tag "v$Version"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al publicar. Asegúrate de tener gh CLI instalado y autenticado." -ForegroundColor Red
    Write-Host "Instalar gh: winget install GitHub.cli" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n=== ¡Publicación completada! ===" -ForegroundColor Cyan
Write-Host "Los usuarios recibirán la actualización automáticamente." -ForegroundColor Green
