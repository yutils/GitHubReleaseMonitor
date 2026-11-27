@echo off
setlocal
cd /d "%~dp0"

where powershell >nul 2>nul
if errorlevel 1 (
    echo PowerShell not found. Windows 10/11 is required.
    if "%~1"=="" pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
set "RESULT=%ERRORLEVEL%"

if not "%RESULT%"=="0" (
    echo.
    echo Build failed with exit code %RESULT%.
) else (
    echo.
    echo Build finished. See the output path above.
)

if "%~1"=="" pause
exit /b %RESULT%
