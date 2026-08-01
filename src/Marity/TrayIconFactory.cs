using System.Drawing;
using System.Drawing.Drawing2D;

namespace Marity;

/// <summary>
/// Draws the tray icon at runtime instead of loading a resource, so state changes
/// (enabled/paused) don't require shipping two separate .ico assets.
///
/// This mirrors assets/marity-logo.svg (the design source of truth) - keep the
/// gradient stops and normalized kite points below in sync with it if the design
/// changes.
/// </summary>
internal static class TrayIconFactory
{
    private static readonly PointF[] NormalizedKite =
    {
        new(0.24f, 0.24f),
        new(0.43f, 0.70f),
        new(0.50f, 0.66f),
        new(0.70f, 0.43f),
    };

    public static Icon Create(bool enabled)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var badgePath = RoundedRect(0.5f, 0.5f, size - 1, size - 1, size * 0.22f);
            Color c1 = enabled ? ColorTranslator.FromHtml("#4F46E5") : ColorTranslator.FromHtml("#6B7280");
            Color c2 = enabled ? ColorTranslator.FromHtml("#7C3AED") : ColorTranslator.FromHtml("#9CA3AF");
            using (var bgBrush = new LinearGradientBrush(new PointF(0, 0), new PointF(size, size), c1, c2))
            {
                g.FillPath(bgBrush, badgePath);
            }

            PointF[] kite = Array.ConvertAll(NormalizedKite, p => new PointF(p.X * size, p.Y * size));
            using (var outline = new Pen(Color.FromArgb(90, 30, 27, 75), size * 0.05f) { LineJoin = LineJoin.Round })
            {
                g.DrawPolygon(outline, kite);
            }
            using (var fill = new SolidBrush(Color.White))
            {
                g.FillPolygon(fill, kite);
            }
        }

        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(hIcon);
        }
    }

    private static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
    {
        var path = new GraphicsPath();
        path.AddArc(x, y, r, r, 180, 90);
        path.AddArc(x + w - r, y, r, r, 270, 90);
        path.AddArc(x + w - r, y + h - r, r, r, 0, 90);
        path.AddArc(x, y + h - r, r, r, 90, 90);
        path.CloseFigure();
        return path;
    }
}
