# Builds Frostbyte.ico, Frostbyte.exe (GUI) and CoolMode.exe (CLI) with the .NET Framework compiler built into Windows.
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$here = $PSScriptRoot
$sys = "/r:System.Windows.Forms.dll", "/r:System.Drawing.dll"

# 1) icon (drawn in code by Logo.cs)
& $csc /nologo /target:exe /r:System.Drawing.dll /out:"$env:TEMP\IconGen.exe" "$here\tools\IconGen.cs" "$here\Logo.cs"
& "$env:TEMP\IconGen.exe" "$here\Frostbyte.ico"

# 2) CLI
& $csc /nologo /target:exe /main:CoolMode /win32manifest:"$here\CoolMode.manifest" /out:"$here\CoolMode.exe" "$here\CoolMode.cs"

# 3) GUI
& $csc /nologo /target:winexe /main:CoolModeGui $sys `
    /win32icon:"$here\Frostbyte.ico" /win32manifest:"$here\CoolMode.manifest" `
    /out:"$here\Frostbyte.exe" "$here\CoolMode.cs" "$here\CoolModeGui.cs" "$here\Settings.cs" "$here\Logo.cs"
