dotnet build
Push-Location ..\YoFi.AspNet
dotnet build
Start-Job -Name uitestsbg -ScriptBlock { dotnet run } -WorkingDirectory $(Get-Location)
Pop-Location