// CoolMode GUI - window with CPU temperature and a Cooling Mode ON/OFF switch.
// Uses CoolMode.cs (same project) for the actual power-scheme logic.
// Build: see build.ps1.   "--snap file.png" renders the window to a PNG and exits (layout preview).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Management;
using System.Threading;
using System.Windows.Forms;

class Toggle : Control
{
    bool on; float pos; readonly System.Windows.Forms.Timer anim = new System.Windows.Forms.Timer();
    public event EventHandler Toggled;
    public Toggle()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(144, 56); Cursor = Cursors.Hand; anim.Interval = 15;
        anim.Tick += (s, e) =>
        {
            float t = on ? 1f : 0f; pos += (t - pos) * 0.35f;
            if (Math.Abs(t - pos) < 0.01f) { pos = t; anim.Stop(); }
            Invalidate();
        };
    }
    public bool On { get { return on; } set { if (on != value) { on = value; anim.Start(); } } }
    protected override void OnClick(EventArgs e) { base.OnClick(e); if (Toggled != null) Toggled(this, e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        var r = new Rectangle(1, 1, Width - 3, Height - 3);
        using (var path = Pill(r))
        {
            Color a = Blend(Color.FromArgb(58, 58, 62), Color.FromArgb(62, 108, 226), pos);
            Color b = Blend(Color.FromArgb(70, 70, 74), Color.FromArgb(30, 178, 232), pos);
            using (var br = new LinearGradientBrush(r, a, b, 0f)) g.FillPath(br, path);
        }
        int d = r.Height - 8; float x = r.X + 4 + pos * (r.Width - d - 8);
        using (var f = new Font("Segoe UI Semibold", 13f, FontStyle.Bold))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            if (pos > 0.5f) g.DrawString("ON", f, Brushes.White, new RectangleF(r.X + 6, r.Y, r.Width - d - 14, r.Height), sf);
            else g.DrawString("OFF", f, Brushes.Gainsboro, new RectangleF(r.X + d + 8, r.Y, r.Width - d - 14, r.Height), sf);
        }
        using (var sh = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillEllipse(sh, x, r.Y + 5, d, d);
        g.FillEllipse(Brushes.White, x, r.Y + 4, d, d);
    }
    static GraphicsPath Pill(Rectangle r)
    {
        var p = new GraphicsPath(); int d = r.Height;
        p.AddArc(r.X, r.Y, d, d, 90, 180); p.AddArc(r.Right - d, r.Y, d, d, 270, 180); p.CloseFigure(); return p;
    }
    static Color Blend(Color a, Color b, float t)
    {
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
}

class FanIcon : Control
{
    public FanIcon() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); Size = new Size(52, 52); BackColor = Color.Transparent; }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        float c = Width / 2f;
        using (var ring = new Pen(Color.FromArgb(40, 120, 220), 4f)) g.DrawEllipse(ring, 2, 2, Width - 5, Height - 5);
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

class Header : Panel
{
    public Header() { DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = ClientRectangle;
        using (var br = new LinearGradientBrush(r, Color.FromArgb(14, 52, 120), Color.FromArgb(38, 138, 232), 20f)) g.FillRectangle(br, r);
        var rnd = new Random(7);
        using (var pen = new Pen(Color.FromArgb(70, 170, 225, 255), 1.4f))
        using (var pad = new SolidBrush(Color.FromArgb(70, 170, 225, 255)))
        {
            for (int i = 0; i < 60; i++)
            {
                int x = rnd.Next(0, r.Width), y = rnd.Next(0, r.Height), l = rnd.Next(20, 90);
                int dx = rnd.Next(2) == 0 ? l : 0, dy = dx == 0 ? l : 0;
                g.DrawLine(pen, x, y, x + dx, y + dy);
                g.FillEllipse(pad, x - 3, y - 3, 6, 6);
                if (rnd.Next(3) == 0) { g.DrawLine(pen, x + dx, y + dy, x + dx + (dx == 0 ? 18 : 0), y + dy + (dx == 0 ? 0 : 18)); }
            }
            var chip = new Rectangle(r.Width / 2 - 22, r.Height / 2 - 30, 62, 62);
            using (var fillc = new SolidBrush(Color.FromArgb(40, 150, 210, 255))) g.FillRectangle(fillc, chip);
            g.DrawRectangle(pen, chip);
            for (int i = 0; i < 6; i++) { g.DrawLine(pen, chip.X + 8 + i * 9, chip.Y - 8, chip.X + 8 + i * 9, chip.Y); g.DrawLine(pen, chip.X + 8 + i * 9, chip.Bottom, chip.X + 8 + i * 9, chip.Bottom + 8); }
        }
    }
}

