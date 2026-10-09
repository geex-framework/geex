#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$NuGetSource = (Join-Path $env:USERPROFILE '.nuget/packages'),
    [string]$SdkImage = 'mcr.microsoft.com/dotnet/sdk:10.0',
    [string]$MongoImage = 'mongo:6.0.25',
    [string]$RedisImage = 'redis:7-alpine',
    [ValidateSet('DEVELOPMENT', 'INDEPENDENT_ACCEPTANCE')]
    [string]$Stage = 'DEVELOPMENT'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$runRoot = Join-Path $repository ".test-evidence/$runId"
$invocationRoot = Join-Path $runRoot 'runner/docker'
$mongoName = "geex-batchload-mongo-$runId".ToLowerInvariant()
$sdkName = "geex-batchload-sdk-$runId".ToLowerInvariant()
$redisName = "geex-batchload-redis-$runId".ToLowerInvariant()
$suites = @(
    @{ id='orm'; project='framework_modules/Geex.MongoDB.Entities/test/Tests.csproj'
        filter='FullyQualifiedName~MongoDB.Entities.Tests.TestBatchLoad|FullyQualifiedName~MongoDB.Entities.Tests.TestLazyQueryMetadata' },
    @{ id='api'; project='framework_modules/Geex.Tests/Geex.Tests.csproj'
        filter='FullyQualifiedName~Geex.Tests.FeatureTests.CoreBatchLoadApiTests' },
    @{ id='enum'; project='framework_modules/Geex.Tests/Geex.Tests.csproj'
        filter='FullyQualifiedName~EnumerationDynamicTests.RegisteredLoginProviders_ParentLookupDiscoversChildrenAndDeserializersReuseThem' }
)
$discovered = @()
$startedAt = [DateTimeOffset]::UtcNow.ToString('o')
$commands = [Collections.Generic.List[object]]::new()
$exitCode = 1
$failure = $null
New-Item -ItemType Directory -Path $invocationRoot -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $runRoot 'scope') | Out-Null

