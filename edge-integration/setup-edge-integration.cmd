@echo off
setlocal

call "%~dp0build-native-host.cmd"
if errorlevel 1 exit /b %errorlevel%

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0register-native-host.ps1"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Edge integration is registered.
echo In Edge, enable Developer mode and choose "Load unpacked".
echo Select this folder:
echo %~dp0..\edge-extension
echo.

set "EDGE=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
if not exist "%EDGE%" set "EDGE=C:\Program Files\Microsoft\Edge\Application\msedge.exe"
if exist "%EDGE%" (
  start "" "%EDGE%" "edge://extensions"
) else (
  echo Could not locate Microsoft Edge automatically. Open edge://extensions manually.
)
start "" explorer.exe "%~dp0..\edge-extension"
