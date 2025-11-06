@echo off
echo Visualised Markup - Windows Installation
echo ========================================
echo.

REM Check for .NET 9
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 9 SDK not found!
    echo Please install from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Extracting source from database...
bash stub.sh

echo Building application...
dotnet build -c Release

if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Installation complete!
echo ========================================
echo.
echo Run: bin\Release\net9.0\VB.exe
echo.
pause
