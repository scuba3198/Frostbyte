# Builds CoolMode.exe (CLI) and CoolModeGui.exe (window) with the built-in .NET Framework compiler.
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$here = $PSScriptRoot
& $csc /nologo /target:exe /main:CoolMode /win32manifest:"$here\CoolMode.manifest" /out:"$here\CoolMode.exe" "$here\CoolMode.cs"
& $csc /nologo /target:winexe /main:CoolModeGui /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll `
    /win32manifest:"$here\CoolMode.manifest" /out:"$here\Frostbyte.exe" "$here\CoolMode.cs" "$here\CoolModeGui.cs"
