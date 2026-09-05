@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "VERSION=0.8.3"
set "RELEASE=%cd%\release"
set "PORTABLE_DIR=%RELEASE%\portable\MultiplePointers"
set "PORTABLE_ZIP=%RELEASE%\MultiplePointers_Portable_v%VERSION%.zip"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo Nie znaleziono .NET SDK.
    echo SDK jest potrzebne tylko na komputerze tworzacym release.
    pause
    exit /b 1
)

if not exist "%RELEASE%" mkdir "%RELEASE%"
if exist "%PORTABLE_DIR%" rmdir /s /q "%PORTABLE_DIR%"
mkdir "%PORTABLE_DIR%"

dotnet publish MultiplePointers.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -p:PublishTrimmed=false ^
  -p:DebugType=None ^
  -o "%PORTABLE_DIR%"

if errorlevel 1 goto :fail

copy /y "INSTRUKCJA_PL.txt" "%PORTABLE_DIR%\INSTRUKCJA_PL.txt" >nul
copy /y "README.md" "%PORTABLE_DIR%\README.md" >nul

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "if (Test-Path '%PORTABLE_ZIP%') { Remove-Item -Force '%PORTABLE_ZIP%' };" ^
  "Compress-Archive -Path '%PORTABLE_DIR%\*' -DestinationPath '%PORTABLE_ZIP%' -CompressionLevel Optimal"

if errorlevel 1 goto :fail

echo.
echo GOTOWE:
echo %PORTABLE_ZIP%
start "" "%RELEASE%"
pause
exit /b 0

:fail
echo.
echo Build zakonczyl sie bledem.
pause
exit /b 1
