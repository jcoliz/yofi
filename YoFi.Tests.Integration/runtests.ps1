$ErrorActionPreference = "Ignore"
del bin\result -Recurse
$ErrorActionPreference = "Stop"
dotnet test --collect:"XPlat Code Coverage" --settings:coverlet.runsettings -r bin\result
reportgenerator -reports:.\bin\result\*\coverage.cobertura.xml -targetdir:.\bin\result