$ErrorActionPreference = "Ignore"
Remove-Item .\*\TestResults\* -Recurse
$ErrorActionPreference = "Stop"
dotnet test --collect:"XPlat Code Coverage" --settings:test\coverlet.runsettings
reportgenerator -reports:.\*\TestResults\*\coverage.cobertura.xml -targetdir:.\bin\result