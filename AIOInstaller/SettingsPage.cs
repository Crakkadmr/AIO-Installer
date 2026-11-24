using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AIOInstaller.Ui;

namespace AIOInstaller
{
    /// <summary>
    /// Ayarlar sayfası: kayıtlı Wi-Fi ağları, MEB EBYS kurulum dosyası ve klasör konumları.
    /// Değişiklikler anında <see cref="AppSettings"/> üzerinden ayarlar.json'a yazılır.
    /// </summary>
    public class SettingsPage : Panel
    {
        private readonly Panel scroll;

        // Wi-Fi
        private ListBox listNetworks;
        private Label listEmpty;
        private DarkTextBox txtSsid;
        private DarkTextBox txtPassword;
        private ModernButton btnScan;
        private ModernButton[] securityButtons;
        private string selectedSecurity = "WPA2";
        private Label lblEditorTitle;
        private Label lblWifiError;
        private ModernButton btnSaveNetwork;
        private ModernButton btnDeleteNetwork;
        private ModernButton btnUp;
        private ModernButton btnDown;
        private Label lblToast;
        private Timer toastTimer;

        // EBYS
        private Label lblEbysName;
        private Label lblEbysMeta;
        private IconBadge ebysState;
        private DarkTextBox txtEbysArgs;
        private Timer argsSaveTimer;

        private AppSettings Settings { get { return AppSettings.Current; } }

        public SettingsPage()
        {
            BackColor = Theme.Bg;

            var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Theme.Bg };
            var title = UiFactory.Label("Ayarlar", Theme.Heading(22f), Theme.Text);
            title.Location = new Point(0, 0);
            var sub = UiFactory.Label("Ayarlar programın yanındaki ayarlar.json dosyasında saklanır — USB ile birlikte taşınır.",
                Theme.Body(10f), Theme.TextMuted);
            sub.Location = new Point(2, 40);
            lblToast = new Label
            {
                AutoSize = false,
                Size = new Size(160, 32),
                Font = Theme.Body(9.5f, FontStyle.Bold),
                ForeColor = Theme.Success,
                BackColor = Theme.Bg,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };
            header.Controls.AddRange(new Control[] { title, sub, lblToast });
            header.Layout += (s, e) => lblToast.Location = new Point(header.Width - lblToast.Width, 6);

            toastTimer = new Timer { Interval = 2200 };
            toastTimer.Tick += (s, e) => { lblToast.Visible = false; toastTimer.Stop(); };

            scroll = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, AutoScroll = true, Padding = new Padding(0, 0, 8, 8) };

            var wifiCard = BuildWifiCard();
            var ebysCard = BuildEbysCard();
            var pathsCard = BuildPathsCard();

            // Dock=Top: ters sırayla ekle
            scroll.Controls.Add(pathsCard);
            scroll.Controls.Add(Spacer());
            scroll.Controls.Add(ebysCard);
            scroll.Controls.Add(Spacer());
            scroll.Controls.Add(wifiCard);

            Theme.DarkScrollbars(scroll);
            Controls.Add(scroll);
            Controls.Add(header);

