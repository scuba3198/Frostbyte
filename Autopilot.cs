// Autopilot - Cooling Mode switches off while chosen apps (or anything they start) are running, and back on
// when they have all closed. Holds the process watcher, the "Autopilot" dialog, the running-app picker and the
// row shown in the main window. The switching itself is done by MainForm.AutopilotTick (CoolModeGui.cs).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

static class ApNative
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PROCESSENTRY32
    {
        public uint dwSize, cntUsage, th32ProcessID; public IntPtr th32DefaultHeapID;
        public uint th32ModuleID, cntThreads, th32ParentProcessID; public int pcPriClassBase; public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szExeFile;
    }
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool Process32FirstW(IntPtr h, ref PROCESSENTRY32 e);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool Process32NextW(IntPtr h, ref PROCESSENTRY32 e);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] static extern bool GetProcessTimes(IntPtr h, out long create, out long exit, out long kernel, out long user);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool QueryFullProcessImageNameW(IntPtr h, uint flags, StringBuilder sb, ref uint size);
    [DllImport("kernel32.dll")] public static extern bool ProcessIdToSessionId(uint pid, out uint session);
    delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint cmd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowTextW(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern uint PrivateExtractIconsW(string file, int index, int cx, int cy, IntPtr[] icons, uint[] ids, uint count, uint flags);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr h);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] public static extern int SetWindowTheme(IntPtr h, string app, string id);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);

    const uint TH32CS_SNAPPROCESS = 2, PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, GW_OWNER = 4;

    public struct Proc { public uint Pid, Parent; public string Exe; }

    // Every running process with its parent id (no access rights needed, so it sees elevated and protected ones too).
    public static List<Proc> Snapshot()
    {
        var list = new List<Proc>();
        IntPtr snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snap == new IntPtr(-1)) throw new InvalidOperationException("process snapshot failed");
        try
        {
            var e = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32)) };
            for (bool ok = Process32FirstW(snap, ref e); ok; ok = Process32NextW(snap, ref e))
                list.Add(new Proc { Pid = e.th32ProcessID, Parent = e.th32ParentProcessID, Exe = e.szExeFile ?? "" });
        }
        finally { CloseHandle(snap); }
        return list;
    }

    // Start time (FILETIME ticks) of a process, 0 if it can't be read.
    public static long StartTime(uint pid)
    {
        IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (h == IntPtr.Zero) return 0;
        try { long c, x, k, u; return GetProcessTimes(h, out c, out x, out k, out u) ? c : 0; }
        finally { CloseHandle(h); }
    }

    public static string ImagePath(uint pid)
    {
        IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (h == IntPtr.Zero) return null;
        try { var sb = new StringBuilder(1024); uint n = (uint)sb.Capacity; return QueryFullProcessImageNameW(h, 0, sb, ref n) ? sb.ToString(0, (int)n) : null; }
        finally { CloseHandle(h); }
    }

    // Process ids that own a visible top-level window, with that window's title.
    public static Dictionary<uint, string> WindowOwners()
    {
        var d = new Dictionary<uint, string>();
        EnumWindows((h, l) =>
        {
            if (!IsWindowVisible(h) || GetWindow(h, GW_OWNER) != IntPtr.Zero) return true;
            var sb = new StringBuilder(256); if (GetWindowTextW(h, sb, sb.Capacity) == 0) return true;
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (!d.ContainsKey(pid)) d[pid] = sb.ToString();
            return true;
        }, IntPtr.Zero);
        return d;
    }
}

