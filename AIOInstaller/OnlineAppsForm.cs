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
            // ── Tarayıcılar ───────────────────────────────────────────────
            new OnlineApp("Google Chrome", "Dünyanın en popüler tarayıcısı",
                "Tarayıcı", "https://dl.google.com/chrome/install/latest/chrome_installer.exe",
                "/silent /install", "Son", "~90 MB", "🌐"),
            new OnlineApp("Mozilla Firefox", "Hızlı ve gizlilik odaklı tarayıcı",
                "Tarayıcı", "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=tr",
                "-ms", "Son", "~55 MB", "🦊"),
            new OnlineApp("Brave Browser", "Reklam engelleyici dahili tarayıcı",
                "Tarayıcı", "https://laptop-updates.brave.com/latest/winx64",
                "/silent /install", "Son", "~85 MB", "🦁"),

            // ── Medya ─────────────────────────────────────────────────────
            new OnlineApp("VLC Media Player", "Evrensel medya oynatıcı",
                "Medya", "https://get.videolan.org/vlc/last/win64/vlc-last-win64.exe",
                "/S", "Son", "~40 MB", "🎬"),
            new OnlineApp("OBS Studio", "Yayın ve ekran kaydı yazılımı",
                "Medya", "https://github.com/obsproject/obs-studio/releases/latest/download/OBS-Studio-30.0.0-Windows-Installer.exe",
                "/S", "Son", "~180 MB", "📡"),
            new OnlineApp("Spotify", "Müzik ve podcast akış platformu",
                "Medya", "https://download.scdn.co/SpotifySetup.exe",
                "/silent", "Son", "~80 MB", "🎵"),
            new OnlineApp("Audacity", "Ücretsiz ses düzenleme yazılımı",
                "Medya", "https://github.com/audacity/audacity/releases/latest/download/audacity-win-3.4.2-64bit.exe",
                "/S", "3.4.2", "~35 MB", "🎙️"),
            new OnlineApp("HandBrake", "Video dönüştürme ve sıkıştırma aracı",
                "Medya", "https://github.com/HandBrake/HandBrake/releases/latest/download/HandBrake-1.7.3-x86_64-Win_GUI.exe",
                "/S", "1.7.3", "~20 MB", "🎞️"),
            new OnlineApp("MPC-HC", "Hafif ve güçlü medya oynatıcı",
                "Medya", "https://github.com/clsid2/mpc-hc/releases/latest/download/MPC-HC.2.3.0.x64.exe",
                "/SP- /VERYSILENT /NORESTART", "Son", "~15 MB", "▶️"),
            new OnlineApp("K-Lite Codec Pack", "Eksiksiz video/ses codec paketi",
                "Medya", "https://download3.codecguide.com/K-Lite_Codec_Pack_1830_Standard.exe",
                "/VERYSILENT /NORESTART", "Son", "~35 MB", "🎬"),
            new OnlineApp("Kdenlive", "Profesyonel ücretsiz video editörü",
                "Medya", "https://download.kde.org/stable/kdenlive/23.08/windows/kdenlive-23.08.5.exe",
                "/S", "Son", "~250 MB", "🎥"),

            // ── Araçlar ───────────────────────────────────────────────────
            new OnlineApp("7-Zip", "Ücretsiz dosya sıkıştırma aracı",
                "Araçlar", "https://www.7-zip.org/a/7z2301-x64.exe",
                "/S", "23.01", "~1.5 MB", "🗜"),
            new OnlineApp("WinRAR", "Popüler arşivleme programı",
                "Araçlar", "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701.exe",
                "/S", "7.01", "~3.5 MB", "📦"),
            new OnlineApp("AnyDesk", "Uzak masaüstü erişim aracı",
                "Araçlar", "https://download.anydesk.com/AnyDesk.exe",
                "--install C:\\Program Files\\AnyDesk --start-with-win --create-shortcuts --silent",
                "Son", "~4 MB", "🖥"),
            new OnlineApp("TeamViewer", "Uzak bağlantı ve destek aracı",
                "Araçlar", "https://download.teamviewer.com/download/TeamViewer_Setup_x64.exe",
                "/S", "Son", "~50 MB", "📡"),
            new OnlineApp("qBittorrent", "Açık kaynaklı torrent istemcisi",
                "Araçlar", "https://github.com/qbittorrent/qBittorrent/releases/latest/download/qbittorrent_4.6.3_x64_setup.exe",
                "/S", "4.6.3", "~35 MB", "⬇️"),
            new OnlineApp("ShareX", "Ekran görüntüsü ve ekran kaydı aracı",
                "Araçlar", "https://github.com/ShareX/ShareX/releases/latest/download/ShareX-16.1.0-setup.exe",
                "/VERYSILENT /NORESTART", "Son", "~8 MB", "📸"),
            new OnlineApp("CPU-Z", "İşlemci, RAM ve anakart bilgi aracı",
                "Araçlar", "https://download.cpuid.com/cpu-z/cpu-z_2.09-en.exe",
                "/S", "2.09", "~2 MB", "🔍"),
            new OnlineApp("HWiNFO64", "Kapsamlı sistem donanım izleme aracı",
                "Araçlar", "https://www.hwinfo.com/files/hwi_780.exe",
                "/VERYSILENT /NORESTART", "7.80", "~12 MB", "🖥️"),
            new OnlineApp("CrystalDiskInfo", "Disk sağlık durumu izleme aracı",
                "Araçlar", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller",
                "/VERYSILENT /NORESTART", "Son", "~6 MB", "💿"),
            new OnlineApp("Bitwarden", "Açık kaynaklı şifre yöneticisi",
                "Araçlar", "https://github.com/bitwarden/clients/releases/latest/download/Bitwarden-Installer-x64.exe",
                "/S", "Son", "~90 MB", "🔐"),
            new OnlineApp("Everything", "Anlık dosya arama motoru",
                "Araçlar", "https://www.voidtools.com/Everything-1.4.1.1024.x64-Setup.exe",
                "/S", "1.4.1", "~2 MB", "🔎"),
            new OnlineApp("CCleaner", "Sistem temizleme ve optimizasyon aracı",
                "Araçlar", "https://download.ccleaner.com/ccsetup624.exe",
                "/S", "6.24", "~30 MB", "🧹"),
            new OnlineApp("Revo Uninstaller", "Gelişmiş program kaldırma aracı",
                "Araçlar", "https://www.revouninstaller.com/download/revouninstaller_setup.exe",
                "/VERYSILENT /NORESTART", "Son", "~7 MB", "🗑️"),
            new OnlineApp("VirtualBox", "Ücretsiz sanallaştırma platformu",
                "Araçlar", "https://download.virtualbox.org/virtualbox/7.0.14/VirtualBox-7.0.14-161095-Win.exe",
                "--silent", "7.0.14", "~110 MB", "📦"),

            // ── Geliştirici Araçları ──────────────────────────────────────
            new OnlineApp("VS Code", "Hafif ve güçlü kod editörü",
                "Geliştirici", "https://update.code.visualstudio.com/latest/win32-x64-user/stable",
                "/VERYSILENT /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath",
                "Son", "~80 MB", "🖊"),
            new OnlineApp("Git", "Dağıtık versiyon kontrol sistemi",
                "Geliştirici", "https://github.com/git-for-windows/git/releases/latest/download/Git-2.44.0-64-bit.exe",
                "/SILENT /NORESTART", "2.44.0", "~50 MB", "🐙"),
            new OnlineApp("Notepad++", "Gelişmiş metin ve kod editörü",
                "Geliştirici", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/latest/download/npp.8.6.4.Installer.x64.exe",
                "/S", "8.6.4", "~4 MB", "📝"),
            new OnlineApp("Python 3", "Güçlü ve kolay programlama dili",
                "Geliştirici", "https://www.python.org/ftp/python/3.12.3/python-3.12.3-amd64.exe",
                "/quiet InstallAllUsers=1 PrependPath=1", "3.12.3", "~25 MB", "🐍"),
            new OnlineApp("Node.js LTS", "JavaScript çalışma zamanı ortamı",
                "Geliştirici", "https://nodejs.org/dist/v20.11.1/node-v20.11.1-x64.msi",
                "/quiet /norestart", "20.11.1 LTS", "~30 MB", "🟢"),
            new OnlineApp("Java JDK 21", "Java Geliştirme Kiti (LTS sürümü)",
                "Geliştirici", "https://download.oracle.com/java/21/latest/jdk-21_windows-x64_bin.exe",
                "/s", "21 LTS", "~170 MB", "☕"),
            new OnlineApp("WinSCP", "Güvenli dosya transfer istemcisi (SFTP/FTP)",
                "Geliştirici", "https://winscp.net/download/WinSCP-6.3.2-Setup.exe",
                "/VERYSILENT /NORESTART", "6.3.2", "~12 MB", "📁"),
            new OnlineApp("PuTTY", "SSH ve Telnet istemcisi",
                "Geliştirici", "https://the.earth.li/~sgtatham/putty/latest/w64/putty-64bit-0.80-installer.msi",
                "/quiet /norestart", "0.80", "~3 MB", "🔒"),
            new OnlineApp("Postman", "API geliştirme ve test aracı",
                "Geliştirici", "https://dl.pstmn.io/download/latest/win64",
                "/S", "Son", "~150 MB", "📮"),
            new OnlineApp("Docker Desktop", "Konteyner geliştirme ortamı",
                "Geliştirici", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe",
                "install --quiet", "Son", "~600 MB", "🐳"),
            new OnlineApp("HeidiSQL", "MySQL/MariaDB/PostgreSQL veritabanı istemcisi",
                "Geliştirici", "https://www.heidisql.com/installers/HeidiSQL_12.6.0.6765_Setup.exe",
                "/VERYSILENT /NORESTART", "12.6", "~20 MB", "🗄️"),

            // ── İletişim ──────────────────────────────────────────────────
            new OnlineApp("Zoom", "Video konferans ve toplantı aracı",
                "İletişim", "https://zoom.us/client/latest/ZoomInstallerFull.exe",
                "/quiet /norestart", "Son", "~15 MB", "📹"),
            new OnlineApp("Discord", "Sesli sohbet ve topluluk platformu",
                "İletişim", "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64",
                "--silent", "Son", "~80 MB", "🎮"),
            new OnlineApp("WhatsApp", "Dünya genelinde popüler mesajlaşma uygulaması",
                "İletişim", "https://web.whatsapp.com/desktop/windows/release/x64/WhatsAppSetup.exe",
                "--silent", "Son", "~180 MB", "💬"),
            new OnlineApp("Telegram", "Hızlı ve güvenli mesajlaşma platformu",
                "İletişim", "https://telegram.org/dl/desktop/win64",
                "/S", "Son", "~45 MB", "✈️"),
            new OnlineApp("Microsoft Teams", "Kurumsal ekip iletişim platformu",
                "İletişim", "https://teams.microsoft.com/downloads/desktopurl?env=production&plat=windows&arch=x64&managedInstaller=true&download=true",
                "/quiet /norestart", "Son", "~130 MB", "💼"),
            new OnlineApp("Slack", "Ekip iletişimi ve iş birliği platformu",
                "İletişim", "https://slack.com/ssb/download-win64-msi",
                "/quiet", "Son", "~120 MB", "💬"),
            new OnlineApp("Skype", "Microsoft video arama ve mesajlaşma uygulaması",
                "İletişim", "https://get.skype.com/go/getskype-full",
                "/VERYSILENT /SP-", "Son", "~80 MB", "📞"),

            // ── Oyun İstemcisi ────────────────────────────────────────────
            new OnlineApp("Steam", "Dünyanın en büyük oyun platformu",
                "Oyun İstemcisi", "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe",
                "/S", "Son", "~3 MB", "🎮"),
            new OnlineApp("Epic Games Launcher", "Epic Games oyun istemcisi ve mağazası",
                "Oyun İstemcisi", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi",
                "/quiet", "Son", "~60 MB", "⚡"),
            new OnlineApp("Riot Client", "Riot Games oyunları istemcisi (LoL, Valorant, TFT...)",
                "Oyun İstemcisi", "https://valorant.secure.dyn.riotcdn.net/channels/public/x/installer/current/live.live.eu.exe",
                "--skip-to-install", "Son", "~50 MB", "⚔️"),
            new OnlineApp("Battle.net", "Blizzard oyun istemcisi (WoW, Overwatch 2, Diablo 4...)",
                "Oyun İstemcisi", "https://downloader.battle.net/desktop/Battle.net-Setup.exe",
                "--lang=trTR", "Son", "~2 MB", "🔵"),
            new OnlineApp("Ubisoft Connect", "Ubisoft oyun istemcisi (Assassin's Creed, Far Cry...)",
                "Oyun İstemcisi", "https://ubi.li/4vxt9",
                "/S", "Son", "~90 MB", "🔷"),
            new OnlineApp("EA App", "EA Games istemcisi (FIFA, Battlefield, The Sims...)",
                "Oyun İstemcisi", "https://origin-a.akamaihd.net/EA-Desktop-Client/installer-data/EAappInstaller.exe",
                "/S", "Son", "~75 MB", "🎯"),
            new OnlineApp("GOG Galaxy", "DRM-free oyun platformu",
                "Oyun İstemcisi", "https://cdn.gog.com/open/galaxy/client/installer/win/GOGGalaxySetup_2.0.exe",
                "/VERYSILENT /NORESTART", "Son", "~60 MB", "🌌"),

            // ── Ofis ──────────────────────────────────────────────────────
            new OnlineApp("LibreOffice", "Ücretsiz ve açık kaynaklı ofis paketi",
                "Ofis", "https://download.documentfoundation.org/libreoffice/stable/latest/win/x86_64/LibreOffice_latest_Win_x86-64.msi",
                "/quiet /norestart", "Son", "~350 MB", "📄"),
            new OnlineApp("SumatraPDF", "Hafif ve hızlı PDF okuyucu",
                "Ofis", "https://www.sumatrapdfreader.org/dl/rel/SumatraPDF-3.4.6-64-install.exe",
                "/S", "3.4.6", "~10 MB", "📖"),
            new OnlineApp("Foxit PDF Reader", "Kapsamlı özellikli PDF okuyucu",
                "Ofis", "https://cdn01.foxitsoftware.com/pub/foxit/reader/desktop/win/2024.2/2024.2.0.25138/FoxitPDFReader2024.2.0.25138_Setup_Prom.exe",
                "/quiet /norestart", "Son", "~75 MB", "📕"),
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
