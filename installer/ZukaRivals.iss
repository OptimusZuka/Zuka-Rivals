; Zuka Rivals - instalador (Inno Setup 6). Compilar com:  iscc installer\ZukaRivals.iss /DAppVersion=1.0.0
; Espera a pasta "publish" (dotnet publish -c Release -o publish) na raiz do repositorio.
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define AppName "Zuka Rivals"
#define AppExe  "ZukaRivals.exe"

[Setup]
AppId={{6B0C7B7E-4C1D-4F7B-9C55-5A2E7D1A9F10}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Zuka
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Instala so para o utilizador atual, sem pedir administrador. O utilizador pode escolher "todos os utilizadores".
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\Output
OutputBaseFilename=ZukaRivals-Setup-{#AppVersion}
SetupIconFile=..\icon.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion createallsubdirs
Source: "..\LEIA-ME.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// Ao desinstalar: remove o arranque com o Windows e pergunta se tambem apaga o historico e as definicoes.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Dados: string;
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'iRacingTracker');
  if CurUninstallStep = usPostUninstall then
  begin
    Dados := ExpandConstant('{userappdata}\iRacingTracker');
    if DirExists(Dados) then
      if MsgBox('Queres apagar tambem o historico, as tags, as notas e as definicoes?' + #13#10 +
                'Se escolheres Nao, ficam guardados em:' + #13#10 + Dados,
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(Dados, True, True, True);
  end;
end;
