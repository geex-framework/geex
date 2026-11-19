#requires -Version 7.0
param([string]$NuGetSource)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$runRoot = Join-Path $repository ".test-evidence/$runId"
New-Item -ItemType Directory -Path $runRoot | Out-Null
$commands = [Collections.Generic.List[object]]::new()
$cases = [Collections.Generic.List[object]]::new()
$nodeResults = [Collections.Generic.List[object]]::new()
$scope = 'patch-extraction'
$startedAt = [DateTimeOffset]::UtcNow.ToString('o')

function Save-Json([string]$path, $value) {
    $value | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $runRoot $path) -Encoding utf8
}
function Invoke-Recorded([string]$name, [string]$file, [string[]]$arguments, [string]$directory = $repository) {
    $invocationId = "$name-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
    $folder = Join-Path $runRoot "runner/$invocationId"
    New-Item -ItemType Directory -Path $folder | Out-Null
    $record = [ordered]@{ invocationId = $invocationId; file = $file; arguments = $arguments;
        workingDirectory = $directory; startedAt = [DateTimeOffset]::UtcNow.ToString('o');
        log = "runner/$invocationId/output.log" }
    Save-Json "runner/$invocationId/command.json" $record
    Push-Location $directory
    try {
        & $file @arguments 2>&1 | Tee-Object -FilePath (Join-Path $folder 'output.log') | ForEach-Object {
            if ($_ -notmatch ': warning ') { Write-Host $_ }
        }
        $code = $LASTEXITCODE
    } finally { Pop-Location }
    $record.exitCode = $code
    $record.finishedAt = [DateTimeOffset]::UtcNow.ToString('o')
    Save-Json "runner/$invocationId/command.json" $record
    $commands.Add($record)
    return [pscustomobject]$record
}
function Add-CommandCase([string]$id, $record) {
    $cases.Add([ordered]@{ scopeId = $scope; caseId = $id; status = $(if ($record.exitCode -eq 0) { 'PASS' } else { 'FAIL' });
        evidence = $record.log; invocationId = $record.invocationId })
}
function Add-TrxCases([string]$relativePath, $record) {
    $path = Join-Path $runRoot $relativePath
    if (!(Test-Path -LiteralPath $path)) {
        $cases.Add(@{ scopeId = $scope; caseId = "$($record.invocationId)-results"; status = 'FAIL'; reason = 'Missing TRX report.' })
        return
    }
    [xml]$trx = Get-Content -LiteralPath $path -Raw
    $results = @($trx.TestRun.Results.UnitTestResult | Where-Object { $null -ne $_ })
    if ($results.Count -eq 0) {
        $cases.Add(@{ scopeId = $scope; caseId = "$($record.invocationId)-results"; status = 'FAIL'; reason = 'Empty TRX report.'; evidence = $relativePath })
    }
    foreach ($result in $results) {
        $cases.Add([ordered]@{ scopeId = $scope; caseId = $result.testName;
            status = $(if ($result.outcome -eq 'Passed') { 'PASS' } else { 'FAIL' }); evidence = $relativePath;
            invocationId = $record.invocationId; message = $result.Output.ErrorInfo.Message })
    }
}
function Add-NodeResult([string]$name, $record) {
    $log = Get-Content -LiteralPath (Join-Path $runRoot $record.log) -Raw
    $result = [ordered]@{ name = $name; invocationId = $record.invocationId; evidence = $record.log }
    foreach ($key in @('tests', 'pass', 'fail', 'skipped', 'todo')) {
        $match = [regex]::Match($log, "(?m)^# $key (\d+)\s*$")
        $result[$key] = if ($match.Success) { [int]$match.Groups[1].Value } else { $null }
    }
    $nodeResults.Add($result)
    if ($null -eq $result.tests -or $result.tests -eq 0 -or $result.pass -ne $result.tests) {
        $cases.Add(@{ scopeId = $scope; caseId = "$name-results"; status = 'FAIL';
            reason = 'Missing, empty or non-passing Node test summary.'; evidence = $record.log })
    }
}