function Save-Json([string]$path, $value) {
    $value | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $runRoot $path) -Encoding utf8
}
function Invoke-Recorded([string]$name, [string]$executable, [string[]]$arguments) {
    $record = [ordered]@{ invocationId = $name; executable = $executable; arguments = $arguments
        startedAt = [DateTimeOffset]::UtcNow.ToString('o'); log = "runner/docker/$name.log" }
    & $executable @arguments 2>&1 | Tee-Object -FilePath (Join-Path $runRoot $record.log) | ForEach-Object {
        if ($_ -notmatch ': warning ') { Write-Host $_ }
    }
    $record.exitCode = $LASTEXITCODE
    $record.finishedAt = [DateTimeOffset]::UtcNow.ToString('o')
    $commands.Add($record)
    Save-Json "runner/docker/$name.json" $record
    if ($record.exitCode -ne 0) { throw "$name failed with exit code $($record.exitCode)" }
}
Push-Location $repository
try {
    Save-Json 'scope/coverage.json' @{
        scopeId = 'batchload-polymorphism'; stage = $Stage; suites = $suites
        rules = @(
            'Resolve GraphQL object types from compiled selections without name inference.',
            'Preserve interface, fragment, directive, nested and offset paging selections.',
            'Apply navigation plans only to compatible concrete entities.',
            'Merge manual and automatic paths without dropping nested manual paths or duplicate loads.',
            'Preserve parent and related generic types, instance-bound sources and cold initialization.',
            'Validate returned data and MongoDB query counts against fixed synthetic fixtures.'
        )
        boundaries = @('Real MongoDB and Redis in isolated Linux containers.', 'GraphQL in-process host and TestServer HTTP API in the Linux SDK container.',
            'No UI changes. No application deployment, production data or final performance acceptance.')
    }
    git status --porcelain=v1 | Set-Content -LiteralPath (Join-Path $runRoot 'worktree-status.txt') -Encoding utf8
    git rev-parse HEAD | Set-Content -LiteralPath (Join-Path $runRoot 'head.txt') -Encoding utf8
    Invoke-Recorded 'snapshot' 'tar' @('-cf',(Join-Path $runRoot 'source.tar'),'--exclude=bin','--exclude=obj',
        '--exclude=.git','--exclude=node_modules','framework_modules','global.json','scripts/testing/test-batchload.ps1')
    $baselineId = (Get-FileHash -LiteralPath (Join-Path $runRoot 'source.tar') -Algorithm SHA256).Hash.ToLowerInvariant()
    Invoke-Recorded 'mongo-start' 'docker' @('run','--detach','--name',$mongoName,$MongoImage,'--replSet','rs0','--bind_ip_all')
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        & docker exec $mongoName mongosh --quiet --eval 'db.runCommand({ping:1}).ok' *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'Isolated MongoDB did not become ready.' }
    Invoke-Recorded 'mongo-replica' 'docker' @('exec',$mongoName,'mongosh','--quiet','--eval',
        'rs.initiate({_id:"rs0",members:[{_id:0,host:"localhost:27017"}]})')
    Invoke-Recorded 'redis-start' 'docker' @('run','--detach','--name',$redisName,'--network',"container:$mongoName",$RedisImage)
    Invoke-Recorded 'sdk-start' 'docker' @('run','--detach','--name',$sdkName,'--network',"container:$mongoName",
        '--mount',"type=bind,source=$runRoot,target=/evidence",
        '--mount',"type=bind,source=$((Resolve-Path -LiteralPath $NuGetSource).Path),target=/packages,readonly",
        $SdkImage,'sleep','infinity')
    Invoke-Recorded 'workspace' 'docker' @('exec',$sdkName,'mkdir','/workspace')
    Invoke-Recorded 'extract' 'docker' @('exec',$sdkName,'tar','-xf','/evidence/source.tar','-C','/workspace')
    Invoke-Recorded 'sdk-info' 'docker' @('exec','-w','/workspace',$sdkName,'dotnet','--info')
    $builtProjects = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($suite in $suites) {
        $suiteId = $suite.id
        if ($builtProjects.Add($suite.project)) {
            Invoke-Recorded "$suiteId-restore" 'docker' @('exec','-w','/workspace',$sdkName,'dotnet','restore',$suite.project,
                '--source','/packages','--source','https://api.nuget.org/v3/index.json','-p:NuGetAudit=false','--verbosity','minimal')
            Invoke-Recorded "$suiteId-build" 'docker' @('exec','-w','/workspace',$sdkName,'dotnet','build',$suite.project,
                '--no-restore','--configuration','Release','--verbosity','minimal')
        }
        Invoke-Recorded "$suiteId-discover" 'docker' @('exec','-w','/workspace',$sdkName,'dotnet','test',$suite.project,
            '--no-build','--no-restore','--configuration','Release','--filter',$suite.filter,'--list-tests')
        $suiteCases = @(Get-Content -LiteralPath (Join-Path $invocationRoot "$suiteId-discover.log") |
            Where-Object { $_ -match '^\s{4}\S' } | ForEach-Object { "${suiteId}:$($_.Trim())" })
        if ($suiteCases.Count -eq 0) { throw "No planned test cases discovered for $suiteId." }
        $discovered += $suiteCases
    }
    Save-Json 'scope/planned-instances.json' $discovered
    foreach ($suite in $suites) {
        Invoke-Recorded "$($suite.id)-test" 'docker' @('exec','-w','/workspace',$sdkName,'dotnet','test',$suite.project,
            '--no-build','--no-restore','--configuration','Release','--filter',$suite.filter,
            '--logger',"trx;LogFileName=$($suite.id).trx",'--results-directory','/evidence/runner/docker')
    }
    $exitCode = 0
} catch {
    $failure = $_.Exception.Message
    Write-Host $failure
} finally {
    if ((& docker ps -aq --filter "name=^/$mongoName$")) {
        & docker logs $mongoName 2>&1 | Set-Content -LiteralPath (Join-Path $invocationRoot 'mongo.log') -Encoding utf8
        & docker inspect --format '{{.Image}}' $mongoName | Set-Content -LiteralPath (Join-Path $invocationRoot 'mongo-image.txt')
    }
    if ((& docker ps -aq --filter "name=^/$sdkName$")) {
        & docker inspect --format '{{.Image}}' $sdkName | Set-Content -LiteralPath (Join-Path $invocationRoot 'sdk-image.txt')
        & docker rm --force $sdkName | Out-Null
    }
    if ((& docker ps -aq --filter "name=^/$redisName$")) {
        & docker logs $redisName 2>&1 | Set-Content -LiteralPath (Join-Path $invocationRoot 'redis.log') -Encoding utf8
        & docker inspect --format '{{.Image}}' $redisName | Set-Content -LiteralPath (Join-Path $invocationRoot 'redis-image.txt')
        & docker rm --force $redisName | Out-Null
    }
    if ((& docker ps -aq --filter "name=^/$mongoName$")) { & docker rm --force $mongoName | Out-Null }
    Pop-Location
}
$results = @()
foreach ($suite in $suites) {
    $trxPath = Join-Path $invocationRoot "$($suite.id).trx"
    if (Test-Path -LiteralPath $trxPath) {
        [xml]$trx = Get-Content -LiteralPath $trxPath -Raw
        $results += @($trx.TestRun.Results.UnitTestResult | ForEach-Object {
            [ordered]@{ instanceId = "$($suite.id):$($_.testName)"; status = if ($_.outcome -eq 'Passed') { 'PASS' } else { 'FAIL' }
                outcome = $_.outcome; duration = $_.duration; evidence = "runner/docker/$($suite.id).trx"; message = $_.Output.ErrorInfo.Message }
        })
    }
}
$passed = @($results | Where-Object status -eq PASS).Count
$plannedIds = [Collections.Generic.HashSet[string]]::new([string[]]$discovered, [StringComparer]::Ordinal)
$actualIds = [Collections.Generic.HashSet[string]]::new([string[]]@($results | ForEach-Object instanceId), [StringComparer]::Ordinal)
$identitiesMatch = $discovered.Count -gt 0 -and $plannedIds.Count -eq $discovered.Count -and
    $actualIds.Count -eq $results.Count -and $plannedIds.SetEquals($actualIds)
