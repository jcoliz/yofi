$ErrorActionPreference = "Stop"
del bin\result -Recurse
dotnet test --collect:"XPlat Code Coverage" --settings:coverlet.runsettings -r bin\result
reportgenerator -reports:.\bin\result\*\coverage.cobertura.xml -targetdir:.\bin\result