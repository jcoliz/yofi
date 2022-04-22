dotnet build
Push-Location ..\YoFi.AspNet
dotnet build
$env:Demo__IsEnabled = "false"
$env:Storage__BlobContainerName = "yofi-uitest"
$env:ConnectionStrings__DefaultConnection = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=yofi-test-functional;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
Start-Job -Name uitestsbg -ScriptBlock { dotnet watch run } -WorkingDirectory $(Get-Location)
Pop-Location
Remove-Item Env:\ConnectionStrings__DefaultConnection
Remove-Item Env:\Storage__BlobContainerName
Remove-Item Env:\Demo__IsEnabled