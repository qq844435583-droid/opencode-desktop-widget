@echo off
setlocal
cd /d "%~dp0"

dotnet publish OpenCode.Desktop.Widget.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\win-x64-self-contained || exit /b 1

echo.
echo Done: publish\win-x64-self-contained\OpenCode.Desktop.Widget.exe
pause
