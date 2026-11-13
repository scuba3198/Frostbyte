; Frostbyte installer - build with Inno Setup 6:  ISCC.exe installer.iss
; (build.ps1 must have been run first so Frostbyte.exe and CoolMode.exe exist)
#define AppName "Frostbyte"
#define AppVersion "1.1.0"
#define AppPublisher "scuba3198"
#define AppURL "https://github.com/scuba3198/Frostbyte"

[Setup]
AppId={{7B1F6C0E-3D55-4E0A-9A42-5C0FB10C7A11}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\Frostbyte.exe
UninstallDisplayName={#AppName}
LicenseFile=LICENSE
SetupIconFile=Frostbyte.ico
OutputDir=dist
OutputBaseFilename=Frostbyte-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "Frostbyte.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "CoolMode.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\Frostbyte.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\Frostbyte.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Frostbyte.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop the tray app, make sure the user is not left on the Frostbyte power plan, and remove the startup task.
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM Frostbyte.exe"; Flags: runhidden; RunOnceId: "FrostbyteKill"
Filename: "{app}\CoolMode.exe"; Parameters: "off"; Flags: runhidden; RunOnceId: "FrostbyteCoolModeOff"
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /F /TN Frostbyte"; Flags: runhidden; RunOnceId: "FrostbyteTaskDelete"

[Code]
// Frostbyte lives in the tray, so close any running copy before installing/upgrading.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM Frostbyte.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
