@echo off
title Chat vs Streamer Installer
echo.
echo   Chat vs Streamer Installer
echo   ===========================
echo.
echo   1. Classic installer (console, text-based)
echo   2. Modern installer (graphical, windows and buttons)
echo.
choice /C 12 /N /M "  Choose an option (1 or 2): "

if errorlevel 2 goto modern
if errorlevel 1 goto classic

:classic
PowerShell -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
goto end

:modern
PowerShell -ExecutionPolicy Bypass -File "%~dp0install-gui.ps1"
goto end

:end
pause