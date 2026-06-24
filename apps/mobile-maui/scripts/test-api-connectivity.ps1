#Requires -Version 5.1
param(
  [string]$ApiBaseUrl = $env:MUFUTU_API_URL,
  [int]$TimeoutSec = 20
)

$ErrorActionPreference = "Stop"
if (-not $ApiBaseUrl) { $ApiBaseUrl = "https://api.mufutu.ao/api" }
$healthUrl = $ApiBaseUrl.TrimEnd('/') + "/health"
Write-Host "A testar $healthUrl …"
$response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec $TimeoutSec
Write-Host "OK $($response.StatusCode) — $($response.Content)"