$inputs = @(
    'framework_modules/Geex.MongoDB.Entities/src/InnerQuery/ExpressionSimplifier.cs',
    'framework_modules/Geex.Extensions.BlobStorage/src/Core/Entities/BlobObject.cs',
    'framework_modules/Geex.Extensions.BlobStorage/src/Core/Handlers/BlobObjectHandler.cs',
    'frontend_libs/geex-angular/src/provide-geex-apollo.ts',
    'frontend_libs/geex-angular/src/provide-geex-apollo.node-test.mjs',
    'frontend_libs/geex-extensions-blob-storage/src/attach-blob.ts',
    'frontend_libs/geex-extensions-blob-storage/src/attach-blob.node-test.mjs',
    'frontend_libs/geex-extensions-blob-storage/src/blob-storage.module.ts',
    'frontend_libs/geex-extensions-blob-storage/src/blob-storage.types.ts',
    'frontend_libs/geex-extensions-blob-storage/src/public-api.ts',
    'frontend_libs/geex-extensions-blob-storage/src/provide-geex-blob-storage.node-test.mjs',
    'frontend_libs/geex-extensions-blob-storage/package.json',
    'frontend_libs/geex-module-schematics/files/extensions/blob-storage/widgets/upload/geex-upload.widget.ts',
    'frontend_libs/geex-module-schematics/files/extensions/blob-storage/components/upload/geex-upload.component.ts',
    'frontend_libs/geex-module-schematics/src/add-module/add-module.node-test.cjs',
    'framework_modules/Geex.Extensions.BlobStorage/tests/Geex.Extensions.BlobStorage.Tests.csproj',
    'framework_modules/Geex.Extensions.BlobStorage/tests/BlobFixture.cs',
    'framework_modules/Geex.Extensions.BlobStorage/tests/BlobUploadTests.cs',
    'framework_modules/Geex.Extensions.BlobStorage/tests/BlobUploadReadinessTests.cs',
    'framework_modules/Geex.Extensions.BlobStorage/tests/SpanContainsTests.cs',
    'framework_modules/Geex.Extensions.Backups/tests/BackupIntegrationTests.cs',
    'framework_modules/Geex.Extensions.Backups/tests/BackupFixture.cs',
    'scripts/testing/test-patch-extraction.ps1'
)
function Snapshot-Inputs {
    @($inputs | ForEach-Object { [ordered]@{ path = $_; sha256 = (Get-FileHash -LiteralPath (Join-Path $repository $_) -Algorithm SHA256).Hash.ToLowerInvariant() } })
}
$before = Snapshot-Inputs
Save-Json 'manifest.json' @{ runId = $runId; scopeId = $scope; stage = 'DEVELOPMENT'; executor = 'codex';
    startedAt = $startedAt; baseline = (& git -C $repository rev-parse HEAD); inputs = $before;
    coverage = @('Span Contains translation', 'frontend attachment lookup and reuse without uploading',
        'transformed content and metadata', 'expiration and pagination', 'lookup and upload errors',
        'ordinary backend creation', 'pending and failed upload visibility', 'legacy upload compatibility',
        'shared content storage and deletion', 'Cache lifetime',
        'Apollo source and distribution runtime policies', 'generated attachment upload and removal');
    exclusions = @('Independent acceptance', 'browser E2E', 'deployment', 'performance') }
Save-Json 'environment.json' @{ dotnet = (& dotnet --version); node = (& node --version); pnpm = (& pnpm --version);
    operatingSystem = [Environment]::OSVersion.ToString() }

