$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
dotnet run --project src/AccessCity.Api
exit $LASTEXITCODE
