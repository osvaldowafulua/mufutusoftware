#Requires -Version 5.1
<#
.SYNOPSIS
  Build completo MUFUTU Desktop Windows: publish → obfuscar → WiX → assinar → verificar.
#>
param(
  [string]$Configuration = "Release",
  [string]$Version = "1.0.0",
  [switch]$SkipSign,
  [switch]$SkipObfuscate,
  [switch]$AllowZipFallback
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$PublishDir = Join-Path $Root "artifacts\publish"
$ObfuscatedDir = Join-Path $Root "artifacts\obfuscated"
$InstallerDir = Join-Path $Root "artifacts\installer"
$LogDir = Join-Path $Root "artifacts\logs"

New-Item -ItemType Directory -Force -Path $PublishDir, $ObfuscatedDir, $InstallerDir, $LogDir | Out-Null

Write-Host "::group::dotnet restore & test"
Push-Location $Root
dotnet restore Mufutu.sln 2>&1 | Tee-Object (Join-Path $LogDir "restore.log")
dotnet test tests\Mufutu.Desktop.Tests\Mufutu.Desktop.Tests.csproj -c $Configuration --no-restore 2>&1 | Tee-Object (Join-Path $LogDir "test.log")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "::endgroup::"

Write-Host "::group::dotnet publish"
# Self-contained: o runtime .NET vai dentro do instalador — máquinas de mina
# sem internet não conseguem descarregar o runtime no primeiro arranque.
dotnet publish src\Mufutu.Desktop\Mufutu.Desktop.csproj `
  -c $Configuration `
  -r win-x64 `
  --self-contained true `
  -p:Version=$Version `
  -o $PublishDir 2>&1 | Tee-Object (Join-Path $LogDir "publish.log")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "::endgroup::"

$SourceDir = $PublishDir
if (-not $SkipObfuscate) {
  Write-Host "::group::Obfuscar"
  dotnet tool install --global Obfuscar.GlobalTool --version 2.2.49 2>$null
  $coreDll = Join-Path $PublishDir "Mufutu.Desktop.Core.dll"
  $licDll = Join-Path $PublishDir "Mufutu.Desktop.Licensing.dll"
  Copy-Item $coreDll, $licDll -Destination $ObfuscatedDir
  Push-Location $ObfuscatedDir
  $obfConfig = Join-Path $Root "scripts\obfuscar.xml"
  (Get-Content $obfConfig) -replace '\$\(InPath\)', $ObfuscatedDir -replace '\$\(OutPath\)', (Join-Path $ObfuscatedDir "out") | Set-Content (Join-Path $ObfuscatedDir "obfuscar.run.xml")
  obfuscar.console (Join-Path $ObfuscatedDir "obfuscar.run.xml") 2>&1 | Tee-Object (Join-Path $LogDir "obfuscar.log")
  Copy-Item (Join-Path $ObfuscatedDir "out\*.dll") -Destination $PublishDir -Force
  Pop-Location
  Write-Host "::endgroup::"
}

Write-Host "::group::WiX installer"
dotnet tool install --global wix --version 5.0.2 2>$null
Push-Location (Join-Path $Root "installer\wix")
dotnet build Mufutu.Package.wixproj -c $Configuration -p:PublishDir=$PublishDir -p:Version=$Version 2>&1 | Tee-Object (Join-Path $LogDir "wix-msi.log")
dotnet build Mufutu.Bundle.wixproj -c $Configuration -p:Version=$Version 2>&1 | Tee-Object (Join-Path $LogDir "wix-bundle.log")
$msi = Get-ChildItem -Recurse -Filter "*.msi" | Select-Object -First 1
$exe = Get-ChildItem -Recurse -Filter "MUFUTU-Setup.exe" | Select-Object -First 1
if ($msi) { Copy-Item $msi.FullName -Destination (Join-Path $InstallerDir "MUFUTU-$Version-x64.msi") -Force }
if ($exe) { Copy-Item $exe.FullName -Destination (Join-Path $InstallerDir "MUFUTU-Setup-$Version-x64.exe") -Force }
Pop-Location
Write-Host "::endgroup::"

if (-not $SkipSign) {
  Write-Host "::group::Assinatura Authenticode"
  $pfxPath = Join-Path $Root "scripts\certs\mufutu-codesign.pfx"
  if (-not (Test-Path $pfxPath)) {
    & (Join-Path $Root "scripts\generate-selfsigned-cert.ps1")
  }
  $password = $env:WIN_CODESIGN_PASSWORD
  if (-not $password) { $password = "MufutuDevSign!2026" }

  $signtool = Get-ChildItem -Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
  if (-not $signtool) {
    Write-Warning "signtool não encontrado — saltar assinatura"
  } else {
    Get-ChildItem $InstallerDir -Include *.exe,*.msi -Recurse | ForEach-Object {
      & $signtool.FullName sign /fd SHA256 /f $pfxPath /p $password /tr http://timestamp.digicert.com /td SHA256 $_.FullName 2>&1 |
        Tee-Object -FilePath (Join-Path $LogDir "signtool.log") -Append
    }
  }
  Write-Host "::endgroup::"
}

Write-Host "::group::Verificação de integridade"
# ZIP portátil publica sempre (IT/testes) — o Setup.exe é o artefacto principal
$zip = Join-Path $InstallerDir "MUFUTU-$Version-win-x64.zip"
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $zip -Force

$setup = @(Get-ChildItem $InstallerDir -Recurse -Include *.exe,*.msi | ForEach-Object { $_.FullName })
if ($setup.Count -eq 0) {
  if ($AllowZipFallback) {
    Write-Warning "WiX não produziu Setup/MSI — a publicar apenas o ZIP (fallback autorizado)."
  } else {
    Write-Error "WiX não produziu MUFUTU-Setup — ver $LogDir\wix-msi.log e $LogDir\wix-bundle.log."
    exit 1
  }
}
$artifacts = @(Get-ChildItem $InstallerDir -Recurse -Include *.exe,*.msi,*.zip | ForEach-Object { $_.FullName })
& (Join-Path $Root "scripts\verify-artifact.ps1") -Artifacts $artifacts
Write-Host "::endgroup::"

Pop-Location
Write-Host "Build concluído → $InstallerDir"
