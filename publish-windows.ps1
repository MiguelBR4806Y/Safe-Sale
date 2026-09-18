# Safe-Sale - Script de Publicación (Windows)
# Requiere: dotnet, vpk (dotnet tool install --global vpk), gh (winget install GitHub.cli)

$ErrorActionPreference = "Stop"

$PublishDir = ".\publish"
$ProjectPath = ".\src\SS\SS.csproj"
$ReleaseDir = ".\Releases"

# Leer versión del .csproj
$Version = (Select-String -Path $ProjectPath -Pattern '<Version>(.*?)</Version>' | ForEach-Object { $_.Matches.Groups[1].Value } | Select-Object -First 1).Trim()

if (-not $Version) {
    Write-Host "Error: No se pudo leer la versión del .csproj" -ForegroundColor Red
    exit 1
}

Write-Host "=== Safe-Sale - Publicación Windows ===" -ForegroundColor Cyan
Write-Host "Versión: $Version" -ForegroundColor Yellow

# Generar tag único con timestamp
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$Tag = "v$Version-$Timestamp"
Write-Host "Tag: $Tag" -ForegroundColor Yellow

# 1. Publicar la app
Write-Host "`n[1/4] Publicando aplicación..." -ForegroundColor Green
dotnet publish $ProjectPath -c Release -r win-x64 --self-contained -o "$PublishDir\win-x64"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al publicar" -ForegroundColor Red
    exit 1
}

# 2. Empaquetar con Velopack
Write-Host "`n[2/4] Empaquetando instalador..." -ForegroundColor Green
vpk pack --packId SafeSale --packVersion $Version --packDir "$PublishDir\win-x64" --mainExe SafeSale.exe

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al empaquetar" -ForegroundColor Red
    exit 1
}

# Renombrar instalador
$oldName = Get-ChildItem -Path $ReleaseDir -Filter "SafeSale-win-Setup.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($oldName) {
    $newName = Join-Path $oldName.Directory "SS-Installer.exe"
    Rename-Item -Path $oldName.FullName -NewName $newName -Force
    Write-Host "Renombrado: $($oldName.Name) → SS-Installer.exe" -ForegroundColor Yellow
}

# 3. Crear Release en GitHub
Write-Host "`n[3/4] Creando Release en GitHub..." -ForegroundColor Green
gh release create "$Tag" --title "Safe-Sale v$Version" --notes "Versión $Version" --draft

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al crear release. Asegúrate de tener gh CLI instalado y autenticado." -ForegroundColor Red
    Write-Host "Instalar gh: winget install GitHub.cli" -ForegroundColor Yellow
    Write-Host "Autenticar: gh auth login" -ForegroundColor Yellow
    exit 1
}

# 4. Subir archivos al Release
Write-Host "`n[4/4] Subiendo archivos al Release..." -ForegroundColor Green
$setupExe = Get-ChildItem -Path $ReleaseDir -Filter "SS-Installer.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$nupkg = Get-ChildItem -Path $ReleaseDir -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($setupExe) {
    gh release upload "$Tag" $setupExe.FullName --clobber
    Write-Host "Subido: $($setupExe.Name)" -ForegroundColor Green
}

if ($nupkg) {
    gh release upload "$Tag" $nupkg.FullName --clobber
    Write-Host "Subido: $($nupkg.Name)" -ForegroundColor Green
}

# 5. Publicar el Release (quitar draft)
gh release edit "$Tag" --draft=false

Write-Host "`n=== ¡Publicación completada! ===" -ForegroundColor Cyan
Write-Host "Los usuarios recibirán la actualización automáticamente." -ForegroundColor Green
Write-Host "URL: https://github.com/MiguelBR4806Y/Safe-Sale/releases/tag/$Tag" -ForegroundColor Yellow