class MainForm : Form
{
    readonly Label temp = new Label(), unit = new Label(), cpu = new Label(), name = new Label(), title = new Label();
    readonly Toggle toggle = new Toggle();
    readonly System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer();
    volatile int busy;

    public MainForm(bool snap)
    {
        Text = "Frostbyte"; ClientSize = new Size(440, 270); FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(18, 18, 18);
        Font = new Font("Segoe UI", 10f); DoubleBuffered = true;

        var head = new Header { Dock = DockStyle.Top, Height = 150 };
        cpu.Text = "CPU"; cpu.Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);
        name.Text = CpuName(); name.Font = new Font("Segoe UI", 12.5f); name.AutoSize = false; name.Size = new Size(250, 60);
        foreach (var l in new[] { cpu, name, temp, unit }) { l.BackColor = Color.Transparent; l.ForeColor = Color.White; head.Controls.Add(l); }
        cpu.AutoSize = true; cpu.Location = new Point(18, 12); name.Location = new Point(18, 42);
        temp.Font = new Font("Segoe UI Light", 66f); temp.AutoSize = true; temp.Text = "--";
        unit.Font = new Font("Segoe UI Light", 20f); unit.AutoSize = true; unit.Text = "°C";
        Controls.Add(head);

        var fan = new FanIcon { Location = new Point(22, 188) };
        title.Text = "Cooling Mode"; title.ForeColor = Color.White; title.Font = new Font("Segoe UI Semibold", 15f);
        title.AutoSize = true; title.Location = new Point(86, 196);
        toggle.Location = new Point(270, 186); toggle.Toggled += OnToggle;
        Controls.AddRange(new Control[] { fan, title, toggle });
        head.Resize += (s, e) => PlaceTemp(); PlaceTemp();

        try { toggle.On = CoolMode.IsOn(); } catch { }
        if (snap) { temp.Text = "47"; PlaceTemp(); toggle.On = true; return; }
        poll.Interval = 2000; poll.Tick += (s, e) => Refresh2(); poll.Start(); Refresh2();
    }
    void PlaceTemp()
    {
        int right = 440 - 14;
        unit.Location = new Point(right - unit.PreferredWidth, 22);
        temp.Location = new Point(right - unit.PreferredWidth - temp.PreferredWidth + 14, 2);
    }
    void OnToggle(object s, EventArgs e)
    {
        try { if (CoolMode.IsOn()) CoolMode.Disable(); else CoolMode.Enable(); toggle.On = CoolMode.IsOn(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Frostbyte", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    void Refresh2()
    {
        try { toggle.On = CoolMode.IsOn(); } catch { }
        if (Interlocked.Exchange(ref busy, 1) == 1) return;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            double t = ReadTemp();
            try { BeginInvoke((Action)(() => { temp.Text = t > 0 ? Math.Round(t).ToString("0") : "--"; PlaceTemp(); })); } catch { }
            busy = 0;
        });
    }
    static string CpuName()
    {
        try
        {
            using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                return ((string)k.GetValue("ProcessorNameString")).Trim();
        }
        catch { return "Processor"; }
    }
    static double ReadTemp()
    {
        try   // ACPI thermal zones (tenths of Kelvin); needs administrator
        {
            double best = -1;
            using (var s = new ManagementObjectSearcher(@"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature"))
                foreach (ManagementObject o in s.Get()) { double c = Convert.ToDouble(o["CurrentTemperature"]) / 10.0 - 273.15; if (c > best && c < 150) best = c; }
            if (best > 0) return best;
        }
        catch { }
        try   // Windows thermal-zone perf counters (Kelvin)
        {
            double best = -1;
            using (var s = new ManagementObjectSearcher(@"root\CIMV2", "SELECT Temperature FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation"))
                foreach (ManagementObject o in s.Get()) { double c = Convert.ToDouble(o["Temperature"]) - 273.15; if (c > best && c < 150) best = c; }
            return best;
        }
        catch { return -1; }
    }
}

static class CoolModeGui
{
    [STAThread]
    static void Main(string[] a)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (a.Length == 2 && a[0] == "--snap")
        {
            var f = new MainForm(true); f.Show(); Application.DoEvents(); Thread.Sleep(600); Application.DoEvents();
            using (var bmp = new Bitmap(f.ClientSize.Width, f.ClientSize.Height))
            { f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.ClientSize)); bmp.Save(a[1], System.Drawing.Imaging.ImageFormat.Png); }
            return;
        }
        Application.Run(new MainForm(false));
    }
}
