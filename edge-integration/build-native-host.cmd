@echo off
setlocal

set "ROOT=%~dp0.."
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
  echo Could not find the .NET Framework C# compiler.
  exit /b 1
)

"%CSC%" /nologo /target:exe /optimize+ /out:"%ROOT%\DevspaceNgrokFoot.NativeHost.exe" "%ROOT%\EdgeNativeHost.cs"
if errorlevel 1 exit /b %errorlevel%

echo Built: %ROOT%\DevspaceNgrokFoot.NativeHost.exe
