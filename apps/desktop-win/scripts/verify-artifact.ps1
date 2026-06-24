#Requires -Version 5.1
param(
  [Parameter(Mandatory = $true)]
  [string[]]$Artifacts,
  [string]$ManifestPath = "",
  [switch]$RequireAuthenticode
)

$ErrorActionPreference = "Stop"

function Get-FileHashes([string]$Path) {
  $sha256 = (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLowerInvariant()
  $sha512 = (Get-FileHash -Path $Path -Algorithm SHA512).Hash.ToLowerInvariant()
  return @{ sha256 = $sha256; sha512 = $sha512 }
}

$manifest = [ordered]@{
  generatedAt = (Get-Date).ToUniversalTime().ToString("o")
  artifacts   = @()
}

$checksumLines = @()
$failed = $false

foreach ($artifact in $Artifacts) {
  if (-not (Test-Path $artifact)) {
    Write-Error "Artefacto não encontrado: $artifact"
    $failed = $true
    continue
  }

  $name = Split-Path $artifact -Leaf
  $hashes = Get-FileHashes $artifact
  $sig = Get-AuthenticodeSignature -FilePath $artifact

  Write-Host "::group::Verificar $name"
  Write-Host "SHA-256: $($hashes.sha256)"
  Write-Host "SHA-512: $($hashes.sha512)"
  Write-Host "Assinatura: $($sig.Status)"

  if ($RequireAuthenticode -and $sig.Status -notin @("Valid", "UnknownError")) {
    # Self-signed pode reportar UnknownError em alguns runners; Valid é o ideal.
    if ($sig.Status -ne "Valid") {
      Write-Warning "Assinatura não Valid: $($sig.Status)"
    }
  }

  $manifest.artifacts += [ordered]@{
    file   = $name
    path   = (Resolve-Path $artifact).Path
    sha256 = $hashes.sha256
    sha512 = $hashes.sha512
    signatureStatus = $sig.Status.ToString()
    signer = $sig.SignerCertificate?.Subject
  }

  $checksumLines += "$($hashes.sha256)  $name"
  $checksumLines += "$($hashes.sha512)  $name (sha512)"
  Write-Host "::endgroup::"
}

$outDir = Split-Path ($Artifacts[0]) -Parent
$checksumFile = Join-Path $outDir "checksums.sha256"
$checksumLines | Where-Object { $_ -match "sha256" } | ForEach-Object { $_ -replace " \(sha512\)", "" } | Set-Content $checksumFile -Encoding utf8

$manifestFile = if ($ManifestPath) { $ManifestPath } else { Join-Path $outDir "manifest.json" }
$manifest | ConvertTo-Json -Depth 6 | Set-Content $manifestFile -Encoding utf8

Write-Host "Checksums: $checksumFile"
Write-Host "Manifest:  $manifestFile"

if ($failed) { exit 1 }
exit 0
