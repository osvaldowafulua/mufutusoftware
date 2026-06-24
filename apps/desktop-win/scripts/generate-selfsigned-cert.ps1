#Requires -Version 5.1
<#
.SYNOPSIS
  Gera certificado self-signed para assinatura Authenticode (dev/CI sem certificado comercial).
.DESCRIPTION
  Exporta PFX para uso local ou GitHub Secrets (WIN_CODESIGN_PFX em base64).
#>
param(
  [string]$OutputDir = "$PSScriptRoot\certs",
  [string]$Password = "MufutuDevSign!2026",
  [string]$Subject = "CN=MUFUTU Desktop Dev, O=Muapi Holding, C=AO"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$pfxPath = Join-Path $OutputDir "mufutu-codesign.pfx"
$cerPath = Join-Path $OutputDir "mufutu-codesign.cer"

if (Test-Path $pfxPath) {
  Write-Host "Certificado já existe: $pfxPath"
  exit 0
}

$cert = New-SelfSignedCertificate `
  -Type CodeSigningCert `
  -Subject $Subject `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -HashAlgorithm SHA256 `
  -NotAfter (Get-Date).AddYears(3) `
  -CertStoreLocation "Cert:\CurrentUser\My"

$secure = ConvertTo-SecureString -String $Password -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secure | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Write-Host "::group::Certificado gerado"
Write-Host "PFX: $pfxPath"
Write-Host "CER: $cerPath"
Write-Host "Password (guardar em WIN_CODESIGN_PASSWORD): $Password"
Write-Host ""
Write-Host "GitHub Secret WIN_CODESIGN_PFX (base64):"
[Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath))
Write-Host "::endgroup::"
