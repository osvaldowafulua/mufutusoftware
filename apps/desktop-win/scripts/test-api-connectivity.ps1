#Requires -Version 5.1
<#
.SYNOPSIS
  Valida ligação HTTPS à API MUFUTU (produção ou override).
#>
param(
  [string]$ApiBaseUrl = $env:MUFUTU_API_URL,
  [int]$TimeoutSec = 20
)

$ErrorActionPreference = "Stop"

if (-not $ApiBaseUrl) {
  $ApiBaseUrl = "https://api.mufutu.ao/api"
}

$healthUrl = $ApiBaseUrl.TrimEnd('/') + "/health"
Write-Host "A testar $healthUrl (timeout ${TimeoutSec}s)…"

try {
  $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec $TimeoutSec
  Write-Host "OK $($response.StatusCode) — $($response.Content)"
  exit 0
} catch {
  Write-Error "Falha de conectividade: $_"
  exit 1
}
