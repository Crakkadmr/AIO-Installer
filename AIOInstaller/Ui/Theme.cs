using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AIOInstaller.Ui
{
    /// <summary>
    /// Uygulamanın tek tasarım kaynağı: renkler, yazı tipleri, ikon glifleri ve çizim yardımcıları.
    /// Bileşenler ham renk yerine buradaki adlandırılmış tokenları kullanır.
    /// </summary>
    public static class Theme
    {
        // ── Yüzeyler (derin gece mavisi, katmanlar yukarı çıktıkça açılır) ──
        public static readonly Color Bg            = Color.FromArgb(9, 12, 22);
        public static readonly Color Sidebar       = Color.FromArgb(13, 17, 31);
        public static readonly Color Surface       = Color.FromArgb(18, 24, 42);
        public static readonly Color SurfaceHover  = Color.FromArgb(25, 32, 56);
        public static readonly Color SurfaceRaised = Color.FromArgb(30, 38, 66);
        public static readonly Color Border        = Color.FromArgb(37, 46, 76);
        public static readonly Color BorderStrong  = Color.FromArgb(56, 68, 105);

        // ── Metin ──
        public static readonly Color Text      = Color.FromArgb(241, 245, 249);
        public static readonly Color TextMuted = Color.FromArgb(152, 166, 190);
        public static readonly Color TextDim   = Color.FromArgb(104, 119, 146);

        // ── Vurgu ve durum renkleri ──
        public static readonly Color Accent      = Color.FromArgb(76, 141, 255);
        public static readonly Color AccentHover = Color.FromArgb(104, 160, 255);
        public static readonly Color AccentSoft  = Color.FromArgb(28, 45, 88);
        public static readonly Color HeroFrom    = Color.FromArgb(34, 197, 94);
        public static readonly Color HeroTo      = Color.FromArgb(13, 148, 136);
        public static readonly Color Success     = Color.FromArgb(52, 211, 153);
        public static readonly Color Warning     = Color.FromArgb(251, 191, 36);
        public static readonly Color Danger      = Color.FromArgb(248, 99, 99);
        public static readonly Color DangerSoft  = Color.FromArgb(66, 26, 36);
        public static readonly Color Info        = Color.FromArgb(125, 170, 255);

        public const int Radius = 10;

        // ── Yazı tipleri ──
        private static HashSet<string> installed;

        private static bool HasFont(string name)
        {
            if (installed == null)
            {
                try
                {
                    using (var col = new InstalledFontCollection())
                        installed = new HashSet<string>(col.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
                }
                catch
                {
                    installed = new HashSet<string>();
                }
            }
            return installed.Contains(name);
        }

        private static readonly Dictionary<string, Font> fontCache = new Dictionary<string, Font>();

        private static Font Make(string[] families, float size, FontStyle style)
        {
            string key = string.Join("|", families) + size + style;
            Font f;
            if (fontCache.TryGetValue(key, out f)) return f;

            string family = families.FirstOrDefault(HasFont) ?? SystemFonts.MessageBoxFont.FontFamily.Name;
            f = new Font(family, size, style, GraphicsUnit.Point);
            fontCache[key] = f;
            return f;
        }

        /// <summary>Gövde metni: Segoe UI Variable.</summary>
        public static Font Body(float size = 9.75f, FontStyle style = FontStyle.Regular)
        {
            return style.HasFlag(FontStyle.Bold)
                ? Make(new[] { "Segoe UI Variable Text Semibold", "Segoe UI Semibold", "Segoe UI" }, size, style & ~FontStyle.Bold)
                : Make(new[] { "Segoe UI Variable Text", "Segoe UI" }, size, style);
        }

        /// <summary>Başlıklar ve sayılar: Bahnschrift (teknik, sıkı, karakterli).</summary>
        public static Font Heading(float size, bool strong = true)
        {
            return strong
                ? Make(new[] { "Bahnschrift SemiBold", "Segoe UI Semibold", "Segoe UI" }, size, FontStyle.Regular)
                : Make(new[] { "Bahnschrift", "Segoe UI" }, size, FontStyle.Regular);
        }

        /// <summary>Windows'un ikon fontu (Win11: Segoe Fluent Icons, Win10: Segoe MDL2 Assets).</summary>
        public static Font Icon(float size)
        {
            return Make(new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" }, size, FontStyle.Regular);
        }

        // ── İkon glifleri ──
        public static class Glyph
        {
            public const string Programs  = "";
            public const string Download  = "";
            public const string Tools     = "";
            public const string Settings  = "";
            public const string Rocket    = "";
            public const string Add       = "";
            public const string Delete    = "";
            public const string Folder    = "";
            public const string Refresh   = "";
            public const string Search    = "";
            public const string Play      = "";
            public const string Stop      = "";
            public const string Check     = "";
            public const string Close     = "";
            public const string Wifi      = "";
            public const string Shield    = "";
            public const string Update    = "";
            public const string Broom     = "";
            public const string Key       = "";
            public const string Terminal  = "";
            public const string Up        = "";
            public const string Down      = "";
            public const string Eye       = "";
            public const string External  = "";
            public const string Info      = "";
            public const string Warning   = "";
            public const string Error     = "";
            public const string Question  = "";
            public const string School    = "";
            public const string Edit      = "";
            public const string Save      = "";
            public const string Lock      = "";
            public const string Network   = "";
            public const string Uac       = "";
            public const string Office    = "";
            public const string Share     = "";
            public const string SelectAll = "";
        }

        // ── Yardımcılar ──
        public static Color Mix(Color a, Color b, float t)
        {
            if (t <= 0) return a;
            if (t >= 1) return b;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static Color WithAlpha(Color c, int alpha)
        {
            return Color.FromArgb(Math.Max(0, Math.Min(255, alpha)), c);
        }

        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0.5f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void HighQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        /// <summary>Metni GDI ile çizer (WinForms metniyle aynı görünüm, keskin ClearType).</summary>
        public static void DrawText(Graphics g, string text, Font font, Rectangle bounds, Color color,
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix)
        {
            TextRenderer.DrawText(g, text, font, bounds, color, flags);
        }

        // ── Koyu başlık çubuğu (Windows 10 20H1+ / Windows 11) ──
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

        /// <summary>Kaydırma çubuklarını Windows'un koyu temasıyla çizer (Win10 1809+).</summary>
        public static void DarkScrollbars(Control control)
        {
            Action apply = () =>
            {
                try
                {
                    SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
                    foreach (Control child in control.Controls)
                        if (child is ScrollBar) SetWindowTheme(child.Handle, "DarkMode_Explorer", null);
                }
                catch { }
            };
            if (control.IsHandleCreated) apply();
            control.HandleCreated += (s, e) => apply();
        }

        public static void ApplyDarkTitleBar(Form form)
        {
            form.HandleCreated += (s, e) =>
            {
                try
                {
                    int on = 1;
                    if (DwmSetWindowAttribute(form.Handle, 20, ref on, sizeof(int)) != 0)
                        DwmSetWindowAttribute(form.Handle, 19, ref on, sizeof(int));

                    // Windows 11: başlık çubuğunu uygulamanın zemin rengine boya
                    int caption = ColorTranslator.ToWin32(Sidebar);
                    DwmSetWindowAttribute(form.Handle, 35, ref caption, sizeof(int));
                }
                catch
                {
                    // eski Windows: varsayılan başlık çubuğu
                }
            };
        }
    }
}
