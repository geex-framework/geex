Get-ChildItem  | ForEach-Object { dotnet.exe build ($_.FullName + "\" + $_.Name + ".sln") }
