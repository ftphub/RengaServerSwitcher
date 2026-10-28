@echo off
setlocal
cd /d "%~dp0"
if not exist "lib\RengaCOMAPI.tlb" (
  echo [ERROR] Missing official SDK file: lib\RengaCOMAPI.tlb
  echo Run: powershell -NoProfile -ExecutionPolicy Bypass -File scripts\prepare-sdk.ps1 -SdkZip "C:\path\93 RengaSDK.zip"
  exit /b 1
)
if not exist "lib\Renga.NET8.PluginUtility.dll" (
  echo [ERROR] Missing official SDK file: lib\Renga.NET8.PluginUtility.dll
  exit /b 1
)
set "MSBUILD="
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
  for /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do if not defined MSBUILD set "MSBUILD=%%i"
)
if not defined MSBUILD (
  for %%X in (MSBuild.exe) do set "MSBUILD=%%~$PATH:X"
)
if not defined MSBUILD (
  echo [ERROR] MSBuild from Visual Studio 2022 not found. Open solution in Visual Studio and build Release x64.
  exit /b 1
)
echo [INFO] Using MSBuild: %MSBUILD%
"%MSBUILD%" "RengaServerSwitcher.sln" /m /p:Configuration=Release /p:Platform=x64 /t:Build
if errorlevel 1 (
  echo [ERROR] Build failed. Check the compiler output.
  exit /b 1
)
echo [OK] Output: %~dp0_build\RengaServerSwitcher
