@echo off
setlocal

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
  echo Could not find the .NET Framework C# compiler.
  exit /b 1
)

set "ICON_TOOL=%TEMP%\DevspaceNgrokFoot.IconGenerator.%RANDOM%%RANDOM%.exe"

"%CSC%" /nologo /target:exe /optimize+ /out:"%ICON_TOOL%" /reference:System.Drawing.dll IconGenerator.cs
if errorlevel 1 exit /b %errorlevel%

"%ICON_TOOL%" DevspaceNgrokFoot.ico icon-preview.png
set "ICON_EXIT=%ERRORLEVEL%"
del /q "%ICON_TOOL%" >nul 2>&1
if not "%ICON_EXIT%"=="0" exit /b %ICON_EXIT%

del /q icon-source.png >nul 2>&1

"%CSC%" /nologo /target:winexe /optimize+ /out:DevspaceNgrokFoot.exe /win32icon:DevspaceNgrokFoot.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll TrayApp.cs
if errorlevel 1 exit /b %errorlevel%

echo Built: %CD%\DevspaceNgrokFoot.exe
