// Frostbyte GUI - main window, settings dialog, tray icon.
// Uses CoolMode.cs for the power-scheme logic, Settings.cs for persistence/startup, Logo.cs for the icon.
// "--snap file.png [on|off]" and "--snap-settings file.png" capture the real window and exit (layout previews).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading;
using System.Windows.Forms;

static class Dpi
{
    public static readonly float K;
    static Dpi() { using (var g = Graphics.FromHwnd(IntPtr.Zero)) K = g.DpiX / 96f; }
    public static int S(int v) { return (int)Math.Round(v * K); }
}

static class Theme
{
    public static Color Blend(Color a, Color b, float t)
    {
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
    public static readonly Color Accent = Color.FromArgb(33, 150, 243);
    public static readonly Color Dark = Color.FromArgb(18, 18, 18);
}

class Toggle : Control
{
    bool on; float pos; readonly System.Windows.Forms.Timer anim = new System.Windows.Forms.Timer();
    public event EventHandler Toggled;
    public event EventHandler PosChanged;
    public float Pos { get { return pos; } }
    public Toggle()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor, true);
        Cursor = Cursors.Hand; anim.Interval = 15;
        anim.Tick += (s, e) =>
        {
            float t = on ? 1f : 0f; pos += (t - pos) * 0.35f;
            if (Math.Abs(t - pos) < 0.01f) { pos = t; anim.Stop(); }
            Invalidate(); if (PosChanged != null) PosChanged(this, EventArgs.Empty);
        };
    }
    public bool On { get { return on; } set { if (on != value) { on = value; anim.Start(); } } }
    public void SetImmediate(bool v) { on = v; pos = v ? 1f : 0f; Invalidate(); if (PosChanged != null) PosChanged(this, EventArgs.Empty); }
    protected override void OnClick(EventArgs e) { base.OnClick(e); if (Toggled != null) Toggled(this, e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        var r = new Rectangle(1, 1, Width - 3, Height - 3);
        Color orange = Color.FromArgb(238, 128, 78);
        using (var path = Pill(r))
        {
            Color a = Theme.Blend(Color.FromArgb(78, 40, 34), Color.FromArgb(62, 108, 226), pos);
            Color b = Theme.Blend(Color.FromArgb(96, 54, 42), Color.FromArgb(30, 178, 232), pos);
            using (var br = new LinearGradientBrush(r, a, b, 0f)) g.FillPath(br, path);
        }
        int d = r.Height - 8; float x = r.X + 4 + pos * (r.Width - d - 8);
        using (var f = new Font("Segoe UI Semibold", 13f, FontStyle.Bold))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        using (var tb = new SolidBrush(Theme.Blend(Color.FromArgb(246, 150, 60), Color.White, pos)))
        {
            if (pos > 0.5f) g.DrawString("ON", f, tb, new RectangleF(r.X + 6, r.Y, r.Width - d - 14, r.Height), sf);
            else g.DrawString("OFF", f, tb, new RectangleF(r.X + d + 8, r.Y, r.Width - d - 14, r.Height), sf);
        }
        using (var sh = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillEllipse(sh, x, r.Y + 5, d, d);
        using (var kb = new SolidBrush(Theme.Blend(orange, Color.White, pos))) g.FillEllipse(kb, x, r.Y + 4, d, d);
    }
    static GraphicsPath Pill(Rectangle r)
    {
        var p = new GraphicsPath(); int d = r.Height;
        p.AddArc(r.X, r.Y, d, d, 90, 180); p.AddArc(r.Right - d, r.Y, d, d, 270, 180); p.CloseFigure(); return p;
    }
}

class FanIcon : Control
{
    public FanIcon() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); BackColor = Color.Transparent; }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.ScaleTransform(Width / 52f, Height / 52f);   // design units are 52x52
        const float c = 26f;
        using (var ring = new Pen(Color.FromArgb(40, 120, 220), 4f)) g.DrawEllipse(ring, 2, 2, 47, 47);
        using (var fill = new SolidBrush(Color.FromArgb(235, 238, 242)))
        using (var edge = new Pen(Color.FromArgb(120, 130, 145), 1f))
            for (int i = 0; i < 7; i++)
            {
                var st = g.Save(); g.TranslateTransform(c, c); g.RotateTransform(i * 360f / 7);
                var blade = new GraphicsPath();
                blade.AddBezier(0, -3, 11, -8, 13, -18, 4, -21);
                blade.AddBezier(4, -21, -3, -19, -6, -9, 0, -3);
                g.FillPath(fill, blade); g.DrawPath(edge, blade); g.Restore(st);
            }
        using (var hub = new SolidBrush(Color.FromArgb(235, 170, 20))) g.FillEllipse(hub, c - 5, c - 5, 10, 10);
        using (var hub2 = new SolidBrush(Color.FromArgb(40, 120, 220))) g.FillEllipse(hub2, c - 2, c - 2, 4, 4);
    }
}

