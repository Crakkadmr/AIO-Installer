using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace AIOInstaller
{
    public class OnlineAppsForm : Form
    {
        // ─── UI ───────────────────────────────────────────────────────────
        private ListBox listCategories;
        private Panel   panelAppList;
        private Panel   panelStatus;
        private Label   lblStatus;
        private ProgressBar progressBar;
        private Button  btnCancelDownload;
        private Label   lblHeader;

        // ─── State ────────────────────────────────────────────────────────
        private List<OnlineApp>      allApps;
        private CancellationTokenSource cts;

        // ─── Font/Color helpers ──────────────────────────────────────────
        private static Font UiFont(float size, FontStyle style = FontStyle.Regular)
        {
            foreach (var name in new[] { "Bahnschrift", "Segoe UI Variable", "Segoe UI", "Calibri" })
                try { return new Font(name, size, style, GraphicsUnit.Point); } catch { }
            return new Font(SystemFonts.MessageBoxFont.FontFamily, size, style, GraphicsUnit.Point);
        }
        private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);

        // ─── App Catalogue ────────────────────────────────────────────────
        private static List<OnlineApp> BuildCatalogue() => new List<OnlineApp>
        {
            // Tarayıcılar
            new OnlineApp("Google Chrome",  "Dünyanın en popüler tarayıcısı",
                "Tarayıcı",
                "https://dl.google.com/chrome/install/latest/chrome_installer.exe",
                "/silent /install", "Son", "~90 MB", "🌐"),

            new OnlineApp("Mozilla Firefox", "Hızlı ve gizlilik odaklı tarayıcı",
                "Tarayıcı",
                "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=tr",
                "-ms", "Son", "~55 MB", "🦊"),

            new OnlineApp("Brave Browser", "Reklam engelleyici dahili tarayıcı",
                "Tarayıcı",
                "https://laptop-updates.brave.com/latest/winx64",
                "/silent /install", "Son", "~85 MB", "🦁"),

            // Medya
            new OnlineApp("VLC Media Player", "Evrensel medya oynatıcı",
                "Medya",
                "https://get.videolan.org/vlc/last/win64/vlc-last-win64.exe",
                "/S", "Son", "~40 MB", "🎬"),

            new OnlineApp("OBS Studio", "Yayın ve ekran kaydı yazılımı",
                "Medya",
                "https://github.com/obsproject/obs-studio/releases/latest/download/OBS-Studio-30.0.0-Windows-Installer.exe",
                "/S", "Son", "~180 MB", "📡"),

            // Araçlar
            new OnlineApp("7-Zip", "Ücretsiz dosya sıkıştırma aracı",
                "Araçlar",
                "https://www.7-zip.org/a/7z2301-x64.exe",
                "/S", "23.01", "~1.5 MB", "🗜"),

            new OnlineApp("WinRAR", "Popüler arşivleme programı",
                "Araçlar",
                "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701.exe",
                "/S", "7.01", "~3.5 MB", "📦"),

            new OnlineApp("AnyDesk", "Uzak masaüstü erişim aracı",
                "Araçlar",
                "https://download.anydesk.com/AnyDesk.exe",
                "--install C:\\Program Files\\AnyDesk --start-with-win --create-shortcuts --silent",
                "Son", "~4 MB", "🖥"),

            new OnlineApp("TeamViewer", "Uzak bağlantı ve destek aracı",
                "Araçlar",
                "https://download.teamviewer.com/download/TeamViewer_Setup_x64.exe",
                "/S", "Son", "~50 MB", "📡"),

            // Geliştirici Araçları
            new OnlineApp("VS Code", "Hafif ve güçlü kod editörü",
                "Geliştirici",
                "https://update.code.visualstudio.com/latest/win32-x64-user/stable",
                "/VERYSILENT /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath",
                "Son", "~80 MB", "🖊"),

            new OnlineApp("Git", "Dağıtık versiyon kontrol sistemi",
                "Geliştirici",
                "https://github.com/git-for-windows/git/releases/latest/download/Git-2.44.0-64-bit.exe",
                "/SILENT /NORESTART", "2.44.0", "~50 MB", "🐙"),

            new OnlineApp("Notepad++", "Gelişmiş metin ve kod editörü",
                "Geliştirici",
                "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/latest/download/npp.8.6.4.Installer.x64.exe",
                "/S", "8.6.4", "~4 MB", "📝"),

            new OnlineApp("Python 3", "Güçlü ve kolay programlama dili",
                "Geliştirici",
                "https://www.python.org/ftp/python/3.12.3/python-3.12.3-amd64.exe",
                "/quiet InstallAllUsers=1 PrependPath=1",
                "3.12.3", "~25 MB", "🐍"),

            // İletişim
            new OnlineApp("Zoom", "Video konferans ve toplantı aracı",
                "İletişim",
                "https://zoom.us/client/latest/ZoomInstallerFull.exe",
                "/quiet /norestart", "Son", "~15 MB", "📹"),

            new OnlineApp("Discord", "Sesli sohbet ve topluluk platformu",
                "İletişim",
                "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64",
                "--silent", "Son", "~80 MB", "🎮"),
        };

        // ─── Constructor ────────────────────────────────────────────────
        public OnlineAppsForm()
        {
            allApps = BuildCatalogue();
            InitializeUi();
            // İlk kategoriyi seç
            if (listCategories.Items.Count > 0)
                listCategories.SelectedIndex = 0;
        }

        // ─── UI Setup ────────────────────────────────────────────────────
        private void InitializeUi()
        {
            this.SuspendLayout();
            this.Text              = "🌐 Online Program İndir";
            this.Size              = new Size(960, 660);
            this.MinimumSize       = new Size(800, 500);
            this.StartPosition     = FormStartPosition.CenterParent;
            this.BackColor         = C(15, 23, 42);
            this.Font              = UiFont(9F);
            this.AutoScaleMode     = AutoScaleMode.Dpi;

            // ── Header ──────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top, Height = 64,
                BackColor = C(15, 23, 42),
                Padding   = new Padding(20, 0, 20, 0)
            };
            lblHeader = new Label
            {
                Text      = "🌐 Online Program İndir",
                ForeColor = Color.White,
                Font      = UiFont(16F, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(20, 10)
            };
            var lblSub = new Label
            {
                Text      = "Katılımsız (sessiz) kurulum destekli popüler uygulamalar",
                ForeColor = C(148, 163, 184),
                Font      = UiFont(9F, FontStyle.Italic),
                AutoSize  = true,
                Location  = new Point(23, 38)
            };
            header.Controls.Add(lblHeader);
            header.Controls.Add(lblSub);

            // ── Status / Progress Panel ──────────────────────────────────
            panelStatus = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 60,
                BackColor = C(30, 41, 59),
                Padding   = new Padding(16, 8, 16, 8),
                Visible   = false
            };
            lblStatus = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 18,
                ForeColor = Color.White,
                Font      = UiFont(9F, FontStyle.Bold),
                Text      = ""
            };
            progressBar = new ProgressBar
            {
                Dock    = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value   = 0,
                Style   = ProgressBarStyle.Continuous
            };
            btnCancelDownload = new Button
            {
                Text      = "✕ İptal",
                Size      = new Size(80, 22),
                BackColor = C(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = UiFont(8F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Dock      = DockStyle.Right
            };
            btnCancelDownload.FlatAppearance.BorderSize = 0;
            btnCancelDownload.Click += (s, e) => cts?.Cancel();
            var statusLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                BackColor = Color.Transparent
            };
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            lblStatus.Dock   = DockStyle.Fill;
            progressBar.Dock = DockStyle.Fill;
            statusLayout.Controls.Add(lblStatus,        0, 0);
            statusLayout.Controls.Add(btnCancelDownload, 1, 0);
            statusLayout.SetColumnSpan(progressBar, 2);
            statusLayout.Controls.Add(progressBar, 0, 1);
            panelStatus.Controls.Add(statusLayout);

            // ── Left: Categories ─────────────────────────────────────────
            var panelLeft = new Panel
            {
                Dock = DockStyle.Left, Width = 170,
                BackColor = C(30, 41, 59),
                Padding   = new Padding(8)
            };
            var lblCatTitle = new Label
            {
                Text      = "KATEGORİLER",
                ForeColor = C(148, 163, 184),
                Font      = UiFont(8F, FontStyle.Bold),
                Dock      = DockStyle.Top,
                Height    = 24,
                TextAlign = ContentAlignment.MiddleLeft
            };
            listCategories = new ListBox
            {
                Dock            = DockStyle.Fill,
                BackColor       = C(30, 41, 59),
                ForeColor       = Color.White,
                BorderStyle     = BorderStyle.None,
                Font            = UiFont(10F, FontStyle.Bold),
                ItemHeight      = 36,
                SelectionMode   = SelectionMode.One,
                DrawMode        = DrawMode.OwnerDrawFixed
            };
            listCategories.DrawItem    += ListCategories_DrawItem;
            listCategories.SelectedIndexChanged += (s, e) => RefreshAppList();

            // Kategorileri doldur
            var categories = allApps.Select(a => a.Category).Distinct().OrderBy(c => c).ToList();
            categories.Insert(0, "Tümü");
            foreach (var cat in categories)
                listCategories.Items.Add(cat);

            panelLeft.Controls.Add(listCategories);
            panelLeft.Controls.Add(lblCatTitle);

            // ── Right: App Cards ──────────────────────────────────────────
            panelAppList = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = C(15, 23, 42),
                AutoScroll  = true,
                Padding     = new Padding(8)
            };

            // ── Splitter ─────────────────────────────────────────────────
            var splitter = new Splitter
            {
                Dock      = DockStyle.Left,
                Width     = 4,
                BackColor = C(51, 65, 85)
            };

            // Controls add order matters for Dock layout
            this.Controls.Add(panelAppList);
            this.Controls.Add(splitter);
            this.Controls.Add(panelLeft);
            this.Controls.Add(panelStatus);
            this.Controls.Add(header);

            this.ResumeLayout(false);
        }

        // ─── Category list custom draw ────────────────────────────────────
        private void ListCategories_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            string text     = listCategories.Items[e.Index].ToString();
            bool selected   = (e.State & DrawItemState.Selected) != 0;
            var backColor   = selected ? C(59, 130, 246) : C(30, 41, 59);
            var foreColor   = selected ? Color.White : C(203, 213, 225);
            e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);
            var sf = new System.Drawing.StringFormat { LineAlignment = System.Drawing.StringAlignment.Center };
            using (var font = UiFont(10F, selected ? FontStyle.Bold : FontStyle.Regular))
                e.Graphics.DrawString(text, font, new SolidBrush(foreColor),
                    new RectangleF(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height), sf);
        }

        // ─── Refresh right panel ──────────────────────────────────────────
        private void RefreshAppList()
        {
            string cat = listCategories.SelectedItem?.ToString() ?? "Tümü";
            var filtered = cat == "Tümü"
                ? allApps
                : allApps.Where(a => a.Category == cat).ToList();

            panelAppList.SuspendLayout();
            panelAppList.Controls.Clear();

            // Build cards from bottom (so they stack top→down when added at Top)
            for (int i = filtered.Count - 1; i >= 0; i--)
                panelAppList.Controls.Add(BuildAppCard(filtered[i]));

            panelAppList.ResumeLayout();
        }

        // ─── Build a single card ──────────────────────────────────────────
        private Panel BuildAppCard(OnlineApp app)
        {
            var card = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = C(30, 41, 59),
                Margin    = new Padding(0, 0, 0, 4),
                Padding   = new Padding(12, 8, 12, 8),
                Cursor    = Cursors.Default
            };

            // Colored left bar
            var bar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 4,
                BackColor = C(59, 130, 246)
            };

            // Icon label
            var lblIcon = new Label
            {
                Text      = app.IconEmoji,
                Font      = UiFont(20F),
                ForeColor = Color.White,
                Size      = new Size(44, 54),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Name + description
            var lblName = new Label
            {
                Text      = app.Name,
                ForeColor = Color.White,
                Font      = UiFont(11F, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(60, 8)
            };
            var lblDesc = new Label
            {
                Text      = app.Description,
                ForeColor = C(148, 163, 184),
                Font      = UiFont(8.5F),
                AutoSize  = true,
                Location  = new Point(61, 30)
            };
            var lblMeta = new Label
            {
                Text      = $"v{app.Version}  •  {app.SizeMB}",
                ForeColor = C(100, 116, 139),
                Font      = UiFont(8F),
                AutoSize  = true,
                Location  = new Point(61, 48)
            };

            // Download button
            var btn = new Button
            {
                Text      = "⬇ İndir & Kur",
                Size      = new Size(120, 36),
                BackColor = C(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = UiFont(9F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = C(52, 211, 153);
            btn.FlatAppearance.MouseDownBackColor = C(5, 150, 105);

            // Position btn on right - use Resize to keep it anchored
            void PositionBtn()
            {
                btn.Location = new Point(card.ClientSize.Width - btn.Width - 16,
                                         (card.ClientSize.Height - btn.Height) / 2);
            }
            card.Resize += (s, e) => PositionBtn();

            // Capture for lambda
            OnlineApp capturedApp = app;
            btn.Click += async (s, e) => await DownloadAndInstallAsync(capturedApp, btn);

            lblIcon.Location = new Point(8, 8);
            card.Controls.Add(btn);
            card.Controls.Add(lblMeta);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblName);
            card.Controls.Add(lblIcon);
            card.Controls.Add(bar);

            // Trigger initial positioning
            card.PerformLayout();

            return card;
        }

        // ─── Download + Silent Install ────────────────────────────────────
        private async Task DownloadAndInstallAsync(OnlineApp app, Button triggerBtn)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                MessageBox.Show("Başka bir indirme işlemi devam ediyor. Lütfen bekleyin veya iptal edin.",
                    "Meşgul", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            cts = new CancellationTokenSource();
            var token = cts.Token;

            // Disable all download buttons while downloading
            SetAllDownloadButtonsEnabled(false);
            panelStatus.Visible  = true;
            progressBar.Value    = 0;
            lblStatus.Text       = $"Hazırlanıyor: {app.Name}...";

            string tempFile = Path.Combine(Path.GetTempPath(), $"AIOOnline_{Guid.NewGuid():N}.exe");

            try
            {
                // ── Download ──────────────────────────────────────────────
                using (var wc = new WebClient())
                {
                    wc.DownloadProgressChanged += (s, e) =>
                    {
                        if (progressBar.IsHandleCreated)
                            progressBar.BeginInvoke((Action)(() =>
                            {
                                progressBar.Value = e.ProgressPercentage;
                                long totalMB = e.TotalBytesToReceive > 0
                                    ? e.TotalBytesToReceive / (1024 * 1024) : 0;
                                long dlMB   = e.BytesReceived / (1024 * 1024);
                                lblStatus.Text = totalMB > 0
                                    ? $"İndiriliyor: {app.Name}  —  {dlMB} MB / {totalMB} MB"
                                    : $"İndiriliyor: {app.Name}  —  {dlMB} MB";
                            }));
                    };

                    token.Register(() => wc.CancelAsync());

                    await wc.DownloadFileTaskAsync(new Uri(app.DownloadUrl), tempFile);
                }

                if (token.IsCancellationRequested)
                {
                    lblStatus.Text = "İndirme iptal edildi.";
                    return;
                }

                // ── Silent Install ────────────────────────────────────────
                lblStatus.Text    = $"Kuruluyor: {app.Name}...";
                progressBar.Style = ProgressBarStyle.Marquee;

                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName        = tempFile,
                        Arguments       = app.SilentArgs ?? "",
                        UseShellExecute = true,
                        Verb            = "runas"
                    };
                    var proc = Process.Start(psi);
                    proc?.WaitForExit();
                }, token);

                if (!token.IsCancellationRequested)
                {
                    lblStatus.Text    = $"✅ {app.Name} başarıyla kuruldu!";
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = 100;
                    MessageBox.Show($"{app.Name} başarıyla indirildi ve kuruldu!",
                        "Kurulum Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                lblStatus.Text    = $"❌ Hata: {ex.Message}";
                if (!token.IsCancellationRequested)
                    MessageBox.Show($"İndirme/kurulum sırasında hata oluştu:\n{ex.Message}",
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Cleanup temp file
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }

                cts?.Dispose();
                cts = null;

                progressBar.Style = ProgressBarStyle.Continuous;
                SetAllDownloadButtonsEnabled(true);

                // Hide status bar after a short delay if successful
                await Task.Delay(3000);
                if (cts == null) // no new download started
                    panelStatus.Visible = false;
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────
        private void SetAllDownloadButtonsEnabled(bool enabled)
        {
            foreach (Panel card in panelAppList.Controls.OfType<Panel>())
                foreach (Button btn in card.Controls.OfType<Button>())
                    btn.Enabled = enabled;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            cts?.Cancel();
        }
    }
}
