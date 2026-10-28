@echo off
setlocal
set "SRC=%~dp0_build\RengaServerSwitcher"
set "DST=%ProgramFiles%\Renga Professional\Plugins\RengaServerSwitcher"
if not exist "%SRC%\RengaServerSwitcher.Plugin.dll" (
  echo [ERROR] Build the solution first: build.cmd
  pause
  exit /b 1
)
echo Installing to:
echo %DST%
mkdir "%DST%" 2>nul
xcopy "%SRC%\*" "%DST%\" /E /I /Y
if errorlevel 1 (
  echo.
  echo [ERROR] Copy failed. Run this file as Administrator or copy the files manually.
  pause
  exit /b 1
)
echo.
echo Installed. Restart Renga.
pause
