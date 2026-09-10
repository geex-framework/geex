#Requires -Version 5.1
<#
.SYNOPSIS
  Build and publish all @geexcode frontend packages under this directory.
.DESCRIPTION
  Uses the same discovery/order as build-all.ps1.
  Publishes from the package root (not ./dist). package.json files is ["dist"],
  so `npm publish ./dist` packs an empty tarball.
.PARAMETER Version
  npm version written into each package before build, e.g. 10.0.7
.PARAMETER Otp
  npm 2FA one-time password. Required when the npm account has OTP enabled.
.PARAMETER SkipBuild
  Skip `npm run build` and publish existing dist/.
.PARAMETER DryRun
  Run `npm publish --dry-run` without uploading.
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [string]$Version,
  [string]$Otp,
  [switch]$SkipBuild,
  [switch]$DryRun
)

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

Write-Host "Packages to publish ($($ordered.Count)) as $Version :" -ForegroundColor Cyan
$ordered | ForEach-Object { Write-Host "  - $_" }
if ($DryRun) {
  Write-Host "DryRun: no package will be uploaded." -ForegroundColor Yellow
}

$failed = @()
$succeeded = @()

foreach ($name in $ordered) {
  $dir = Join-Path $Root $name
  Write-Host ""
  Write-Host "==== Publishing $name ====" -ForegroundColor Cyan
  Push-Location -LiteralPath $dir
  try {
    npm version $Version --no-git-tag-version --allow-same-version
    if ($LASTEXITCODE -ne 0) {
      throw "npm version exited with code $LASTEXITCODE"
    }

    if (-not $SkipBuild) {
      npm run build
      if ($LASTEXITCODE -ne 0) {
        throw "npm run build exited with code $LASTEXITCODE"
      }
    }

    $publishArgs = @('publish', '--access', 'public', '--ignore-scripts')
    if ($DryRun) {
      $publishArgs += '--dry-run'
    }
    if (-not [string]::IsNullOrWhiteSpace($Otp)) {
      $publishArgs += "--otp=$Otp"
    }

    & npm @publishArgs
    if ($LASTEXITCODE -ne 0) {
      throw "npm publish exited with code $LASTEXITCODE"
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

Write-Host "All packages published successfully." -ForegroundColor Green
