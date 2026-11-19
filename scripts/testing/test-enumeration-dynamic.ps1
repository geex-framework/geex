#requires -Version 7.0
param(
    [ValidateSet('DEVELOPMENT', 'ACCEPTANCE')][string]$Stage = 'DEVELOPMENT',
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Executor,
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = 'framework_modules/Geex.Tests/Geex.Tests.csproj'
$testSource = 'framework_modules/Geex.Tests/UnitTests/EnumerationDynamicTests.cs'
$scopeId = 'enumeration-dynamic'
$scopeDocument = 'docs/enumeration.md'
$schemaSource = Join-Path $PSScriptRoot 'schemas/enumeration-dynamic-evidence.schema.json'
$sourcePaths = @('framework_modules/Geex.Abstractions/src/Enumeration.cs',
    'framework_modules/Geex.Abstractions/src/Enumeration.Definition.cs',
    'framework_modules/Geex.Abstractions/src/Authorization/AppPermission.cs', $testSource,
    'framework_modules/Geex.Abstractions/src/LoginProviderEnum.cs',
    'framework_modules/Geex.Abstractions/src/Utilities/EnumerationReflectionCache.cs',
    'framework_modules/Geex.Abstractions/src/Extensions/System.Text.Json.cs',
    'framework_modules/Geex.Abstractions/src/Json/EnumerationConverter.cs',
    'framework_modules/Geex.Abstractions/src/Gql/GeexTypeInspector.cs',
    'framework_modules/Geex.Abstractions/src/Extensions/Microsoft.Extensions.DependencyInjection.cs',
    'framework_modules/Geex.Abstractions/src/Bson/EnumerationSerializer.cs',
    'framework_modules/Geex.Abstractions/src/Bson/EnumerationRepresentationConvention.cs',
    'framework_modules/Geex.MongoDB.Entities/src/Core/IEnumeration.cs',
    'framework_modules/Geex.Extensions.Authentication.Wechat/src/WechatLoginProviders.cs',
    'framework_modules/Geex.Extensions.Identity/src/IdentityPermission.cs',
    'framework_modules/Geex.Extensions.MultiTenant/src/MultiTenantPermission.cs',
    'framework_modules/Geex.Extensions.Authorization/src/AuthorizationPermission.cs',
    'framework_modules/Geex.Extensions.Settings/src/Abstractions/SettingsPermission.cs',
    'framework_modules/Geex.Extensions.Backups/src/BackupsPermission.cs',
    'framework_modules/Geex.Extensions.AuditLogs/src/AuditLogsPermission.cs',
    'docs/enumeration.md')
$testSnapshotRef = "environment/sources/$([Array]::IndexOf($sourcePaths, $testSource))-$(Split-Path $testSource -Leaf)"
$startedAt = [DateTimeOffset]::UtcNow.ToString('o')
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N')
$runRoot = Join-Path $repository ".test-evidence/$runId"
$schemaRef = 'environment/evidence.schema.json'
$commands = [Collections.Generic.List[object]]::new()
$issues = [Collections.Generic.List[string]]::new()
$results = [Collections.Generic.List[object]]::new()
$scope = @()
$baselineId = 'unavailable'
$build = $null
$test = $null
$baseline = $null
$inputBefore = @()
$testArtifacts = @()
$toolFailure = $false
$exitCode = 2

function Save-Json([string]$RelativePath, $Value) {
    $Value | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath (Join-Path $runRoot $RelativePath) -Encoding utf8
}
function Save-JsonLines([string]$RelativePath, [object[]]$Values) {
    @($Values | ForEach-Object { ConvertTo-Json -InputObject $_ -Depth 50 -Compress }) |
        Set-Content -LiteralPath (Join-Path $runRoot $RelativePath) -Encoding utf8
}
function Get-Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Get-TextHash([string]$Value) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Value))).ToLowerInvariant()
}
function Relative-Path([string]$Path) { [IO.Path]::GetRelativePath($repository, $Path).Replace('\', '/') }
function File-Identity([string]$Path) {
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        [ordered]@{ path = Relative-Path $Path; sha256 = Get-Hash $Path; sizeBytes = (Get-Item -LiteralPath $Path).Length }
    }
}
function Invoke-Recorded([string]$Name, [string]$File, [string[]]$Arguments) {
    $invocationId = $Name + '-' + [Guid]::NewGuid().ToString('N')
    $outputDirectory = "runner/$invocationId"
    New-Item -ItemType Directory -Path (Join-Path $runRoot $outputDirectory) | Out-Null
    $record = [ordered]@{
        invocationId = $invocationId; name = $Name; executable = $File; arguments = $Arguments
        workingDirectory = $repository; outputDirectory = $outputDirectory
        logRef = "$outputDirectory/output.log"; commandRef = "$outputDirectory/command.json"
        startedAt = [DateTimeOffset]::UtcNow.ToString('o'); endedAt = $null; exitCode = $null
    }
    Save-Json $record.commandRef $record
    try {
        & $File @Arguments *> (Join-Path $runRoot $record.logRef)
        $record.exitCode = $LASTEXITCODE
    }
    catch {
        $_ | Out-String | Add-Content -LiteralPath (Join-Path $runRoot $record.logRef) -Encoding utf8
        $record.exitCode = 2
        $record.startFailure = $_.Exception.Message
    }
    finally {
        $record.endedAt = [DateTimeOffset]::UtcNow.ToString('o')
        Save-Json $record.commandRef $record
        $commands.Add($record)
    }
    Write-Host "$Name exit code: $($record.exitCode). Log: $($record.logRef)"
    return $record
}
function Get-ProjectGraph {
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $pending = [Collections.Generic.Queue[string]]::new()
    $pending.Enqueue((Join-Path $repository $project))
    while ($pending.Count -gt 0) {
        $path = [IO.Path]::GetFullPath($pending.Dequeue())
        if (-not $seen.Add($path)) { continue }
        if (-not (Test-Path -LiteralPath $path)) { throw "Referenced project does not exist: $path" }
        [xml]$xml = Get-Content -LiteralPath $path -Raw
        foreach ($reference in $xml.SelectNodes('//ProjectReference[@Include]')) {
            if ($reference.Include.Contains('$(')) { throw "Unresolved project reference: $($reference.Include)" }
            $pending.Enqueue([IO.Path]::GetFullPath((Join-Path (Split-Path $path) $reference.Include)))
        }
        $assets = Join-Path (Split-Path $path) 'obj/project.assets.json'
        if (Test-Path -LiteralPath $assets) {
            $assetJson = Get-Content -LiteralPath $assets -Raw | ConvertFrom-Json -AsHashtable
            foreach ($framework in $assetJson.project.restore.frameworks.Values) {
                foreach ($reference in $framework.projectReferences.Keys) { $pending.Enqueue($reference) }
            }
        }
    }
    @($seen | Sort-Object)
}
function Get-Inputs([string[]]$Projects) {
    $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $Projects) {
        [void]$paths.Add($path)
        $directory = Split-Path $path
        Get-ChildItem -LiteralPath $directory -Recurse -File |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.git)[\\/]' -and $_.Extension -in @('.cs', '.props', '.targets', '.resx') } |
            ForEach-Object { [void]$paths.Add($_.FullName) }
        while ($directory -and $directory.StartsWith($repository, [StringComparison]::OrdinalIgnoreCase)) {
            foreach ($name in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', 'NuGet.Config', 'nuget.config')) {
                $candidate = Join-Path $directory $name
                if (Test-Path -LiteralPath $candidate -PathType Leaf) { [void]$paths.Add($candidate) }
            }
            if ($directory -eq $repository) { break }
            $directory = Split-Path $directory
        }
        [xml]$xml = Get-Content -LiteralPath $path -Raw
        foreach ($hint in $xml.SelectNodes('//Reference/HintPath')) {
            $candidate = [IO.Path]::GetFullPath((Join-Path (Split-Path $path) $hint.InnerText))
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { [void]$paths.Add($candidate) }
        }
    }
    foreach ($relative in $sourcePaths + @('scripts/testing/test-enumeration-dynamic.ps1', 'scripts/testing/schemas/enumeration-dynamic-evidence.schema.json', $scopeDocument)) {
        [void]$paths.Add((Join-Path $repository $relative))
    }
    @($paths | Sort-Object | ForEach-Object { File-Identity $_ })
}
function Get-Assets([string[]]$Projects) {
    @($Projects | ForEach-Object {
        $dir = Split-Path $_
        foreach ($relative in @('obj/project.assets.json', 'packages.lock.json')) { File-Identity (Join-Path $dir $relative) }
    })
}
function Get-Products {
    $directory = Join-Path $repository 'framework_modules/Geex.Tests/bin/Debug'
    if (Test-Path -LiteralPath $directory) {
        @(Get-ChildItem -LiteralPath $directory -File -Recurse |
            Where-Object { $_.Extension -in @('.dll', '.pdb') -or $_.Name -match '\.(deps|runtimeconfig)\.json$' } |
            Sort-Object FullName | ForEach-Object { File-Identity $_.FullName })
    }
}
function Artifact-Id([string]$RelativePath) { 'artifact-' + (Get-TextHash $RelativePath).Substring(0, 20) }
function Assert-Record($Record) {
    if (-not (Test-Json -Json (ConvertTo-Json -InputObject $Record -Depth 50) -SchemaFile (Join-Path $runRoot $schemaRef) -ErrorAction SilentlyContinue)) {
        throw "Schema validation failed: $($Record.recordType)"
    }
}