class GearButton : Control
{
    bool hot;
    public GearButton() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); BackColor = Color.Transparent; Cursor = Cursors.Hand; }
    protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hot = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        if (hot) using (var b = new SolidBrush(Color.FromArgb(50, 255, 255, 255))) g.FillEllipse(b, 0, 0, Width - 1, Height - 1);
        float c = Width / 2f, r = Width * 0.30f;
        using (var pen = new Pen(Color.White, Width * 0.075f)) g.DrawEllipse(pen, c - r * 0.62f, c - r * 0.62f, r * 1.24f, r * 1.24f);
        using (var pen = new Pen(Color.White, Width * 0.11f) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
            for (int i = 0; i < 8; i++)
            {
                var st = g.Save(); g.TranslateTransform(c, c); g.RotateTransform(i * 45f);
                g.DrawLine(pen, 0, -r * 0.82f, 0, -r * 1.12f); g.Restore(st);
            }
    }
}

class Header : Panel
{
    float t;   // 0 = off (orange), 1 = on (blue)
    public float T { get { return t; } set { t = value; Invalidate(); } }
    public Header() { DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = ClientRectangle;
        Color c1 = Theme.Blend(Color.FromArgb(168, 56, 44), Color.FromArgb(14, 52, 120), t);
        Color c2 = Theme.Blend(Color.FromArgb(224, 104, 74), Color.FromArgb(38, 138, 232), t);
        Color lc = Theme.Blend(Color.FromArgb(255, 176, 140), Color.FromArgb(170, 225, 255), t);
        using (var br = new LinearGradientBrush(r, c1, c2, 20f)) g.FillRectangle(br, r);
        float k = Width / 440f;
        var rnd = new Random(7);
        using (var pen = new Pen(Color.FromArgb(70, lc), 1.4f * k))
        using (var pad = new SolidBrush(Color.FromArgb(70, lc)))
        {
            for (int i = 0; i < 60; i++)
            {
                int x = rnd.Next(0, r.Width), y = rnd.Next(0, r.Height), l = (int)(rnd.Next(20, 90) * k);
                int dx = rnd.Next(2) == 0 ? l : 0, dy = dx == 0 ? l : 0;
                g.DrawLine(pen, x, y, x + dx, y + dy);
                g.FillEllipse(pad, x - 3 * k, y - 3 * k, 6 * k, 6 * k);
                if (rnd.Next(3) == 0) g.DrawLine(pen, x + dx, y + dy, x + dx + (dx == 0 ? 18 * k : 0), y + dy + (dx == 0 ? 0 : 18 * k));
            }
            float cs = 70 * k; var chip = new RectangleF(r.Width * 0.72f, r.Height / 2f - cs / 2, cs, cs);
            using (var fillc = new SolidBrush(Color.FromArgb(40, lc))) g.FillRectangle(fillc, chip.X, chip.Y, chip.Width, chip.Height);
            g.DrawRectangle(pen, chip.X, chip.Y, chip.Width, chip.Height);
            for (int i = 0; i < 6; i++)
            {
                float px = chip.X + (9 + i * 10) * k;
                g.DrawLine(pen, px, chip.Y - 9 * k, px, chip.Y); g.DrawLine(pen, px, chip.Bottom, px, chip.Bottom + 9 * k);
            }
        }
    }
}