            ReloadNetworks();
            ClearEditor();
            RefreshEbys();
        }

        private static Panel Spacer()
        {
            return new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Theme.Bg };
        }

        /// <summary>Kurulum sürerken ayarların değiştirilmesini engeller.</summary>
        public void SetBusy(bool busy)
        {
            scroll.Enabled = !busy;
        }

        private void ShowToast(string text, bool error = false)
        {
            lblToast.Text = text;
            lblToast.ForeColor = error ? Theme.Danger : Theme.Success;
            lblToast.Visible = true;
            toastTimer.Stop();
            toastTimer.Start();
        }

        private void Persist()
        {
            string err = Settings.Save();
            if (err == null) ShowToast("✓ Kaydedildi");
            else ShowToast("Kaydedilemedi", true);
        }

        private static Control CardHeader(Card card, string glyph, Color tint, string title, string description)
        {
            var badge = new IconBadge(glyph, tint) { Location = new Point(24, 22), Size = new Size(44, 44) };
            var lblTitle = UiFactory.Label(title, Theme.Heading(14f), Theme.Text);
            lblTitle.Location = new Point(82, 22);
            var lblDesc = UiFactory.Label(description, Theme.Body(9.5f), Theme.TextMuted);
            lblDesc.Location = new Point(83, 48);
            card.Controls.Add(badge);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblDesc);
            return lblDesc;
        }

        private static Label FieldLabel(string text)
        {
            return UiFactory.Label(text, Theme.Body(8.75f, FontStyle.Bold), Theme.TextMuted);
        }

        // ══════════════════════════════════════════════════════════════════
        // Wi-Fi
        // ══════════════════════════════════════════════════════════════════

        private Card BuildWifiCard()
        {
            var card = new Card { Dock = DockStyle.Top, Height = 392 };
            CardHeader(card, Theme.Glyph.Wifi, Theme.Info, "Wi-Fi Ağları",
                "Tam Kurulum bu ağları yukarıdan aşağı dener ve çevrede olan ilkine bağlanır.");

            // ── Sol: kayıtlı ağlar ──
            var lblList = FieldLabel("KAYITLI AĞLAR");
            listNetworks = new ListBox
            {
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 54,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Bg,
                ForeColor = Theme.Text,
                IntegralHeight = false
            };
            listNetworks.DrawItem += ListNetworks_DrawItem;
            listNetworks.SelectedIndexChanged += (s, e) => LoadSelectedIntoEditor();
            var listFrame = new Card { FillColor = Theme.Bg, BorderColor = Theme.Border, CornerRadius = 10, Padding = new Padding(6) };
            listNetworks.Dock = DockStyle.Fill;
            listEmpty = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Henüz kayıtlı ağ yok.\nSağdan ağ adı ve şifre girip kaydet.",
                Font = Theme.Body(9.5f),
                ForeColor = Theme.TextDim,
                BackColor = Theme.Bg,
                TextAlign = ContentAlignment.MiddleCenter
            };
            listFrame.Controls.Add(listEmpty);
            listFrame.Controls.Add(listNetworks);
            Theme.DarkScrollbars(listNetworks);

            btnUp = new ModernButton { Glyph = Theme.Glyph.Up, Size = new Size(40, 34), Kind = ButtonKind.Secondary };
            btnDown = new ModernButton { Glyph = Theme.Glyph.Down, Size = new Size(40, 34), Kind = ButtonKind.Secondary };
            btnDeleteNetwork = new ModernButton { Text = "Sil", Glyph = Theme.Glyph.Delete, Kind = ButtonKind.Danger, Height = 34 }.AutoWidth(80);
            var tip = new ToolTip();
            tip.SetToolTip(btnUp, "Yukarı taşı (önce denenir)");
            tip.SetToolTip(btnDown, "Aşağı taşı");
            btnUp.Click += (s, e) => MoveSelected(-1);
            btnDown.Click += (s, e) => MoveSelected(1);
            btnDeleteNetwork.Click += (s, e) => DeleteSelected();

            // ── Sağ: düzenleyici ──
            lblEditorTitle = UiFactory.Label("Yeni ağ ekle", Theme.Body(10.5f, FontStyle.Bold), Theme.Text);
            var lblSsid = FieldLabel("AĞ ADI (SSID)");
            txtSsid = new DarkTextBox(Theme.Glyph.Wifi) { Placeholder = "ör. TNC_AP", Height = 40 };
            btnScan = new ModernButton { Text = "Tara", Glyph = Theme.Glyph.Search, Height = 40 }.AutoWidth(90);
            tip.SetToolTip(btnScan, "Çevredeki ağları listele");
            btnScan.Click += BtnScan_Click;

            var lblPass = FieldLabel("ŞİFRE");
            txtPassword = new DarkTextBox(Theme.Glyph.Lock) { Placeholder = "Ağ şifresi", Height = 40 };
            txtPassword.EnableReveal();

            var lblSec = FieldLabel("GÜVENLİK");
            securityButtons = new[]
            {
                SecurityButton("WPA2", "WPA2"),
                SecurityButton("WPA3", "WPA3"),
                SecurityButton("Açık", "OPEN")
            };

            lblWifiError = UiFactory.Label("", Theme.Body(9f), Theme.Danger);
            lblWifiError.Visible = false;

            var btnNew = new ModernButton { Text = "Yeni", Glyph = Theme.Glyph.Add, Kind = ButtonKind.Ghost, Height = 38 }.AutoWidth(90);
            btnNew.Click += (s, e) => { listNetworks.ClearSelected(); ClearEditor(); txtSsid.Inner.Focus(); };
            btnSaveNetwork = new ModernButton { Text = "Ağı Kaydet", Glyph = Theme.Glyph.Save, Kind = ButtonKind.Primary, Height = 38 }.AutoWidth(130);
            btnSaveNetwork.Click += (s, e) => SaveEditor();
            txtPassword.Inner.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SaveEditor(); e.SuppressKeyPress = true; } };
            txtSsid.Inner.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { txtPassword.Inner.Focus(); e.SuppressKeyPress = true; } };

            card.Controls.AddRange(new Control[]
            {
                lblList, listFrame, btnUp, btnDown, btnDeleteNetwork,
                lblEditorTitle, lblSsid, txtSsid, btnScan, lblPass, txtPassword, lblSec,
                lblWifiError, btnNew, btnSaveNetwork
            });
            card.Controls.AddRange(securityButtons);

            card.Layout += (s, e) =>
            {
                int left = 24, top = 92;
                int inner = card.Width - 48;
                int listW = Math.Max(240, (int)(inner * 0.44));
                int edX = left + listW + 28;
                int edW = card.Width - edX - 24;

                lblList.Location = new Point(left, top);
                listFrame.SetBounds(left, top + 22, listW, 216);
                int by = listFrame.Bottom + 10;
                btnUp.Location = new Point(left, by);
                btnDown.Location = new Point(btnUp.Right + 6, by);
                btnDeleteNetwork.Location = new Point(left + listW - btnDeleteNetwork.Width, by);

                lblEditorTitle.Location = new Point(edX, top - 2);
                lblSsid.Location = new Point(edX, top + 28);
                txtSsid.SetBounds(edX, top + 48, edW - btnScan.Width - 8, 40);
                btnScan.Location = new Point(edX + edW - btnScan.Width, top + 48);
                lblPass.Location = new Point(edX, top + 100);
                txtPassword.SetBounds(edX, top + 120, edW, 40);
                lblSec.Location = new Point(edX, top + 172);
                int segW = (edW - 12) / 3;
                for (int i = 0; i < securityButtons.Length; i++)
                    securityButtons[i].SetBounds(edX + i * (segW + 6), top + 192, segW, 36);
                lblWifiError.Location = new Point(edX, top + 238);
                btnSaveNetwork.Location = new Point(edX + edW - btnSaveNetwork.Width, by);
                btnNew.Location = new Point(btnSaveNetwork.Left - btnNew.Width - 8, by);
            };
            return card;
        }

        private ModernButton SecurityButton(string label, string value)
        {
            var b = new ModernButton { Text = label, Kind = ButtonKind.Secondary, Height = 36, Tag = value };
            b.Click += (s, e) => SetSecurity(value);
            return b;
        }

        private void SetSecurity(string value)
        {
            selectedSecurity = value;
            foreach (var b in securityButtons)
            {
                b.Kind = (string)b.Tag == value ? ButtonKind.Primary : ButtonKind.Secondary;
                b.Invalidate();
            }
            bool open = value == "OPEN";
            txtPassword.Enabled = !open;
            if (open) txtPassword.Text = "";
        }

        private void ListNetworks_DrawItem(object sender, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            g.FillRectangle(new SolidBrush(Theme.Bg), e.Bounds);
            if (e.Index < 0 || e.Index >= Settings.WifiNetworks.Count) return;
            Theme.HighQuality(g);

            var net = Settings.WifiNetworks[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;
            var r = new RectangleF(e.Bounds.X + 2, e.Bounds.Y + 3, e.Bounds.Width - 4, e.Bounds.Height - 6);
            if (selected)
            {
                using (var path = Theme.RoundRect(r, 8))
                using (var b = new SolidBrush(Theme.AccentSoft))
                    g.FillPath(b, path);
            }

            var num = new RectangleF(r.X + 10, r.Y + (r.Height - 26) / 2, 26, 26);
            using (var b = new SolidBrush(selected ? Theme.Accent : Theme.SurfaceRaised))
                g.FillEllipse(b, num);
            Theme.DrawText(g, (e.Index + 1).ToString(), Theme.Body(8.75f, FontStyle.Bold), Rectangle.Round(num),
                selected ? Color.White : Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            int tx = (int)r.X + 48;
            int tw = (int)r.Right - tx - 8;
            Theme.DrawText(g, net.Ssid, Theme.Body(10f, FontStyle.Bold), new Rectangle(tx, (int)r.Y + 5, tw, 22), Theme.Text);
            string detail = net.SecurityLabel + (string.IsNullOrEmpty(net.Password) ? "" : "  ·  " + new string('•', Math.Min(10, net.Password.Length)));
            Theme.DrawText(g, detail, Theme.Body(8.75f), new Rectangle(tx, (int)r.Y + 25, tw, 20), Theme.TextMuted);
        }

        private void ReloadNetworks(int selectIndex = -1)
        {
            listNetworks.BeginUpdate();
            listNetworks.Items.Clear();
            foreach (var n in Settings.WifiNetworks) listNetworks.Items.Add(n.Ssid);
            listNetworks.EndUpdate();
            listEmpty.Visible = listNetworks.Items.Count == 0;
            listNetworks.Visible = listNetworks.Items.Count > 0;
            if (selectIndex >= 0 && selectIndex < listNetworks.Items.Count) listNetworks.SelectedIndex = selectIndex;
            UpdateListButtons();
        }

        private void UpdateListButtons()
        {
            int i = listNetworks.SelectedIndex;
            btnDeleteNetwork.Enabled = i >= 0;
            btnUp.Enabled = i > 0;
            btnDown.Enabled = i >= 0 && i < listNetworks.Items.Count - 1;
        }

        private void LoadSelectedIntoEditor()
        {
            UpdateListButtons();
            int i = listNetworks.SelectedIndex;
            lblWifiError.Visible = false;
            if (i < 0 || i >= Settings.WifiNetworks.Count) return;
            var n = Settings.WifiNetworks[i];
            lblEditorTitle.Text = "Ağı düzenle";
            btnSaveNetwork.Text = "Değişikliği Kaydet";
            btnSaveNetwork.AutoWidth(130);
            txtSsid.Text = n.Ssid;
            SetSecurity((n.Security ?? "WPA2").ToUpperInvariant());
            txtPassword.Text = n.Password ?? "";
            btnSaveNetwork.Parent?.PerformLayout();
        }

        private void ClearEditor()
        {
            lblEditorTitle.Text = "Yeni ağ ekle";
            btnSaveNetwork.Text = "Ağı Kaydet";
            btnSaveNetwork.AutoWidth(130);
            txtSsid.Text = "";
            txtPassword.Text = "";
            SetSecurity("WPA2");
            lblWifiError.Visible = false;
            UpdateListButtons();
            btnSaveNetwork.Parent?.PerformLayout();
        }

        private void ShowWifiError(string text)
        {
            lblWifiError.Text = text;
            lblWifiError.Visible = true;
        }

        private void SaveEditor()
        {
            string ssid = (txtSsid.Text ?? "").Trim();
            string pass = txtPassword.Text ?? "";

            if (ssid.Length == 0) { ShowWifiError("Ağ adı boş olamaz."); txtSsid.Inner.Focus(); return; }
            if (selectedSecurity != "OPEN" && (pass.Length < 8 || pass.Length > 63))
            {
                ShowWifiError("WPA şifresi 8–63 karakter olmalı.");
                txtPassword.Inner.Focus();
                return;
            }

            int editing = listNetworks.SelectedIndex;
            int dup = Settings.WifiNetworks.FindIndex(n => n.Ssid == ssid);
            if (dup >= 0 && dup != editing) { ShowWifiError("Bu ağ zaten kayıtlı."); return; }

            var net = editing >= 0 ? Settings.WifiNetworks[editing] : new WifiNetwork();
            net.Ssid = ssid;
            net.Password = selectedSecurity == "OPEN" ? "" : pass;
            net.Security = selectedSecurity;
            if (editing < 0) Settings.WifiNetworks.Add(net);

            Persist();
            int index = editing >= 0 ? editing : Settings.WifiNetworks.Count - 1;
            ReloadNetworks(index);
            if (editing < 0) { listNetworks.ClearSelected(); ClearEditor(); }
            listNetworks.Invalidate();
        }

        private void DeleteSelected()
        {
            int i = listNetworks.SelectedIndex;
            if (i < 0) return;
            string ssid = Settings.WifiNetworks[i].Ssid;
            if (Dialogs.Show($"'{ssid}' ağı listeden silinsin mi?", "Ağı sil",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            Settings.WifiNetworks.RemoveAt(i);
            Persist();
            ReloadNetworks();
            ClearEditor();
        }

        private void MoveSelected(int delta)
        {
            int i = listNetworks.SelectedIndex;
            int j = i + delta;
            if (i < 0 || j < 0 || j >= Settings.WifiNetworks.Count) return;
            var item = Settings.WifiNetworks[i];
            Settings.WifiNetworks.RemoveAt(i);
            Settings.WifiNetworks.Insert(j, item);
            Persist();
            ReloadNetworks(j);
        }

        private async void BtnScan_Click(object sender, EventArgs e)
        {
            btnScan.Enabled = false;
            btnScan.Text = "Taranıyor…";
            try
            {
                var ssids = await Task.Run(() => WifiService.ScanSsids());
                var menu = new ContextMenuStrip
                {
                    Renderer = new DarkMenuRenderer(),
                    BackColor = Theme.SurfaceRaised,
                    ForeColor = Theme.Text,
                    Font = Theme.Body(10f),
                    ShowImageMargin = false
                };
                if (ssids.Count == 0)
                {
                    menu.Items.Add(new ToolStripMenuItem("Ağ bulunamadı (Wi-Fi kartı kapalı olabilir)") { Enabled = false, ForeColor = Theme.TextMuted });
                }
                foreach (var ssid in ssids)
                {
                    bool saved = Settings.WifiNetworks.Any(n => n.Ssid == ssid);
                    var item = new ToolStripMenuItem(saved ? ssid + "   (kayıtlı)" : ssid) { ForeColor = saved ? Theme.TextMuted : Theme.Text, Padding = new Padding(4, 6, 4, 6) };
                    string pick = ssid;
                    item.Click += (s2, e2) => { txtSsid.Text = pick; txtPassword.Inner.Focus(); };
                    menu.Items.Add(item);
                }
                menu.Show(txtSsid, new Point(0, txtSsid.Height + 4));
            }
            finally
            {
                btnScan.Text = "Tara";
                btnScan.Enabled = true;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // MEB EBYS
        // ══════════════════════════════════════════════════════════════════

        private Card BuildEbysCard()
        {
            var card = new Card { Dock = DockStyle.Top, Height = 296 };
            CardHeader(card, Theme.Glyph.School, Color.FromArgb(192, 132, 252), "MEB EBYS",
                "Programın klasöründeki en yeni EBYS*.exe kullanılır. Güncelleme gelince yeni dosyayı oraya atman yeterli.");

            var fileBox = new Card { FillColor = Theme.Bg, BorderColor = Theme.Border, CornerRadius = 10, Padding = new Padding(0) };
            ebysState = new IconBadge(Theme.Glyph.Check, Theme.Success) { Size = new Size(38, 38), Location = new Point(14, 14) };
            ebysState.BackColor = Theme.Bg;
            lblEbysName = UiFactory.Label("", Theme.Body(10.5f, FontStyle.Bold), Theme.Text);
            lblEbysName.Location = new Point(64, 13);
            lblEbysMeta = UiFactory.Label("", Theme.Body(9f), Theme.TextMuted);
            lblEbysMeta.Location = new Point(64, 35);
            var btnRescan = new ModernButton { Text = "Yeniden tara", Glyph = Theme.Glyph.Refresh, Height = 36 }.AutoWidth();
            btnRescan.Click += (s, e) => { RefreshEbys(); ShowToast("✓ Tarandı"); };
            var btnOpen = new ModernButton { Text = "Klasörü aç", Glyph = Theme.Glyph.Folder, Height = 36 }.AutoWidth();
            btnOpen.Click += (s, e) => OpenFolder(EbysLocator.SearchFolder);
            fileBox.Controls.AddRange(new Control[] { ebysState, lblEbysName, lblEbysMeta, btnRescan, btnOpen });
            fileBox.Layout += (s, e) =>
            {
                btnOpen.Location = new Point(fileBox.Width - btnOpen.Width - 12, 15);
                btnRescan.Location = new Point(btnOpen.Left - btnRescan.Width - 8, 15);
            };

            var lblArgs = FieldLabel("KURULUM PARAMETRESİ (İSTEĞE BAĞLI)");
            txtEbysArgs = new DarkTextBox(Theme.Glyph.Terminal)
            {
                Placeholder = "Boş bırakılırsa kurulum penceresi açılır (ör. /S, /VERYSILENT)",
                Height = 40,
                Text = Settings.EbysArguments
            };
            argsSaveTimer = new Timer { Interval = 700 };
            argsSaveTimer.Tick += (s, e) =>
            {
                argsSaveTimer.Stop();
                Settings.EbysArguments = (txtEbysArgs.Text ?? "").Trim();
                Persist();
            };
            txtEbysArgs.TextChanged += (s, e) => { argsSaveTimer.Stop(); argsSaveTimer.Start(); };

            card.Controls.AddRange(new Control[] { fileBox, lblArgs, txtEbysArgs });
            card.Layout += (s, e) =>
            {
                int w = card.Width - 48;
                fileBox.SetBounds(24, 92, w, 66);
                lblArgs.Location = new Point(24, 178);
                txtEbysArgs.SetBounds(24, 198, w, 40);
            };
            return card;
        }

        public void RefreshEbys()
        {
            var ebys = EbysLocator.Find();
            if (ebys != null)
            {
                lblEbysName.Text = ebys.DisplayName;
                lblEbysMeta.Text = ebys.FileName + "  ·  " + ProgramManager.FormatFileSize(ebys.Size) +
                                   "  ·  " + ebys.Modified.ToString("dd.MM.yyyy HH:mm");
                ebysState.Glyph = Theme.Glyph.Check;
                ebysState.Tint = Theme.Success;
            }
            else
            {
                lblEbysName.Text = "EBYS kurulum dosyası bulunamadı";
                lblEbysMeta.Text = "EBYS*.exe dosyasını programın bulunduğu klasöre koy (programlar klasörüne değil).";
                ebysState.Glyph = Theme.Glyph.Warning;
                ebysState.Tint = Theme.Warning;
            }
            ebysState.Invalidate();
        }

        // ══════════════════════════════════════════════════════════════════
        // Konumlar
        // ══════════════════════════════════════════════════════════════════

        private Card BuildPathsCard()
        {
            var card = new Card { Dock = DockStyle.Top, Height = 252 };
            CardHeader(card, Theme.Glyph.Folder, Theme.Accent, "Konumlar",
                "Uygulamanın kullandığı klasörler. Tümü programın yanında durur.");

            var rows = new[]
            {
                PathRow("Program klasörü", AppDomain.CurrentDomain.BaseDirectory, false),
                PathRow("Kurulum dosyaları (programlar)", ProgramManager.DatabaseFolder, false),
                PathRow("Ayarlar dosyası", AppSettings.FilePath, true)
            };
            foreach (var r in rows) card.Controls.Add(r);
            card.Layout += (s, e) =>
            {
                for (int i = 0; i < rows.Length; i++)
                    rows[i].SetBounds(24, 88 + i * 50, card.Width - 48, 44);
            };
            return card;
        }

        private Control PathRow(string label, string path, bool isFile)
        {
            var row = new Panel { BackColor = Theme.Surface };
            var lbl = UiFactory.Label(label, Theme.Body(9.5f, FontStyle.Bold), Theme.Text);
            var val = new Label
            {
                Text = path,
                Font = Theme.Body(9f),
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                AutoEllipsis = true,
                UseMnemonic = false
            };
            var btn = new ModernButton { Text = "Aç", Glyph = Theme.Glyph.External, Kind = ButtonKind.Ghost, Height = 34 }.AutoWidth(72);
            btn.Click += (s, e) =>
            {
                if (isFile)
                {
                    if (!File.Exists(path)) Settings.Save();
                    try { Process.Start("explorer.exe", "/select,\"" + path + "\""); } catch { }
                }
                else OpenFolder(path);
            };
            row.Controls.AddRange(new Control[] { lbl, val, btn });
            row.Layout += (s, e) =>
            {
                lbl.Location = new Point(0, 2);
                val.SetBounds(0, 22, Math.Max(40, row.Width - btn.Width - 16), 18);
                btn.Location = new Point(row.Width - btn.Width, 5);
            };
            return row;
        }

        private static void OpenFolder(string folder)
        {
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch (Exception ex)
            {
                Dialogs.Show("Klasör açılamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>Açılır menüler için koyu tema çizicisi.</summary>
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(Theme.SurfaceRaised);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var p = new Pen(Theme.BorderStrong))
                e.Graphics.DrawRectangle(p, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var r = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
            using (var b = new SolidBrush(e.Item.Selected && e.Item.Enabled ? Theme.AccentSoft : Theme.SurfaceRaised))
                e.Graphics.FillRectangle(b, r);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? e.Item.ForeColor : Theme.TextDim;
            base.OnRenderItemText(e);
        }
    }
}
