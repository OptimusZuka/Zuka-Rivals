@echo off
setlocal
cd /d "%~dp0"
echo === Zuka Rivals - instalacao ===
echo.
where dotnet >nul 2>&1
if errorlevel 1 (
  echo O .NET SDK 10 nao esta instalado. A instalar com o winget...
  winget install --id Microsoft.DotNet.SDK.10 -e --accept-package-agreements --accept-source-agreements
  echo.
  echo Se a instalacao terminou sem erros, FECHA esta janela e abre o instalar.bat outra vez.
  echo Se deu erro, instala manualmente em dotnet.microsoft.com ^(SDK 10^) e volta aqui.
  pause
  exit /b 0
)
set "DEST=%LocalAppData%\ZukaRivals"
echo A instalar em %DEST% ...
if exist "%DEST%\app" rmdir /s /q "%DEST%\app"
if exist "%DEST%\app" (
  echo Nao consegui limpar a instalacao anterior. Fecha o Zuka Rivals e tenta de novo.
  pause
  exit /b 1
)
dotnet publish "%~dp0IRacingTracker.csproj" -c Release -o "%DEST%\app" --nologo
if errorlevel 1 (
  echo.
  echo A instalacao falhou. Copia a mensagem acima, ou tira uma captura, e envia.
  pause
  exit /b 1
)
copy /y "%~dp0icon.ico" "%DEST%\zukarivals.ico" >nul
copy /y "%~dp0launcher.vbs" "%DEST%\launcher.vbs" >nul
powershell -NoProfile -Command "$d=$env:LocalAppData+'\ZukaRivals'; foreach($p in @([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('Programs'))){ foreach($old in 'Zuka Rival.lnk','iRacing Tracker.lnk'){Remove-Item ($p+'\'+$old) -ErrorAction SilentlyContinue}; $s=(New-Object -ComObject WScript.Shell).CreateShortcut($p+'\Zuka Rivals.lnk'); $s.TargetPath=$env:SystemRoot+'\System32\wscript.exe'; $s.Arguments='\"'+$d+'\launcher.vbs\"'; $s.WorkingDirectory=$d; $s.IconLocation=$d+'\zukarivals.ico'; $s.Save() }"
ie4uinit.exe -show >nul 2>&1
echo.
echo Pronto. Instalado em %DEST%
echo Atalho "Zuka Rivals" criado na Area de Trabalho e no Menu Iniciar.
echo Ja podes apagar esta pasta de download.
echo Para arrancar com o Windows: Definicoes ^> Comportamento.
echo Se o icone antigo ainda aparecer, reinicia o Explorador ou o PC (cache de icones do Windows).
pause