if (!$results.Count -or $passed -ne $results.Count -or !$identitiesMatch) { $exitCode = 1 }
$manifest = [ordered]@{ runId=$runId; scopeId='batchload-polymorphism'; stage=$Stage; baselineId=$baselineId
    startedAt=$startedAt; finishedAt=[DateTimeOffset]::UtcNow.ToString('o'); commands=$commands
    plannedInstanceIds=$discovered; discovered=$discovered.Count; executed=$results.Count; passed=$passed
    exitCode=$exitCode; failure=$failure; results=$results
    validationBoundary='Scoped regression with real MongoDB/Redis and GraphQL HTTP API in Linux Docker; not final project acceptance.'
}
Save-Json 'manifest.json' $manifest
Save-Json 'gate-result.json' @{ exitCode=$exitCode; discovered=$discovered.Count; executed=$results.Count; passed=$passed
    checks=@{ build=(@($commands | Where-Object { $_.invocationId -like '*-build' -and $_.exitCode -eq 0 }).Count -eq @($suites.project | Select-Object -Unique).Count)
        discovery=$identitiesMatch; results=($results.Count -gt 0 -and $passed -eq $results.Count) }
    failure=$failure }
$results | ForEach-Object { $_ | ConvertTo-Json -Depth 12 -Compress } | Set-Content -LiteralPath (Join-Path $runRoot 'cases.jsonl') -Encoding utf8
@"
# BatchLoad regression
- Run: $runId
- Baseline: $baselineId
- Stage: $Stage
- Result: $passed/$($results.Count), exit code $exitCode
- Failure: $failure
- Scope: scope/coverage.json and scope/planned-instances.json
- Raw build/test output and TRX: runner/docker/
- Boundary: real MongoDB/Redis and GraphQL HTTP API in isolated Linux containers. No deployed application/UI or final performance acceptance.
"@ | Set-Content -LiteralPath (Join-Path $runRoot 'report.md') -Encoding utf8
Get-ChildItem -LiteralPath $runRoot -Recurse -File | Where-Object Name -ne 'artifacts.json' | ForEach-Object {
    @{ path=[IO.Path]::GetRelativePath($runRoot,$_.FullName); bytes=$_.Length; sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runRoot 'artifacts.json') -Encoding utf8
Write-Host "EVIDENCE: $runRoot"
exit $exitCode
