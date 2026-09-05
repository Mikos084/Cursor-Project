@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "VERSION=0.8.3"
set "APP=MultiplePointers"
set "RELEASE=%cd%\release"
set "PORTABLE_DIR=%RELEASE%\portable\MultiplePointers"
set "PORTABLE_ZIP=%RELEASE%\MultiplePointers_Portable_v%VERSION%.zip"
set "SETUP_EXE=%RELEASE%\MultiplePointers_Setup_v%VERSION%.exe"

echo ==========================================================
echo       Multiple Pointers - RELEASE BUILDER v%VERSION%
echo ==========================================================
echo.
echo Ten skrypt tworzy DWA pliki do wysylania innym osobom:
echo.
echo   1. MultiplePointers_Setup_v%VERSION%.exe
echo      - normalny instalator Windows
echo.
echo   2. MultiplePointers_Portable_v%VERSION%.zip
echo      - wypakuj i uruchom, bez instalacji
echo.
echo Odbiorcy NIE potrzebuja .NET SDK ani .NET Runtime.
echo.
pause

where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo [BLAD] Nie znaleziono .NET SDK na komputerze BUDUJACYM release.
    echo.
    echo .NET SDK jest potrzebne tylko osobie, ktora tworzy paczki.
    echo Odbiorcy programu go nie potrzebuja.
    echo.
    pause
    exit /b 1
)

echo.
echo [1/5] Czyszczenie starego release...
if exist "%RELEASE%" rmdir /s /q "%RELEASE%"
mkdir "%PORTABLE_DIR%"

echo.
echo [2/5] Budowanie wersji self-contained Windows x64...
dotnet publish MultiplePointers.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -p:PublishTrimmed=false ^
  -p:DebugType=None ^
  -o "%PORTABLE_DIR%"

if errorlevel 1 goto :buildfail

echo.
echo [3/5] Dodawanie instrukcji do Portable...
copy /y "INSTRUKCJA_PL.txt" "%PORTABLE_DIR%\INSTRUKCJA_PL.txt" >nul
copy /y "README.md" "%PORTABLE_DIR%\README.md" >nul

echo.
echo [4/5] Tworzenie Portable ZIP...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "if (Test-Path '%PORTABLE_ZIP%') { Remove-Item -Force '%PORTABLE_ZIP%' };" ^
  "Compress-Archive -Path '%PORTABLE_DIR%\*' -DestinationPath '%PORTABLE_ZIP%' -CompressionLevel Optimal"

if errorlevel 1 (
    echo.
    echo [BLAD] Nie udalo sie utworzyc ZIP.
    goto :buildfail
)

echo.
echo [5/5] Tworzenie instalatora EXE...

set "ISCC="

if exist "%ProgramFiles%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"

if not defined ISCC (
    echo.
    echo ----------------------------------------------------------
    echo PORTABLE ZIP JEST GOTOWY.
    echo ----------------------------------------------------------
    echo.
    echo Nie znaleziono Inno Setup na komputerze budujacym,
    echo dlatego nie moge utworzyc instalatora EXE.
    echo.
    echo Aby tworzyc Setup.exe, zainstaluj Inno Setup 7.
    echo Potem uruchom ponownie BUILD_RELEASE.bat.
    echo.
    echo Portable:
    echo %PORTABLE_ZIP%
    echo.
    start "" "%RELEASE%"
    pause
    exit /b 2
)

echo Uzywam:
echo "%ISCC%"
echo.

"%ISCC%" "installer.iss"
if errorlevel 1 goto :installerfail

echo.
echo ==========================================================
echo                     RELEASE GOTOWY
echo ==========================================================
echo.
echo Do wyslania innym osobom:
echo.
echo INSTALATOR:
echo %SETUP_EXE%
echo.
echo PORTABLE:
echo %PORTABLE_ZIP%
echo.
echo Nie wysylaj folderow bin ani obj.
echo Odbiorcy nie uruchamiaja build.bat.
echo.
start "" "%RELEASE%"
pause
exit /b 0

:installerfail
echo.
echo [BLAD] Portable ZIP zostal utworzony, ale kompilacja instalatora
echo nie powiodla sie.
echo.
echo Portable nadal znajdziesz tutaj:
echo %PORTABLE_ZIP%
echo.
pause
exit /b 3

:buildfail
echo.
echo [BLAD] Budowanie release zakonczylo sie niepowodzeniem.
echo.
pause
exit /b 1
