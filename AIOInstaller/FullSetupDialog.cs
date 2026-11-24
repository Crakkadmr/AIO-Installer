using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using AIOInstaller.Ui;

namespace AIOInstaller
{
    /// <summary>Tam Kurulum'da hangi adımların çalışacağı (hepsi baştan seçilir).</summary>
    public class FullSetupOptions
    {
        public bool DisableUac { get; set; }
        public bool DisableDefender { get; set; }
        public bool ConnectWifi { get; set; }
        public bool WindowsUpdate { get; set; }
        public bool InstallPrograms { get; set; }
        public bool InstallEbys { get; set; }
        public bool ApplySmb { get; set; }
        public bool OfficeShortcuts { get; set; }

        public int StepCount
        {
            get
            {
                return new[] { DisableUac, DisableDefender, ConnectWifi, WindowsUpdate,
                               InstallPrograms, InstallEbys, ApplySmb, OfficeShortcuts }.Count(b => b);
            }
        }
    }

    /// <summary>
    /// Tam Kurulum başlamadan önce tüm soruları tek ekranda soran pencere.
    /// Her adım bir anahtarla açılıp kapanır; MEB EBYS varsayılan olarak kapalıdır.
    /// </summary>
    public class FullSetupDialog : Form
    {
        private readonly List<Tuple<ToggleSwitch, Action<FullSetupOptions, bool>>> rows =
            new List<Tuple<ToggleSwitch, Action<FullSetupOptions, bool>>>();
        private readonly Label lblCount;
        private readonly ModernButton btnStart;

        public FullSetupOptions Options { get; private set; }

        public FullSetupDialog(int programCount, EbysLocator.EbysInstaller ebys, IList<WifiNetwork> networks)
        {
            Text = "Tam Kurulum";
            BackColor = Theme.Surface;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            Font = Theme.Body(9.75f);
            Theme.ApplyDarkTitleBar(this);

            const int width = 640;

            // ── Başlık şeridi ──
            var hero = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = Theme.Surface };
            hero.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Theme.HighQuality(g);
                var r = new Rectangle(0, 0, hero.Width, hero.Height);
                using (var lg = new LinearGradientBrush(r, Color.FromArgb(18, 58, 48), Theme.Surface, LinearGradientMode.Horizontal))
                    g.FillRectangle(lg, r);
                var badge = new RectangleF(26, 28, 48, 48);
                using (var path = Theme.RoundRect(badge, 12))
                using (var bb = new LinearGradientBrush(badge, Theme.HeroFrom, Theme.HeroTo, 45f))
                    g.FillPath(bb, path);
                Theme.DrawText(g, Theme.Glyph.Rocket, Theme.Icon(17f), Rectangle.Round(badge), Color.FromArgb(5, 26, 18),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                Theme.DrawText(g, "Tam Kurulum", Theme.Heading(17f), new Rectangle(90, 26, 400, 28), Theme.Text);
                Theme.DrawText(g, "Neler yapılacağını seç. Başlattıktan sonra hiçbir soru sorulmaz.",
                    Theme.Body(9.75f), new Rectangle(90, 54, width - 110, 22), Theme.TextMuted);
                using (var p = new Pen(Theme.Border))
                    g.DrawLine(p, 0, hero.Height - 1, hero.Width, hero.Height - 1);
            };

            // ── Adımlar ──
            var list = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, AutoScroll = true, Padding = new Padding(0, 8, 0, 8) };

            string wifiDesc = networks == null || networks.Count == 0
                ? "Kayıtlı ağ yok — Ayarlar > Wi-Fi Ağları'ndan ekleyin."
                : "Sırayla denenir: " + string.Join(", ", networks.Select(n => n.Ssid));
            string ebysDesc = ebys == null
                ? "EBYS*.exe programın klasöründe bulunamadı."
                : ebys.DisplayName + " · programlardan sonra, pencereli kurulur.";

            var rowDefs = new[]
            {
                Row(Theme.Glyph.Uac, Theme.Warning, "UAC'yi kapat", "Kurulumlarda tekrar tekrar onay penceresi çıkmasın.",
                    true, true, (o, v) => o.DisableUac = v),
                Row(Theme.Glyph.Shield, Theme.Danger, "Windows Defender'ı kapat", "Kapanmazsa Windows Güvenliği açılır, elle kapatman istenir.",
                    true, true, (o, v) => o.DisableDefender = v),
                Row(Theme.Glyph.Wifi, Theme.Info, "Wi-Fi'ye bağlan", wifiDesc,
                    networks != null && networks.Count > 0, networks != null && networks.Count > 0, (o, v) => o.ConnectWifi = v),
                Row(Theme.Glyph.Update, Theme.Accent, "Windows Update'i başlat", "İnternet varsa güncellemeler arka planda aranır.",
                    true, true, (o, v) => o.WindowsUpdate = v),
                Row(Theme.Glyph.Programs, Theme.Success, "Programları kur",
                    programCount > 0 ? programCount + " program sırayla kurulur." : "Listede kurulacak program yok.",
                    programCount > 0, programCount > 0, (o, v) => o.InstallPrograms = v),
                Row(Theme.Glyph.School, Color.FromArgb(192, 132, 252), "MEB EBYS kurulsun mu?", ebysDesc,
                    ebys != null, false, (o, v) => o.InstallEbys = v),
                Row(Theme.Glyph.Share, Theme.Info, "SMB ayarlarını uygula", "Misafir erişimi açılır, SMB imza zorunluluğu kalkar.",
                    true, true, (o, v) => o.ApplySmb = v),
                Row(Theme.Glyph.Office, Theme.Warning, "Office kısayolları", "Word, Excel, PowerPoint, Outlook masaüstüne eklenir.",
                    true, true, (o, v) => o.OfficeShortcuts = v),
            };

