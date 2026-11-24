using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AIOInstaller.Ui
{
    /// <summary>
    /// Tüm hover/geçiş animasyonlarını tek bir 60 fps zamanlayıcıyla yürütür.
    /// Kayıtlı adım fonksiyonu false döndürünce listeden çıkar.
    /// </summary>
    public static class Animator
    {
        private static readonly List<Func<bool>> steps = new List<Func<bool>>();
        private static Timer timer;

        public static void Run(Func<bool> step)
        {
            if (!steps.Contains(step)) steps.Add(step);
            if (timer == null)
            {
                timer = new Timer { Interval = 15 };
                timer.Tick += (s, e) =>
                {
                    for (int i = steps.Count - 1; i >= 0; i--)
                    {
                        bool keep;
                        try { keep = steps[i](); } catch { keep = false; }
                        if (!keep) steps.RemoveAt(i);
                    }
                    if (steps.Count == 0) timer.Stop();
                };
            }
            if (!timer.Enabled) timer.Start();
        }

        /// <summary>value'yu target'a doğru yumuşakça yaklaştırır; vardıysa true.</summary>
        public static bool Approach(ref float value, float target, float speed = 0.22f)
        {
            float diff = target - value;
            if (Math.Abs(diff) < 0.01f)
            {
                value = target;
                return true;
            }
            value += diff * speed;
            return false;
        }
    }

    public enum ButtonKind { Primary, Secondary, Ghost, Danger, Success, Hero }

    /// <summary>Yuvarlak köşeli, animasyonlu hover'lı, ikon + metin taşıyan buton.</summary>
    public class ModernButton : Control, IButtonControl
    {
        private float hover;
        private bool pressed;
        private bool mouseInside;
        private readonly Func<bool> animStep;

        public ButtonKind Kind { get; set; }
        public string Glyph { get; set; }
        public int CornerRadius { get; set; }
        public string Subtitle { get; set; }
        public DialogResult DialogResult { get; set; }

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = Theme.Body(9.75f, FontStyle.Bold);
            Size = new Size(140, 38);
            CornerRadius = 8;
            Kind = ButtonKind.Secondary;
            TabStop = true;

            animStep = () =>
            {
                bool done = Animator.Approach(ref hover, mouseInside && Enabled ? 1f : 0f, 0.25f);
                if (!IsDisposed) Invalidate();
                return !done && !IsDisposed;
            };
        }

        public void NotifyDefault(bool value) { }

        public void PerformClick()
        {
            if (Enabled) OnClick(EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            mouseInside = true;
            Animator.Run(animStep);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            mouseInside = false;
            pressed = false;
            Animator.Run(animStep);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressed = false;
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            if (!Enabled) mouseInside = false;
            Animator.Run(animStep);
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) PerformClick();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (DialogResult != DialogResult.None)
            {
                var form = FindForm();
                if (form != null) form.DialogResult = DialogResult;
            }
        }

        private void Palette(out Color bg, out Color bgHover, out Color fg, out Color border)
        {
            border = Color.Empty;
            switch (Kind)
            {
                case ButtonKind.Primary:
                    bg = Theme.Accent; bgHover = Theme.AccentHover; fg = Color.White; break;
                case ButtonKind.Danger:
                    bg = Theme.DangerSoft; bgHover = Color.FromArgb(96, 32, 44); fg = Theme.Danger; break;
                case ButtonKind.Success:
                    bg = Color.FromArgb(20, 64, 52); bgHover = Color.FromArgb(26, 86, 68); fg = Theme.Success; break;
                case ButtonKind.Ghost:
                    bg = Theme.WithAlpha(Theme.SurfaceHover, 0); bgHover = Theme.SurfaceHover; fg = Theme.TextMuted; break;
                case ButtonKind.Hero:
                    bg = Theme.HeroFrom; bgHover = Color.FromArgb(74, 222, 128); fg = Color.FromArgb(5, 26, 18); break;
                default:
                    bg = Theme.SurfaceRaised; bgHover = Color.FromArgb(40, 50, 84); fg = Theme.Text; border = Theme.Border; break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.HighQuality(g);

            Color bg, bgHover, fg, border;
            Palette(out bg, out bgHover, out fg, out border);

            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            if (pressed) rect.Inflate(-1, -1);

            using (var path = Theme.RoundRect(rect, CornerRadius))
            {
                if (Kind == ButtonKind.Hero)
                {
                    using (var lg = new LinearGradientBrush(rect, Theme.Mix(Theme.HeroFrom, Color.White, hover * 0.15f),
                        Theme.Mix(Theme.HeroTo, Color.White, hover * 0.15f), LinearGradientMode.ForwardDiagonal))
                        g.FillPath(lg, path);
                }
                else
                {
                    using (var b = new SolidBrush(Theme.Mix(bg, bgHover, hover)))
                        g.FillPath(b, path);
                }

                if (border != Color.Empty)
                {
                    using (var p = new Pen(Theme.Mix(border, Theme.BorderStrong, hover)))
                        g.DrawPath(p, path);
                }

                if (Focused && ShowFocusCues)
                {
                    using (var p = new Pen(Theme.Accent, 2f))
                    using (var fp = Theme.RoundRect(new RectangleF(1.5f, 1.5f, Width - 3.5f, Height - 3.5f), CornerRadius))
                        g.DrawPath(p, fp);
                }
            }

            if (!Enabled) fg = Theme.WithAlpha(fg, 110);
            if (Kind == ButtonKind.Ghost) fg = Theme.Mix(fg, Theme.Text, hover);

            bool hasText = !string.IsNullOrEmpty(Text);
            bool hasGlyph = !string.IsNullOrEmpty(Glyph);
            bool hasSub = !string.IsNullOrEmpty(Subtitle);

            float iconSize = Kind == ButtonKind.Hero ? 15f : 11f;
            Font iconFont = Theme.Icon(iconSize);

            if (hasSub)
            {
                // Büyük, iki satırlı buton (Tam Kurulum)
                int pad = 16;
                int iconW = hasGlyph ? 30 : 0;
                if (hasGlyph)
                    Theme.DrawText(g, Glyph, iconFont, new Rectangle(pad, 0, iconW, Height), fg,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                int tx = pad + iconW + (hasGlyph ? 8 : 0);
                Theme.DrawText(g, Text, Font, new Rectangle(tx, Height / 2 - 20, Width - tx - 8, 22), fg,
                    TextFormatFlags.Bottom | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                Theme.DrawText(g, Subtitle, Theme.Body(8.25f), new Rectangle(tx, Height / 2 + 1, Width - tx - 8, 18),
                    Theme.WithAlpha(fg, 190),
                    TextFormatFlags.Top | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }

            Size textSize = hasText ? TextRenderer.MeasureText(g, Text, Font, Size.Empty, TextFormatFlags.NoPadding) : Size.Empty;
            int glyphW = hasGlyph ? TextRenderer.MeasureText(g, Glyph, iconFont, Size.Empty, TextFormatFlags.NoPadding).Width : 0;
            int gap = hasGlyph && hasText ? 8 : 0;
            int total = glyphW + gap + textSize.Width;
            int x = Math.Max(6, (Width - total) / 2);

            if (hasGlyph)
            {
                Theme.DrawText(g, Glyph, iconFont, new Rectangle(x, 0, glyphW + 2, Height), fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                x += glyphW + gap;
            }
            if (hasText)
            {
                Theme.DrawText(g, Text, Font, new Rectangle(x, 0, Width - x - 4, Height), fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            }
        }

        /// <summary>Metnin sığacağı genişliği hesaplayıp butonu ona göre boyutlar.</summary>
        public ModernButton AutoWidth(int minWidth = 0)
        {
            int glyphW = string.IsNullOrEmpty(Glyph) ? 0 : 22;
            int textW = string.IsNullOrEmpty(Text) ? 0 : TextRenderer.MeasureText(Text, Font).Width;
            Width = Math.Max(minWidth, glyphW + textW + 30);
            return this;
        }
    }

    /// <summary>Sol menü öğesi: ikon + etiket, seçili göstergesi, kompakt (yalnız ikon) mod.</summary>
    public class NavItem : Control
    {
        private float hover;
        private bool mouseInside;
        private bool selected;
        private bool compact;
        private readonly Func<bool> animStep;

        public string Glyph { get; set; }
        public bool ShowExternal { get; set; }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        public bool Compact
        {
            get { return compact; }
            set { compact = value; Invalidate(); }
        }

        public NavItem()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand;
            Height = 44;
            Font = Theme.Body(10f);
            BackColor = Theme.Sidebar;
            TabStop = true;
            animStep = () =>
            {
                bool done = Animator.Approach(ref hover, mouseInside ? 1f : 0f, 0.25f);
                if (!IsDisposed) Invalidate();
                return !done && !IsDisposed;
            };
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); mouseInside = true; Animator.Run(animStep); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); mouseInside = false; Animator.Run(animStep); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) OnClick(EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.HighQuality(g);
            g.Clear(BackColor);

            var rect = new RectangleF(8, 2, Width - 16, Height - 4);
            Color fill = selected ? Theme.AccentSoft : Theme.WithAlpha(Theme.SurfaceHover, (int)(255 * hover));
            using (var path = Theme.RoundRect(rect, 8))
            using (var b = new SolidBrush(fill))
                g.FillPath(b, path);

            if (Focused && ShowFocusCues)
            {
                using (var p = new Pen(Theme.Accent, 1.5f))
                using (var path = Theme.RoundRect(rect, 8))
                    g.DrawPath(p, path);
            }

            if (selected)
            {
                using (var b = new SolidBrush(Theme.Accent))
                using (var bar = Theme.RoundRect(new RectangleF(8, Height / 2f - 9, 3, 18), 1.5f))
                    g.FillPath(b, bar);
            }

            Color fg = selected ? Theme.Text : Theme.Mix(Theme.TextMuted, Theme.Text, hover);
            Color iconColor = selected ? Theme.Accent : fg;

            if (compact)
            {
                Theme.DrawText(g, Glyph, Theme.Icon(13f), ClientRectangle, iconColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                return;
            }

            Theme.DrawText(g, Glyph, Theme.Icon(12.5f), new Rectangle(24, 0, 24, Height), iconColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            Theme.DrawText(g, Text, selected ? Theme.Body(10f, FontStyle.Bold) : Font,
                new Rectangle(56, 0, Width - 56 - 30, Height), fg);

            if (ShowExternal)
            {
                Theme.DrawText(g, Theme.Glyph.External, Theme.Icon(8.5f), new Rectangle(Width - 36, 0, 18, Height),
                    Theme.TextDim, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            }
        }
    }

    /// <summary>Yuvarlak köşeli, kenarlıklı içerik kartı.</summary>
    public class Card : Panel
    {
        private Color fillColor;

        /// <summary>Kartın iç rengi; çocuk kontrollerin zemini olsun diye BackColor da buna eşitlenir.</summary>
        public Color FillColor
        {
            get { return fillColor; }
            set { fillColor = value; BackColor = value; Invalidate(); }
        }

        public Color BorderColor { get; set; }
        public int CornerRadius { get; set; }

        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            FillColor = Theme.Surface;
            BorderColor = Theme.Border;
            CornerRadius = 12;
            Padding = new Padding(20);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            Theme.HighQuality(g);
            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Theme.RoundRect(rect, CornerRadius))
            {
                using (var b = new SolidBrush(FillColor)) g.FillPath(b, path);
                using (var p = new Pen(BorderColor)) g.DrawPath(p, path);
            }
        }
    }

    /// <summary>Animasyonlu açma/kapama anahtarı.</summary>
    public class ToggleSwitch : Control
    {
        private bool isChecked;
        private float knob;
        private readonly Func<bool> animStep;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                Animator.Run(animStep);
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            Size = new Size(44, 24);
            Cursor = Cursors.Hand;
            TabStop = true;
            animStep = () =>
            {
                bool done = Animator.Approach(ref knob, isChecked ? 1f : 0f, 0.3f);
                if (!IsDisposed) Invalidate();
                return !done && !IsDisposed;
            };
        }

        /// <summary>Animasyonsuz ilk değer ataması.</summary>
        public void SetInitial(bool value)
        {
            isChecked = value;
            knob = value ? 1f : 0f;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (Enabled) Checked = !Checked;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)) Checked = !Checked;
        }

        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            Theme.HighQuality(g);

            var track = new RectangleF(1, 1, Width - 3, Height - 3);
            Color on = Enabled ? Theme.Accent : Theme.BorderStrong;
            Color off = Theme.SurfaceRaised;
            using (var path = Theme.RoundRect(track, track.Height / 2))
            {
                using (var b = new SolidBrush(Theme.Mix(off, on, knob))) g.FillPath(b, path);
                using (var p = new Pen(Theme.Mix(Theme.BorderStrong, on, knob))) g.DrawPath(p, path);
                if (Focused && ShowFocusCues)
                    using (var fp = new Pen(Theme.AccentHover, 1.5f)) g.DrawPath(fp, path);
            }

            float d = track.Height - 6;
            float x = track.X + 3 + (track.Width - d - 6) * knob;
            using (var b = new SolidBrush(Enabled ? Color.White : Theme.TextDim))
                g.FillEllipse(b, x, track.Y + 3, d, d);
        }
    }

    /// <summary>İnce, yuvarlak uçlu ilerleme çubuğu; belirsiz modda kayan parlak şerit.</summary>
    public class ModernProgress : Control
    {
        private float shown;
        private float marqueePos;
        private bool marquee;
        private int value;
        private int maximum = 100;
        private readonly Func<bool> animStep;

        public ModernProgress()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = 6;
            animStep = () =>
            {
                if (IsDisposed) return false;
                bool done = true;
                if (marquee && Visible)
                {
                    marqueePos += 0.012f;
                    if (marqueePos > 1.3f) marqueePos = -0.3f;
                    done = false;
                }
                float target = maximum > 0 ? (float)value / maximum : 0f;
                if (!Animator.Approach(ref shown, target, 0.15f)) done = false;
                Invalidate();
                return !done;
            };
        }

        public bool Marquee
        {
            get { return marquee; }
            set { marquee = value; Animator.Run(animStep); }
        }

        /// <summary>ProgressBar ile uyumluluk için (Marquee / Continuous).</summary>
        public ProgressBarStyle Style
        {
            get { return marquee ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous; }
            set { Marquee = value == ProgressBarStyle.Marquee; }
        }

        /// <summary>ProgressBar ile uyumluluk için; her zaman 0 kabul edilir.</summary>
        public int Minimum { get; set; }

        public int Maximum
        {
            get { return maximum; }
            set { maximum = Math.Max(1, value); Animator.Run(animStep); }
        }

        public int Value
        {
            get { return value; }
            set { this.value = Math.Max(0, Math.Min(maximum, value)); Animator.Run(animStep); }
        }

        /// <summary>Animasyonsuz sıfırlama.</summary>
        public void Reset(int max)
        {
            maximum = Math.Max(1, max);
            value = 0;
            shown = 0;
            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) Animator.Run(animStep);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            Theme.HighQuality(g);
            var track = new RectangleF(0, 0, Width - 1, Height - 1);
            float r = track.Height / 2;
            using (var path = Theme.RoundRect(track, r))
            using (var b = new SolidBrush(Theme.SurfaceRaised))
                g.FillPath(b, path);

            RectangleF fill;
            if (marquee)
            {
                float w = track.Width * 0.3f;
                float x = track.Width * marqueePos - w / 2;
                fill = RectangleF.Intersect(new RectangleF(x, 0, w, track.Height), track);
            }
            else
            {
                fill = new RectangleF(0, 0, Math.Max(track.Height, track.Width * shown), track.Height);
                if (shown <= 0.001f) return;
            }
            if (fill.Width <= 0) return;

            using (var path = Theme.RoundRect(fill, r))
            using (var lg = new LinearGradientBrush(new RectangleF(fill.X - 1, 0, fill.Width + 2, track.Height),
                Theme.Accent, Theme.HeroFrom, LinearGradientMode.Horizontal))
                g.FillPath(lg, path);
        }
    }

    /// <summary>Koyu, yuvarlak kenarlıklı metin kutusu (ikon, ipucu metni, şifre göster).</summary>
    public class DarkTextBox : Panel
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);

        private readonly TextBox box;
        private readonly Label icon;
        private ModernButton revealButton;
        private string placeholder;

        public TextBox Inner { get { return box; } }

        public DarkTextBox(string glyph = null)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = 38;
            BackColor = Theme.Bg;
            Padding = new Padding(glyph == null ? 12 : 36, 0, 10, 0);

            box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Bg,
                ForeColor = Theme.Text,
                Font = Theme.Body(10f)
            };
            box.GotFocus += (s, e) => Invalidate();
            box.LostFocus += (s, e) => Invalidate();
            box.HandleCreated += (s, e) => ApplyPlaceholder();
            Controls.Add(box);

            if (glyph != null)
            {
                icon = new Label
                {
                    Text = glyph,
                    Font = Theme.Icon(10.5f),
                    ForeColor = Theme.TextDim,
                    BackColor = Theme.Bg,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(26, 26),
                    Location = new Point(8, 6)
                };
                Controls.Add(icon);
            }

            Click += (s, e) => box.Focus();
        }

        public string Placeholder
        {
            get { return placeholder; }
            set { placeholder = value; ApplyPlaceholder(); }
        }

        private void ApplyPlaceholder()
        {
            if (box.IsHandleCreated && placeholder != null)
                SendMessage(box.Handle, 0x1501 /* EM_SETCUEBANNER */, (IntPtr)1, placeholder);
        }

        public override string Text
        {
            get { return box.Text; }
            set { box.Text = value; }
        }

        public new event EventHandler TextChanged
        {
            add { box.TextChanged += value; }
            remove { box.TextChanged -= value; }
        }

        public bool Password
        {
            get { return box.UseSystemPasswordChar; }
            set { box.UseSystemPasswordChar = value; }
        }

        /// <summary>Sağ tarafa "şifreyi göster" düğmesi ekler.</summary>
        public void EnableReveal()
        {
            if (revealButton != null) return;
            Password = true;
            revealButton = new ModernButton
            {
                Kind = ButtonKind.Ghost,
                Glyph = Theme.Glyph.Eye,
                Size = new Size(30, 28),
                CornerRadius = 6,
                TabStop = false
            };
            revealButton.Click += (s, e) =>
            {
                Password = !Password;
                revealButton.Kind = Password ? ButtonKind.Ghost : ButtonKind.Primary;
                revealButton.Invalidate();
            };
            Controls.Add(revealButton);
            Padding = new Padding(Padding.Left, 0, 40, 0);
            PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (box == null) return; // kurucu henüz tamamlanmadı
            int h = box.PreferredHeight;
            box.SetBounds(Padding.Left, (Height - h) / 2, Math.Max(10, Width - Padding.Left - Padding.Right), h);
            if (revealButton != null) revealButton.Location = new Point(Width - revealButton.Width - 5, (Height - revealButton.Height) / 2);
            if (icon != null) icon.Location = new Point(8, (Height - icon.Height) / 2);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            box.ForeColor = Enabled ? Theme.Text : Theme.TextDim;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            Theme.HighQuality(g);
            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Theme.RoundRect(rect, 8))
            {
                using (var b = new SolidBrush(Theme.Bg)) g.FillPath(b, path);
                Color border = box.Focused ? Theme.Accent : Theme.Border;
                using (var p = new Pen(border, box.Focused ? 1.5f : 1f)) g.DrawPath(p, path);
            }
        }
    }

    /// <summary>Kartlar ve sayfalar için başlık + açıklama bloğu.</summary>
    public static class UiFactory
    {
        public static Label Label(string text, Font font, Color color, bool autoSize = true)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = autoSize,
                UseMnemonic = false
            };
        }

        public static Label IconLabel(string glyph, float size, Color color, Color back, int box)
        {
            return new Label
            {
                Text = glyph,
                Font = Theme.Icon(size),
                ForeColor = color,
                BackColor = back,
                AutoSize = false,
                Size = new Size(box, box),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }
    }

    /// <summary>Yuvarlak zeminli ikon rozeti (kart başlıkları için).</summary>
    public class IconBadge : Control
    {
        public string Glyph { get; set; }
        public Color Tint { get; set; }

        public IconBadge(string glyph, Color tint)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Glyph = glyph;
            Tint = tint;
            Size = new Size(40, 40);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            Theme.HighQuality(g);
            using (var path = Theme.RoundRect(new RectangleF(0, 0, Width - 1, Height - 1), 10))
            using (var b = new SolidBrush(Theme.WithAlpha(Tint, 38)))
                g.FillPath(b, path);
            Theme.DrawText(g, Glyph, Theme.Icon(Height * 0.33f), ClientRectangle, Tint,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