$backend = 'framework_modules/Geex.Extensions.BlobStorage/tests/Geex.Extensions.BlobStorage.Tests.csproj'
$output = Join-Path $runRoot 'build/backend'
$buildArgs = @('build', $backend, '--artifacts-path', $output, '-p:NuGetAudit=false', '-m:1')
$buildArgs += '--ignore-failed-sources'
if ($NuGetSource) { $buildArgs += "-p:RestoreSources=$NuGetSource" }
$build = Invoke-Recorded 'backend-build' 'dotnet' $buildArgs
Add-CommandCase 'backend-build' $build
if ($build.exitCode -eq 0) {
    $oldMongo = $env:GEEX_BLOB_MONGOD
    $oldEvidence = $env:GEEX_BLOB_TEST_EVIDENCE
    if (!$env:GEEX_BLOB_MONGOD -and (Test-Path -LiteralPath 'C:/Program Files/MongoDB/Server/5.2/bin/mongod.exe')) {
        $env:GEEX_BLOB_MONGOD = 'C:/Program Files/MongoDB/Server/5.2/bin/mongod.exe'
    }
    $env:GEEX_BLOB_TEST_EVIDENCE = Join-Path $runRoot 'environment/storage'
    try {
        $test = Invoke-Recorded 'backend-test' 'dotnet' @('test', $backend, '--artifacts-path', $output,
            '--no-build', '--no-restore', '--logger', 'trx;LogFileName=blob.trx', '--results-directory', (Join-Path $runRoot 'results'))
        Add-CommandCase 'backend-tests' $test
        Add-TrxCases 'results/blob.trx' $test
    } finally { $env:GEEX_BLOB_MONGOD = $oldMongo; $env:GEEX_BLOB_TEST_EVIDENCE = $oldEvidence }
} else {
    $cases.Add(@{ scopeId = $scope; caseId = 'backend-tests'; status = 'BLOCKED'; reason = 'Backend build failed.' })
}

$backups = 'framework_modules/Geex.Extensions.Backups/tests/Geex.Extensions.Backups.Tests.csproj'
$regressionArgs = @('build', $backups, '--artifacts-path', $output,
    '--ignore-failed-sources', '-p:NuGetAudit=false', '-m:1')
if ($NuGetSource) { $regressionArgs += "-p:RestoreSources=$NuGetSource" }
$regressionBuild = Invoke-Recorded 'backups-build' 'dotnet' $regressionArgs
Add-CommandCase 'backups-build' $regressionBuild
if ($regressionBuild.exitCode -eq 0) {
    $oldMongo = $env:GEEX_BACKUP_MONGOD
    $oldEvidence = $env:GEEX_BACKUP_TEST_EVIDENCE
    if (!$env:GEEX_BACKUP_MONGOD -and (Test-Path -LiteralPath 'C:/Program Files/MongoDB/Server/5.2/bin/mongod.exe')) {
        $env:GEEX_BACKUP_MONGOD = 'C:/Program Files/MongoDB/Server/5.2/bin/mongod.exe'
    }
    $env:GEEX_BACKUP_TEST_EVIDENCE = Join-Path $runRoot 'environment/backups-storage'
    try {
        $regressionTest = Invoke-Recorded 'backups-test' 'dotnet' @('test', $backups, '--artifacts-path', $output,
            '--no-build', '--no-restore', '--filter',
            'FullyQualifiedName~OriginalBlobHandlerAndSharedFileDeletionRemainCompatible|FullyQualifiedName~BlobWriteCancellationDisposesStreamAndRemovesPartialFile',
            '--logger', 'trx;LogFileName=backups.trx', '--results-directory', (Join-Path $runRoot 'results'))
        Add-CommandCase 'backups-regression' $regressionTest
        Add-TrxCases 'results/backups.trx' $regressionTest
    } finally { $env:GEEX_BACKUP_MONGOD = $oldMongo; $env:GEEX_BACKUP_TEST_EVIDENCE = $oldEvidence }
} else {
    $cases.Add(@{ scopeId = $scope; caseId = 'backups-regression'; status = 'BLOCKED'; reason = 'Backups build failed.' })
}

