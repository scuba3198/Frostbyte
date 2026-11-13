# Frostbyte

A small Windows utility that toggles a "cool mode" power scheme: it duplicates the Balanced
plan, disables CPU boost, caps max processor state at 99% and sets min state to 5% (AC), then
activates it. Turning it off restores your previous plan and deletes the scheme.

## Features
- One-click **Cooling Mode** switch (window, tray menu, or `CoolMode.exe on|off|toggle|status`)
- **Settings** (gear button): launch on PC startup, launch in background mode, enable Cooling Mode
  on app launch, close button minimizes to the system tray
- System tray icon (blue = cooling on, orange = off)
- Installer with Start Menu/desktop shortcuts and a clean uninstaller

## Build
Needs only the .NET Framework compiler that ships with Windows.

    powershell -File build.ps1

Produces `Frostbyte.ico`, `Frostbyte.exe` (GUI) and `CoolMode.exe` (CLI). Both require administrator rights.
"Launch on startup" uses a Task Scheduler task so it starts elevated without a UAC prompt, also on battery.

## Installer
Install [Inno Setup 6](https://jrsoftware.org/isinfo.php), run `build.ps1`, then `ISCC.exe installer.iss`
to produce `dist\Frostbyte-Setup-<version>.exe`.

## Notes
- The power-scheme settings were derived by analysing the behaviour of a comparable commercial tool
  for interoperability; no code or assets from it are included.

## License
MIT - see [LICENSE](LICENSE).
