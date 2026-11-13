// Generates Frostbyte.ico (PNG-compressed multi-size icon) from Logo.cs.  Usage: IconGen.exe out.ico
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

static class IconGen
{
    static int Main(string[] a)
    {
        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
        var pngs = new List<byte[]>();
        foreach (int s in sizes)
            using (var b = new Bitmap(s, s, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(b)) Logo.Draw(g, s, true);
                using (var ms = new MemoryStream()) { b.Save(ms, System.Drawing.Imaging.ImageFormat.Png); pngs.Add(ms.ToArray()); }
            }
        using (var f = new BinaryWriter(File.Create(a[0])))
        {
            f.Write((short)0); f.Write((short)1); f.Write((short)sizes.Length);
            int off = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                f.Write((byte)(s >= 256 ? 0 : s)); f.Write((byte)(s >= 256 ? 0 : s));
                f.Write((byte)0); f.Write((byte)0); f.Write((short)1); f.Write((short)32);
                f.Write(pngs[i].Length); f.Write(off); off += pngs[i].Length;
            }
            foreach (var p in pngs) f.Write(p);
        }
        return 0;
    }
}
