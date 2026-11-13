# Frostbyte

A small Windows utility that toggles a "cool mode" power scheme: it duplicates the Balanced
plan, disables CPU boost, caps max processor state at 99% and sets min state to 5% (AC), then
activates it. Turning it off restores your previous plan and deletes the scheme.

## Build
Needs only the .NET Framework compiler that ships with Windows.

    powershell -File build.ps1

Produces `Frostbyte.exe` (GUI) and `CoolMode.exe` (CLI: `on | off | toggle | status`).
Both require administrator rights.

## Notes
- The power-scheme settings were derived by analysing the behaviour of a comparable commercial tool
  for interoperability; no code or assets from it are included.

## License
MIT - see [LICENSE](LICENSE).

