#Requires -Version 5.1
<#
.SYNOPSIS
  Build all frontend packages under this directory that define a "build" script.
.DESCRIPTION
  Discovers direct child folders with package.json containing scripts.build,
  then builds them in dependency order via `npm run build`.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot

$PreferredOrder = @(
  'geex-angular',
  'geex-extensions-settings',
  'geex-extensions-multi-tenant',
  'geex-extensions-identity',
  'geex-extensions-authentication',
  'geex-extensions-authorization',
  'geex-extensions-authentication-wechat',
  'geex-extensions-mocking'
)

# Packages that declare build but currently cannot build (e.g. missing src).
$Exclude = @(
  'geex-cli'
)

function Test-HasBuildScript {
  param([string]$PackageJsonPath)
  try {
    $pkg = Get-Content -LiteralPath $PackageJsonPath -Raw | ConvertFrom-Json
    return $null -ne $pkg.scripts -and $null -ne $pkg.scripts.build
  }
  catch {
    Write-Warning "Failed to parse $PackageJsonPath : $_"
    return $false
  }
}

$discovered = @(
  Get-ChildItem -LiteralPath $Root -Directory |
    Where-Object {
      $pkgPath = Join-Path $_.FullName 'package.json'
      (Test-Path -LiteralPath $pkgPath) -and (Test-HasBuildScript -PackageJsonPath $pkgPath)
    } |
    ForEach-Object { $_.Name } |
    Where-Object { $Exclude -notcontains $_ }
)

if ($Exclude.Count -gt 0) {
  Write-Host "Excluded: $($Exclude -join ', ')" -ForegroundColor Yellow
}

if ($discovered.Count -eq 0) {
  Write-Error "No packages with a build script found under $Root"
  exit 1
}

$ordered = [System.Collections.Generic.List[string]]::new()
foreach ($name in $PreferredOrder) {
  if ($discovered -contains $name) {
    [void]$ordered.Add($name)
  }
}
foreach ($name in ($discovered | Sort-Object)) {
  if (-not ($ordered -contains $name)) {
    [void]$ordered.Add($name)
  }
}

Write-Host "Packages to build ($($ordered.Count)):" -ForegroundColor Cyan
$ordered | ForEach-Object { Write-Host "  - $_" }

$failed = @()
$succeeded = @()

foreach ($name in $ordered) {
  $dir = Join-Path $Root $name
  Write-Host ""
  Write-Host "==== Building $name ====" -ForegroundColor Cyan
  Push-Location -LiteralPath $dir
  try {
    npm run build
    if ($LASTEXITCODE -ne 0) {
      throw "npm run build exited with code $LASTEXITCODE"
    }
    $succeeded += $name
    Write-Host "OK: $name" -ForegroundColor Green
  }
  catch {
    Write-Host "FAIL: $name - $_" -ForegroundColor Red
    $failed += $name
    break
  }
  finally {
    Pop-Location
  }
}

Write-Host ""
Write-Host "==== Summary ====" -ForegroundColor Cyan
Write-Host "Succeeded: $($succeeded.Count)  $($succeeded -join ', ')"
if ($failed.Count -gt 0) {
  Write-Host "Failed:    $($failed.Count)  $($failed -join ', ')" -ForegroundColor Red
  exit 1
}

Write-Host "All packages built successfully." -ForegroundColor Green
