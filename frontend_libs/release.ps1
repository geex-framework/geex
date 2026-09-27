#Requires -Version 7.0
[CmdletBinding()]
param(
  [Parameter(Mandatory)]
  [ValidatePattern('^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(\.(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*)?$')]
  [string]$Version,
  [Parameter(Mandatory)]
  [ValidateSet('Prepare', 'Verify')]
  [string]$Mode
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
  $listing = & pnpm -r list --depth -1 --json
  if ($LASTEXITCODE -ne 0) { throw 'Could not list frontend workspace packages.' }
  $packages = @($listing | ConvertFrom-Json | Where-Object { -not $_.private })
  if ($packages.Count -eq 0) { throw 'No publishable frontend packages found.' }
  $names = @($packages.name)

  foreach ($package in $packages) {
    $manifestPath = Join-Path $package.path 'package.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
    if ($Mode -eq 'Prepare') {
      $manifest.version = $Version
      $manifest.repository = @{
        type = 'git'
        url = 'git+https://github.com/geex-framework/geex.git'
        directory = "frontend_libs/$(Split-Path $package.path -Leaf)"
      }
      # Exact internal peers also allow prereleases to resolve to this same release.
      foreach ($section in @('dependencies', 'optionalDependencies', 'peerDependencies')) {
        if ($manifest[$section]) {
          foreach ($name in @($manifest[$section].Keys)) {
            if ($names -contains $name) { $manifest[$section][$name] = $Version }
          }
        }
      }
      [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 100) + "`n")
      Write-Host "Prepared $($manifest.name)@$Version"
      continue
    }

    if ($manifest.version -cne $Version) { throw "Version mismatch in $manifestPath" }
    Push-Location $package.path
    try {
      $packed = & npm pack --dry-run --json --ignore-scripts
      if ($LASTEXITCODE -ne 0) { throw "Could not inspect $($manifest.name)." }
      $archive = @($packed | ConvertFrom-Json)[0]
      $files = @($archive.files.path)
      if ($archive.version -cne $Version) { throw "Archive version mismatch: $($manifest.name)" }
      $entries = @($manifest.main, $manifest.module, $manifest.typings, $manifest.schematics)
      if ($manifest.bin -is [System.Collections.IDictionary]) { $entries += @($manifest.bin.Values) }
      elseif ($manifest.bin) { $entries += $manifest.bin }
      if ($manifest.exports) {
        $collect = {
          param($value)
          if ($value -is [string]) { $value }
          elseif ($value -is [System.Collections.IDictionary]) {
            foreach ($item in $value.Values) { & $collect $item }
          }
        }
        $entries += @(& $collect $manifest.exports)
      }
      foreach ($entry in ($entries | Where-Object { $_ } | Select-Object -Unique)) {
        if ($files -notcontains ($entry -replace '^\./', '')) {
          throw "Missing published entry '$entry' in $($manifest.name)."
        }
      }
      if ($manifest.scripts.build) {
        $dist = Get-Content dist/package.json -Raw | ConvertFrom-Json
        if ($dist.version -cne $Version) { throw "Stale build version in $($manifest.name)." }
        if (-not ($files | Where-Object { $_ -like 'dist/*.mjs' })) {
          throw "Missing compiled JavaScript in $($manifest.name)."
        }
      }
      Write-Host "Verified $($manifest.name)@$Version ($($files.Count) files)"
    }
    finally { Pop-Location }
  }
}
finally { Pop-Location }
