@echo off
setlocal
cd /d "%~dp0"

echo [1/3] Restoring packages...
dotnet restore OpenCode.Desktop.Widget.csproj || exit /b 1

echo [2/3] Publishing self-contained client...
dotnet publish OpenCode.Desktop.Widget.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\win-x64-self-contained || exit /b 1

echo [3/3] Creating client-only ZIP...
if not exist release mkdir release
powershell -NoProfile -ExecutionPolicy Bypass -Command "$out='release\OpenCode-Desktop-Widget-Pro-client.zip'; if(Test-Path $out){Remove-Item $out}; Compress-Archive -Path 'publish\win-x64-self-contained\*' -DestinationPath $out -CompressionLevel Optimal" || exit /b 1

echo.
echo Done: release\OpenCode-Desktop-Widget-Pro-client.zip
echo The seller private key is NOT included.
pause