// The apps chosen for Autopilot are kept in Settings.AutopilotApps as full paths (or bare "name.exe").
// They are matched by file name, so an app is recognised wherever it runs from.
static class ApApps
{
    static readonly Dictionary<string, string> labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, Icon> icons = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);

    public static string Key(string entry) { return Path.GetFileName(entry).ToLowerInvariant(); }

    // Friendly name: the program's own description ("Google Chrome"), else its file name without ".exe".
    public static string Label(string path)
    {
        string s;
        lock (labels)
            if (labels.TryGetValue(path, out s)) return s;
        s = null;
        try
        {
            if (Path.IsPathRooted(path) && File.Exists(path))
            {
                string d = (FileVersionInfo.GetVersionInfo(path).FileDescription ?? "").Trim();
                if (d.Length > 1 && d.Length <= 48) s = d;
            }
        }
        catch { }
        if (s == null) s = Path.GetFileNameWithoutExtension(path);
        lock (labels) labels[path] = s;
        return s;
    }

    public static Icon IconOf(string path)
    {
        Icon i;
        lock (icons)
            if (icons.TryGetValue(path, out i)) return i;
        i = null;
        try
        {
            if (Path.IsPathRooted(path) && File.Exists(path))
            {
                // at the size the list draws it, so it stays sharp on scaled (high-DPI) screens
                int sz = Dpi.S(28); var h = new IntPtr[1]; var ids = new uint[1];
                if (ApNative.PrivateExtractIconsW(path, 0, sz, sz, h, ids, 1, 0) == 1 && h[0] != IntPtr.Zero)
                {
                    using (var tmp = Icon.FromHandle(h[0])) i = (Icon)tmp.Clone();
                    ApNative.DestroyIcon(h[0]);
                }
                if (i == null) i = Icon.ExtractAssociatedIcon(path);   // no icon of its own: the standard program icon
            }
        }
        catch { }
        lock (icons) icons[path] = i;
        return i;
    }

    // file name -> friendly name, for the watcher
    public static Dictionary<string, string> WatchMap()
    {
        var m = new Dictionary<string, string>();
        foreach (var e in Settings.AutopilotApps) { string k = Key(e); if (k.Length > 0 && !m.ContainsKey(k)) m[k] = Label(e); }
        return m;
    }

    public static bool Contains(string path)
    {
        string k = Key(path);
        foreach (var e in Settings.AutopilotApps) if (Key(e) == k) return true;
        return false;
    }
}

// Finds which chosen apps are running, counting every process they start (children, grandchildren...) as part of
// them, even after the app that started it has closed (a launcher that starts a game and exits, for example).
class ProcessWatch
{
    class Tracked { public string Exe; public long Start; public string Root; }
    readonly Dictionary<uint, Tracked> tracked = new Dictionary<uint, Tracked>();
    readonly HashSet<string> notChildren = new HashSet<string>();   // "pid:parentStart" pairs already ruled out

    public void Reset() { tracked.Clear(); notChildren.Clear(); }

    // watch: lower-case file name -> label. Returns the file names of chosen apps that have something running.
    public HashSet<string> Scan(Dictionary<string, string> watch)
    {
        var procs = ApNative.Snapshot();
        var alive = new Dictionary<uint, string>();
        foreach (var p in procs) alive[p.Pid] = p.Exe;

        // forget processes that ended (or whose id Windows has handed to another program) and apps taken off the list
        var gone = new List<uint>();
        foreach (var kv in tracked)
        {
            string exe;
            if (!alive.TryGetValue(kv.Key, out exe) || !string.Equals(exe, kv.Value.Exe, StringComparison.OrdinalIgnoreCase) || !watch.ContainsKey(kv.Value.Root))
                gone.Add(kv.Key);
        }
        foreach (var pid in gone) tracked.Remove(pid);

        // the chosen apps themselves
        foreach (var p in procs)
        {
            if (p.Pid == 0 || tracked.ContainsKey(p.Pid)) continue;
            string k = p.Exe.ToLowerInvariant();
            if (watch.ContainsKey(k)) tracked[p.Pid] = new Tracked { Exe = p.Exe, Start = ApNative.StartTime(p.Pid), Root = k };
        }

        // everything they started, however deep
        if (notChildren.Count > 4000) notChildren.Clear();
        for (bool grew = true; grew; )
        {
            grew = false;
            foreach (var p in procs)
            {
                Tracked parent;
                if (p.Pid == 0 || p.Pid == p.Parent || tracked.ContainsKey(p.Pid) || !tracked.TryGetValue(p.Parent, out parent)) continue;
                string pair = p.Pid + ":" + parent.Start;
                if (notChildren.Contains(pair)) continue;
                long start = ApNative.StartTime(p.Pid);
                // Windows reuses process ids, so a process that is older than the "parent" only points at a dead one
                if (start != 0 && parent.Start != 0 && start < parent.Start) { notChildren.Add(pair); continue; }
                tracked[p.Pid] = new Tracked { Exe = p.Exe, Start = start, Root = parent.Root };
                grew = true;
            }
        }

        var roots = new HashSet<string>();
        foreach (var t in tracked.Values) roots.Add(t.Root);
        return roots;
    }
}