// ---- checkbox used by the settings dialog ----
class DarkCheck : Control
{
    bool chk;
    public event EventHandler Changed;
    public bool Checked { get { return chk; } set { chk = value; Invalidate(); } }
    public DarkCheck() { SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Cursor = Cursors.Hand; }
    protected override void OnClick(EventArgs e) { chk = !chk; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); base.OnClick(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);
        float bs = Height * 0.62f, by = (Height - bs) / 2f; var box = new RectangleF(1, by, bs, bs);
        using (var path = Round(box, bs * 0.22f))
        {
            if (chk) using (var b = new SolidBrush(Theme.Accent)) g.FillPath(b, path);
            else using (var p = new Pen(Color.FromArgb(120, 120, 125), 1.6f)) g.DrawPath(p, path);
        }
        if (chk)
            using (var p = new Pen(Color.White, bs * 0.12f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(p, new[] { new PointF(box.X + bs * 0.24f, box.Y + bs * 0.54f), new PointF(box.X + bs * 0.43f, box.Y + bs * 0.72f), new PointF(box.X + bs * 0.77f, box.Y + bs * 0.30f) });
        using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            g.DrawString(Text, Font, Brushes.White, new RectangleF(bs + Dpi.S(14), 0, Width - bs - Dpi.S(14), Height), sf);
    }
    static GraphicsPath Round(RectangleF r, float rad)
    {
        float d = rad * 2; var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
    }
}

class SettingsForm : Form
{
    public SettingsForm()
    {
        AutoScaleMode = AutoScaleMode.None;
        Text = "Settings"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent; BackColor = Theme.Dark; ForeColor = Color.White; DoubleBuffered = true;
        Font = new Font("Segoe UI", 11.5f);
        try { Icon = Logo.MakeIcon(32, true); } catch { }
        int top = 24, row = 54;
        ClientSize = new Size(Dpi.S(440), Dpi.S(top * 2 + row * 4 - 14));

        Add(0, top, row, "Launch app on PC startup", Startup.IsEnabled(), v =>
        {
            try { if (v) Startup.Enable(); else Startup.Disable(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Frostbyte", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }, true);
        Add(1, top, row, "Launch app in background mode", Settings.Background, v => { Settings.Background = v; Settings.Save(); }, false);
        Add(2, top, row, "Enable Cooling Mode on app launch", Settings.CoolOnLaunch, v => { Settings.CoolOnLaunch = v; Settings.Save(); }, false);
        Add(3, top, row, "Close button minimizes to system tray", Settings.CloseToTray, v => { Settings.CloseToTray = v; Settings.Save(); }, false);
    }
    void Add(int i, int top, int row, string text, bool val, Action<bool> on, bool resync)
    {
        var c = new DarkCheck { Text = text, Checked = val, BackColor = Theme.Dark, Font = Font, Size = new Size(Dpi.S(400), Dpi.S(36)), Location = new Point(Dpi.S(24), Dpi.S(top + i * row)) };
        c.Changed += (s, e) => { on(c.Checked); if (resync) c.Checked = Startup.IsEnabled(); };
        Controls.Add(c);
    }
}

class MainForm : Form
{
    readonly Label cpu = new Label(), name = new Label(), title = new Label();
    readonly Toggle toggle = new Toggle();
    readonly Header head = new Header();
    readonly FanIcon fan = new FanIcon();
    readonly GearButton gear = new GearButton();
    readonly System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer();
    readonly NotifyIcon tray = new NotifyIcon();
    readonly ToolStripMenuItem coolItem = new ToolStripMenuItem("Cooling Mode");
    Icon trayOn, trayOff;
    bool allowVisible, exiting;

    void DoLayout()
    {
        ClientSize = new Size(Dpi.S(440), Dpi.S(270));
        head.Height = Dpi.S(150);
        cpu.Location = new Point(Dpi.S(18), Dpi.S(14)); name.Location = new Point(Dpi.S(18), Dpi.S(46)); name.Size = new Size(Dpi.S(290), Dpi.S(60));
        gear.Size = new Size(Dpi.S(34), Dpi.S(34)); gear.Location = new Point(Dpi.S(440 - 44), Dpi.S(10));
        fan.Size = new Size(Dpi.S(52), Dpi.S(52)); fan.Location = new Point(Dpi.S(22), Dpi.S(188));
        title.Location = new Point(Dpi.S(86), Dpi.S(196));
        toggle.Size = new Size(Dpi.S(144), Dpi.S(56)); toggle.Location = new Point(Dpi.S(270), Dpi.S(186));
    }
    protected override void SetVisibleCore(bool v) { if (!allowVisible) v = false; base.SetVisibleCore(v); }
    public void ShowMain()
    {
        allowVisible = true; Show(); if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate(); BringToFront();
    }

    public MainForm(bool snap, bool snapOn)
    {
        AutoScaleMode = AutoScaleMode.None;   // we scale by hand in DoLayout
        Text = "Frostbyte"; FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen; BackColor = Theme.Dark;
        Font = new Font("Segoe UI", 10f); DoubleBuffered = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { try { Icon = Logo.MakeIcon(32, true); } catch { } }

        head.Dock = DockStyle.Top;
        cpu.Text = "CPU"; cpu.Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);
        name.Text = CpuName(); name.Font = new Font("Segoe UI", 12.5f); name.AutoSize = false;
        foreach (var l in new[] { cpu, name }) { l.BackColor = Color.Transparent; l.ForeColor = Color.White; head.Controls.Add(l); }
        cpu.AutoSize = true; head.Controls.Add(gear);
        Controls.Add(head);

        title.Text = "Cooling Mode"; title.ForeColor = Color.White; title.Font = new Font("Segoe UI Semibold", 15f);
        title.AutoSize = true; title.BackColor = Color.Transparent;
        toggle.Toggled += (s, e) => DoToggle();
        toggle.PosChanged += (s, e) => head.T = toggle.Pos;
        gear.Click += (s, e) => { using (var f = new SettingsForm()) f.ShowDialog(this); };
        Controls.AddRange(new Control[] { fan, title, toggle });
        DoLayout();

        if (snap) { toggle.SetImmediate(snapOn); return; }

        // tray icon (blue = cooling on, orange = off)
        Size si = SystemInformation.SmallIconSize;
        trayOn = Logo.MakeIcon(si.Width, true); trayOff = Logo.MakeIcon(si.Width, false);
        var menu = new ContextMenuStrip();
        var open = new ToolStripMenuItem("Open Frostbyte"); open.Font = new Font(open.Font, FontStyle.Bold); open.Click += (s, e) => ShowMain();
        coolItem.Click += (s, e) => DoToggle();
        var exit = new ToolStripMenuItem("Exit"); exit.Click += (s, e) => { exiting = true; Close(); };
        menu.Items.AddRange(new ToolStripItem[] { open, coolItem, new ToolStripSeparator(), exit });
        tray.ContextMenuStrip = menu; tray.Visible = true;
        tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowMain(); };

        if (Settings.CoolOnLaunch) { try { if (!CoolMode.IsOn()) CoolMode.Enable(); } catch { } }
        SyncState(true);
        poll.Interval = 2000; poll.Tick += (s, e) => SyncState(false); poll.Start();

        // a second launch asks this instance to show itself
        try
        {
            var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\Frostbyte.Show");
            var th = new Thread(() => { while (true) { evt.WaitOne(); try { BeginInvoke((Action)ShowMain); } catch { } } }) { IsBackground = true };
            th.Start();
        }
        catch { }
    }

    void SyncState(bool immediate)
    {
        bool on = false; try { on = CoolMode.IsOn(); } catch { }
        if (immediate) toggle.SetImmediate(on); else toggle.On = on;
        coolItem.Checked = on;
        if (trayOn != null) tray.Icon = on ? trayOn : trayOff;
        tray.Text = "Frostbyte – Cooling Mode " + (on ? "ON" : "OFF");
    }
    void DoToggle()
    {
        try { if (CoolMode.IsOn()) CoolMode.Disable(); else CoolMode.Enable(); SyncState(false); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Frostbyte", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!exiting && Settings.CloseToTray && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
        tray.Visible = false; base.OnFormClosing(e);
    }
    protected override void Dispose(bool d) { if (d) tray.Dispose(); base.Dispose(d); }

    static string CpuName()
    {
        try
        {
            using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                return ((string)k.GetValue("ProcessorNameString")).Trim();
        }
        catch { return "Processor"; }
    }
}

static class CoolModeGui
{
    static void Snap(Form f, string path)
    {
        f.Show(); Application.DoEvents(); Thread.Sleep(700); Application.DoEvents();
        using (var bmp = new Bitmap(f.Width, f.Height))
        using (var g = Graphics.FromImage(bmp))
        { g.CopyFromScreen(f.Location, Point.Empty, f.Size); bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png); }
    }

    [STAThread]
    static void Main(string[] a)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        Settings.Load();
        if (a.Length >= 2 && a[0] == "--snap")
        {
            var f = new MainForm(true, a.Length < 3 || a[2] != "off"); f.ShowMain(); Snap(f, a[1]); return;
        }
        if (a.Length >= 2 && a[0] == "--snap-settings")
        {
            var f = new SettingsForm(); f.StartPosition = FormStartPosition.CenterScreen; Snap(f, a[1]); return;
        }

        if (a.Length >= 2 && a[0] == "--startup")   // scripting hook: Frostbyte.exe --startup on|off
        {
            try { if (a[1] == "on") Startup.Enable(); else Startup.Disable(); } catch { Environment.ExitCode = 1; }
            return;
        }

        Mutex mtx = null; bool created = true;
        try
        {
            mtx = new Mutex(true, "Local\\Frostbyte.Single", out created);
            if (!created) { try { EventWaitHandle.OpenExisting("Local\\Frostbyte.Show").Set(); } catch { } return; }
        }
        catch { }

        var form = new MainForm(false, false);
        var h = form.Handle;                        // create the handle so the tray can reach us while hidden
        if (!Settings.Background) form.ShowMain();
        Application.Run(form);
        GC.KeepAlive(mtx);
    }
}
