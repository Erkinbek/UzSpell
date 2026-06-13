using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// UzSpell ikonkasi generatori (GDI+).
// Gradient yumaloq kvadrat + oq "✓" belgisi + qizil toʻlqinli chiziq (imlo motivi).
// Bir nechta oʻlchamda PNG render qilib, ularni .ico fayliga jamlaydi.
class IconGen
{
    static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

    static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "uzspell.ico";
        var pngs = new byte[Sizes.Length][];
        for (int i = 0; i < Sizes.Length; i++)
            pngs[i] = RenderPng(Sizes[i]);
        WriteIco(outPath, Sizes, pngs);
        Console.WriteLine("Yaratildi: " + outPath);
        // Preview (256px)
        File.WriteAllBytes(Path.ChangeExtension(outPath, ".preview.png"), pngs[Sizes.Length - 1]);
    }

    static byte[] RenderPng(int s)
    {
        using var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float margin = s * 0.055f;
            float radius = s * 0.22f;
            var rect = new RectangleF(margin, margin, s - 2 * margin, s - 2 * margin);

            // Gradient fon (koʻk -> moviy-yashil), diagonal
            using (var path = RoundedRect(rect, radius))
            using (var brush = new LinearGradientBrush(
                rect, Color.FromArgb(0x21, 0x96, 0xF3), Color.FromArgb(0x0E, 0x9E, 0x83),
                LinearGradientMode.ForwardDiagonal))
            {
                g.FillPath(brush, path);
                // Yengil ichki yorugʻlik (yuqori qism)
                var hi = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height * 0.5f);
                using var hiBrush = new LinearGradientBrush(
                    hi, Color.FromArgb(60, 255, 255, 255), Color.FromArgb(0, 255, 255, 255),
                    LinearGradientMode.Vertical);
                using var hiPath = RoundedRect(rect, radius);
                var oldClip = g.Clip;
                g.SetClip(hiPath);
                g.FillRectangle(hiBrush, hi);
                g.Clip = oldClip;
            }

            // Oq belgi ✓
            using (var pen = new Pen(Color.White, s * 0.115f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                var p1 = new PointF(s * 0.275f, s * 0.505f);
                var p2 = new PointF(s * 0.435f, s * 0.665f);
                var p3 = new PointF(s * 0.730f, s * 0.330f);
                // Yengil soya
                using (var shadow = new Pen(Color.FromArgb(45, 0, 0, 0), s * 0.115f))
                {
                    shadow.StartCap = LineCap.Round; shadow.EndCap = LineCap.Round; shadow.LineJoin = LineJoin.Round;
                    float d = s * 0.012f;
                    g.DrawLines(shadow, new[] {
                        new PointF(p1.X + d, p1.Y + d), new PointF(p2.X + d, p2.Y + d), new PointF(p3.X + d, p3.Y + d) });
                }
                g.DrawLines(pen, new[] { p1, p2, p3 });
            }

            // Qizil toʻlqinli chiziq (imlo tekshiruv motivi) — faqat yetarli oʻlchamlarda
            if (s >= 28)
            {
                float y = s * 0.775f;
                float x1 = s * 0.265f, x2 = s * 0.735f;
                float amp = s * 0.032f;
                float step = s * 0.058f;
                using var wave = new Pen(Color.FromArgb(0xFF, 0x52, 0x52), Math.Max(1f, s * 0.038f));
                wave.StartCap = LineCap.Round; wave.EndCap = LineCap.Round;
                var pts = new System.Collections.Generic.List<PointF>();
                bool up = true;
                pts.Add(new PointF(x1, y));
                for (float x = x1 + step; x < x2 + step; x += step)
                {
                    float px = Math.Min(x, x2);
                    pts.Add(new PointF(px, up ? y - amp : y + amp));
                    up = !up;
                }
                g.DrawCurve(wave, pts.ToArray(), 0.4f);
            }
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    static void WriteIco(string path, int[] sizes, byte[][] pngs)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var w = new BinaryWriter(fs);
        w.Write((short)0);            // reserved
        w.Write((short)1);            // type = icon
        w.Write((short)sizes.Length); // count

        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            int s = sizes[i];
            w.Write((byte)(s >= 256 ? 0 : s)); // width
            w.Write((byte)(s >= 256 ? 0 : s)); // height
            w.Write((byte)0);                  // palette
            w.Write((byte)0);                  // reserved
            w.Write((short)1);                 // planes
            w.Write((short)32);                // bpp
            w.Write(pngs[i].Length);           // bytes
            w.Write(offset);                   // offset
            offset += pngs[i].Length;
        }
        foreach (var png in pngs)
            w.Write(png);
    }
}
