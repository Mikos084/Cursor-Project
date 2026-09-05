@echo off
setlocal
cd /d "%~dp0"

if not exist "release\portable\MultiplePointers\MultiplePointers.exe" (
    echo Najpierw trzeba zbudowac wersje portable.
    echo Uruchom BUILD_RELEASE.bat albo BUILD_PORTABLE_ONLY.bat.
    pause
    exit /b 1
)

set "ISCC="
if exist "%ProgramFiles%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"

if not defined ISCC (
    echo Nie znaleziono Inno Setup 6 lub 7.
    echo Inno Setup jest potrzebny tylko do TWORZENIA instalatora.
    pause
    exit /b 2
)

"%ISCC%" "installer.iss"
if errorlevel 1 (
    echo Kompilacja instalatora nie powiodla sie.
    pause
    exit /b 3
)

echo.
echo Instalator gotowy w folderze release.
start "" "release"
pause
