@echo off
REM ---------------------------------------------------------------------------
REM Build UnityBleWindows.dll with clang-cl (no CMake required).
REM
REM Requirements:
REM   - LLVM/Clang for Windows (clang-cl). Either the "C++ Clang tools for
REM     Windows" Visual Studio component, or a standalone LLVM install on PATH.
REM   - Windows 10/11 SDK (provides the C++/WinRT headers and WindowsApp.lib).
REM
REM This script locates Visual Studio via vswhere and calls vcvars64.bat so the
REM Windows SDK / CRT include and lib paths are available to clang-cl, then
REM compiles the DLL. Target architecture: x64.
REM ---------------------------------------------------------------------------
setlocal enableextensions

set "SCRIPT_DIR=%~dp0"
set "OUT_DIR=%SCRIPT_DIR%build"

REM --- Set up the Windows SDK / CRT environment (INCLUDE, LIB) ---
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo [build] vswhere not found. Install Visual Studio with the C++ workload.
    exit /b 1
)
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -property installationPath`) do set "VSPATH=%%i"
if not defined VSPATH (
    echo [build] Visual Studio installation not found.
    exit /b 1
)
if not exist "%VSPATH%\VC\Auxiliary\Build\vcvars64.bat" (
    echo [build] vcvars64.bat not found. Install the C++ build tools / Windows SDK.
    exit /b 1
)
call "%VSPATH%\VC\Auxiliary\Build\vcvars64.bat" >nul || (echo [build] vcvars64 failed & exit /b 1)

REM Prefer clang-cl when available, otherwise fall back to MSVC cl.exe.
REM Both use the same MSVC-style flags and the Windows SDK set up by vcvars64.
set "CXX=clang-cl"
where clang-cl >nul 2>&1 || set "CXX=cl"

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

echo [build] Compiling UnityBleWindows.dll with %CXX% ...
%CXX% /nologo /std:c++20 /EHsc /MD /LD /O2 ^
    /DWIN32_LEAN_AND_MEAN /DNOMINMAX /DUNICODE /D_UNICODE ^
    "%SCRIPT_DIR%src\UnityBleWindows.cpp" ^
    /Fo"%OUT_DIR%\\" ^
    /Fe"%OUT_DIR%\UnityBleWindows.dll" ^
    /link WindowsApp.lib

if errorlevel 1 (
    echo [build] FAILED
    exit /b 1
)

echo [build] OK -^> "%OUT_DIR%\UnityBleWindows.dll"
echo [build] Copy it to Runtime\Plugins\Windows\x86_64\UnityBleWindows.dll
endlocal
