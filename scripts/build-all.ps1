$ErrorActionPreference = "Stop"
Write-Host "Building Backend..."
Set-Location "$PSScriptRoot\.."
dotnet build src\backend\PTKD-ERP.sln --warnaserror

Write-Host "Building Frontend..."
Set-Location "$PSScriptRoot\..\src\frontend"
npm run build

Write-Host "Build Complete!"
