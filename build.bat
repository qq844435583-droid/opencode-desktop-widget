@echo off
setlocal
cd /d "%~dp0"

echo [1/2] Restoring packages...
dotnet restore OpenCode.Desktop.Widget.csproj || exit /b 1

echo [2/2] Publishing WebView2 app...
dotnet publish OpenCode.Desktop.Widget.csproj -c Release -r win-x64 --self-contained false -o publish\win-x64 || exit /b 1

echo.
echo Done: publish\win-x64\OpenCode.Desktop.Widget.exe
pause
