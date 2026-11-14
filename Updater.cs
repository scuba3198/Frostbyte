// Update check: asks GitHub for the latest Frostbyte release and (on request) downloads + launches its installer.
// The only network access Frostbyte ever does; it can be turned off in Settings.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

class UpdateInfo
{
    public Version Latest;
    public string Tag, PageUrl, AssetName, AssetUrl, Sha256;
}

static class Updater
{
    const string Api = "https://api.github.com/repos/scuba3198/Frostbyte/releases/latest";
    const string DownloadPrefix = "https://github.com/scuba3198/Frostbyte/releases/download/";

    static Version Norm(Version v)
    {
        return new Version(Math.Max(0, v.Major), Math.Max(0, v.Minor), Math.Max(0, v.Build), Math.Max(0, v.Revision));
    }
    public static Version Current { get { return Norm(Assembly.GetEntryAssembly().GetName().Version); } }
    public static string Format(Version v) { return v.Major + "." + v.Minor + "." + v.Build; }
    public static string CurrentText { get { return Format(Current); } }
    public static bool IsNewer(UpdateInfo i) { return i.Latest > Current; }
    // A release is published a few minutes before CI attaches its installer; until then there is nothing to offer.
    public static bool InstallerPending(UpdateInfo i) { return IsNewer(i) && i.AssetUrl == null; }

    static void Tls() { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; }   // TLS 1.2 (needed on .NET 4.x)

    // Throws on network/parse errors.
    public static UpdateInfo Fetch()
    {
        Tls();
        var req = (HttpWebRequest)WebRequest.Create(Api);
        req.UserAgent = "Frostbyte-UpdateCheck"; req.Accept = "application/vnd.github+json";
        req.Timeout = 10000; req.ReadWriteTimeout = 10000;
        string json;
        using (var r = (HttpWebResponse)req.GetResponse())
        using (var s = new StreamReader(r.GetResponseStream())) json = s.ReadToEnd();

        var d = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(json);
        var info = new UpdateInfo { Tag = (string)d["tag_name"], PageUrl = (string)d["html_url"] };
        Version v;
        if (!Version.TryParse(info.Tag.TrimStart('v', 'V'), out v)) throw new Exception("Unrecognized version tag: " + info.Tag);
        info.Latest = Norm(v);

        object assets;
        if (d.TryGetValue("assets", out assets) && assets is IEnumerable)
            foreach (object o in (IEnumerable)assets)
            {
                var a = (Dictionary<string, object>)o;
                string name = (string)a["name"];
                if (!Regex.IsMatch(name, @"^Frostbyte-Setup-[0-9.]+\.exe$")) continue;
                info.AssetName = name; info.AssetUrl = (string)a["browser_download_url"];
                object dg;
                if (a.TryGetValue("digest", out dg) && dg != null)
                {
                    var m = Regex.Match((string)dg, @"^sha256:([0-9a-fA-F]{64})$");
                    if (m.Success) info.Sha256 = m.Groups[1].Value;
                }
                break;
            }
        return info;
    }

    // Downloads the installer to %TEMP%, verifies it against the SHA-256 GitHub publishes, returns the path.
    // "hold" keeps the verified file open (no write/delete sharing) so it cannot be swapped before it is started;
    // the caller disposes it once the installer is running.
    public static string Download(UpdateInfo i, out FileStream hold)
    {
        hold = null;
        if (i.AssetUrl == null || !i.AssetUrl.StartsWith(DownloadPrefix, StringComparison.Ordinal))
            throw new Exception("This release has no installer that can be downloaded automatically.");
        if (i.Sha256 == null)
            throw new Exception("GitHub published no checksum for this installer, so it can't be verified.");
        Tls();
        string dir = Path.Combine(Path.GetTempPath(), "Frostbyte-Update"); Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, i.AssetName);
        using (var wc = new WebClient()) { wc.Headers[HttpRequestHeader.UserAgent] = "Frostbyte-UpdateCheck"; wc.DownloadFile(i.AssetUrl, path); }
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        bool ok = false;
        try
        {
            using (var sha = SHA256.Create())
                ok = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").Equals(i.Sha256, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (!ok) { fs.Dispose(); try { File.Delete(path); } catch { } } }
        if (!ok) throw new Exception("The downloaded file failed its integrity check, so it was discarded.");
        hold = fs; return path;
    }
}

static class UpdateFlow
{
    // Ask -> download -> run the installer -> close Frostbyte (the installer replaces it). Call on the UI thread.
    static bool active;   // UI thread only: a prompt is open or a download is running, so ignore further requests

    public static void Prompt(MainForm owner, UpdateInfo info)
    {
        if (active) return;
        active = true;
        IWin32Window w = owner.Visible ? (IWin32Window)owner : null;
        var r = MessageBox.Show(w,
            "Frostbyte " + Updater.Format(info.Latest) + " is available (you have " + Updater.CurrentText + ").\n\n" +
            "Download and install it now? Frostbyte will close while the installer runs.",
            "Frostbyte update", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (r != DialogResult.Yes) { active = false; return; }
        ThreadPool.QueueUserWorkItem(_ =>
        {
            FileStream hold = null; string path = null, error = null;
            try { path = Updater.Download(info, out hold); } catch (Exception ex) { error = ex.Message; }
            try
            {
                owner.BeginInvoke((Action)(() =>
                {
                    if (error == null)
                        try
                        {
                            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                            hold.Dispose(); owner.ExitApp(); return;
                        }
                        catch (Exception ex) { error = ex.Message; hold.Dispose(); }
                    active = false;
                    MessageBox.Show(owner.Visible ? (IWin32Window)owner : null,
                        "The update could not be installed:\n" + error + "\n\nYou can get it manually from:\n" + info.PageUrl,
                        "Frostbyte update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
            }
            catch { if (hold != null) hold.Dispose(); }   // the window is gone: the app is closing
        });
    }
}
