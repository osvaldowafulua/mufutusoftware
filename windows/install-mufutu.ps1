# MUFUTU — Instalador PowerShell (quando só existe ZIP portátil)
# Executar como Administrador no Windows:
#   Set-ExecutionPolicy Bypass -Scope Process -Force
#   .\install-mufutu.ps1

$ErrorActionPreference = "Stop"
$AppName = "MUFUTU"
$InstallDir = "${env:ProgramFiles}\MUFUTU"
$ZipName = "MUFUTU-*-win-x64.zip"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== Instalador $AppName ===" -ForegroundColor Cyan

$Zip = Get-ChildItem -Path $ScriptDir -Filter $ZipName -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $Zip) {
    $Zip = Get-ChildItem -Path (Get-Location) -Filter $ZipName -ErrorAction SilentlyContinue | Select-Object -First 1
}
if (-not $Zip) {
    Write-Error "Coloque este script na mesma pasta que $ZipName"
}

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Execute como Administrador (clique direito → Executar como administrador)"
}

Write-Host "A instalar em: $InstallDir"
if (Test-Path $InstallDir) { Remove-Item -Recurse -Force $InstallDir }
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

$Temp = Join-Path $env:TEMP "mufutu-install"
if (Test-Path $Temp) { Remove-Item -Recurse -Force $Temp }
Expand-Archive -Path $Zip.FullName -DestinationPath $Temp -Force
Copy-Item -Path "$Temp\*" -Destination $InstallDir -Recurse -Force
Remove-Item -Recurse -Force $Temp

$Exe = Join-Path $InstallDir "MUFUTU.exe"
if (-not (Test-Path $Exe)) { Write-Error "MUFUTU.exe nao encontrado apos extrair" }

# Atalho Ambiente de trabalho
$Wsh = New-Object -ComObject WScript.Shell
$Desktop = [Environment]::GetFolderPath("Desktop")
$Shortcut = $Wsh.CreateShortcut("$Desktop\$AppName.lnk")
$Shortcut.TargetPath = $Exe
$Shortcut.WorkingDirectory = $InstallDir
$Shortcut.Save()

# Atalho Menu Iniciar
$StartMenu = [Environment]::GetFolderPath("Programs")
$Shortcut2 = $Wsh.CreateShortcut("$StartMenu\$AppName.lnk")
$Shortcut2.TargetPath = $Exe
$Shortcut2.WorkingDirectory = $InstallDir
$Shortcut2.Save()

Write-Host ""
Write-Host "Instalacao concluida." -ForegroundColor Green
Write-Host "  Pasta: $InstallDir"
Write-Host "  Atalhos: Ambiente de trabalho + Menu Iniciar"
Write-Host ""
Write-Host "Preferir no futuro: MUFUTU-Setup-*.exe (instalador oficial NSIS)" -ForegroundColor Yellow
$open = Read-Host "Abrir MUFUTU agora? (S/n)"
if ($open -ne "n" -and $open -ne "N") { Start-Process $Exe }
