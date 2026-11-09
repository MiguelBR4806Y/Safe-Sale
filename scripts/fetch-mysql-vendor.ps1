#requires -Version 5.1
<#
.SYNOPSIS
  Descarga MySQL Community portable a vendor/mysql/<rid> para empaquetarlo con Safe-Sale.

.DESCRIPTION
  Los binarios NO se commitean al repo. Cada plataforma se descarga antes de publicar.
  Si la URL por defecto deja de ser válida, pasá -Url con el link oficial del ZIP.

.EXAMPLE
  .\scripts\fetch-mysql-vendor.ps1
  .\scripts\fetch-mysql-vendor.ps1 -Version "8.4.5" -Url "https://dev.mysql.com/get/Downloads/MySQL-8.4/mysql-8.4.5-winx64.zip"
#>
param(
    [string]$Version = "8.4.5",
    [string]$Runtime = "win-x64",
    [string]$Url = ""
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Dest = Join-Path $Root "vendor\mysql\$Runtime"
$Exe = Join-Path $Dest "bin\mysqld.exe"

if (Test-Path $Exe) {
    Write-Host "Ya existe el motor en $Dest"
    exit 0
}

if (-not $Url) {
    $Url = "https://dev.mysql.com/get/Downloads/MySQL-8.4/mysql-$Version-winx64.zip"
}

Write-Host "Descargando MySQL Community $Version ($Runtime)"
Write-Host "URL: $Url"

$tempZip = Join-Path $env:TEMP "safesale-mysql-$Version.zip"
$tempDir = Join-Path $env:TEMP "safesale-mysql-$Version"

Invoke-WebRequest -Uri $Url -OutFile $tempZip -UseBasicParsing

if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
Expand-Archive -Path $tempZip -DestinationPath $tempDir -Force

$inner = Get-ChildItem -Path $tempDir -Directory | Select-Object -First 1
if (-not $inner) { throw "El ZIP no contiene una carpeta raíz." }

New-Item -ItemType Directory -Path (Split-Path $Dest -Parent) -Force | Out-Null
if (Test-Path $Dest) { Remove-Item $Dest -Recurse -Force }
Move-Item $inner.FullName $Dest

Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue

if (-not (Test-Path $Exe)) { throw "No se encontró bin\mysqld.exe en el paquete descargado." }

Write-Host "OK: $Dest"
