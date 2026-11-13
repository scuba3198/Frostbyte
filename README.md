<h1 align="center">❄️ Frostbyte</h1>

<p align="center"><b>One click to run your PC cooler and quieter.</b></p>

<p align="center">
  <a href="https://github.com/scuba3198/Frostbyte/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/scuba3198/Frostbyte?style=for-the-badge&color=2196f3&label=latest"></a>
  <a href="https://github.com/scuba3198/Frostbyte/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/scuba3198/Frostbyte/total?style=for-the-badge&color=1e88e5"></a>
  <a href="LICENSE"><img alt="MIT license" src="https://img.shields.io/github/license/scuba3198/Frostbyte?style=for-the-badge&color=43a047"></a>
  <img alt="Platform: Windows 10 and 11" src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078d4?style=for-the-badge">
</p>
<p align="center">
  <img alt="Built with C# and .NET Framework" src="https://img.shields.io/badge/built%20with-C%23%20%2F%20.NET%20Framework-7b1fa2?style=for-the-badge">
  <img alt="Installer: Inno Setup" src="https://img.shields.io/badge/installer-Inno%20Setup-ef6c00?style=for-the-badge">
  <a href="https://github.com/scuba3198/Frostbyte/stargazers"><img alt="Stars" src="https://img.shields.io/github/stars/scuba3198/Frostbyte?style=for-the-badge&color=fbc02d"></a>
  <a href="https://github.com/scuba3198/Frostbyte/commits/main"><img alt="Last commit" src="https://img.shields.io/github/last-commit/scuba3198/Frostbyte?style=for-the-badge&color=00acc1"></a>
  <img alt="PRs welcome" src="https://img.shields.io/badge/PRs-welcome-4caf50?style=for-the-badge">
</p>

<p align="center">
  <a href="https://github.com/scuba3198/Frostbyte/releases/latest"><b>⬇ Download</b></a> ·
  <a href="#get-started">Get started</a> ·
  <a href="#settings">Settings</a> ·
  <a href="#questions">FAQ</a> ·
  <a href="#how-it-works">How it works</a>
</p>

Frostbyte is a tiny Windows app with a single big switch: **Cooling Mode**. Turn it on and your processor (CPU) stops pushing itself to its absolute maximum speed. That means less heat, a calmer fan, and often better battery life, at the cost of some peak performance. Turn it off and everything goes back exactly as it was.

| Cooling Mode OFF | Cooling Mode ON |
|---|---|
| ![Frostbyte with Cooling Mode off](docs/window-off.png) | ![Frostbyte with Cooling Mode on](docs/window-on.png) |