// ---------------------------------------------------------------- shared look

static class ApLook
{
    public static readonly Color ListBack = Color.FromArgb(26, 26, 28);
    public static readonly Color Hover = Color.FromArgb(40, 40, 44);
    public static readonly Color Running = Color.FromArgb(110, 190, 255);

    public static GraphicsPath Round(RectangleF r, float rad)
    {
        float d = rad * 2; var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
    }

    public static Button MakeButton(string text, bool primary, int w)
    {
        var b = new Button
        {
            Text = text, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand,
            BackColor = primary ? Theme.Accent : Color.FromArgb(56, 56, 60),
            Font = new Font("Segoe UI Semibold", 10.5f), Size = new Size(Dpi.S(w), Dpi.S(38))
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.BorderColor = b.BackColor;   // the default (Enter) button would otherwise get a white outline
        b.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(66, 165, 245) : Color.FromArgb(70, 70, 76);
        return b;
    }

    // a steering wheel, drawn in a 32x32 design box
    public static void DrawWheel(Graphics g, float x, float y, float s, Color c)
    {
        var st = g.Save(); g.TranslateTransform(x, y); g.ScaleTransform(s / 32f, s / 32f);
        using (var pen = new Pen(c, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawEllipse(pen, 3, 3, 26, 26); g.DrawEllipse(pen, 12.5f, 12.5f, 7, 7);
            g.DrawLine(pen, 4, 16, 12.5f, 16); g.DrawLine(pen, 19.5f, 16, 28, 16); g.DrawLine(pen, 16, 19.5f, 16, 28);
        }
        g.Restore(st);
    }

    public static void DrawSwitch(Graphics g, Rectangle r, bool on)
    {
        using (var p = Round(r, r.Height / 2f))
        using (var b = new SolidBrush(on ? Theme.Accent : Color.FromArgb(72, 72, 78))) g.FillPath(b, p);
        int d = r.Height - Dpi.S(8), kx = on ? r.Right - d - Dpi.S(4) : r.X + Dpi.S(4);
        using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, kx, r.Y + Dpi.S(4), d, d);
    }
}

// ---------------------------------------------------------------- the row in the main window

class AutopilotBar : Control
{
    bool on, busy, hot; string status = "";
    public event EventHandler SwitchClicked, OpenClicked;
    public AutopilotBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Dark; Cursor = Cursors.Hand;
    }
    public void SetState(bool isOn, bool isBusy, string text)
    {
        if (on == isOn && busy == isBusy && status == text) return;
        on = isOn; busy = isBusy; status = text; Invalidate();
    }
    Rectangle SwitchRect { get { int w = Dpi.S(48), h = Dpi.S(26); return new Rectangle(Width - Dpi.S(24) - Dpi.S(22) - w, (Height - h) / 2, w, h); } }
    protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        var sw = SwitchRect; sw.Inflate(Dpi.S(8), Dpi.S(8));
        var h = sw.Contains(e.Location) ? SwitchClicked : OpenClicked;
        if (h != null) h(this, EventArgs.Empty);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);
        if (hot) using (var b = new SolidBrush(Color.FromArgb(28, 28, 30))) g.FillRectangle(b, ClientRectangle);
        using (var line = new Pen(Color.FromArgb(58, 58, 60))) g.DrawLine(line, Dpi.S(22), 0, Width - Dpi.S(22), 0);
        int isz = Dpi.S(30);
        ApLook.DrawWheel(g, Dpi.S(26), (Height - isz) / 2f, isz, busy ? ApLook.Running : Color.White);
        float tx = Dpi.S(26) + isz + Dpi.S(14);
        var sw = SwitchRect;
        using (var tf = new Font("Segoe UI Semibold", 12f)) using (var sf = new Font("Segoe UI", 9.5f))
        using (var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
        using (var sb = new SolidBrush(busy ? ApLook.Running : Color.Silver))
        {
            g.DrawString("Autopilot", tf, Brushes.White, new RectangleF(tx, Height / 2f - Dpi.S(22), sw.X - tx, Dpi.S(26)), fmt);
            g.DrawString(status, sf, sb, new RectangleF(tx, Height / 2f + Dpi.S(1), sw.X - tx - Dpi.S(8), Dpi.S(22)), fmt);
        }
        ApLook.DrawSwitch(g, sw, on);
        // chevron: click the row to choose apps
        float cx = Width - Dpi.S(30), cy = Height / 2f, a = Dpi.S(5);
        using (var p = new Pen(Color.Gray, Dpi.S(2)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLines(p, new[] { new PointF(cx - a / 2, cy - a), new PointF(cx + a / 2, cy), new PointF(cx - a / 2, cy + a) });
    }
}

// ---------------------------------------------------------------- app list (used by both dialogs)

class AppRow : Control
{
    public string Path, Title, Sub; public Icon Ico; public bool Checkable;
    bool chk, hot, running; public string SearchText;
    public event EventHandler RemoveClicked, CheckedChanged;
    public bool Checked { get { return chk; } set { chk = value; Invalidate(); } }
    public bool Running { get { return running; } set { if (running != value) { running = value; Invalidate(); } } }
    public AppRow()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = ApLook.ListBack; Height = Dpi.S(54); Margin = Padding.Empty;
    }
    Rectangle RightBox { get { int s = Dpi.S(26); return new Rectangle(Width - Dpi.S(16) - s, (Height - s) / 2, s, s); } }
    protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var r = RightBox; r.Inflate(Dpi.S(6), Dpi.S(6));
        Cursor = Checkable || r.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
    }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        if (Checkable) { Checked = !chk; if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); return; }
        var r = RightBox; r.Inflate(Dpi.S(6), Dpi.S(6));
        if (r.Contains(e.Location) && RemoveClicked != null) RemoveClicked(this, EventArgs.Empty);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(hot ? ApLook.Hover : BackColor);
        int isz = Dpi.S(28), ix = Dpi.S(14), iy = (Height - isz) / 2;
        if (Ico != null) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; using (var bmp = Ico.ToBitmap()) g.DrawImage(bmp, ix, iy, isz, isz); }
        else
        {
            using (var p = ApLook.Round(new RectangleF(ix, iy, isz, isz), Dpi.S(6))) using (var b = new SolidBrush(Color.FromArgb(70, 70, 78))) g.FillPath(b, p);
            using (var f = new Font("Segoe UI Semibold", 11f)) using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(string.IsNullOrEmpty(Title) ? "?" : Title.Substring(0, 1).ToUpperInvariant(), f, Brushes.White, new RectangleF(ix, iy, isz, isz), sf);
        }
        var rb = RightBox; float tx = ix + isz + Dpi.S(14), tw = rb.X - tx - Dpi.S(10);
        using (var tf = new Font("Segoe UI", 11f)) using (var sf = new Font("Segoe UI", 9f))
        using (var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
        {
            g.DrawString(Title, tf, Brushes.White, new RectangleF(tx, Height / 2f - Dpi.S(21), tw, Dpi.S(24)), fmt);
            string sub = Sub; float subW = tw;
            if (running)
            {
                const string tag = "● running";
                float w = g.MeasureString(tag, sf).Width;
                using (var rbr = new SolidBrush(ApLook.Running)) g.DrawString(tag, sf, rbr, tx + tw - w, Height / 2f + Dpi.S(2));
                subW = tw - w - Dpi.S(6);
            }
            g.DrawString(sub, sf, Brushes.Silver, new RectangleF(tx, Height / 2f + Dpi.S(2), subW, Dpi.S(20)), fmt);
        }
        if (Checkable)
        {
            var box = new RectangleF(rb.X + Dpi.S(3), rb.Y + Dpi.S(3), rb.Width - Dpi.S(6), rb.Height - Dpi.S(6));
            using (var p = ApLook.Round(box, box.Width * 0.22f))
            {
                if (chk) using (var b = new SolidBrush(Theme.Accent)) g.FillPath(b, p);
                else using (var pen = new Pen(Color.FromArgb(120, 120, 125), 1.6f)) g.DrawPath(pen, p);
            }
            if (chk)
                using (var pen = new Pen(Color.White, box.Width * 0.12f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    g.DrawLines(pen, new[] { new PointF(box.X + box.Width * 0.24f, box.Y + box.Height * 0.54f), new PointF(box.X + box.Width * 0.43f, box.Y + box.Height * 0.72f), new PointF(box.X + box.Width * 0.77f, box.Y + box.Height * 0.30f) });
        }
        else
        {
            // remove button: a round "x"
            if (hot) using (var b = new SolidBrush(Color.FromArgb(64, 64, 70))) g.FillEllipse(b, rb);
            float c = Dpi.S(5); PointF m = new PointF(rb.X + rb.Width / 2f, rb.Y + rb.Height / 2f);
            using (var pen = new Pen(Color.FromArgb(200, 200, 205), Dpi.S(2)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            { g.DrawLine(pen, m.X - c, m.Y - c, m.X + c, m.Y + c); g.DrawLine(pen, m.X - c, m.Y + c, m.X + c, m.Y - c); }
        }
    }
}

class SectionRow : Control
{
    public SectionRow(string text)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Text = text; BackColor = ApLook.ListBack; Height = Dpi.S(30); Margin = Padding.Empty;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit; g.Clear(BackColor);
        using (var f = new Font("Segoe UI Semibold", 8.5f)) using (var b = new SolidBrush(Color.FromArgb(150, 150, 158)))
            g.DrawString(Text.ToUpperInvariant(), f, b, Dpi.S(14), Dpi.S(9));
    }
}

class AppList : FlowLayoutPanel
{
    public string EmptyText = "";
    public AppList()
    {
        FlowDirection = FlowDirection.TopDown; WrapContents = false; AutoScroll = true;
        BackColor = ApLook.ListBack; Margin = Padding.Empty; Padding = Padding.Empty;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try { ApNative.SetWindowTheme(Handle, "DarkMode_Explorer", null); } catch { }   // dark scroll bar (Windows 10 1809+)
    }
    // rows are sized here rather than docked, so a vertical scroll bar never brings a horizontal one with it
    public void FitRows()
    {
        int total = 0;
        foreach (Control c in Controls) if (c.Visible) total += c.Height;
        int w = Width - (total > Height ? SystemInformation.VerticalScrollBarWidth : 0);
        SuspendLayout();
        foreach (Control c in Controls) c.Width = w;
        ResumeLayout(); Invalidate();
    }
    protected override void OnResize(EventArgs e) { base.OnResize(e); if (IsHandleCreated) FitRows(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        bool any = false; foreach (Control c in Controls) if (c.Visible) { any = true; break; }
        if (any || EmptyText.Length == 0) return;
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using (var f = new Font("Segoe UI", 10.5f))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            e.Graphics.DrawString(EmptyText, f, Brushes.Gray, new RectangleF(Dpi.S(20), 0, Width - Dpi.S(40), Height), sf);
    }
}

// ---------------------------------------------------------------- the Autopilot dialog

class AutopilotForm : Form
{
    readonly DarkCheck enable;
    readonly AppList list = new AppList();
    readonly Func<HashSet<string>> running;
    readonly System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer { Interval = 1000 };
    public event EventHandler Changed;

    public AutopilotForm(Func<HashSet<string>> runningApps)
    {
        running = runningApps;
        AutoScaleMode = AutoScaleMode.None;
        Text = "Autopilot"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent; BackColor = Theme.Dark; ForeColor = Color.White; DoubleBuffered = true;
        Font = new Font("Segoe UI", 11.5f);
        try { Icon = Logo.MakeIcon(32, true); } catch { }
        ClientSize = new Size(Dpi.S(460), Dpi.S(584));

        enable = new DarkCheck { Text = "Turn on Autopilot", Checked = Settings.Autopilot, BackColor = Theme.Dark, Font = new Font("Segoe UI Semibold", 12f), Size = new Size(Dpi.S(412), Dpi.S(38)), Location = new Point(Dpi.S(24), Dpi.S(18)) };
        enable.Changed += (s, e) => { Settings.Autopilot = enable.Checked; Settings.Save(); Notify(); };
        var desc = new Label
        {
            Text = "While any app below is open, Cooling Mode switches off so it can run at full speed. " +
                   "Anything the app starts counts too. When they have all closed, Cooling Mode switches back on (if it was on before).",
            ForeColor = Color.Silver, BackColor = Color.Transparent, Font = new Font("Segoe UI", 10f),
            Location = new Point(Dpi.S(24), Dpi.S(62)), Size = new Size(Dpi.S(412), Dpi.S(88))
        };
        var head = new Label { Text = "Apps to watch", Font = new Font("Segoe UI Semibold", 11f), AutoSize = true, BackColor = Color.Transparent, Location = new Point(Dpi.S(24), Dpi.S(156)) };
        list.EmptyText = "No apps yet.\nAdd one with the buttons below.";
        list.Location = new Point(Dpi.S(24), Dpi.S(186)); list.Size = new Size(Dpi.S(412), Dpi.S(264));

        var add = ApLook.MakeButton("Add a running app", true, 202);
        add.Location = new Point(Dpi.S(24), Dpi.S(464));
        add.Click += (s, e) => PickRunning();
        var browse = ApLook.MakeButton("Browse for a program…", false, 202);
        browse.Location = new Point(Dpi.S(460 - 24 - 202), Dpi.S(464));
        browse.Click += (s, e) => Browse();
        var done = ApLook.MakeButton("Done", false, 110);
        done.Location = new Point(Dpi.S(460 - 24 - 110), Dpi.S(526));
        done.DialogResult = DialogResult.OK;
        AcceptButton = done; CancelButton = done;

        Controls.AddRange(new Control[] { enable, desc, head, list, add, browse, done });
        Reload();
        refresh.Tick += (s, e) => MarkRunning(); refresh.Start();
    }

    void Notify() { if (Changed != null) Changed(this, EventArgs.Empty); MarkRunning(); }
    public void ReloadSwitch() { enable.Checked = Settings.Autopilot; MarkRunning(); }   // switched from the tray or main window

    void Reload()
    {
        list.SuspendLayout();
        foreach (Control c in list.Controls) c.Dispose();
        list.Controls.Clear();
        foreach (var entry in Settings.AutopilotApps)
        {
            var row = new AppRow { Path = entry, Title = ApApps.Label(entry), Sub = Path.IsPathRooted(entry) ? Path.GetFileName(entry) : entry, Ico = ApApps.IconOf(entry) };
            string captured = entry;
            row.RemoveClicked += (s, e) => { Settings.AutopilotApps.Remove(captured); Settings.Save(); Reload(); Notify(); };
            list.Controls.Add(row);
        }
        list.ResumeLayout(); list.FitRows();
        MarkRunning();
    }

    void MarkRunning()
    {
        HashSet<string> r = null; try { r = running(); } catch { }
        foreach (Control c in list.Controls)
        {
            var row = c as AppRow;
            if (row != null) row.Running = Settings.Autopilot && r != null && r.Contains(ApApps.Key(row.Path));
        }
    }

    void Add(IEnumerable<string> paths)
    {
        bool any = false;
        foreach (var p in paths)
        {
            if (string.IsNullOrEmpty(p) || !p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || ApApps.Contains(p)) continue;
            Settings.AutopilotApps.Add(p); any = true;
        }
        if (!any) return;
        // choosing apps means you want Autopilot: switch it on for you
        if (!Settings.Autopilot) { Settings.Autopilot = true; enable.Checked = true; }
        Settings.Save(); Reload(); Notify();
    }

    void PickRunning()
    {
        using (var f = new AppPicker())
            if (f.ShowDialog(this) == DialogResult.OK) Add(f.Chosen);
    }

    void Browse()
    {
        using (var d = new OpenFileDialog
        {
            Title = "Choose programs for Autopilot", Filter = "Programs and shortcuts (*.exe, *.lnk)|*.exe;*.lnk",
            Multiselect = true, DereferenceLinks = true, InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        })
        {
            if (d.ShowDialog(this) != DialogResult.OK) return;
            var picked = new List<string>(d.FileNames);
            if (!picked.Exists(p => p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
            { MessageBox.Show(this, "Please choose a program file (.exe).", "Autopilot", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            Add(picked);
        }
    }

    protected override void Dispose(bool d) { if (d) refresh.Dispose(); base.Dispose(d); }
}

// ---------------------------------------------------------------- "Add a running app"

class RunningApp { public string Path, Exe, Title, Window; public bool Windowed; public Icon Ico; }

class AppPicker : Form
{
    readonly TextBox search = new TextBox();
    readonly AppList list = new AppList();
    readonly Button add, cancel;
    readonly List<AppRow> rows = new List<AppRow>();
    SectionRow openHead, bgHead;
    public readonly List<string> Chosen = new List<string>();

    // Processes that are part of Windows itself and would make no sense to watch.
    static readonly HashSet<string> Skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "explorer.exe", "applicationframehost.exe", "textinputhost.exe", "shellexperiencehost.exe", "startmenuexperiencehost.exe",
        "searchhost.exe", "searchapp.exe", "lockapp.exe", "systemsettings.exe", "dwm.exe", "csrss.exe", "winlogon.exe",
        "sihost.exe", "taskhostw.exe", "ctfmon.exe", "runtimebroker.exe", "conhost.exe", "svchost.exe", "dllhost.exe", "widgets.exe" };

    public AppPicker()
    {
        AutoScaleMode = AutoScaleMode.None;
        Text = "Add a running app"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent; BackColor = Theme.Dark; ForeColor = Color.White; DoubleBuffered = true;
        Font = new Font("Segoe UI", 11.5f);
        try { Icon = Logo.MakeIcon(32, true); } catch { }
        ClientSize = new Size(Dpi.S(460), Dpi.S(600));

        var hint = new Label
        {
            Text = "Tick the apps that should turn Cooling Mode off while they're open. Open the app first if you don't see it.",
            ForeColor = Color.Silver, BackColor = Color.Transparent, Font = new Font("Segoe UI", 10f),
            Location = new Point(Dpi.S(24), Dpi.S(16)), Size = new Size(Dpi.S(412), Dpi.S(46))
        };
        search.BorderStyle = BorderStyle.FixedSingle; search.BackColor = Color.FromArgb(38, 38, 42); search.ForeColor = Color.White;
        search.Font = new Font("Segoe UI", 11.5f); search.Location = new Point(Dpi.S(24), Dpi.S(68)); search.Width = Dpi.S(412);
        search.HandleCreated += (s, e) => ApNative.SendMessage(search.Handle, 0x1501 /* EM_SETCUEBANNER */, (IntPtr)1, "Search apps");
        search.TextChanged += (s, e) => Filter();

        list.EmptyText = "Looking for running apps…";
        list.Location = new Point(Dpi.S(24), Dpi.S(108)); list.Size = new Size(Dpi.S(412), Dpi.S(416));

        add = ApLook.MakeButton("Add", true, 130); add.Location = new Point(Dpi.S(460 - 24 - 130), Dpi.S(540)); add.Enabled = false;
        add.Click += (s, e) => { foreach (var r in rows) if (r.Checked) Chosen.Add(r.Path); DialogResult = DialogResult.OK; };
        cancel = ApLook.MakeButton("Cancel", false, 110); cancel.Location = new Point(Dpi.S(460 - 24 - 130 - 12 - 110), Dpi.S(540));
        cancel.DialogResult = DialogResult.Cancel; CancelButton = cancel;

        Controls.AddRange(new Control[] { hint, search, list, add, cancel });
        Shown += (s, e) => { search.Focus(); LoadApps(); };
    }

    void LoadApps()
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            List<RunningApp> apps = null;
            try { apps = Find(); } catch { apps = new List<RunningApp>(); }
            try { BeginInvoke((Action)(() => Fill(apps))); } catch { }
        });
    }

    public static List<RunningApp> Find()
    {
        uint mySession; ApNative.ProcessIdToSessionId((uint)Process.GetCurrentProcess().Id, out mySession);
        uint me = (uint)Process.GetCurrentProcess().Id;
        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\') + "\\";
        var windows = ApNative.WindowOwners();
        var byPath = new Dictionary<string, RunningApp>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in ApNative.Snapshot())
        {
            uint sess;
            if (p.Pid <= 4 || p.Pid == me || Skip.Contains(p.Exe)) continue;
            if (!ApNative.ProcessIdToSessionId(p.Pid, out sess) || sess != mySession) continue;
            string path = ApNative.ImagePath(p.Pid);
            if (path == null || ApApps.Contains(path)) continue;
            string title; bool win = windows.TryGetValue(p.Pid, out title);
            if (!win && path.StartsWith(winDir, StringComparison.OrdinalIgnoreCase)) continue;   // Windows' own background helpers
            RunningApp a;
            if (!byPath.TryGetValue(path, out a)) byPath[path] = a = new RunningApp { Path = path, Exe = Path.GetFileName(path) };
            if (win && !a.Windowed) { a.Windowed = true; a.Window = title; }
        }
        var list = new List<RunningApp>(byPath.Values);
        foreach (var a in list) { a.Title = ApApps.Label(a.Path); a.Ico = ApApps.IconOf(a.Path); }
        list.Sort((x, y) => x.Windowed != y.Windowed ? (x.Windowed ? -1 : 1) : string.Compare(x.Title, y.Title, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

    void Fill(List<RunningApp> apps)
    {
        list.SuspendLayout();
        openHead = new SectionRow("Open apps"); bgHead = new SectionRow("Running in the background");
        list.Controls.Add(openHead);
        bool bgAdded = false;
        foreach (var a in apps)
        {
            if (!a.Windowed && !bgAdded) { list.Controls.Add(bgHead); bgAdded = true; }
            var row = new AppRow { Path = a.Path, Title = a.Title, Sub = a.Exe, Ico = a.Ico, Checkable = true, Tag = a.Windowed };
            row.SearchText = (a.Title + " " + a.Exe + " " + a.Window).ToLowerInvariant();
            row.CheckedChanged += (s, e) => UpdateAdd();
            rows.Add(row); list.Controls.Add(row);
        }
        if (!bgAdded) list.Controls.Add(bgHead);
        list.EmptyText = apps.Count == 0 ? "No other apps are running." : "Nothing matches your search.";
        list.ResumeLayout();
        Filter();
    }

    void Filter()
    {
        if (openHead == null) return;
        string q = search.Text.Trim().ToLowerInvariant();
        bool anyOpen = false, anyBg = false;
        list.SuspendLayout();
        foreach (var r in rows)
        {
            bool v = q.Length == 0 || r.SearchText.Contains(q) || r.Checked;
            r.Visible = v;
            if (v) { if ((bool)r.Tag) anyOpen = true; else anyBg = true; }
        }
        openHead.Visible = anyOpen; bgHead.Visible = anyBg;
        list.ResumeLayout(); list.FitRows();
    }

    void UpdateAdd()
    {
        int n = 0; foreach (var r in rows) if (r.Checked) n++;
        add.Enabled = n > 0; add.Text = n > 1 ? "Add " + n + " apps" : "Add";
    }
}
