$ErrorActionPreference = "Stop"
Write-Host "Starting PTKD ERP Frontend..."
Set-Location "$PSScriptRoot\..\src\frontend"
npm run dev
