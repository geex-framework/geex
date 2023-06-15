echo "执行本脚本之前请确保执行环境满足以下条件."
echo "1. 以管理员权限运行"
echo "2. 科学上网, 满足正常通过网络访问Nuget/Chocolatey"
echo "3. 本机80/443端口未被占用(如iis)"
echo "反向代理服务器依赖于本脚本, 本地调试期间请勿关闭脚本."
$continue = Read-Host -Prompt "enter 'y' to continue";
if ($continue -notlike "y")
{
    exit
}
cd $PSScriptRoot

# if ((Test-Path "Z:")-eq $false) {
#     $dev_user = "kufore"
#     $dev_pwd = ConvertTo-SecureString -String "kufore" -AsPlainText -Force
#     $dev_cred = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $dev_user, $dev_pwd
#     # New-PSDrive -Name "Z" -Root "\\KUFOREDEV\shared" -Persist -PSProvider "FileSystem" -Credential $cred
# }

#region  tools function
function Check-Command($cmdname)
{
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

function Add-Path
{

    param(
        [Parameter(Mandatory, Position = 0)]
        [string] $LiteralPath,
        [ValidateSet('User', 'CurrentUser', 'Machine', 'LocalMachine')]
        [string] $Scope 
    )

    Set-StrictMode -Version 1; $ErrorActionPreference = 'Stop'

    $isMachineLevel = $Scope -in 'Machine', 'LocalMachine'
    if ($isMachineLevel -and -not $($ErrorActionPreference = 'Continue'; net session 2>$null))
    { throw "You must run AS ADMIN to update the machine-level Path environment variable." 
    }  

    $regPath = 'registry::' + ('HKEY_CURRENT_USER\Environment', 'HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\Environment')[$isMachineLevel]

    # Note the use of the .GetValue() method to unsure that the *unexpanded* value is returned.
    $currDirs = (Get-Item -LiteralPath $regPath).GetValue('Path', '', 'DoNotExpandEnvironmentNames') -split ';' -ne ''

    if ($LiteralPath -in $currDirs)
    {
        Write-Verbose "Already present in the persistent $(('user', 'machine')[$isMachineLevel])-level Path: $LiteralPath"
        return
    }

    $newValue = ($currDirs + $LiteralPath) -join ';'

    # Update the registry.
    Set-ItemProperty -Type ExpandString -LiteralPath $regPath Path $newValue

    # Broadcast WM_SETTINGCHANGE to get the Windows shell to reload the
    # updated environment, via a dummy [Environment]::SetEnvironmentVariable() operation.
    $dummyName = [guid]::NewGuid().ToString()
    [Environment]::SetEnvironmentVariable($dummyName, 'foo', 'User')
    [Environment]::SetEnvironmentVariable($dummyName, [NullString]::value, 'User')

    # Finally, also update the current session's `$env:Path` definition.
    # Note: For simplicity, we always append to the in-process *composite* value,
    #        even though for a -Scope Machine update this isn't strictly the same.
    $env:Path = ($env:Path -replace ';$') + ';' + $LiteralPath

    Write-Verbose "`"$LiteralPath`" successfully appended to the persistent $(('user', 'machine')[$isMachineLevel])-level Path and also the current-process value."

}
#endregion
if (-not (Get-Module -ListAvailable -Name "Carbon"))
{
    Install-Module -Name 'Carbon' -AllowClobber
}
Import-Module 'Carbon'

#region cert
$cert = Get-CCertificate -FriendlyName "dev.x_org_x.com" -StoreLocation LocalMachine -StoreName Root
$pwd = ConvertTo-SecureString -String "dev.x_org_x.com" -AsPlainText -Force
if ($cert -eq $null)
{
    if (-not (Check-Command openssl))
    {
        choco install openssl -y
    }
    $cert = New-SelfSignedCertificate -CertStoreLocation Cert:\LocalMachine\My -Subject dev.x_org_x.com -DnsName dev.x_org_x.com, *.api.dev.x_org_x.com, *.dev.x_org_x.com -FriendlyName "dev.x_org_x.com" -NotAfter (Get-Date).AddYears(1000)
    mkdir ./.dev_cert
    Export-PfxCertificate -Cert "Cert:\LocalMachine\My\$($cert.Thumbprint)" -FilePath "./.dev_cert/dev.x_org_x.com.pfx" -Password $pwd
    openssl pkcs12 -in "./.dev_cert/dev.x_org_x.com.pfx" -nodes -out ./.dev_cert/dev.x_org_x.com.pem -passin pass:dev.x_org_x.com
    Install-CCertificate -Path "./.dev_cert/dev.x_org_x.com.pfx" -StoreLocation LocalMachine -StoreName Root -Password $pwd
    cd ./dev_env
    docker-compose up setup -d
    docker-compose up -d
    cd ..
}

#endregion
if (-not (Check-Command npm))
{
    choco install nodejs-lts --version=16.19.1 -y -f
}

if (-not (Check-Command yarn))
{
    choco install yarn -y -f
}

if (-not (Check-Command husky))
{
    yarn global add husky
}

if (-not (Test-Path ".\\client\\x_proj_x\\x_Proj_x.Client\\x_Proj_xClientApp\\node_modules")) {
    $curDir = pwd;
    cd ".\\client\\x_proj_x\\x_Proj_x.Client\\x_Proj_xClientApp"
    yarn
    cd $curDir;
}

if (-not (Test-Path ".\\client\\x_proj_x\\x_Proj_x.Client\\x_Proj_xClientApp\\geex-schematics\\node_modules")) {
    $curDir = pwd;
    cd ".\\client\\x_proj_x\\x_Proj_x.Client\\x_Proj_xClientApp\\geex-schematics"
    yarn
    yarn build
    cd $curDir;
}

#region reverse-proxy
# $redbirdInstalled = npm list redbird -g --depth=0
# if ($redbirdInstalled[1] -like "*(empty)*") {
#     npm install -g redbird -f
# }
$env:NODE_PATH = $(npm root --quiet -g)
node -e @"
var redbird = require('redbird');
redbird({
    port: 80,
    secure: false,
    resolvers:[
      function(host, url, req) {
        if(host == 'x_proj_x.dev.x_org_x.com'){
          return 'http://127.0.0.1:4201'
        }
        if(host == 'x_proj_x.api.dev.x_org_x.com'){
          return 'https://127.0.0.1:8020'
        }
      }
    ],
    ssl: {
        key: './.dev_cert/dev.x_org_x.com.pem',
        cert: './.dev_cert/dev.x_org_x.com.pem',
        port: 443, // SSL port used to serve registered https routes with LetsEncrypt certificate.
    }
});
"@
#endregion

# #(1)将.pfx格式的证书转换为.pem文件格式：
# openssl pkcs12 -in "./.dev_cert/dev.x_org_x.com.pfx" -nodes -out dev.x_org_x.com.pem
# # (2)从.pem文件中导出私钥server.key：
# openssl rsa -in dev.x_org_x.com.pem -out "./.dev_cert/dev.x_org_x.com.key"
# #(3)从.pem文件中导出证书server.crt
# openssl x509 -in dev.x_org_x.com.pem -out "./.dev_cert/dev.x_org_x.com.crt"
