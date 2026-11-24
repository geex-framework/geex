[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$Suffix,
    [string]$OutputDirectory = './nupkg'
)

$ErrorActionPreference = 'Stop'
$projects = @(
    './framework_modules/Geex.Casbin/src/Geex.Casbin.csproj'
    './framework_modules/Geex.MongoDB.Entities/src/Geex.MongoDB.Entities.csproj'
    './framework_modules/Geex.Common/src/Geex.Common.csproj'
    './framework_modules/Geex.Abstractions/src/Geex.Abstractions.csproj'
    './framework_modules/Geex.Extensions.AuditLogs/src/Geex.Extensions.AuditLogs.csproj'
    './framework_modules/Geex.Analyzer/src/Geex.Analyzer.csproj'
    './framework_modules/Geex.Extensions.ApprovalFlows/src/Geex.Extensions.ApprovalFlows.csproj'
    './framework_modules/Geex.Extensions.Favorites/src/Geex.Extensions.Favorites.csproj'
    './framework_modules/Geex.Extensions.Authentication/src/Geex.Extensions.Authentication.csproj'
    './framework_modules/Geex.Extensions.Authentication.Wechat/src/Geex.Extensions.Authentication.Wechat.csproj'
    './framework_modules/Geex.Extensions.Authorization/src/Geex.Extensions.Authorization.csproj'
    './framework_modules/Geex.Extensions.BackgroundJob/src/Geex.Extensions.BackgroundJob.csproj'
    './framework_modules/Geex.Extensions.BlobStorage/src/Geex.Extensions.BlobStorage.csproj'
    './framework_modules/Geex.Extensions.Backups/src/Geex.Extensions.Backups.csproj'
    './framework_modules/Geex.Extensions.Captcha/src/Geex.Extensions.Captcha.csproj'
    './framework_modules/Geex.Extensions.Identity/src/Geex.Extensions.Identity.csproj'
    './framework_modules/Geex.Extensions.Logging/src/Geex.Extensions.Logging.csproj'
    './framework_modules/Geex.Extensions.Messaging/src/Geex.Extensions.Messaging.csproj'
    './framework_modules/Geex.Extensions.Mocking/src/Geex.Extensions.Mocking.csproj'
    './framework_modules/Geex.Extensions.Payments/src/Geex.Extensions.Payments.csproj'
    './framework_modules/Geex.Extensions.MultiTenant/src/Geex.Extensions.MultiTenant.csproj'
    './framework_modules/Geex.Extensions.MultiTenant/src/Abstractions/Geex.Extensions.MultiTenant.Abstractions.csproj'
    './framework_modules/Geex.Extensions.Settings/src/Geex.Extensions.Settings.csproj'
    './framework_modules/Geex.Extensions.Settings/src/Abstractions/Geex.Extensions.Settings.Abstractions.csproj'
    './framework_modules/Geex.MediatX/MediatX/MediatX.csproj'
    './framework_modules/Geex.MediatX/MediatX.RabbitMQ/MediatX.RabbitMQ.csproj'
)
$properties = @{
    Description = $env:PACKAGE_DESCRIPTION
    Authors = $env:PACKAGE_AUTHOR
    Company = $env:PACKAGE_COMPANY
    Copyright = $env:PACKAGE_COPYRIGHT
    PackageIconUrl = $env:PACKAGE_LOGO_URL
    PackageProjectUrl = $env:PACKAGE_PROJECT_URL
    RepositoryUrl = $env:PACKAGE_REPOSITORY_URL
    PackageTags = $env:PACKAGE_TAGS
}
$packArgs = @('--configuration', 'Release', '--no-build', '--output', $OutputDirectory, "-p:PackageVersion=$Version")
if ($Suffix) { $packArgs += '--version-suffix', $Suffix }
foreach ($entry in $properties.GetEnumerator()) {
    if ($entry.Value) { $packArgs += "-p:$($entry.Key)=$($entry.Value)" }
}

Push-Location (Join-Path $PSScriptRoot '..')
try {
    foreach ($project in $projects) {
        dotnet pack $project @packArgs
        if ($LASTEXITCODE -ne 0) { throw "Failed to pack $project." }
    }
} finally {
    Pop-Location
}
