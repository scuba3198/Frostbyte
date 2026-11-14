// Persistent settings (%AppData%\Frostbyte\settings.ini) and the "start with Windows" scheduled task.
using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Text;

static class Settings
{
    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Frostbyte");
    static readonly string FileName = Path.Combine(Dir, "settings.ini");

    public static bool Background;         // start hidden in the tray
    public static bool CoolOnLaunch;        // enable Cooling Mode when the app starts
    public static bool CloseToTray = true;  // close button hides to the tray
    public static bool AutoUpdate = true;   // check GitHub for a newer release (at most once a day)
    public static long LastCheckTicks;      // UTC ticks of the last successful update check

    static readonly object SaveLock = new object();

    public static void Load()
    {
        try
        {
            if (!File.Exists(FileName)) return;
            foreach (var line in File.ReadAllLines(FileName))
            {
                int i = line.IndexOf('='); if (i < 0) continue;
                string k = line.Substring(0, i).Trim(), v = line.Substring(i + 1).Trim();
                bool b = v == "1";
                switch (k)
                {
                    case "Background": Background = b; break;
                    case "CoolOnLaunch": CoolOnLaunch = b; break;
                    case "CloseToTray": CloseToTray = b; break;
                    case "AutoUpdate": AutoUpdate = b; break;
                    case "LastCheck": long.TryParse(v, out LastCheckTicks); break;
                }
            }
        }
        catch { }
    }

    public static void Save()
    {
        lock (SaveLock)   // called from the UI thread and from update-check worker threads
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllLines(FileName, new[] {
                    "Background="+ (Background ? 1 : 0), "CoolOnLaunch=" + (CoolOnLaunch ? 1 : 0),
                    "CloseToTray=" + (CloseToTray ? 1 : 0), "AutoUpdate=" + (AutoUpdate ? 1 : 0),
                    "LastCheck=" + LastCheckTicks });
            }
            catch { }
    }
}

static class Startup
{
    const string TaskName = "Frostbyte";

    static int Run(string args, out string output)
    {
        var psi = new ProcessStartInfo("schtasks.exe", args)
        { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        using (var p = Process.Start(psi))
        {
            output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(); return p.ExitCode;
        }
    }

    public static bool IsEnabled() { string o; try { return Run("/Query /TN " + TaskName, out o) == 0; } catch { return false; } }

    // A scheduled task (not a Run-key entry) so the elevated app starts at logon without a UAC prompt,
    // and also when running on battery.
    public static void Enable()
    {
        string user = SecurityElement.Escape(WindowsIdentity.GetCurrent().Name);
        string exe = SecurityElement.Escape("\"" + System.Windows.Forms.Application.ExecutablePath + "\"");
        string xml =
            "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
            "<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
            " <RegistrationInfo><Description>Starts Frostbyte when you sign in.</Description></RegistrationInfo>\r\n" +
            " <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>" + user + "</UserId></LogonTrigger></Triggers>\r\n" +
            " <Principals><Principal id=\"Author\"><UserId>" + user + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>\r\n" +
            " <Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>" +
            "<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><AllowHardTerminate>true</AllowHardTerminate><StartWhenAvailable>true</StartWhenAvailable>" +
            "<ExecutionTimeLimit>PT0S</ExecutionTimeLimit><Enabled>true</Enabled></Settings>\r\n" +
            " <Actions Context=\"Author\"><Exec><Command>" + exe + "</Command></Exec></Actions>\r\n" +
            "</Task>";
        string tmp = Path.Combine(Path.GetTempPath(), "frostbyte-task-" + Guid.NewGuid().ToString("N") + ".xml");
        File.WriteAllText(tmp, xml, Encoding.Unicode);
        try
        {
            string o; int rc = Run("/Create /F /TN " + TaskName + " /XML \"" + tmp + "\"", out o);
            if (rc != 0) throw new Exception("Could not create the startup task: " + o.Trim());
        }
        finally { try { File.Delete(tmp); } catch { } }
    }

    public static void Disable()
    {
        string o; Run("/Delete /F /TN " + TaskName, out o);
    }
}
