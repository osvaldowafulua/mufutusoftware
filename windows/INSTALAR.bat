@echo off
chcp 65001 >nul
title MUFUTU - Instalador
echo.
echo  MUFUTU - Instalacao em Program Files
echo  =====================================
echo.
echo  Este assistente instala o MUFUTU no computador
echo  (atalhos, Menu Iniciar, desinstalar em Definicoes).
echo.
echo  Preferir no futuro: MUFUTU-Setup-*.exe (instalador oficial)
echo.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0install-mufutu.ps1\"'"
if errorlevel 1 (
  echo.
  echo  Falha ao pedir permissoes de Administrador.
  echo  Clique direito neste ficheiro - Executar como administrador
  pause
)
