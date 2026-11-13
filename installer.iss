; Frostbyte installer - build with Inno Setup 6:  ISCC.exe installer.iss
; (build.ps1 must have been run first so Frostbyte.exe and CoolMode.exe exist)
#define AppName "Frostbyte"
; AppVersion is normally passed in by build.ps1 / CI (ISCC /DAppVersion=x.y.z); this is the local fallback.
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
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
Name: "startmenuicon"; Description: "Create a &Start Menu shortcut"; GroupDescription: "Shortcuts:"
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"
Name: "startup"; Description: "&Launch Frostbyte automatically when I sign in to Windows"; GroupDescription: "Options:"; Flags: unchecked

[Files]
; PawnIO (signed driver for the accurate CPU temperature) is fetched and hash-checked by build.ps1, then installed silently.
Source: "dist\cache\PawnIO_setup.exe"; Flags: dontcopy
Source: "Frostbyte.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "CoolMode.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "third_party\PawnIO\COPYING"; DestDir: "{app}\third_party\PawnIO"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\Frostbyte.exe"; Tasks: startmenuicon
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\Frostbyte.exe"; Tasks: desktopicon

[Run]
; "Launch when I sign in" registers the same Task Scheduler entry as the in-app setting.
Filename: "{app}\Frostbyte.exe"; Parameters: "--startup on"; Flags: runhidden runascurrentuser; Tasks: startup
; runascurrentuser: Frostbyte requires administrator rights, and by default Inno launches "Run" entries
; as the original (non-elevated) user, which fails with CreateProcess error 740.
Filename: "{app}\Frostbyte.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent runascurrentuser

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

// PawnIO is the small signed driver Frostbyte uses to read the real CPU temperature (see third_party\PawnIO).
// Its official installer is bundled and run silently; skipped if PawnIO is already installed.
function PawnIoInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO') or
            RegKeyExists(HKLM32, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO');
end;

procedure InstallPawnIo;
var Rc: Integer; Failed: Boolean;
begin
  if PawnIoInstalled then Exit;
  Failed := True;
  try
    ExtractTemporaryFile('PawnIO_setup.exe');
    // -install -silent is the installer's own silent mode; 3010 just means "restart when convenient"
    if Exec(ExpandConstant('{tmp}\PawnIO_setup.exe'), '-install -silent', '', SW_HIDE, ewWaitUntilTerminated, Rc) then
      Failed := (Rc <> 0) and (Rc <> 3010);
  except
    Log('PawnIO install failed: ' + GetExceptionMessage);
  end;
  if Failed and not WizardSilent then
    MsgBox('Frostbyte was installed, but the PawnIO driver could not be installed. Frostbyte will still work and ' +
           'show a rougher CPU temperature. You can add PawnIO later with "winget install namazso.PawnIO".', mbInformation, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then InstallPawnIo;
end;