The main window also shows your live CPU temperature, CPU speed, SSD temperature and battery level. For an accurate CPU temperature the installer always sets up [PawnIO](https://pawnio.eu), a small free signed driver (without it Windows only gives a rough guess, shown with a `~`).

It's free and open source (MIT license).


---

<a id="get-started"></a>
<details open>
<summary><h2>Get started (2 minutes)</h2></summary>

<details open>
<summary><h3>1. Download</h3></summary>

Go to the **[Releases page](https://github.com/scuba3198/Frostbyte/releases/latest)** and download **`Frostbyte-Setup-x.y.z.exe`**.

</details>

<details open>
<summary><h3>2. Install</h3></summary>

Double-click the file you downloaded and follow the steps (Next → Next → Install). On the options page you can choose whether to create a **Start Menu shortcut**, a **desktop shortcut**, and whether Frostbyte should **launch automatically when you sign in to Windows**. (You can change the last one later in Settings.)

> **Windows says "Windows protected your PC"?**
> That's normal for a new, small app that isn't digitally signed (code-signing certificates cost money). Click **More info**, then **Run anyway**. You can read all of the source code in this repository if you'd like to check what it does.

</details>

<details open>
<summary><h3>3. Use it</h3></summary>

1. Open **Frostbyte** from the Start menu or desktop.
2. Click the big switch so it says **ON** (the window turns blue). Click again to turn it **OFF** (the window turns orange).

That's it. You can leave it on all the time or just switch it on when your laptop gets hot or loud.

Frostbyte asks for **administrator permission** when it opens. That's required because it changes a Windows power setting, and Windows only lets administrators do that.

</details>

</details>

---

<a id="settings"></a>
<details>
<summary><h2>Settings</h2></summary>

Click the **⚙ gear** in the top-right corner.

![Frostbyte settings](docs/settings.png)

| Setting | What it does |
|---|---|
| **Launch app on PC startup** | Frostbyte starts by itself when you sign in to Windows, with no extra permission pop-up. |
| **Launch app in background mode** | Starts hidden in the system tray (the little icons near the clock) instead of opening a window. |
| **Enable Cooling Mode on app launch** | Switches Cooling Mode on every time Frostbyte starts. Combine this with the two options above for "set and forget". |
| **Close button minimizes to system tray** | Clicking **X** hides the window instead of quitting. Frostbyte keeps running in the tray. |
| **Check for updates automatically** | About once a day Frostbyte asks GitHub whether a newer version exists and shows a tray notification if so. It never installs anything without asking you first. Untick it to turn the check off. |
| **Check for updates** (button) | Checks right now. If there's a new version it offers to download and install it (the download is verified against the checksum GitHub publishes for it). |

<details>
<summary><h3>The tray icon</h3></summary>

Look near the clock (you may need to click the **^** arrow). The snowflake icon is **blue when Cooling Mode is on** and **orange when it's off**.
- **Left-click** it to open the window.
- **Right-click** it for a menu: open, switch Cooling Mode on/off, or **Exit** to quit completely.

</details>

</details>

---

<a id="questions"></a>
<details>
<summary><h2>Questions</h2></summary>

<details>
<summary><b>Is it safe?</b></summary>

Yes. Frostbyte only creates and switches a normal Windows *power plan*, the same thing you'd find under *Control Panel → Power Options*. The one extra thing the installer sets up is [PawnIO](https://pawnio.eu), a small free signed driver that lets Frostbyte read your real CPU temperature (read-only; Frostbyte never changes your hardware through it). The only time it goes online is the optional update check (see Settings), which talks only to GitHub and sends nothing about you or your PC.

</details>

<details>
<summary><b>Will it slow my PC down?</b></summary>

Only a little, and only when your CPU would otherwise be pushing to its very top speed (for example in games or heavy exports). Everyday use (browsing, documents, video) usually feels the same.

</details>

<details>
<summary><b>How do I turn it off completely?</b></summary>

Switch Cooling Mode to **OFF**, or just uninstall Frostbyte. Uninstalling automatically switches Cooling Mode off and removes the startup entry, so you're never left in a changed state.

</details>

<details>
<summary><b>Frostbyte says it needs to run as administrator.</b></summary>

Accept the Windows prompt (UAC). It can't work without it.

</details>

<details>
<summary><b>Does it work on desktops and AMD CPUs?</b></summary>

It uses standard Windows power settings, so it works on any Windows PC. How much cooler it gets depends on your CPU and how it handles the "boost" speeds. It was built and tested on an Intel laptop.

</details>

<details>
<summary><b>Where does the temperature come from?</b></summary>

Reading a CPU's true temperature needs a small hardware driver, so the installer sets up [PawnIO](https://pawnio.eu) (free, signed, open source). Frostbyte uses it only to read the CPU's own thermal sensor, and shows that number in the window and when you hover the tray icon. Without PawnIO, Windows only offers a rough, often too-high guess, which Frostbyte marks with a `~`. The SSD temperature comes from the drive itself and the battery level from Windows. Intel graphics temperature isn't shown because Intel's driver doesn't expose it.

</details>

<details>
<summary><b>Windows Security / antivirus flagged it.</b></summary>

Small unsigned apps sometimes get flagged by mistake. The full source is here and you can build it yourself (see *How it works → Build it yourself* below).

</details>

</details>

---

<a id="how-it-works"></a>
<details>
<summary><h2>How it works (for the curious)</h2></summary>

Everything Frostbyte does comes down to **one Windows power plan** and a handful of settings in it.

<details>
<summary><h3>What "Cooling Mode" actually changes</h3></summary>

When you switch it **ON**, Frostbyte:

1. **Remembers** which power plan is active right now.
2. **Copies the built-in *Balanced* plan** into a new plan called **"Frostbyte"** (if Balanced is missing, it copies the currently active plan instead).
3. **Writes five processor settings** into that new plan:

   | Setting (Windows name) | AC (plugged in) | DC (battery) |
   |---|---|---|
   | Processor performance boost mode | Disabled (0) | Disabled (0) |
   | Minimum processor state | 5% | not changed |
   | Maximum processor state | **99%** | **99%** |

4. **Activates** the Frostbyte plan.

When you switch it **OFF**, it re-activates the plan from step 1 (Balanced if that's gone) and **deletes** the Frostbyte plan, leaving no trace.

</details>

<details>
<summary><h3>Why 99%?</h3></summary>

Modern CPUs run at their normal "base" speed and then briefly **boost** well above it. Boosting is what makes them run hot and draw lots of power. Setting the maximum state to **99%** (just under 100%) is a widely used trick that stops Windows from engaging boost, and the boost-mode setting is also explicitly set to Disabled. The CPU still runs at full base speed, so it stays responsive, but it stops producing the short, hot bursts.

</details>

<details>
<summary><h3>Where this came from</h3></summary>

Cooling Mode only uses documented Windows power settings, so you can verify exactly what it does on your own PC with `powercfg /qh` while the "Frostbyte" plan is active. Other tools offer a similar cooling mode (for example Outbyte's Camomile, which sells it in its PRO version); Frostbyte is independent, written from scratch, and not affiliated with or endorsed by them. No code, images or other assets from any other product are included in this project. Product names belong to their owners.

</details>

<details>
<summary><h3>The pieces</h3></summary>

| File | Role |
|---|---|
| `CoolMode.cs` | The core: creates/activates/deletes the power plan using Windows' `powrprof.dll` API (`PowerDuplicateScheme`, `PowerWriteACValueIndex`, `PowerWriteDCValueIndex`, `PowerSetActiveScheme`, …). Also builds the command-line tool `CoolMode.exe`. |
| `CoolModeGui.cs` | The window, settings dialog and tray icon (Windows Forms, all custom-drawn). |
| `Settings.cs` | Saves your choices to `%AppData%\Frostbyte\settings.ini` and manages the startup task. |
| `Updater.cs` | The update check (GitHub releases API) and the download + checksum + launch-installer flow. |
| `Logo.cs`, `tools/IconGen.cs` | The logo is drawn in code; the icon generator turns it into `Frostbyte.ico`. |
| `installer.iss` | The [Inno Setup](https://jrsoftware.org/isinfo.php) script for the installer. |
| `build.ps1` | Builds everything using only the compiler that ships with Windows. |

</details>

<details>
<summary><h3>Details worth knowing</h3></summary>

- **Why admin?** Writing to power plans requires administrator rights, so both programs carry a manifest asking for elevation.
- **How "launch on startup" works:** a normal startup entry can't launch an admin app without a UAC prompt. Frostbyte instead registers a **Task Scheduler** task ("Frostbyte") that runs at sign-in with the highest privileges. It is configured to start even on battery power, which Task Scheduler blocks by default. Turning the option off or uninstalling deletes the task.
- **One copy at a time:** launching Frostbyte again just brings the existing window to the front.
- **Sharp on any screen:** the app declares itself DPI-aware and scales its own layout, so text isn't blurry on high-resolution or scaled displays.
- **Where settings live:** `%AppData%\Frostbyte\settings.ini`, a few `key=1/0` lines you can edit or delete.

</details>

<details>
<summary><h3>Command line</h3></summary>

`CoolMode.exe` (installed next to Frostbyte) does the same thing without a window:

```
CoolMode.exe on        switch Cooling Mode on
CoolMode.exe off       switch it off
CoolMode.exe toggle    flip it
CoolMode.exe status    prints ON/OFF (exit code 1 = on, 0 = off)
```

</details>

<details>
<summary><h3>Updates and releases</h3></summary>

- **In the app:** `Updater.cs` calls GitHub's public API (`/repos/scuba3198/Frostbyte/releases/latest`), compares the release tag with the version stamped into the running exe, and, only if you agree, downloads `Frostbyte-Setup-x.y.z.exe`, verifies its SHA-256 against the digest GitHub publishes, and starts it. Requests carry a generic `Frostbyte-UpdateCheck` user agent and nothing else.
- **For contributors:** `main` is always releasable. Changes arrive as short-lived branches and pull requests that must pass CI (which builds the app *and* the installer). PR titles follow [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, …). A bot ([release-please](https://github.com/googleapis/release-please)) keeps a "Release x.y.z" pull request open with the next version and changelog; merging it tags the release and CI attaches the installer. Nobody bumps version numbers by hand. Details in [CONTRIBUTING.md](CONTRIBUTING.md).

</details>

<details>
<summary><h3>Build it yourself</h3></summary>

You only need Windows. No SDKs or package managers.

```powershell
powershell -File build.ps1
```

This produces `Frostbyte.ico`, `Frostbyte.exe` and `CoolMode.exe`. To also build the installer, install [Inno Setup 6](https://jrsoftware.org/isinfo.php) and add `-Installer`:

```powershell
powershell -File build.ps1 -Installer
```

The installer appears in `dist\`.

</details>

</details>

---

<a id="license"></a>
<details>
<summary><h2>License</h2></summary>

MIT. See [LICENSE](LICENSE). Contributions and issues are welcome.

</details>
