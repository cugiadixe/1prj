$ErrorActionPreference = "Stop"
Write-Host "Testing Backend..."
Set-Location "$PSScriptRoot\.."
dotnet test src\backend\PTKD-ERP.sln

Write-Host "Testing Frontend..."
Set-Location "$PSScriptRoot\..\src\frontend"
npm run test

Write-Host "Tests Complete!"
