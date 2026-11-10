#requires -Version 5.1
# Safe-Sale - Publicación (Windows x64)
# Requiere: dotnet, vpk (dotnet tool install --global vpk), gh (winget install GitHub.cli)

$ErrorActionPreference = "Stop"

$Root        = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Join-Path $Root "src\SS\SS.csproj"
$PublishDir  = Join-Path $Root "publish"
$ReleaseDir  = Join-Path $Root "Releases\win-x64"
$PackId      = "SafeSale"
$MainExe     = "SafeSale.exe"
$Runtime     = "win-x64"
$Channel     = "win"

$Version = $null
$match = Select-String -Path $ProjectPath -Pattern "<Version>(.*?)</Version>" | Select-Object -First 1
if ($match) { $Version = $match.Matches[0].Groups[1].Value.Trim() }

if (-not $Version) {
    Write-Host "Error: No se pudo leer <Version> del .csproj" -ForegroundColor Red
    exit 1
}

$Tag = "v$Version-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Herramientas requeridas
foreach ($tool in @("dotnet", "vpk", "gh")) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        Write-Host "Error: falta '$tool'. Instalalo antes de publicar (dotnet tool install --global vpk / winget install GitHub.cli)." -ForegroundColor Red
        exit 1
    }
}

Write-Host "=== Safe-Sale - Publicación Windows ===" -ForegroundColor Cyan
Write-Host "Versión: $Version" -ForegroundColor Yellow
Write-Host "Canal:   $Channel" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host "Tag:     $Tag" -ForegroundColor Yellow

# 1. Verificar / descargar el motor MySQL incluido
$MysqlVersion = $env:SAFESALE_MYSQL_VERSION
if (-not $MysqlVersion) { $MysqlVersion = "8.4.5" }

$MysqlExe = Join-Path $Root "vendor\mysql\$Runtime\bin\mysqld.exe"
if (-not (Test-Path $MysqlExe)) {
    Write-Host "Falta el motor MySQL para $Runtime. Descargando MySQL $MysqlVersion..." -ForegroundColor Yellow
    & (Join-Path $Root "scripts\fetch-mysql-vendor.ps1") -Version $MysqlVersion -Runtime $Runtime
    if (-not (Test-Path $MysqlExe)) {
        Write-Host "Error: no se pudo obtener el motor MySQL." -ForegroundColor Red
        exit 1
    }
}

# 2. Publicar la aplicación
Write-Host "`n[1/4] Publicando aplicación..." -ForegroundColor Green
$PublishTarget = Join-Path $PublishDir $Runtime
if (Test-Path $PublishTarget) { Remove-Item $PublishTarget -Recurse -Force }
dotnet publish $ProjectPath -c Release -r $Runtime --self-contained true -o $PublishTarget
if ($LASTEXITCODE -ne 0) { Write-Host "Error al publicar" -ForegroundColor Red; exit 1 }

# 3. Empaquetar con Velopack
Write-Host "`n[2/4] Empaquetando instalador..." -ForegroundColor Green
if (Test-Path $ReleaseDir) { Remove-Item $ReleaseDir -Recurse -Force }
vpk pack --packId $PackId --packVersion $Version --packDir (Join-Path $PublishDir $Runtime) `
    --mainExe $MainExe --outputDir $ReleaseDir --channel $Channel `
    --icon (Join-Path $Root "src\SS\Assets\SS-ico.ico") `
    --packAuthors "Safe-Sale Team" --yes
if ($LASTEXITCODE -ne 0) { Write-Host "Error al empaquetar" -ForegroundColor Red; exit 1 }

$setup = Get-ChildItem -Path $ReleaseDir -Filter "*-Setup.exe" -File | Select-Object -First 1
if ($setup) {
    $target = Join-Path $ReleaseDir "SS-Installer.exe"
    if (Test-Path $target) { Remove-Item $target -Force }
    Rename-Item -Path $setup.FullName -NewName "SS-Installer.exe" -Force
    Write-Host "Instalador renombrado a SS-Installer.exe" -ForegroundColor Yellow
}

# 4. Crear Release en GitHub
Write-Host "`n[3/4] Creando Release en GitHub..." -ForegroundColor Green
gh release create $Tag --title "Safe-Sale v$Version (Windows)" --notes "Versión $Version para Windows x64." --draft
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al crear release. Instala/autentica gh: winget install GitHub.cli ; gh auth login" -ForegroundColor Red
    exit 1
}

# 5. Subir todos los artefactos (feed de actualizaciones incluido)
Write-Host "`n[4/4] Subiendo artefactos al Release..." -ForegroundColor Green
Get-ChildItem -Path $ReleaseDir -File | ForEach-Object {
    gh release upload $Tag $_.FullName --clobber
    Write-Host "Subido: $($_.Name)" -ForegroundColor Green
}

gh release edit $Tag --draft=false

Write-Host "`n=== ¡Publicación completada! ===" -ForegroundColor Cyan
Write-Host "URL: https://github.com/MiguelBR4806Y/Safe-Sale/releases/tag/$Tag" -ForegroundColor Yellow
