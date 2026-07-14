$ErrorActionPreference = "Stop"
Write-Host "Starting PTKD ERP Backend..."
Set-Location "$PSScriptRoot\..\src\backend\PTKD.Api"
dotnet run --urls "http://localhost:5057"