$frontend = Join-Path $repository 'frontend_libs/geex-angular'
$frontBuild = Invoke-Recorded 'frontend-build' 'pnpm' @('run', 'build') $frontend
Add-CommandCase 'frontend-build' $frontBuild
if ($frontBuild.exitCode -eq 0) {
    $frontTest = Invoke-Recorded 'frontend-test' 'pnpm' @('run', 'test') $frontend
    Add-CommandCase 'frontend-tests' $frontTest
    Add-NodeResult 'frontend-tests' $frontTest

    $blobFrontend = Join-Path $repository 'frontend_libs/geex-extensions-blob-storage'
    $blobBuild = Invoke-Recorded 'blob-frontend-build' 'pnpm' @('run', 'build') $blobFrontend
    Add-CommandCase 'blob-frontend-build' $blobBuild
    if ($blobBuild.exitCode -eq 0) {
        $blobTest = Invoke-Recorded 'blob-frontend-test' 'pnpm' @('run', 'test') $blobFrontend
        Add-CommandCase 'blob-frontend-tests' $blobTest
        Add-NodeResult 'blob-frontend-tests' $blobTest
    } else {
        $cases.Add(@{ scopeId = $scope; caseId = 'blob-frontend-tests'; status = 'BLOCKED'; reason = 'Blob frontend build failed.' })
    }
} else {
    $cases.Add(@{ scopeId = $scope; caseId = 'frontend-tests'; status = 'BLOCKED'; reason = 'Frontend build failed.' })
    $cases.Add(@{ scopeId = $scope; caseId = 'blob-frontend-tests'; status = 'BLOCKED'; reason = 'Required core frontend build failed.' })
}
$schematics = Invoke-Recorded 'schematics-test' 'pnpm' @('run', 'test') (Join-Path $repository 'frontend_libs/geex-module-schematics')
Add-CommandCase 'schematics-tests' $schematics
Add-NodeResult 'schematics-tests' $schematics
$after = Snapshot-Inputs
if (($before | ConvertTo-Json -Compress) -cne ($after | ConvertTo-Json -Compress)) {
    $cases.Add(@{ scopeId = $scope; caseId = 'frozen-inputs'; status = 'FAIL'; reason = 'Inputs changed during validation.' })
} else { $cases.Add(@{ scopeId = $scope; caseId = 'frozen-inputs'; status = 'PASS' }) }

@($cases | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 30 }) | Set-Content -LiteralPath (Join-Path $runRoot 'cases.jsonl') -Encoding utf8
Save-Json 'commands.json' $commands.ToArray()
Save-Json 'node-results.json' $nodeResults.ToArray()
$artifacts = @(Get-ChildItem -LiteralPath $runRoot -Recurse -File | Where-Object { $_.Extension -in '.log', '.trx', '.json' -and
    !$_.FullName.StartsWith((Join-Path $runRoot 'build')) } | ForEach-Object {
    @{ path = [IO.Path]::GetRelativePath($runRoot, $_.FullName).Replace('\', '/');
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(); sizeBytes = $_.Length }
})
@($artifacts | ForEach-Object { $_ | ConvertTo-Json -Compress }) | Set-Content -LiteralPath (Join-Path $runRoot 'artifacts.jsonl') -Encoding utf8
$passed = @($cases | Where-Object status -eq 'PASS').Count
$failed = @($cases | Where-Object status -eq 'FAIL').Count
$blocked = @($cases | Where-Object status -eq 'BLOCKED').Count
$status = if ($failed -eq 0 -and $blocked -eq 0) { 'PASS' } else { 'FAIL' }
Save-Json 'gate-result.json' @{ runId = $runId; scopeId = $scope; stage = 'DEVELOPMENT'; status = 'INCOMPLETE';
    developmentStatus = $status; evidenceStatus = 'INCOMPLETE';
    incompleteChecks = @('Full instance plan and schema validation', 'Cross-record evidence audit', 'Independent acceptance');
    pass = $passed; fail = $failed; blocked = $blocked; independentAcceptance = 'NOT_RUN';
    finishedAt = [DateTimeOffset]::UtcNow.ToString('o') }
@"
# 补丁提取改动验证

运行: $runId. 开发检查结果: $status. 检查记录 PASS=$passed, FAIL=$failed, BLOCKED=$blocked (包含构建和测试命令, 非用例总数).

逐项结果见 [cases.jsonl](cases.jsonl), 命令和原始输出见 [commands.json](commands.json), 后端用例见 [TRX](results/blob.trx), 前端用例数量见 [node-results.json](node-results.json).
MongoDB 仅监听本机随机端口, 测试状态保存在本目录下的 environment/storage. Schematics 为原生 JavaScript 包, 没有构建步骤.

本次范围包含 Contains 翻译/前端附件复用/后端普通创建和内容存储/缓存策略/生成的附件入口. 规范门禁和证据完整性为 INCOMPLETE, 尚无完整逐实例计划/格式和关联审计/独立验收. 本报告不代表浏览器 E2E/公共 GraphQL 端到端链路/部署或性能验证.
"@ | Set-Content -LiteralPath (Join-Path $runRoot 'report.md') -Encoding utf8
Write-Host "Evidence: $runRoot"
if ($status -ne 'PASS') { exit 1 }
