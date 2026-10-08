@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "NOPAUSE=0"
if /I "%~1"=="/nopause" set "NOPAUSE=1"

title Shutdown Utility Pro - Build

echo ================================================
echo      Shutdown Utility Pro 3.0 - Build
echo ================================================
echo.

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: .NET Framework C# compiler was not found.
  echo Expected .NET Framework 4.x on this Windows installation.
  if "%NOPAUSE%"=="0" pause
  exit /b 1
)

set "SOURCE_DIR=src\ShutdownUtility"
set "ASSET_DIR=assets"
set "CONFIG_DIR=config"
set "OUTPUT_DIR=artifacts"
set "SOURCES=%SOURCE_DIR%\AssemblyInfo.cs %SOURCE_DIR%\AppConfig.cs %SOURCE_DIR%\Logger.cs %SOURCE_DIR%\WindowsServices.cs %SOURCE_DIR%\CountdownForm.cs %SOURCE_DIR%\MainForm.cs %SOURCE_DIR%\Program.cs"
for %%F in (AssemblyInfo.cs AppConfig.cs Logger.cs WindowsServices.cs CountdownForm.cs MainForm.cs Program.cs app.manifest) do (
  if not exist "%SOURCE_DIR%\%%F" (
    echo ERROR: %SOURCE_DIR%\%%F is missing.
    if "%NOPAUSE%"=="0" pause
    exit /b 1
  )
)

if not exist "%CONFIG_DIR%\Shutdown.config" (
  echo ERROR: %CONFIG_DIR%\Shutdown.config is missing.
  if "%NOPAUSE%"=="0" pause
  exit /b 1
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if errorlevel 1 (
  echo ERROR: Could not create %OUTPUT_DIR%.
  if "%NOPAUSE%"=="0" pause
  exit /b 1
)

set "BUILD_EXE=%OUTPUT_DIR%\Shutdown.build.exe"
if exist "%BUILD_EXE%" del /q "%BUILD_EXE%"

set "COMMON=/target:winexe /optimize+ /debug- /checked+ /out:"%BUILD_EXE%" /win32manifest:"%SOURCE_DIR%\app.manifest" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll"

if exist "%ASSET_DIR%\Shutdown.ico" (
  echo Building with embedded Shutdown.ico...
  "%CSC%" %COMMON% /win32icon:"%ASSET_DIR%\Shutdown.ico" %SOURCES%
) else (
  echo Building without custom icon.
  echo Put Shutdown.ico in %ASSET_DIR% and rebuild to embed it.
  "%CSC%" %COMMON% %SOURCES%
)

if errorlevel 1 (
  if exist "%BUILD_EXE%" del /q "%BUILD_EXE%"
  echo.
  echo ================= BUILD FAILED =================
  if "%NOPAUSE%"=="0" pause
  exit /b 1
)

move /Y "%BUILD_EXE%" "%OUTPUT_DIR%\Shutdown.exe" >nul
if errorlevel 1 (
  echo.
  echo ERROR: Could not replace %OUTPUT_DIR%\Shutdown.exe. Close the running application and try again.
  if "%NOPAUSE%"=="0" pause
  exit /b 1
)

copy /Y "%CONFIG_DIR%\Shutdown.config" "%OUTPUT_DIR%\Shutdown.config" >nul
if errorlevel 1 (
  echo ERROR: Could not copy the default configuration.
  if "%NOPAUSE%"=="0" pause
  exit /b 1
)
for %%F in (Shutdown.wav Restart.wav Sleep.wav Hibernate.wav Lock.wav) do (
  if exist "%ASSET_DIR%\%%F" (
    copy /Y "%ASSET_DIR%\%%F" "%OUTPUT_DIR%\%%F" >nul
    if errorlevel 1 (
      echo ERROR: Could not copy %ASSET_DIR%\%%F.
      if "%NOPAUSE%"=="0" pause
      exit /b 1
    )
  )
)
if exist "%ASSET_DIR%\Shutdown.ico" (
  copy /Y "%ASSET_DIR%\Shutdown.ico" "%OUTPUT_DIR%\Shutdown.ico" >nul
  if errorlevel 1 (
    echo ERROR: Could not copy the application icon.
    if "%NOPAUSE%"=="0" pause
    exit /b 1
  )
)

echo.
echo ================= BUILD OK ======================
echo Output: %CD%\%OUTPUT_DIR%\Shutdown.exe
echo.
echo The first launch opens the dashboard in Test mode. Real actions require turning Test mode off.
echo ================================================
if "%NOPAUSE%"=="0" pause
