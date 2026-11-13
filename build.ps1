<#
.SYNOPSIS
  Builds Frostbyte with the .NET Framework compiler that ships with Windows (no SDK needed).
.DESCRIPTION
  Produces Frostbyte.ico, Frostbyte.exe (GUI) and CoolMode.exe (CLI).
  -Version   version to stamp into the exe (default: contents of version.txt)
  -Installer also builds dist\Frostbyte-Setup-<version>.exe (needs Inno Setup 6)
#>
param([string]$Version, [switch]$Installer)
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
if (-not $Version) { $Version = (Get-Content "$here\version.txt" -Raw).Trim() }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version '$Version' is not in major.minor.patch form." }
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$sys = "/r:System.Windows.Forms.dll", "/r:System.Drawing.dll"
# PawnIO's IntelMSR module (LGPL-2.1, see third_party\PawnIO) is embedded for accurate CPU temperature; optional at build time.
$msrBin = "$here\third_party\PawnIO\IntelMSR.bin"
$res = @(); if (Test-Path $msrBin) { $res = @("/resource:$msrBin,IntelMSR.bin") } else { Write-Warning "third_party\PawnIO\IntelMSR.bin not found: CPU temperature will fall back to the coarse ACPI value." }

function Compile { & $csc /nologo @args; if ($LASTEXITCODE -ne 0) { throw "csc failed" } }

# version resources (generated, git-ignored); the in-app updater compares against this
@"
using System.Reflection;
[assembly: AssemblyTitle("Frostbyte")]
[assembly: AssemblyProduct("Frostbyte")]
[assembly: AssemblyCompany("scuba3198")]
[assembly: AssemblyVersion("$Version.0")]
[assembly: AssemblyFileVersion("$Version.0")]
[assembly: AssemblyInformationalVersion("$Version")]
"@ | Set-Content "$here\AssemblyInfo.g.cs"

# 1) icon (drawn in code by Logo.cs)
Compile /target:exe /r:System.Drawing.dll /out:"$env:TEMP\IconGen.exe" "$here\tools\IconGen.cs" "$here\Logo.cs"
& "$env:TEMP\IconGen.exe" "$here\Frostbyte.ico"

# 2) CLI
Compile /target:exe /main:CoolMode /win32manifest:"$here\CoolMode.manifest" /out:"$here\CoolMode.exe" `
    "$here\CoolMode.cs" "$here\AssemblyInfo.g.cs"

# 3) GUI
Compile /target:winexe /main:CoolModeGui @sys /r:System.Web.Extensions.dll @res `
    /win32icon:"$here\Frostbyte.ico" /win32manifest:"$here\CoolMode.manifest" /out:"$here\Frostbyte.exe" `
    "$here\CoolMode.cs" "$here\CoolModeGui.cs" "$here\Sensors.cs" "$here\Settings.cs" "$here\Updater.cs" "$here\Logo.cs" "$here\AssemblyInfo.g.cs"

Write-Host "Built Frostbyte $Version"

# 4) installer (optional)
if ($Installer) {
    # PawnIO's official installer is bundled into ours; fetch it once and verify it against the known hash.
    $pawn = "$here\dist\cache\PawnIO_setup.exe"; $pawnSha = '1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032'
    New-Item -ItemType Directory -Force (Split-Path $pawn) | Out-Null
    if (-not ((Test-Path $pawn) -and ((Get-FileHash $pawn -Algorithm SHA256).Hash -eq $pawnSha.ToUpper()))) {
        Invoke-WebRequest "https://github.com/namazso/PawnIO.Setup/releases/download/2.2.0/PawnIO_setup.exe" -OutFile $pawn
        if ((Get-FileHash $pawn -Algorithm SHA256).Hash -ne $pawnSha.ToUpper()) { Remove-Item $pawn; throw "PawnIO_setup.exe hash mismatch" }
    }
    $iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $iscc) { throw "Inno Setup 6 not found. Install it from https://jrsoftware.org/isinfo.php" }
    Push-Location $here
    try { & $iscc /Qp "/DAppVersion=$Version" installer.iss; if ($LASTEXITCODE -ne 0) { throw "ISCC failed" } } finally { Pop-Location }
    Write-Host "Installer: $here\dist\Frostbyte-Setup-$Version.exe"
}