            // Dock=Top: ters sırayla ekle ki listede yukarıdan aşağı doğru görünsün
            for (int i = rowDefs.Length - 1; i >= 0; i--) list.Controls.Add(rowDefs[i]);

            // ── Alt çubuk ──
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 76, BackColor = Theme.Sidebar };
            footer.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border)) e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
            };
            lblCount = UiFactory.Label("", Theme.Body(9.75f), Theme.TextMuted);
            lblCount.Location = new Point(26, 28);

            btnStart = new ModernButton
            {
                Text = "Başlat",
                Glyph = Theme.Glyph.Play,
                Kind = ButtonKind.Hero,
                Font = Theme.Body(10.5f, FontStyle.Bold),
                Height = 44,
                CornerRadius = 10
            }.AutoWidth(150);
            btnStart.Click += (s, e) =>
            {
                var o = new FullSetupOptions();
                foreach (var r in rows) r.Item2(o, r.Item1.Checked && r.Item1.Enabled);
                Options = o;
                DialogResult = DialogResult.OK;
            };

            var btnCancel = new ModernButton
            {
                Text = "Vazgeç",
                Kind = ButtonKind.Secondary,
                Height = 44,
                DialogResult = DialogResult.Cancel
            }.AutoWidth(110);

            footer.Controls.Add(lblCount);
            footer.Controls.Add(btnStart);
            footer.Controls.Add(btnCancel);
            footer.Layout += (s, e) =>
            {
                btnStart.Location = new Point(footer.Width - btnStart.Width - 24, 16);
                btnCancel.Location = new Point(btnStart.Left - btnCancel.Width - 10, 16);
            };

            Controls.Add(list);
            Controls.Add(footer);
            Controls.Add(hero);

            AcceptButton = btnStart;
            CancelButton = btnCancel;

            int contentHeight = rowDefs.Sum(r => r.Height) + list.Padding.Vertical;
            int maxHeight = Screen.FromPoint(Cursor.Position).WorkingArea.Height - 80;
            ClientSize = new Size(width, Math.Min(hero.Height + contentHeight + footer.Height, maxHeight));

            UpdateCount();
        }

        private Control Row(string glyph, Color tint, string title, string description,
            bool enabled, bool defaultOn, Action<FullSetupOptions, bool> apply)
        {
            var row = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Theme.Surface, Padding = new Padding(26, 0, 26, 0) };
            var badge = new IconBadge(glyph, enabled ? tint : Theme.TextDim) { Size = new Size(38, 38) };
            var lblTitle = UiFactory.Label(title, Theme.Body(10.25f, FontStyle.Bold), enabled ? Theme.Text : Theme.TextDim);
            var lblDesc = new Label
            {
                Text = description,
                Font = Theme.Body(8.75f),
                ForeColor = enabled ? Theme.TextMuted : Theme.TextDim,
                BackColor = Theme.Surface,
                AutoEllipsis = true,
                UseMnemonic = false
            };
            var toggle = new ToggleSwitch { Enabled = enabled };
            toggle.SetInitial(enabled && defaultOn);
            toggle.CheckedChanged += (s, e) => UpdateCount();

            row.Controls.AddRange(new Control[] { badge, lblTitle, lblDesc, toggle });
            row.Layout += (s, e) =>
            {
                badge.Location = new Point(26, 12);
                toggle.Location = new Point(row.Width - toggle.Width - 30, 19);
                lblTitle.Location = new Point(78, 11);
                lblDesc.SetBounds(78, 33, Math.Max(40, toggle.Left - 78 - 16), 18);
            };

            // Satırın herhangi bir yerine tıklamak anahtarı değiştirir
            EventHandler flip = (s, e) => { if (toggle.Enabled) toggle.Checked = !toggle.Checked; };
            row.Click += flip;
            lblTitle.Click += flip;
            lblDesc.Click += flip;
            badge.Click += flip;
            if (enabled)
            {
                row.Cursor = Cursors.Hand;
                foreach (Control c in new Control[] { lblTitle, lblDesc, badge }) c.Cursor = Cursors.Hand;
            }

            rows.Add(Tuple.Create(toggle, apply));
            return row;
        }

        private void UpdateCount()
        {
            if (lblCount == null) return;
            int n = rows.Count(r => r.Item1.Checked && r.Item1.Enabled);
            lblCount.Text = n == 0 ? "Hiçbir adım seçilmedi" : n + " adım çalışacak";
            if (btnStart != null) btnStart.Enabled = n > 0;
        }
    }
}