Push-Location $repository
try {
    foreach ($path in $sourcePaths + @($scopeDocument, (Relative-Path $schemaSource))) {
        if (-not (Test-Path -LiteralPath (Join-Path $repository $path) -PathType Leaf)) { throw "Required input does not exist: $path" }
    }
    $source = Get-Content -LiteralPath (Join-Path $repository $testSource) -Raw
    $facts = [regex]::Matches($source, '\[Fact(?:\([^\]]*\))?\]\s*public\s+(?:async\s+)?(?:void|Task)\s+(?<name>\w+)\s*\(\s*\)')
    if ($facts.Count -eq 0 -or $facts.Count -ne [regex]::Matches($source, '\[Fact\b').Count) {
        throw 'Expected non-parameterized public void/Task [Fact] methods; case registration must not silently omit tests.'
    }
    if (@($facts | ForEach-Object { $_.Groups['name'].Value } | Sort-Object -Unique).Count -ne $facts.Count) { throw 'Duplicate test method names.' }
    if (Test-Path -LiteralPath $runRoot) { throw "Evidence directory already exists: $runRoot" }
    New-Item -ItemType Directory -Path $runRoot | Out-Null
    foreach ($directory in @('scope', 'environment', 'environment/sources', 'runner', 'defects')) {
        New-Item -ItemType Directory -Path (Join-Path $runRoot $directory) | Out-Null
    }
    Copy-Item -LiteralPath $schemaSource -Destination (Join-Path $runRoot $schemaRef)
    Copy-Item -LiteralPath (Join-Path $repository $scopeDocument) -Destination (Join-Path $runRoot 'scope/definition.md')
    for ($i = 0; $i -lt $sourcePaths.Count; $i++) {
        Copy-Item -LiteralPath (Join-Path $repository $sourcePaths[$i]) -Destination (Join-Path $runRoot "environment/sources/$i-$(Split-Path $sourcePaths[$i] -Leaf)")
    }
    $scope = @($facts | ForEach-Object {
        $name = $_.Groups['name'].Value
        $line = 1 + [regex]::Matches($source.Substring(0, $_.Index), '\n').Count
        [ordered]@{
            recordType = 'scope'; schemaRef = $schemaRef; scopeId = $scopeId; caseId = $name
            instanceId = "Geex.Tests.UnitTests.EnumerationDynamicTests.$name"; flowId = 'dynamic-enumeration'
            ruleIds = @('ENUM-DYNAMIC'); testType = 'UNIT'; role = 'in-process'; scenario = $name
            parameters = @{}; browser = $null; viewport = $null
            preconditions = @('Current project and dependencies build successfully', 'No database or application host is started')
            operations = "Execute $name"; expectedRef = "${testSnapshotRef}#L$line"
            implementationRef = "$testSource#L$line"; applicability = 'APPLICABLE'
            requiredStages = @('DEVELOPMENT', 'ACCEPTANCE'); applicabilityReason = 'Focused regression for the dynamic enumeration and definition contract'
            confirmationRef = 'scope/definition.md'; evidenceRequirements = @('build-log', 'test-log', 'trx', 'baseline')
        }
    })
    Save-JsonLines 'scope/cases.jsonl' $scope
    $plannedIds = @($scope.instanceId)
    Save-Json 'scope/plan.json' @{ runId = $runId; scopeId = $scopeId; stage = $Stage; plannedInstanceIds = $plannedIds; frozenAt = [DateTimeOffset]::UtcNow.ToString('o') }
    foreach ($record in $scope) { Assert-Record $record }

    $gitHead = (& git rev-parse HEAD | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to read Git HEAD.' }
    $gitStatus = @(& git status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to read Git status.' }
    $gitStatus | Set-Content -LiteralPath (Join-Path $runRoot 'environment/git-status.txt') -Encoding utf8
    & git diff HEAD -- $sourcePaths > (Join-Path $runRoot 'environment/source.diff')
    if ($LASTEXITCODE -ne 0) { throw 'Unable to capture scoped source diff.' }
    $sdk = Invoke-Recorded 'dotnet-version' 'dotnet' @('--version')
    if ($sdk.exitCode -ne 0) { throw 'dotnet --version failed.' }
    $sdkVersion = (Get-Content -LiteralPath (Join-Path $runRoot $sdk.logRef) -Raw).Trim()
    $projects = @(Get-ProjectGraph)
    $inputBefore = @(Get-Inputs $projects)
    $assetsBefore = @(Get-Assets $projects)
    Save-Json 'environment/inputs-before-build.json' @{ inputs = $inputBefore; dependencyAssets = $assetsBefore; gitHead = $gitHead; capturedAt = [DateTimeOffset]::UtcNow.ToString('o') }
    $buildArgs = @('build', $project, '-m:1')
    if ($NoRestore) { $buildArgs += '--no-restore' }
    $build = Invoke-Recorded 'build' 'dotnet' $buildArgs
    $projectsAfter = @(Get-ProjectGraph)
    $inputAfter = @(Get-Inputs $projectsAfter)
    $inputStable = (ConvertTo-Json -InputObject $inputBefore -Depth 8 -Compress) -ceq (ConvertTo-Json -InputObject $inputAfter -Depth 8 -Compress)
    $baseline = [ordered]@{
        gitHead = $gitHead; workspaceStatusRef = 'environment/git-status.txt'; scopedDiffRef = 'environment/source.diff'
        inputs = $inputBefore; projects = @($projectsAfter | ForEach-Object { Relative-Path $_ })
        dependencyAssets = @(Get-Assets $projectsAfter); buildProducts = @(if ($build.exitCode -eq 0) { Get-Products })
        environment = @{ sdkVersion = $sdkVersion; powershell = $PSVersionTable.PSVersion.ToString(); os = [Runtime.InteropServices.RuntimeInformation]::OSDescription; architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString() }
        configuration = @{ buildConfiguration = 'Debug'; restore = -not $NoRestore; applicationConfiguration = 'Not loaded by these unit tests'; configurationContentsCopied = $false }
        dataset = @{ kind = 'synthetic-in-process'; reference = $testSnapshotRef }
        sourceStableAcrossBuild = $inputStable
    }
    $baselineId = 'sha256-' + (Get-TextHash (ConvertTo-Json -InputObject $baseline -Depth 50 -Compress))
    Save-Json 'environment/baseline.json' @{ baselineId = $baselineId; frozenAt = [DateTimeOffset]::UtcNow.ToString('o'); identity = $baseline }
    if ($build.exitCode -ne 0) { $issues.Add("Build failed with exit code $($build.exitCode); dependent tests were not started. See $($build.logRef).") }
    elseif (-not $inputStable) { $issues.Add('Build inputs changed while building; dependent tests were not started.') }
    else {
        $testId = 'test-' + [Guid]::NewGuid().ToString('N')
        $testResults = Join-Path $runRoot "runner/$testId"
        New-Item -ItemType Directory -Path $testResults | Out-Null
        $test = Invoke-Recorded 'test' 'dotnet' @('test', $project, '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~Geex.Tests.UnitTests.EnumerationDynamicTests', '--results-directory', $testResults, '--logger', 'trx;LogFileName=results.trx')
        $test.resultsDirectory = "runner/$testId"
        Save-Json $test.commandRef $test
        $trxRelative = "runner/$testId/results.trx"
        if (-not (Test-Path -LiteralPath (Join-Path $runRoot $trxRelative))) { throw "TRX report missing: $trxRelative" }
        [xml]$trx = Get-Content -LiteralPath (Join-Path $runRoot $trxRelative) -Raw
        $testArtifacts = @(Artifact-Id $test.logRef; Artifact-Id $test.commandRef; Artifact-Id $trxRelative)
        $definitions = @{}
        foreach ($definition in $trx.SelectNodes('//*[local-name()="TestDefinitions"]/*[local-name()="UnitTest"]')) {
            $method = $definition.SelectSingleNode('*[local-name()="TestMethod"]')
            $definitions[$definition.id] = "$($method.className).$($method.name)"
        }
        $rawResults = @($trx.SelectNodes('//*[local-name()="UnitTestResult"]'))
        $actualNames = @($rawResults | ForEach-Object { $definitions[$_.testId] })
        foreach ($unexpected in $actualNames | Where-Object { $_ -notin $plannedIds }) { $issues.Add("Unexpected discovered test: $unexpected") }
        foreach ($case in $scope) {
            $matches = @($rawResults | Where-Object { $definitions[$_.testId] -eq $case.instanceId })
            $attempts = @()
            $assertions = @()
            foreach ($raw in $matches) {
                $rawOutcome = [string]$raw.outcome
                if ($rawOutcome -in @('NotExecuted', 'Skipped')) { continue }
                if ($rawOutcome -notin @('Passed', 'Failed', 'Error', 'Timeout', 'Aborted', 'Inconclusive')) {
                    $issues.Add("Unrecognized TRX outcome '$rawOutcome' for $($case.instanceId)"); continue
                }
                if (-not $raw.startTime -or -not $raw.endTime -or -not $raw.executionId) { $issues.Add("Missing original TRX execution identity or times: $($case.instanceId)"); continue }
                $status = switch ($rawOutcome) { 'Passed' { 'PASS' } 'Failed' { 'FAIL' } default { 'BLOCKED' } }
                $outputNode = $raw.SelectSingleNode('*[local-name()="Output"]')
                $output = if ($outputNode) { $outputNode.InnerText } else { '' }
                $assertion = if ($status -eq 'PASS') { 'All assertions in the test method completed without failure; xUnit does not emit per-assertion success details.' } else { $output }
                $assertions += $assertion
                $attempts += [ordered]@{
                    executionId = [string]$raw.executionId; attemptId = $attempts.Count + 1; baselineId = $baselineId
                    invocationId = $test.invocationId; rawRunnerStatus = $rawOutcome; startedAt = [string]$raw.startTime; endedAt = [string]$raw.endTime
                    stoppedAt = if ($status -eq 'PASS') { 'test-completed' } else { 'test-method-or-runner' }
                    status = $status; evidenceStatus = 'COMPLETE'; assertionSummary = $assertion; assertionOutput = $output
                    artifactIds = $testArtifacts; defectRefs = @(); blockerRefs = @(if ($status -ne 'PASS') { 'defects/issues.json' })
                    retryOf = $null; trxRef = "$trxRelative#executionId=$($raw.executionId)"; timeSource = 'original-trx'
                }
            }
            $status = if ($attempts.Count -eq 0) { 'NOT_RUN' } elseif ('FAIL' -in $attempts.status) { 'FAIL' } elseif ('BLOCKED' -in $attempts.status) { 'BLOCKED' } elseif ($attempts.Count -ne 1) { 'BLOCKED' } else { 'PASS' }
            if ($matches.Count -ne 1 -or $status -ne 'PASS') { $issues.Add("$($case.instanceId): status=$status, TRX results=$($matches.Count), actual attempts=$($attempts.Count).") }
            $finalExecution = if ($attempts.Count -eq 1) { $attempts[0].executionId } else { $null }
            $results.Add([ordered]@{
                recordType = 'case'; schemaRef = $schemaRef; runId = $runId; instanceId = $case.instanceId
                scopeRef = "scope/cases.jsonl#instanceId=$($case.instanceId)"; status = $status
                evidenceStatus = if ($attempts.Count -eq 1) { 'COMPLETE' } else { 'INCOMPLETE' }
                assertionSummary = ($assertions -join "`n"); artifactIds = $testArtifacts; defectRefs = @()
                blockerRefs = @(if ($status -ne 'PASS') { 'defects/issues.json' }); finalExecutionId = $finalExecution; attempts = $attempts
            })
        }
        if ($test.exitCode -ne 0) { $issues.Add("Test process exited $($test.exitCode). See $($test.logRef).") }
        $postTestIdentity = @{ inputs = @(Get-Inputs $projectsAfter); dependencyAssets = @(Get-Assets $projectsAfter); buildProducts = @(Get-Products) }
        Save-Json 'environment/identity-after-test.json' $postTestIdentity
        if ((ConvertTo-Json -InputObject $baseline.inputs -Depth 8 -Compress) -cne (ConvertTo-Json -InputObject $postTestIdentity.inputs -Depth 8 -Compress) -or
            (ConvertTo-Json -InputObject $baseline.dependencyAssets -Depth 8 -Compress) -cne (ConvertTo-Json -InputObject $postTestIdentity.dependencyAssets -Depth 8 -Compress) -or
            (ConvertTo-Json -InputObject $baseline.buildProducts -Depth 8 -Compress) -cne (ConvertTo-Json -InputObject $postTestIdentity.buildProducts -Depth 8 -Compress)) {
            $issues.Add('Source/dependency assets/build products changed after baseline freeze; results cannot pass the baseline gate.')
        }
    }
}
catch {
    $toolFailure = $true
    $issues.Add($_.Exception.Message)
    if (Test-Path -LiteralPath $runRoot) { $_ | Out-String | Set-Content -LiteralPath (Join-Path $runRoot 'runner/tool-error.log') -Encoding utf8 }
}
finally {
    try {
        if (Test-Path -LiteralPath (Join-Path $runRoot $schemaRef)) {
            foreach ($case in $scope) {
                if ($case.instanceId -notin @($results.instanceId)) {
                    $results.Add([ordered]@{
                        recordType = 'case'; schemaRef = $schemaRef; runId = $runId; instanceId = $case.instanceId
                        scopeRef = "scope/cases.jsonl#instanceId=$($case.instanceId)"; status = 'BLOCKED'; evidenceStatus = 'INCOMPLETE'
                        assertionSummary = 'No verifiable test execution available; see blockers.'; artifactIds = @(); defectRefs = @()
                        blockerRefs = @('defects/issues.json'); finalExecutionId = $null; attempts = @()
                    })
                }
            }
            Save-JsonLines 'cases.jsonl' @($results.ToArray())
            $counts = [ordered]@{ planned = $scope.Count; PASS = 0; FAIL = 0; BLOCKED = 0; NOT_RUN = 0; completeEvidence = 0; attempts = 0; historicalFailures = 0; uniqueDefects = 0 }
            foreach ($result in $results) {
                $counts[$result.status]++; $counts.attempts += $result.attempts.Count
                if ($result.evidenceStatus -eq 'COMPLETE') { $counts.completeEvidence++ }
                $counts.historicalFailures += @($result.attempts | Where-Object status -eq 'FAIL').Count
            }
            $endedAt = [DateTimeOffset]::UtcNow.ToString('o')
            Save-Json 'defects/issues.json' @{ collected = $true; collectedAt = $endedAt; count = $issues.Count; issues = @($issues.ToArray()); classification = 'Failure and blocker reasons; no defect closure is inferred.' }
            Save-Json 'runner/error-collection.json' @{ collected = $true; source = 'TRX outcomes, command exit codes and converter diagnostics'; count = $issues.Count; issueRef = 'defects/issues.json'; browserConsole = 'NOT_COLLECTED: no browser in this scope'; failedRequests = 'NOT_COLLECTED: no HTTP flow in this scope' }
            $artifacts = @(Get-ChildItem -LiteralPath $runRoot -File -Recurse | Sort-Object FullName | ForEach-Object {
                $relative = [IO.Path]::GetRelativePath($runRoot, $_.FullName).Replace('\', '/')
                [ordered]@{
                    recordType = 'artifact'; schemaRef = $schemaRef; artifactId = Artifact-Id $relative; runId = $runId
                    level = 'run'; category = if ($relative -like 'runner/*') { 'runner' } elseif ($relative -like 'scope/*') { 'scope' } else { 'environment-or-record' }
                    path = $relative; createdAt = $_.LastWriteTimeUtc.ToString('o'); sizeBytes = $_.Length; sha256 = Get-Hash $_.FullName
                    mediaType = switch ($_.Extension) { '.json' { 'application/json' } '.jsonl' { 'application/x-ndjson' } '.trx' { 'application/xml' } default { 'text/plain' } }
                    redaction = 'Application configuration contents and environment variables are not copied; source and native tool output are preserved.'
                }
            })
            Save-JsonLines 'artifacts.jsonl' $artifacts
            $validationStarted = [DateTimeOffset]::UtcNow.ToString('o')
            foreach ($record in @($scope) + @($results.ToArray()) + @($artifacts)) { Assert-Record $record }
            foreach ($artifact in $artifacts) {
                if ((Get-Hash (Join-Path $runRoot $artifact.path)) -ne $artifact.sha256) { throw "Artifact hash mismatch: $($artifact.path)" }
            }
            foreach ($result in $results) {
                foreach ($artifactId in $result.artifactIds) { if ($artifactId -notin $artifacts.artifactId) { throw "Unresolved artifact ID: $artifactId" } }
                if ($result.finalExecutionId -and $result.finalExecutionId -notin $result.attempts.executionId) { throw 'Invalid final execution reference.' }
            }
            $exitCode = if ($toolFailure) { 2 } elseif ($issues.Count -gt 0 -or $counts.planned -eq 0 -or $counts.PASS -ne $counts.planned) { 1 } else { 0 }
            $judgement = if ($exitCode -eq 0) { 'PASS' } else { 'BLOCKED' }
            $gates = @(
                @{ gateId = 'G-01'; judgement = 'PASS'; reason = 'Scope registered from the fixed test source before build/test; limited to scope/definition.md.' },
                @{ gateId = 'G-02'; judgement = if ($test -and $counts.attempts -eq $counts.planned -and $issues.Count -eq 0) { 'PASS' } else { 'BLOCKED' }; reason = 'Match all planned method identities to original TRX results.' },
                @{ gateId = 'G-03'; judgement = if ($build -and $build.exitCode -eq 0 -and $counts.PASS -eq $counts.planned -and $counts.planned -gt 0) { 'PASS' } else { 'BLOCKED' }; reason = 'Build prerequisite and required instance execution.' },
                @{ gateId = 'G-04'; judgement = $judgement; reason = 'Native exit codes, original TRX outcomes and all observed attempts are retained.' },
                @{ gateId = 'G-05'; judgement = if ($counts.completeEvidence -eq $counts.planned -and $counts.planned -gt 0) { 'PASS' } else { 'BLOCKED' }; reason = 'JSON Schema, artifact hashes and result references checked.' },
                @{ gateId = 'G-06'; judgement = $judgement; reason = 'Frozen source/project/assets/product identities checked before and after test.' },
                @{ gateId = 'G-07'; judgement = $judgement; reason = 'Any unresolved failure, blocker or repeated execution prevents passing.' }
            )
            foreach ($id in @('G-08', 'G-09', 'G-10')) { $gates += @{ gateId = $id; judgement = 'NOT_EVALUATED'; reason = 'Outside this focused in-process unit-test run; full-project acceptance is not claimed.' } }
            $gate = [ordered]@{
                recordType = 'gate'; schemaRef = $schemaRef; scopeId = $scopeId; baselineId = $baselineId; runIds = @($runId)
                checkScope = 'Enumeration dynamic unit-test batch only'; judgement = $judgement; counts = $counts
                effectiveResults = @($results | ForEach-Object { @{ instanceId = $_.instanceId; runId = $runId; status = $_.status; executionId = $_.finalExecutionId } })
                gates = $gates; reasons = @($issues.ToArray()); confirmationRef = 'scope/definition.md'
                artifactRefs = @('cases.jsonl', 'artifacts.jsonl', 'defects/issues.json'); checker = @{ name = 'test-enumeration-dynamic.ps1'; executor = $Executor; sourceHash = Get-Hash $PSCommandPath; independentReview = 'Not attested by this runner' }
                startedAt = $validationStarted; endedAt = [DateTimeOffset]::UtcNow.ToString('o'); toolExitCode = $exitCode
                fullProjectAcceptance = $false
            }
            $manifest = [ordered]@{
                recordType = 'manifest'; schemaVersion = '1.0'; schemaRef = $schemaRef; runId = $runId; stage = $Stage; baselineId = $baselineId
                startedAt = $startedAt; endedAt = $endedAt; executor = @{ identity = $Executor; role = if ($Stage -eq 'DEVELOPMENT') { 'development-self-test' } else { 'focused-acceptance-executor' } }
                acceptor = $null; independentReview = 'No independent final acceptance recorded by this runner'
                scopeId = $scopeId; scopeRef = 'scope/cases.jsonl'; scopeSummary = 'Fixed EnumerationDynamicTests [Fact] methods; no database, browser or deployment validation'
                plannedInstanceIds = @($scope.instanceId); repository = @{ head = $gitHead; statusRef = 'environment/git-status.txt'; diffRef = 'environment/source.diff' }
                baselineRef = if ($baseline) { 'environment/baseline.json' } else { $null }; testCodeRef = $testSnapshotRef
                productIdentity = @(if ($baseline) { $baseline.buildProducts }); environment = if ($baseline) { $baseline.environment } else { @{} }
                dependenciesRef = if ($baseline) { 'environment/baseline.json#identity/dependencyAssets' } else { $null }
                configurationRef = if ($baseline) { 'environment/baseline.json#identity/configuration' } else { $null }; datasetRef = $testSnapshotRef
                commands = @($commands.ToArray()); counts = $counts; evidenceStatus = if ($exitCode -eq 0) { 'COMPLETE' } else { 'INCOMPLETE' }
                reportRef = 'report.md'; gateResultRef = 'gate-result.json'; artifactIndexRef = 'artifacts.jsonl'; fullProjectAcceptance = $false
            }
            Assert-Record $manifest
            Assert-Record $gate
            Save-Json 'manifest.json' $manifest
            Save-Json 'gate-result.json' $gate
            Save-Json 'runner/validation.json' @{ checkedAt = [DateTimeOffset]::UtcNow.ToString('o'); schemaRef = $schemaRef; schemaRecords = $scope.Count + $results.Count + $artifacts.Count + 2; schemaValid = $true; artifactHashesValid = $true; resultReferencesValid = $true }
            $report = @(
                '# Enumeration dynamic test report', '', "Run: $runId", "Stage: $Stage", "Executor: $Executor", "Time (UTC): $startedAt to $endedAt", "Baseline: $baselineId", '',
                "Result: $judgement. Tool exit code: $exitCode.", 'This result covers the frozen Enumeration unit-test batch only. Independent final review/full-project acceptance is not recorded.', '',
                "Planned: $($counts.planned); PASS: $($counts.PASS); FAIL: $($counts.FAIL); BLOCKED: $($counts.BLOCKED); NOT_RUN: $($counts.NOT_RUN).", "Complete evidence: $($counts.completeEvidence); attempts: $($counts.attempts); historical failures: $($counts.historicalFailures); confirmed unique defects: $($counts.uniqueDefects).", '',
                "Scope/expectations: [frozen scope](scope/cases.jsonl), [scope definition](scope/definition.md), [test source]($testSnapshotRef).",
                'Identity/environment/commands: [manifest](manifest.json), [baseline](environment/baseline.json). Configuration contents are not copied.',
                'Evidence: [case results](cases.jsonl), [artifact index](artifacts.jsonl), [gate result](gate-result.json), [validation](runner/validation.json).',
                'Blockers/failure reasons: [issues](defects/issues.json). No failure is closed automatically.', '',
                'Dependencies/data: real local framework assemblies and synthetic in-process objects. No database, HTTP API, browser, UI, deployment, performance or stability acceptance was executed. No account is needed for this scope.', '',
                '| Instance | Status | Evidence |', '| --- | --- | --- |'
            )
            foreach ($result in $results) { $report += "| $($result.instanceId) | $($result.status) | $($result.evidenceStatus) |" }
            if ($issues.Count) { $report += @('', 'Unresolved reasons:'); foreach ($issue in $issues) { $report += "- $issue" } }
            $report | Set-Content -LiteralPath (Join-Path $runRoot 'report.md') -Encoding utf8
        }
        else { Write-Error -Message ($issues -join "`n") -ErrorAction Continue }
    }
    catch {
        $exitCode = 2
        $_ | Out-String | Set-Content -LiteralPath (Join-Path $runRoot 'runner/aggregation-error.log') -Encoding utf8
        Write-Error -Message "Evidence aggregation failed. Partial records are retained: $runRoot" -ErrorAction Continue
    }
    Pop-Location
}
Write-Host "Evidence: $runRoot"
Write-Host "Exit code: $exitCode"
exit $exitCode
