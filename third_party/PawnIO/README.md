# PawnIO (accurate CPU temperature)

Frostbyte reads the CPU's own thermal sensor through [PawnIO](https://pawnio.eu), a signed, open-source driver
that the user installs separately (`winget install namazso.PawnIO`). Without it Frostbyte falls back to the coarse
Windows ACPI temperature, shown with a `~`.

Place the signed `IntelMSR.bin` module from
<https://github.com/namazso/PawnIO.Modules/releases> in this folder before building; `build.ps1` embeds it.
`IntelMSR.bin` is built from `IntelMSR.p` and is licensed LGPL-2.1-or-later (source in that repository). PawnIO itself
is GPL-2.0 with an exception for programs that talk to it over its device interface, which is what Frostbyte does.
