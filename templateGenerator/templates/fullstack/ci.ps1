# $ENV:WORKSPACE = "C:\Program Files (x86)\Jenkins\workspace\x_proj_x"
Set-Alias -Name PS64 -Value "$env:WINDIR\\sysnative\\windowspowershell\\v1.0\\powershell.exe"
$bitness = ([System.IntPtr]::size * 8)
Write-Output "PowerShell default bitness is $bitness-bit"
PS64 {
    $bitness = ([System.IntPtr]::size * 8)
    Write-Output "PowerShell is running in $bitness-bit"
    # $env:ASPNETCORE_ENVIRONMENT = "Development"
    Import-Module WebAdministration
    Set-Variable DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
    # Set-Variable HTTP_PROXY=http://127.0.0.1:1080
    # Set-Variable HTTPS_PROXY=http://127.0.0.1:1080
    # git config --global http.proxy http://127.0.0.1:1080
    # git config --global https.proxy http://127.0.0.1:1080
    
    # $pub_test = (git tag -l --contains) -match "v_test_.+" -eq $true;
    
    $changes = (git diff HEAD^ --name-status)
    if (($changes | ? { $_ -match "/x_Org_x.x_Proj_x.Server/" -eq $true }).Count -gt 0) {
        # server
        Set-Location $ENV:WORKSPACE"\server\x_Org_x.x_Proj_x.Server\"
        dotnet publish .\x_Org_x.x_Proj_x.Server.csproj /p:Password=x_org_x123@aliyun /p:PublishProfile=.\Properties\PublishProfiles\x_proj_x.api.dev.x_org_x.com.pubxml -o $ENV:Temp\publish\x_proj_x.api.dev.x_org_x.com
        # if ($pub_test) {
        #     dotnet publish .\x_Org_x.x_Proj_x.Server.csproj /p:Password=x_Org_xData@aliyun /p:PublishProfile=.\Properties\PublishProfiles\x_proj_x.api.test.x_org_x.com.pubxml -o $ENV:Temp\publish\x_proj_x.api.test.x_org_x.com
        # }
    }
    if (($changes | ? { $_ -match "/x_Proj_x.Client/" -eq $true }).Count -gt 0) {
        # client
        Set-Location $ENV:WORKSPACE"\client\x_proj_x\x_Proj_x.Client\"
        dotnet publish .\x_Proj_x.Client.csproj /p:Password=x_org_x123@aliyun /p:PublishProfile=.\Properties\PublishProfiles\x_proj_x.dev.x_org_x.com.pubxml /p:Env=dev -o $ENV:Temp\publish\x_proj_x.dev.x_org_x.com
        # if ($pub_test) {
        #     dotnet publish .\x_Proj_x.Client.csproj /p:Password=x_Org_xData@aliyun /p:PublishProfile=.\Properties\PublishProfiles\x_proj_x.test.x_org_x.com.pubxml /p:Env=test -o $ENV:Temp\publish\x_proj_x.test.x_org_x.com
        # }
    }
}
