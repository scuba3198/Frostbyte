// Frostbyte logo: a snowflake on a rounded gradient tile. Blue = cooling ON, orange = OFF.
// Shared by the app (tray/window icon) and tools\IconGen.cs (builds Frostbyte.ico).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

public static class Logo
{
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    public static void Draw(Graphics g, int size, bool on)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        float s = size;
        var r = new RectangleF(0, 0, s, s);
        Color c1 = on ? Color.FromArgb(14, 52, 120) : Color.FromArgb(168, 56, 44);
        Color c2 = on ? Color.FromArgb(38, 160, 244) : Color.FromArgb(240, 128, 72);
        using (var path = RoundRect(r, s * 0.22f))
        using (var br = new LinearGradientBrush(r, c1, c2, 45f)) g.FillPath(br, path);

        float c = s / 2f, len = s * 0.34f, w = Math.Max(1.3f, s * 0.062f);
        double a = 50 * Math.PI / 180.0;
        float by = -len * 0.55f, bl = len * 0.40f;
        float bx = (float)(bl * Math.Sin(a)), byy = by - (float)(bl * Math.Cos(a));
        using (var pen = new Pen(Color.White, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
        {
            for (int i = 0; i < 6; i++)
            {
                var st = g.Save();
                g.TranslateTransform(c, c); g.RotateTransform(i * 60f);
                g.DrawLine(pen, 0, 0, 0, -len);
                g.DrawLine(pen, 0, by, bx, byy);
                g.DrawLine(pen, 0, by, -bx, byy);
                g.Restore(st);
            }
            using (var hub = new SolidBrush(Color.White)) g.FillEllipse(hub, c - w * 0.9f, c - w * 0.9f, w * 1.8f, w * 1.8f);
        }
    }

    public static Icon MakeIcon(int size, bool on)
    {
        using (var b = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(b)) Draw(g, size, on);
            IntPtr h = b.GetHicon();
            Icon ic = (Icon)Icon.FromHandle(h).Clone();
            DestroyIcon(h);
            return ic;
        }
    }

    static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        float d = rad * 2; var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure(); return p;
    }
}
