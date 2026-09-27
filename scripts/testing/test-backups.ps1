param(
    [ValidateSet('All', 'Backend', 'Frontend')]
    [string]$Scope = 'All'
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$evidence = Join-Path $repository ('.test-evidence/database-backup/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $evidence -Force | Out-Null

function Invoke-Check([string]$Name, [scriptblock]$Command) {
    $log = Join-Path $evidence ($Name + '.log')
    & $Command *> $log
    if ($LASTEXITCODE -ne 0) { throw "$Name failed. See $log." }
    Write-Host "$Name passed."
}

Push-Location $repository
try {
    if ($Scope -in @('All', 'Backend')) {
        $project = 'framework_modules/Geex.Extensions.Backups/tests/Geex.Extensions.Backups.Tests.csproj'
        Invoke-Check 'backend-build' { dotnet build $project -m:1 -p:UseAppHost=false }
        $env:GEEX_BACKUP_TEST_EVIDENCE = $evidence
        Invoke-Check 'backend-tests' { dotnet test $project --no-build --no-restore --results-directory $evidence --logger 'trx;LogFileName=results.trx' }
        Invoke-Check 'backend-package' {
            dotnet pack framework_modules/Geex.Extensions.Backups/src/Geex.Extensions.Backups.csproj --configuration Debug --no-build --no-restore --output (Join-Path $evidence 'packages')
        }
    }
    if ($Scope -in @('All', 'Frontend')) {
        Invoke-Check 'frontend-build' { pnpm --dir frontend_libs --filter '@geexcode/geex-extensions-backups...' --filter '@geexcode/geex-extensions-messaging...' run build }
        Invoke-Check 'page-build' { pnpm --dir frontend_libs --filter '@geexcode/geex-extensions-backups' run build:page }
        Invoke-Check 'frontend-tests' { pnpm --dir frontend_libs --filter '@geexcode/geex-extensions-backups' test }
        # Schematics executes JavaScript directly and has no build step.
        Invoke-Check 'schematics-tests' { pnpm --dir frontend_libs --filter '@geexcode/geex-module-schematics' test }
    }
    Write-Host "Evidence: $evidence"
}
finally { Pop-Location }
