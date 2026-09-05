@echo off
setlocal
cd /d "%~dp0"

echo =============================================
echo Multiple Pointers v0.8.3 - framework build
echo =============================================

where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo Nie znaleziono .NET SDK.
    pause
    exit /b 1
)

dotnet restore MultiplePointers.csproj
if errorlevel 1 goto :fail

dotnet publish MultiplePointers.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained false ^
  -p:PublishSingleFile=false ^
  -p:PublishTrimmed=false

if errorlevel 1 goto :fail

echo.
echo GOTOWE:
echo %cd%\bin\Release\net8.0-windows\win-x64\publish\
pause
exit /b 0

:fail
echo.
echo Build zakonczyl sie bledem.
pause
exit /b 1
