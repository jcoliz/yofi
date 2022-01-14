.\builddemo.ps1
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:Demo__IsEnabled = "true"
.\bin\Debug\netcoreapp3.1\publish\YoFi.AspNet.exe
Remove-Item Env:\ASPNETCORE_ENVIRONMENT
Remove-Item Env:\Demo__IsEnabled