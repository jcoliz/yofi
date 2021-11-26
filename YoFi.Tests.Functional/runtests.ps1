param ($settings='local.runsettings')
dotnet build
Push-Location ..\YoFi.AspNet
dotnet build
Start-Job -Name uitests -ScriptBlock { dotnet run } -WorkingDirectory $(Get-Location)
Pop-Location
dotnet test -s $settings
Stop-Job -Name uitests
Remove-Job -Name uitests