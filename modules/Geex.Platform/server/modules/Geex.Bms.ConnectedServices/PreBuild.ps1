param(
    [string]$env
)
$cwd = Split-Path -Parent $MyInvocation.MyCommand.Definition;

echo $env

if ($env -eq "dev" -or $env -eq "Dev") {
    $env = "Development";
}
elseif ($env -eq "prod" -or $env -eq "Prod") {
    $env = "Production";
}
if ((("Development", "Production") -notcontains $env)) {
    Write-Output "Environment is invalid.";
    $env = "Development";
}

function Check-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}
$JsonDocumentOptions = [System.Text.Json.JsonDocumentOptions]::new();
$JsonDocumentOptions.AllowTrailingCommas = $true;
$JsonDocumentOptions.CommentHandling = [System.Text.Json.JsonCommentHandling]::Skip;
function writeUtf8 {
    param(
        [string]$path,
        [string]$content
    );
    $Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $False
    Write-Output "=============================================="
    Write-Output $path
    Write-Output "=============================================="
    [System.IO.File]::WriteAllText($path, $content, $Utf8NoBomEncoding)
}

echo "building gql proxy with config '$cwd/../../Geex.Bms.Server/appsettings.$env.json'"
# 删除注释
$configJson = (get-content -Path "$cwd/../../Geex.Bms.Server/appsettings.$env.json") -replace '^\s*\/\/.+$', '';
# Json.Net兼容尾逗号
$config = ([System.Text.Json.JsonDocument]::Parse($configJson, $JsonDocumentOptions)).RootElement.ToString() | ConvertFrom-Json
# $config = (get-content -Path "$cwd/../../Geex.Bms.Server/appsettings.$env.json") | ConvertFrom-Json
echo $config.ConnectedServices
$services = $config.ConnectedServices
dotnet tool restore

($services.psobject.properties) | foreach-object {
    echo $_.name
    $apiName = $_.name
    $apiType = $_.value.Type
    $apiEndpoint = $_.value.Endpoint
    echo "apiEndpoint：" $apiEndpoint
    $init = (Test-Path "$cwd/$apiName/.graphqlrc.json") -eq $false;

    if (($apiType -eq "graphql")) {
        if ($init) {
            mkdir "$cwd/$apiName/" -ErrorAction Ignore
            mkdir "$cwd/$apiName/Operations/" -ErrorAction Ignore
            echo "dotnet graphql init $apiEndpoint -p $cwd/$apiName"
            dotnet graphql init $apiEndpoint -p $cwd/$apiName
            if ((Test-Path "$cwd/$apiName/graphql.config.yml") -eq $false) {
                $ymlContent = @"
schema: schema.graphql
documents: $cwd/$apiName/Operations/**/*.graphql
"@
                writeUtf8 -content $ymlContent -path $cwd/$apiName/graphql.config.yml
            }

            $graphqlrc = (Get-Content "$cwd/$apiName/.graphqlrc.json") | ConvertFrom-Json
            $graphqlrc.schema = $null
            $graphqlrc.documents = $null
            $graphqlrc.extensions.strawberryShake.name = $apiName
            $graphqlrc.extensions.strawberryShake.strictSchemaValidation = $false
            # $graphqlrc.extensions.strawberryShake.records.inputs = $true
            $json = ($graphqlrc | ConvertTo-Json -Depth 5)
            writeUtf8 -content $json -path $cwd/$apiName/.graphqlrc.json
        }
        else {
            $graphqlrc = (Get-Content "$cwd/$apiName/.graphqlrc.json") | ConvertFrom-Json
            echo ($graphqlrc | ConvertTo-Json -Depth 5)
            $graphqlrc.extensions.strawberryShake.url = $apiEndpoint
            $json = ($graphqlrc | ConvertTo-Json -Depth 5)
            writeUtf8 -content $json -path $cwd/$apiName/.graphqlrc.json
            echo "dotnet graphql update -p $cwd/$apiName"
            dotnet graphql update -p $cwd/$apiName
        }
    }
}
