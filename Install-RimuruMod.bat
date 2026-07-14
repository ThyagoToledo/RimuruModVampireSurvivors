@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0installer\Install-RimuruMod.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
echo.
if not "%EXITCODE%"=="0" echo A instalacao terminou com erro. Veja a mensagem acima.
pause
exit /b %EXITCODE%
