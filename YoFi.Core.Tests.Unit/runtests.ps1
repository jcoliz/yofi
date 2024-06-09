$ErrorActionPreference = "Ignore"
del TestResults -Recurse
del bin\results -Recurse
$ErrorActionPreference = "Stop"
dotnet test --collect:"XPlat Code Coverage" --settings:coverlet.runsettings
reportgenerator -reports:.\TestResults\*\coverage.cobertura.xml -targetdir:.\bin\result
start .\bin\result\index.html
