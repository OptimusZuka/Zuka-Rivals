@echo off
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 (
  echo Falta instalar o .NET SDK 10 ^(dotnet.microsoft.com^).
  pause
  exit /b 1
)
dotnet run -c Release
if errorlevel 1 (
  echo.
  echo O programa nao arrancou. Copia a mensagem acima, ou tira uma captura, e envia.
  pause
)
