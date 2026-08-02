# Script for building and packaging ERPiFinansije using Velopack (vpk)
$ErrorActionPreference = "Stop"

Write-Host "================================================="
Write-Host "ERPiFinansije -- Build and Packaging (Velopack)"
Write-Host "================================================="

$version = (Get-Content "version.txt").Trim()
Write-Host "Version: $version"

Write-Host "1. Building self-contained binaries..."
dotnet publish ERPiFinansijeApp/ERPiFinansijeApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish_output

Write-Host "2. Packaging with Velopack..."
if (Test-Path "ReleasePackage") {
    Remove-Item -Recurse -Force "ReleasePackage"
}

vpk pack --packId "ERPiFinansije" --packVersion "$version" --packDir "publish_output" --mainExe "ERPiFinansijeApp.exe" --outputDir "ReleasePackage" --packTitle "ERPi Finansije" --packAuthors "Blagojevic Boban" --icon "ERPiFinansijeApp\app.ico"

Write-Host "================================================="
Write-Host "SUCCESS! Installation package created in ReleasePackage\"
Write-Host "Installer: ReleasePackage\ERPiFinansije-$version-win-x64-Setup.exe"
Write-Host "================================================="
