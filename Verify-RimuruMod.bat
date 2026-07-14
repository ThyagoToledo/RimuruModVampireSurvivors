@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0installer\Verify-RimuruMod.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
echo.
if "%EXITCODE%"=="0" echo Verificacao concluida com sucesso.
if not "%EXITCODE%"=="0" echo A verificacao encontrou problemas.
pause
exit /b %EXITCODE%
