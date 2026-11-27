using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GitHubReleaseMonitor
{
    internal static class Theme
    {
        public static readonly Color Background = ColorTranslator.FromHtml("#F6F8FA");
        public static readonly Color Card = Color.White;
        public static readonly Color Border = ColorTranslator.FromHtml("#D0D7DE");
        public static readonly Color BorderStrong = ColorTranslator.FromHtml("#AFB8C1");
        public static readonly Color Text = ColorTranslator.FromHtml("#1F2328");
        public static readonly Color Muted = ColorTranslator.FromHtml("#656D76");
        public static readonly Color Faint = ColorTranslator.FromHtml("#8C959F");
        public static readonly Color Accent = ColorTranslator.FromHtml("#1F6FEB");
        public static readonly Color AccentHover = ColorTranslator.FromHtml("#0969DA");
        public static readonly Color AccentSoft = ColorTranslator.FromHtml("#DDF4FF");
        public static readonly Color Green = ColorTranslator.FromHtml("#1A7F37");
        public static readonly Color GreenSoft = ColorTranslator.FromHtml("#DAFBE1");
        public static readonly Color Orange = ColorTranslator.FromHtml("#BC4C00");
        public static readonly Color OrangeSoft = ColorTranslator.FromHtml("#FFF1E5");
        public static readonly Color Red = ColorTranslator.FromHtml("#CF222E");
        public static readonly Color RedSoft = ColorTranslator.FromHtml("#FFEBE9");
        public static readonly Color Hover = ColorTranslator.FromHtml("#F3F4F6");
        public static readonly Color SoftGray = ColorTranslator.FromHtml("#EFF2F5");

        public static Font Ui(float size)
        {
            return new Font("Microsoft YaHei UI", size, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font Ui(float size, FontStyle style)
        {
            return new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
        }

        public static Font Mono(float size)
        {
            return new Font("Consolas", size, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Color Mix(Color a, Color b, double amount)
        {
            if (amount < 0) amount = 0;
            if (amount > 1) amount = 1;
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * amount),
                (int)Math.Round(a.G + (b.G - a.G) * amount),
                (int)Math.Round(a.B + (b.B - a.B) * amount));
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            int diameter = radius * 2;
            if (diameter > bounds.Width) diameter = bounds.Width;
            if (diameter > bounds.Height) diameter = bounds.Height;
            if (diameter <= 1)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            Rectangle r = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            path.AddArc(r.X, r.Y, diameter, diameter, 180, 90);
            path.AddArc(r.Right - diameter, r.Y, diameter, diameter, 270, 90);
            path.AddArc(r.Right - diameter, r.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(r.X, r.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawCenteredText(Graphics g, string text, Font font, Color color, Rectangle bounds)
        {
            TextRenderer.DrawText(g, text, font, bounds, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
    }
}
