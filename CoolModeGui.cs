// Frostbyte GUI - window with the CPU name and a Cooling Mode ON/OFF switch.
// Uses CoolMode.cs (same project) for the actual power-scheme logic.
// Build: see build.ps1.   "--snap file.png [on|off]" renders the window to a PNG and exits (layout preview).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

static class Theme
{
    // OFF = orange/red, ON = blue (blended by t in 0..1)
    public static Color Blend(Color a, Color b, float t)
    {
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
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
        Size = new Size(144, 56); Cursor = Cursors.Hand; anim.Interval = 15;
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
        {
            Color tc = Theme.Blend(Color.FromArgb(246, 150, 60), Color.White, pos);
            using (var tb = new SolidBrush(tc))
            {
                if (pos > 0.5f) g.DrawString("ON", f, tb, new RectangleF(r.X + 6, r.Y, r.Width - d - 14, r.Height), sf);
                else g.DrawString("OFF", f, tb, new RectangleF(r.X + d + 8, r.Y, r.Width - d - 14, r.Height), sf);
            }
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
    public FanIcon() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); Size = new Size(52, 52); BackColor = Color.Transparent; }
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

class MainForm : Form
{
    readonly Label cpu = new Label(), name = new Label(), title = new Label();
    readonly Toggle toggle = new Toggle();
    readonly Header head = new Header();
    readonly System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer();

    readonly FanIcon fan = new FanIcon();

    // Lay everything out in 96-dpi design units multiplied by k (= current DPI / 96).
    void DoLayout(float k)
    {
        Func<int, int> s = v => (int)Math.Round(v * k);
        ClientSize = new Size(s(440), s(270));
        head.Height = s(150);
        cpu.Location = new Point(s(18), s(14)); name.Location = new Point(s(18), s(46)); name.Size = new Size(s(290), s(60));
        fan.Size = new Size(s(52), s(52)); fan.Location = new Point(s(22), s(188));
        title.Location = new Point(s(86), s(196));
        toggle.Size = new Size(s(144), s(56)); toggle.Location = new Point(s(270), s(186));
        head.Invalidate();
    }
    static float SystemScale() { using (var g = Graphics.FromHwnd(IntPtr.Zero)) return g.DpiX / 96f; }

    public MainForm(bool snap, bool snapOn)
    {
        AutoScaleMode = AutoScaleMode.None;   // we scale by hand in DoLayout
        Text = "Frostbyte"; FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(18, 18, 18);
        Font = new Font("Segoe UI", 10f); DoubleBuffered = true;

        head.Dock = DockStyle.Top;
        cpu.Text = "CPU"; cpu.Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);
        name.Text = CpuName(); name.Font = new Font("Segoe UI", 12.5f); name.AutoSize = false;
        foreach (var l in new[] { cpu, name }) { l.BackColor = Color.Transparent; l.ForeColor = Color.White; head.Controls.Add(l); }
        cpu.AutoSize = true;
        Controls.Add(head);

        title.Text = "Cooling Mode"; title.ForeColor = Color.White; title.Font = new Font("Segoe UI Semibold", 15f);
        title.AutoSize = true; title.BackColor = Color.Transparent;
        toggle.Toggled += OnToggle;
        toggle.PosChanged += (s, e) => head.T = toggle.Pos;
        Controls.AddRange(new Control[] { fan, title, toggle });
        DoLayout(SystemScale());

        if (snap) { toggle.SetImmediate(snapOn); return; }
        try { toggle.SetImmediate(CoolMode.IsOn()); } catch { }
        poll.Interval = 2000; poll.Tick += (s, e) => { try { toggle.On = CoolMode.IsOn(); } catch { } }; poll.Start();
    }
    void OnToggle(object s, EventArgs e)
    {
        try { if (CoolMode.IsOn()) CoolMode.Disable(); else CoolMode.Enable(); toggle.On = CoolMode.IsOn(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Frostbyte", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
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
}

static class CoolModeGui
{
    [STAThread]
    static void Main(string[] a)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (a.Length >= 2 && a[0] == "--snap")
        {
            var f = new MainForm(true, a.Length < 3 || a[2] != "off"); f.Show(); Application.DoEvents(); System.Threading.Thread.Sleep(600); Application.DoEvents();
            System.IO.File.WriteAllText(a[1] + ".txt", "DeviceDpi=" + f.DeviceDpi + " size=" + f.Size + " screen=" + Screen.PrimaryScreen.Bounds + " client=" + f.ClientSize);
            using (var bmp = new Bitmap(f.Width, f.Height))
            using (var g = Graphics.FromImage(bmp))
            { g.CopyFromScreen(f.Location, Point.Empty, f.Size); bmp.Save(a[1], System.Drawing.Imaging.ImageFormat.Png); }
            return;
        }
        Application.Run(new MainForm(false, false));
    }
}
