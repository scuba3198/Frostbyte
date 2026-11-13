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
Compile /target:winexe /main:CoolModeGui @sys /r:System.Web.Extensions.dll `
    /win32icon:"$here\Frostbyte.ico" /win32manifest:"$here\CoolMode.manifest" /out:"$here\Frostbyte.exe" `
    "$here\CoolMode.cs" "$here\CoolModeGui.cs" "$here\Settings.cs" "$here\Updater.cs" "$here\Logo.cs" "$here\AssemblyInfo.g.cs"

Write-Host "Built Frostbyte $Version"

# 4) installer (optional)
if ($Installer) {
    $iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $iscc) { throw "Inno Setup 6 not found. Install it from https://jrsoftware.org/isinfo.php" }
    Push-Location $here
    try { & $iscc /Qp "/DAppVersion=$Version" installer.iss; if ($LASTEXITCODE -ne 0) { throw "ISCC failed" } } finally { Pop-Location }
    Write-Host "Installer: $here\dist\Frostbyte-Setup-$Version.exe"
}
