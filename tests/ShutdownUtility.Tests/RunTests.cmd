@echo off
setlocal EnableExtensions
cd /d "%~dp0..\.."

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: .NET Framework C# compiler was not found.
  exit /b 1
)

set "TEST_EXE=%TEMP%\ShutdownUtilityPro.Tests.%RANDOM%.exe"
"%CSC%" /nologo /target:exe /optimize+ /checked+ /out:"%TEST_EXE%" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "tests\ShutdownUtility.Tests\RegressionTests.cs" "src\ShutdownUtility\AppConfig.cs" "src\ShutdownUtility\CommandLineParser.cs" "src\ShutdownUtility\InstallationDiagnostics.cs" "src\ShutdownUtility\Logger.cs" "src\ShutdownUtility\WindowsServices.cs"
if errorlevel 1 (
  if exist "%TEST_EXE%" del /q "%TEST_EXE%"
  exit /b 1
)

"%TEST_EXE%"
set "TEST_RESULT=%ERRORLEVEL%"
if exist "%TEST_EXE%" del /q "%TEST_EXE%"
exit /b %TEST_RESULT%
