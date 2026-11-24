using System;
using System.Drawing;
using System.Windows.Forms;

namespace AIOInstaller.Ui
{
    /// <summary>
    /// MessageBox.Show ile aynı imzaya sahip, uygulamanın koyu temasına uyan mesaj penceresi.
    /// </summary>
    public static class Dialogs
    {
        public static DialogResult Show(string text)
        {
            return Show(text, "AIO Yükleyici", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption)
        {
            return Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return Show(text, caption, buttons, MessageBoxIcon.None);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return Show(null, text, caption, buttons, icon);
        }

        public static DialogResult Show(IWin32Window owner, string text)
        {
            return Show(owner, text, "AIO Yükleyici", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption)
        {
            return Show(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons)
        {
            return Show(owner, text, caption, buttons, MessageBoxIcon.None);
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (var dlg = new MessageDialog(text, caption, buttons, icon))
            {
                if (owner == null) owner = Form.ActiveForm;
                return owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
            }
        }

        private class MessageDialog : Form
        {
            public MessageDialog(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
            {
                Text = caption;
                BackColor = Theme.Surface;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
                AutoScaleMode = AutoScaleMode.None;
                KeyPreview = true;
                Theme.ApplyDarkTitleBar(this);

                string glyph;
                Color tint;
                switch (icon)
                {
                    case MessageBoxIcon.Error: glyph = Theme.Glyph.Error; tint = Theme.Danger; break;
                    case MessageBoxIcon.Warning: glyph = Theme.Glyph.Warning; tint = Theme.Warning; break;
                    case MessageBoxIcon.Question: glyph = Theme.Glyph.Question; tint = Theme.Accent; break;
                    case MessageBoxIcon.Information: glyph = Theme.Glyph.Info; tint = Theme.Success; break;
                    default: glyph = null; tint = Theme.Accent; break;
                }

                const int width = 500;
                int left = 24;
                if (glyph != null)
                {
                    var badge = new IconBadge(glyph, tint) { Location = new Point(24, 26), Size = new Size(44, 44) };
                    Controls.Add(badge);
                    left = 84;
                }

                var title = UiFactory.Label(caption, Theme.Heading(13f), Theme.Text);
                title.Location = new Point(left, 24);
                Controls.Add(title);

                var body = new Label
                {
                    Text = text,
                    Font = Theme.Body(10f),
                    ForeColor = Theme.TextMuted,
                    BackColor = Color.Transparent,
                    UseMnemonic = false,
                    AutoSize = true,
                    MaximumSize = new Size(width - left - 24, 0),
                    Location = new Point(left, 56)
                };
                Controls.Add(body);

                int bodyBottom = Math.Max(body.Bottom, glyph != null ? 74 : 0);
                int footerY = bodyBottom + 24;

                var footer = new Panel
                {
                    BackColor = Theme.Sidebar,
                    Location = new Point(0, footerY),
                    Size = new Size(width, 68)
                };
                Controls.Add(footer);

                var defs = ButtonsFor(buttons);
                int x = width - 20;
                for (int i = defs.Length - 1; i >= 0; i--)
                {
                    var d = defs[i];
                    var b = new ModernButton
                    {
                        Text = d.Item1,
                        Kind = i == 0 ? (icon == MessageBoxIcon.Warning && buttons == MessageBoxButtons.YesNo ? ButtonKind.Danger : ButtonKind.Primary) : ButtonKind.Secondary,
                        DialogResult = d.Item2,
                        Height = 38
                    }.AutoWidth(96);
                    x -= b.Width;
                    b.Location = new Point(x, 15);
                    x -= 10;
                    footer.Controls.Add(b);
                    if (i == 0) AcceptButton = b;
                    if (d.Item2 == DialogResult.Cancel || d.Item2 == DialogResult.No || defs.Length == 1) CancelButton = b;
                }

                ClientSize = new Size(width, footerY + footer.Height);
                Shown += (s, e) => { if (AcceptButton is Control) ((Control)AcceptButton).Focus(); };
            }

            private static Tuple<string, DialogResult>[] ButtonsFor(MessageBoxButtons buttons)
            {
                switch (buttons)
                {
                    case MessageBoxButtons.YesNo:
                        return new[] { Tuple.Create("Evet", DialogResult.Yes), Tuple.Create("Hayır", DialogResult.No) };
                    case MessageBoxButtons.YesNoCancel:
                        return new[] { Tuple.Create("Evet", DialogResult.Yes), Tuple.Create("Hayır", DialogResult.No), Tuple.Create("İptal", DialogResult.Cancel) };
                    case MessageBoxButtons.OKCancel:
                        return new[] { Tuple.Create("Tamam", DialogResult.OK), Tuple.Create("İptal", DialogResult.Cancel) };
                    case MessageBoxButtons.RetryCancel:
                        return new[] { Tuple.Create("Yeniden dene", DialogResult.Retry), Tuple.Create("İptal", DialogResult.Cancel) };
                    default:
                        return new[] { Tuple.Create("Tamam", DialogResult.OK) };
                }
            }
        }
    }
}
