using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class IconGenerator
{
    private static readonly int[] Sizes = { 16, 24, 32, 48, 64, 128, 256 };

    private static int Main(string[] args)
    {
        string icoPath = args.Length > 0 ? args[0] : "DevspaceNgrokFoot.ico";
        string previewPath = args.Length > 1 ? args[1] : "icon-preview.png";

        try
        {
            var pngImages = new List<byte[]>();
            foreach (int size in Sizes)
            {
                using (var bitmap = Render(size))
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    pngImages.Add(stream.ToArray());
                }
            }

            WriteIcon(icoPath, pngImages);

            using (var preview = Render(256))
            {
                preview.Save(previewPath, ImageFormat.Png);
            }

            Console.WriteLine("Generated: " + Path.GetFullPath(icoPath));
            Console.WriteLine("Preview:   " + Path.GetFullPath(previewPath));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static Bitmap Render(int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float s = size / 256f;
            DrawShell(g, s);

            if (size <= 24)
            {
                DrawCompactMark(g, s);
            }
            else
            {
                DrawGlobe(g, s);
                DrawCodeTile(g, s);
                DrawTunnel(g, s);
                DrawCloud(g, s, size >= 64);
                DrawDevSpaceBadge(g, s, size >= 48);
            }
        }

        return bitmap;
    }

    private static void DrawShell(Graphics g, float s)
    {
        using (var shadowPath = RoundedRect(14f * s, 18f * s, 228f * s, 228f * s, 52f * s))
        using (var shadow = new SolidBrush(Color.FromArgb(58, 7, 18, 55)))
        {
            g.FillPath(shadow, shadowPath);
        }

        using (var shellPath = RoundedRect(10f * s, 10f * s, 236f * s, 236f * s, 54f * s))
        using (var shellBrush = new LinearGradientBrush(
            new PointF(24f * s, 14f * s),
            new PointF(232f * s, 238f * s),
            Color.FromArgb(255, 16, 137, 255),
            Color.FromArgb(255, 37, 30, 190)))
        {
            g.FillPath(shellBrush, shellPath);
            using (var edge = new Pen(Color.FromArgb(180, 113, 226, 255), Math.Max(1f, 2.2f * s)))
            {
                g.DrawPath(edge, shellPath);
            }
        }

        using (var highlight = new Pen(Color.FromArgb(130, 255, 255, 255), Math.Max(1f, 1.3f * s)))
        {
            g.DrawArc(highlight, 25f * s, 16f * s, 205f * s, 82f * s, 195f, 150f);
        }
    }

    private static void DrawCompactMark(Graphics g, float s)
    {
        using (var tile = RoundedRect(42f * s, 48f * s, 120f * s, 140f * s, 28f * s))
        using (var brush = new LinearGradientBrush(
            new PointF(42f * s, 48f * s),
            new PointF(162f * s, 188f * s),
            Color.FromArgb(245, 9, 39, 102),
            Color.FromArgb(250, 11, 21, 72)))
        {
            g.FillPath(brush, tile);
        }

        DrawVsCodeRibbon(g, s, 58f, 78f, 82f, 70f);

        using (var cloud = new SolidBrush(Color.FromArgb(245, 236, 249, 255)))
        {
            g.FillEllipse(cloud, 150f * s, 92f * s, 58f * s, 50f * s);
            g.FillEllipse(cloud, 172f * s, 78f * s, 48f * s, 48f * s);
            g.FillRectangle(cloud, 158f * s, 110f * s, 68f * s, 35f * s);
        }
    }

    private static void DrawGlobe(Graphics g, float s)
    {
        var globeRect = new RectangleF(108f * s, 34f * s, 126f * s, 126f * s);
        using (var globe = new SolidBrush(Color.FromArgb(72, 36, 215, 255)))
        using (var outline = new Pen(Color.FromArgb(135, 70, 229, 255), Math.Max(1f, 1.5f * s)))
        using (var grid = new Pen(Color.FromArgb(110, 136, 238, 255), Math.Max(1f, 1.0f * s)))
        {
            g.FillEllipse(globe, globeRect);
            g.DrawEllipse(outline, globeRect);
            g.DrawArc(grid, 124f * s, 34f * s, 94f * s, 126f * s, 88f, 184f);
            g.DrawArc(grid, 145f * s, 34f * s, 54f * s, 126f * s, 88f, 184f);
            g.DrawArc(grid, 108f * s, 62f * s, 126f * s, 66f * s, 8f, 164f);
            g.DrawArc(grid, 108f * s, 78f * s, 126f * s, 70f * s, 188f, 164f);
        }

        using (var node = new SolidBrush(Color.FromArgb(245, 79, 244, 255)))
        {
            g.FillEllipse(node, 153f * s, 59f * s, 7f * s, 7f * s);
            g.FillEllipse(node, 195f * s, 77f * s, 7f * s, 7f * s);
            g.FillEllipse(node, 171f * s, 103f * s, 7f * s, 7f * s);
        }
    }

    private static void DrawCodeTile(Graphics g, float s)
    {
        using (var shadow = RoundedRect(24f * s, 68f * s, 126f * s, 104f * s, 25f * s))
        using (var shadowBrush = new SolidBrush(Color.FromArgb(70, 5, 15, 48)))
        {
            g.FillPath(shadowBrush, shadow);
        }

        using (var tile = RoundedRect(22f * s, 64f * s, 126f * s, 104f * s, 25f * s))
        using (var brush = new LinearGradientBrush(
            new PointF(22f * s, 64f * s),
            new PointF(148f * s, 168f * s),
            Color.FromArgb(250, 20, 72, 155),
            Color.FromArgb(250, 8, 25, 82)))
        {
            g.FillPath(brush, tile);
            using (var edge = new Pen(Color.FromArgb(220, 91, 226, 255), Math.Max(1f, 2f * s)))
            {
                g.DrawPath(edge, tile);
            }
        }

        DrawVsCodeRibbon(g, s, 38f, 80f, 90f, 72f);
    }

    private static void DrawVsCodeRibbon(Graphics g, float s, float x, float y, float width, float height)
    {
        float stroke = Math.Max(1.6f, 11f * s);
        using (var cyan = new Pen(Color.FromArgb(255, 33, 222, 255), stroke))
        using (var blue = new Pen(Color.FromArgb(255, 22, 139, 255), stroke))
        using (var light = new Pen(Color.FromArgb(255, 89, 239, 255), Math.Max(1.2f, 8f * s)))
        {
            cyan.StartCap = cyan.EndCap = LineCap.Round;
            blue.StartCap = blue.EndCap = LineCap.Round;
            light.StartCap = light.EndCap = LineCap.Round;

            PointF leftTop = new PointF(x * s, (y + height * 0.23f) * s);
            PointF middle = new PointF((x + width * 0.36f) * s, (y + height * 0.50f) * s);
            PointF leftBottom = new PointF(x * s, (y + height * 0.77f) * s);
            PointF rightTop = new PointF((x + width * 0.82f) * s, y * s);
            PointF rightBottom = new PointF((x + width * 0.82f) * s, (y + height) * s);

            g.DrawLine(cyan, leftTop, middle);
            g.DrawLine(cyan, middle, leftBottom);
            g.DrawLine(blue, middle, rightTop);
            g.DrawLine(blue, middle, rightBottom);
            g.DrawLine(light, rightTop, rightBottom);
        }
    }

    private static void DrawTunnel(Graphics g, float s)
    {
        using (var glow = new Pen(Color.FromArgb(90, 73, 229, 255), Math.Max(2f, 13f * s)))
        using (var cyan = new Pen(Color.FromArgb(235, 63, 232, 255), Math.Max(1f, 2.2f * s)))
        using (var violet = new Pen(Color.FromArgb(220, 143, 94, 255), Math.Max(1f, 1.8f * s)))
        {
            glow.StartCap = glow.EndCap = LineCap.Round;
            cyan.StartCap = cyan.EndCap = LineCap.Round;
            violet.StartCap = violet.EndCap = LineCap.Round;

            g.DrawLine(glow, 130f * s, 113f * s, 187f * s, 108f * s);
            for (int i = 0; i < 5; i++)
            {
                float dy = (i - 2) * 3.0f;
                g.DrawLine(i % 2 == 0 ? cyan : violet,
                    129f * s, (113f + dy) * s,
                    190f * s, (108f + dy * 0.4f) * s);
            }
        }

        using (var ringGlow = new Pen(Color.FromArgb(95, 72, 240, 255), Math.Max(2f, 9f * s)))
        using (var ring = new Pen(Color.FromArgb(250, 77, 235, 255), Math.Max(1f, 3f * s)))
        {
            g.DrawEllipse(ringGlow, 177f * s, 90f * s, 34f * s, 38f * s);
            g.DrawEllipse(ring, 177f * s, 90f * s, 34f * s, 38f * s);
        }
    }

    private static void DrawCloud(Graphics g, float s, bool drawText)
    {
        using (var shadow = new SolidBrush(Color.FromArgb(55, 7, 28, 90)))
        {
            g.FillEllipse(shadow, 178f * s, 68f * s, 52f * s, 52f * s);
            g.FillEllipse(shadow, 160f * s, 88f * s, 45f * s, 44f * s);
            g.FillRectangle(shadow, 166f * s, 103f * s, 72f * s, 34f * s);
        }

        using (var cloud = new SolidBrush(Color.FromArgb(250, 239, 250, 255)))
        using (var edge = new Pen(Color.FromArgb(230, 95, 231, 255), Math.Max(1f, 2f * s)))
        {
            g.FillEllipse(cloud, 176f * s, 64f * s, 52f * s, 52f * s);
            g.FillEllipse(cloud, 158f * s, 84f * s, 45f * s, 44f * s);
            g.FillEllipse(cloud, 201f * s, 90f * s, 39f * s, 39f * s);
            g.FillRectangle(cloud, 164f * s, 99f * s, 73f * s, 34f * s);
            g.DrawArc(edge, 176f * s, 64f * s, 52f * s, 52f * s, 190f, 235f);
            g.DrawArc(edge, 158f * s, 84f * s, 45f * s, 44f * s, 125f, 210f);
            g.DrawArc(edge, 201f * s, 90f * s, 39f * s, 39f * s, 230f, 180f);
            g.DrawLine(edge, 168f * s, 132f * s, 225f * s, 132f * s);
        }

        if (drawText)
        {
            using (var font = CreateFont(Math.Max(7f, 12f * s), FontStyle.Bold))
            using (var text = new SolidBrush(Color.FromArgb(255, 7, 38, 102)))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("ngrok", font, text, new RectangleF(166f * s, 99f * s, 67f * s, 31f * s), format);
            }
        }
    }

    private static void DrawDevSpaceBadge(Graphics g, float s, bool drawText)
    {
        using (var badge = RoundedRect(27f * s, 181f * s, 202f * s, 45f * s, 18f * s))
        using (var brush = new LinearGradientBrush(
            new PointF(27f * s, 181f * s),
            new PointF(229f * s, 226f * s),
            Color.FromArgb(245, 6, 44, 110),
            Color.FromArgb(245, 75, 33, 174)))
        {
            g.FillPath(brush, badge);
            using (var edge = new Pen(Color.FromArgb(205, 109, 232, 255), Math.Max(1f, 1.6f * s)))
            {
                g.DrawPath(edge, badge);
            }
        }

        using (var terminal = RoundedRect(40f * s, 190f * s, 34f * s, 28f * s, 8f * s))
        using (var terminalBrush = new SolidBrush(Color.FromArgb(230, 8, 64, 145)))
        using (var terminalPen = new Pen(Color.FromArgb(230, 91, 235, 255), Math.Max(1f, 1.5f * s)))
        {
            g.FillPath(terminalBrush, terminal);
            g.DrawPath(terminalPen, terminal);
        }

        using (var mark = new Pen(Color.White, Math.Max(1f, 2.4f * s)))
        {
            mark.StartCap = mark.EndCap = LineCap.Round;
            g.DrawLines(mark, new[]
            {
                new PointF(48f * s, 198f * s),
                new PointF(54f * s, 204f * s),
                new PointF(48f * s, 210f * s)
            });
            g.DrawLine(mark, 58f * s, 211f * s, 66f * s, 211f * s);
        }

        if (drawText)
        {
            using (var font = CreateFont(Math.Max(8f, 20f * s), FontStyle.Bold))
            using (var text = new SolidBrush(Color.White))
            using (var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("DevSpace", font, text, new RectangleF(82f * s, 183f * s, 138f * s, 40f * s), format);
            }
        }
    }

    private static Font CreateFont(float size, FontStyle style)
    {
        try
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Pixel);
        }
        catch
        {
            return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Pixel);
        }
    }

    private static GraphicsPath RoundedRect(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(Math.Min(radius * 2f, width), height);
        if (diameter <= 0f)
        {
            path.AddRectangle(new RectangleF(x, y, width, height));
            return path;
        }

        var arc = new RectangleF(x, y, diameter, diameter);
        path.AddArc(arc, 180f, 90f);
        arc.X = x + width - diameter;
        path.AddArc(arc, 270f, 90f);
        arc.Y = y + height - diameter;
        path.AddArc(arc, 0f, 90f);
        arc.X = x;
        path.AddArc(arc, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    private static void WriteIcon(string path, IList<byte[]> pngImages)
    {
        using (var stream = File.Create(path))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)pngImages.Count);

            int offset = 6 + (16 * pngImages.Count);
            for (int i = 0; i < pngImages.Count; i++)
            {
                int size = Sizes[i];
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(pngImages[i].Length);
                writer.Write(offset);
                offset += pngImages[i].Length;
            }

            foreach (byte[] image in pngImages)
            {
                writer.Write(image);
            }
        }
    }
}
