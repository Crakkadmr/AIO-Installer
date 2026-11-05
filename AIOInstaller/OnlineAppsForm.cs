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
        private ListBox     listCategories;
        private Panel       panelAppList;
        private Panel       panelStatus;
        private Label       lblStatus;
        private ProgressBar progressBar;
        private Button      btnCancelDownload;
        private Label       lblHeader;
        private TextBox     txtSearch;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);

        // ─── State ────────────────────────────────────────────────────────
        private List<OnlineApp>         allApps;
        private List<InstallProfile>    profiles;
        private CancellationTokenSource cts;

        // ─── VIP Kimlik Bilgileri (oturum süresi, kalıcı değil) ──────────
        private static string _vipUser = null;
        private static string _vipPass = null;

        // ─── programlar klasör yolu ─────────────────────────────────────────
        private static readonly string DatabaseDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "programlar");

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
            new OnlineApp("Opera GX", "Oyuncular için özel tarayıcı",
                "Tarayıcı", "https://net.geo.opera.com/opera_gx/stable/windows",
                "/silent /install", "Son", "~100 MB", "🎮"),
            new OnlineApp("Microsoft Edge", "Microsoft'un Chromium tabanlı tarayıcısı",
                "Tarayıcı", "https://go.microsoft.com/fwlink/?linkid=2108834&Channel=Stable&language=tr&brand=M100",
                "/silent /install", "Son", "~110 MB", "🌊"),

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
            new OnlineApp("foobar2000", "Gelişmiş ve hafif müzik çalar",
                "Medya", "https://www.foobar2000.org/files/foobar2000_v2.1.4.exe",
                "/S", "2.1.4", "~6 MB", "🎵"),
            new OnlineApp("AIMP", "Profesyonel özellikli müzik çalar",
                "Medya", "https://www.aimp.ru/files/windows/builds/aimp_5.10.2418_w64.exe",
                "/S", "5.10", "~25 MB", "🎶"),
            new OnlineApp("MediaInfo", "Medya dosyası bilgi görüntüleme aracı",
                "Medya", "https://mediaarea.net/download/binary/mediainfo-gui/24.01/MediaInfo_GUI_24.01_Windows_x64.exe",
                "/S", "24.01", "~8 MB", "ℹ️"),
            new OnlineApp("Kodi", "Açık kaynaklı medya merkezi",
                "Medya", "https://mirrors.kodi.tv/releases/windows/win64/kodi-21.0-Omega-x64.exe",
                "/S", "21.0", "~90 MB", "📺"),

            // ── Araçlar ───────────────────────────────────────────────────
            new OnlineApp("7-Zip", "Ücretsiz dosya sıkıştırma aracı",
                "Araçlar", "https://www.7-zip.org/a/7z2301-x64.exe",
                "/S", "23.01", "~1.5 MB", "🗜️"),
            new OnlineApp("WinRAR", "Popüler arşivleme programı",
                "Araçlar", "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701.exe",
                "/S", "7.01", "~3.5 MB", "📦"),
            new OnlineApp("NanaZip", "Modern arayüzlü 7-Zip çatalı",
                "Araçlar", "https://github.com/M2Team/NanaZip/releases/latest/download/NanaZip.exe",
                "/S", "Son", "~3 MB", "🗜️"),
            new OnlineApp("AnyDesk", "Uzak masaüstü erişim aracı",
                "Araçlar", "https://download.anydesk.com/AnyDesk.exe",
                "--install C:\\Program Files\\AnyDesk --start-with-win --create-shortcuts --silent",
                "Son", "~4 MB", "🖥️"),
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
            new OnlineApp("GPU-Z", "Ekran kartı bilgi ve izleme aracı",
                "Araçlar", "https://download.techpowerup.com/GPU-Z.2.57.0.exe",
                "/S", "2.57", "~5 MB", "🎮"),
            new OnlineApp("CrystalDiskInfo", "Disk sağlık durumu izleme aracı",
                "Araçlar", "https://crystalmark.info/redirect.php?product=CrystalDiskInfoInstaller",
                "/VERYSILENT /NORESTART", "Son", "~6 MB", "💿"),
            new OnlineApp("CrystalDiskMark", "Disk hız testi aracı",
                "Araçlar", "https://crystalmark.info/redirect.php?product=CrystalDiskMarkInstaller",
                "/VERYSILENT /NORESTART", "Son", "~5 MB", "⚡"),
            new OnlineApp("Bitwarden", "Açık kaynaklı şifre yöneticisi",
                "Araçlar", "https://github.com/bitwarden/clients/releases/latest/download/Bitwarden-Installer-x64.exe",
                "/S", "Son", "~90 MB", "🔐"),
            new OnlineApp("KeePassXC", "Yerel şifre yöneticisi",
                "Araçlar", "https://github.com/keepassxreboot/keepassxc/releases/latest/download/KeePassXC-2.7.7-Win64.msi",
                "/quiet /norestart", "2.7.7", "~35 MB", "🔑"),
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
            new OnlineApp("WinDirStat", "Disk kullanım görselleştirme aracı",
                "Araçlar", "https://github.com/windirstat/windirstat/releases/latest/download/windirstat1_1_2_setup.exe",
                "/S", "1.1.2", "~1 MB", "📊"),
            new OnlineApp("Bulk Rename Utility", "Toplu dosya yeniden adlandırma aracı",
                "Araçlar", "https://www.bulkrenameutility.co.uk/Downloads/BRU_Setup.exe",
                "/S", "Son", "~8 MB", "📝"),
            new OnlineApp("Autoruns", "Başlangıç programlarını yönetme aracı",
                "Araçlar", "https://download.sysinternals.com/files/Autoruns.zip",
                "", "Son", "~1 MB", "⚙️"),
            new OnlineApp("Rufus", "USB önyükleme diski oluşturma aracı",
                "Araçlar", "https://github.com/pbatard/rufus/releases/latest/download/rufus-4.4.exe",
                "", "4.4", "~1.5 MB", "💾"),
            new OnlineApp("BalenaEtcher", "SD kart / USB imaj yazma aracı",
                "Araçlar", "https://github.com/balena-io/etcher/releases/latest/download/balenaEtcher-Setup-1.18.11.exe",
                "/S", "Son", "~140 MB", "💿"),
            new OnlineApp("Ventoy", "Çoklu ISO USB önyükleme aracı",
                "Araçlar", "https://github.com/ventoy/Ventoy/releases/latest/download/ventoy-1.0.97-windows.zip",
                "", "Son", "~15 MB", "🔧"),
            new OnlineApp("PowerToys", "Windows güç kullanıcı araçları paketi",
                "Araçlar", "https://github.com/microsoft/PowerToys/releases/latest/download/PowerToysSetup-0.79.0-x64.exe",
                "/quiet", "Son", "~200 MB", "⚡"),
            new OnlineApp("f.lux", "Ekran renk sıcaklığı ayarlama aracı",
                "Araçlar", "https://justgetflux.com/dlwin.html",
                "/S", "Son", "~2 MB", "🌙"),
            new OnlineApp("Rainmeter", "Masaüstü özelleştirme platformu",
                "Araçlar", "https://github.com/rainmeter/rainmeter/releases/latest/download/Rainmeter-4.5.17.exe",
                "/S", "Son", "~5 MB", "🎨"),
            new OnlineApp("TreeSize Free", "Klasör boyutu analiz aracı",
                "Araçlar", "https://downloads.jam-software.de/treesize_free/TreeSizeFreeSetup.exe",
                "/S", "Son", "~25 MB", "🌳"),

            // ── Güvenlik ──────────────────────────────────────────────────
            new OnlineApp("Malwarebytes", "Gelişmiş kötü yazılım temizleme aracı",
                "Güvenlik", "https://www.malwarebytes.com/api/downloads/mb-windows?filename=MBSetup.exe",
                "/VERYSILENT /NORESTART", "Son", "~80 MB", "🛡️"),
            new OnlineApp("Avast Free Antivirus", "Ücretsiz antivirüs yazılımı",
                "Güvenlik", "https://down.avast.com/softw/full/avast_free_antivirus_setup_online.exe",
                "/silent /sp-", "Son", "~200 MB", "🔒"),
            new OnlineApp("AdwCleaner", "Reklam yazılımı temizleme aracı",
                "Güvenlik", "https://downloads.malwarebytes.com/file/adwcleaner",
                "/VERYSILENT", "Son", "~10 MB", "🧹"),
            new OnlineApp("Glasswire", "Ağ izleme ve güvenlik duvarı",
                "Güvenlik", "https://download.glasswire.com/GlassWireSetup.exe",
                "/VERYSILENT /NORESTART", "Son", "~70 MB", "🌐"),

            // ── Geliştirici Araçları ──────────────────────────────────────
            new OnlineApp("VS Code", "Hafif ve güçlü kod editörü",
                "Geliştirici", "https://update.code.visualstudio.com/latest/win32-x64-user/stable",
                "/VERYSILENT /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders,associatewithfiles,addtopath",
                "Son", "~80 MB", "💻"),
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
                "Geliştirici", "https://the.earth.li/~sgtatham/putty/latest/w64/putty-64bit-0.80-installer.exe",
                "/S", "0.80", "~3 MB", "🔒"),
            new OnlineApp("Postman", "API geliştirme ve test aracı",
                "Geliştirici", "https://dl.pstmn.io/download/latest/win64",
                "/S", "Son", "~150 MB", "📮"),
            new OnlineApp("Docker Desktop", "Konteyner geliştirme ortamı",
                "Geliştirici", "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe",
                "install --quiet", "Son", "~600 MB", "🐳"),
            new OnlineApp("HeidiSQL", "MySQL/MariaDB/PostgreSQL veritabanı istemcisi",
                "Geliştirici", "https://www.heidisql.com/installers/HeidiSQL_12.6.0.6765_Setup.exe",
                "/VERYSILENT /NORESTART", "12.6", "~20 MB", "🗄️"),
            new OnlineApp("DBeaver Community", "Evrensel veritabanı yönetim aracı",
                "Geliştirici", "https://dbeaver.io/files/dbeaver-ce-latest-x86_64-setup.exe",
                "/S", "Son", "~120 MB", "🗃️"),
            new OnlineApp("Insomnia", "REST API test ve debug aracı",
                "Geliştirici", "https://github.com/Kong/insomnia/releases/latest/download/Insomnia.Setup.9.3.3.exe",
                "/S", "Son", "~180 MB", "😴"),
            new OnlineApp("FileZilla", "FTP/SFTP istemcisi",
                "Geliştirici", "https://download.filezilla-project.org/client/FileZilla_3.66.5_win64-setup.exe",
                "/S", "3.66.5", "~12 MB", "📂"),
            new OnlineApp("Windows Terminal", "Modern terminal emülatörü",
                "Geliştirici", "https://github.com/microsoft/terminal/releases/latest/download/Microsoft.WindowsTerminal_Win10_1.19.10821.0_8wekyb3d8bbwe.msixbundle",
                "", "Son", "~100 MB", "⌨️"),
            new OnlineApp("GitKraken", "Görsel Git istemcisi",
                "Geliştirici", "https://release.gitkraken.com/win64/GitKrakenSetup.exe",
                "/S", "Son", "~200 MB", "🦑"),
            new OnlineApp("Sublime Text", "Hızlı ve şık kod editörü",
                "Geliştirici", "https://download.sublimetext.com/sublime_text_build_4169_x64_setup.exe",
                "/VERYSILENT", "4.4169", "~20 MB", "✏️"),

            // ── İletişim ──────────────────────────────────────────────────
            new OnlineApp("Discord", "Sesli sohbet ve topluluk platformu",
                "İletişim", "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64",
                "--silent", "Son", "~80 MB", "🎮"),
            new OnlineApp("WhatsApp", "Dünya genelinde popüler mesajlaşma uygulaması",
                "İletişim", "https://web.whatsapp.com/desktop/windows/release/x64/WhatsAppSetup.exe",
                "--silent", "Son", "~180 MB", "💬"),
            new OnlineApp("Telegram", "Hızlı ve güvenli mesajlaşma platformu",
                "İletişim", "https://telegram.org/dl/desktop/win64",
                "/S", "Son", "~45 MB", "✈️"),
            new OnlineApp("Zoom", "Video konferans ve toplantı aracı",
                "İletişim", "https://zoom.us/client/latest/ZoomInstallerFull.exe",
                "/quiet /norestart", "Son", "~15 MB", "📹"),
            new OnlineApp("Microsoft Teams", "Kurumsal ekip iletişim platformu",
                "İletişim", "https://teams.microsoft.com/downloads/desktopurl?env=production&plat=windows&arch=x64&managedInstaller=true&download=true",
                "/quiet /norestart", "Son", "~130 MB", "💼"),
            new OnlineApp("Slack", "Ekip iletişimi ve iş birliği platformu",
                "İletişim", "https://slack.com/ssb/download-win64-msi",
                "/quiet", "Son", "~120 MB", "🧵"),
            new OnlineApp("Skype", "Microsoft video arama ve mesajlaşma uygulaması",
                "İletişim", "https://get.skype.com/go/getskype-full",
                "/VERYSILENT /SP-", "Son", "~80 MB", "📞"),
            new OnlineApp("Viber", "Ücretsiz sesli ve görüntülü arama uygulaması",
                "İletişim", "https://download.cdn.viber.com/cdn/desktop/Windows/ViberSetup.exe",
                "/S", "Son", "~200 MB", "📱"),
            new OnlineApp("Signal", "Gizlilik öncelikli güvenli mesajlaşma",
                "İletişim", "https://updates.signal.org/desktop/signal-desktop-win-7.20.0.exe",
                "/S", "Son", "~200 MB", "🔏"),

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
            new OnlineApp("Battle.net", "Blizzard oyun istemcisi (WoW, OW2, Diablo 4...)",
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
            new OnlineApp("Playnite", "Açık kaynaklı oyun kütüphane yöneticisi",
                "Oyun İstemcisi", "https://github.com/JosefNemec/Playnite/releases/latest/download/PlayniteInstaller.exe",
                "/S", "Son", "~15 MB", "🎲"),
            new OnlineApp("Xbox App", "Microsoft Xbox oyun istemcisi",
                "Oyun İstemcisi", "https://go.microsoft.com/fwlink/?linkid=2213773",
                "/quiet", "Son", "~5 MB", "🎮"),

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
            new OnlineApp("Okular", "KDE'nin çok formatlı belge okuyucusu",
                "Ofis", "https://cdn.kde.org/ci-builds/graphics/okular/master/windows/okular-latest-amd64.exe",
                "/S", "Son", "~80 MB", "📚"),
            new OnlineApp("OnlyOffice Desktop", "Microsoft Office uyumlu ofis paketi",
                "Ofis", "https://download.onlyoffice.com/install/desktop/editors/windows/onlyoffice_desktopeditors_x64.exe",
                "/S", "Son", "~400 MB", "📊"),
            new OnlineApp("Notion", "Not alma ve proje yönetim aracı",
                "Ofis", "https://desktop-release.notion-static.com/Notion%20Setup%203.13.0.exe",
                "/S", "Son", "~140 MB", "📌"),
            new OnlineApp("Obsidian", "Markdown tabanlı bilgi yönetim uygulaması",
                "Ofis", "https://github.com/obsidianmd/obsidian-releases/releases/latest/download/Obsidian-1.5.8.exe",
                "/S", "Son", "~80 MB", "🔮"),

            // ── Grafik & Tasarım ──────────────────────────────────────────
            new OnlineApp("GIMP", "Ücretsiz ve güçlü görüntü düzenleme yazılımı",
                "Grafik", "https://download.gimp.org/mirror/pub/gimp/v2.10/windows/gimp-2.10.36-setup-2.exe",
                "/S", "2.10.36", "~250 MB", "🖼️"),
            new OnlineApp("Inkscape", "Vektör grafik editörü",
                "Grafik", "https://inkscape.org/gallery/item/44680/inkscape-1.3.2_2023-11-25_091e20e-x64.exe",
                "/S", "1.3.2", "~120 MB", "✒️"),
            new OnlineApp("paint.net", "Kullanımı kolay görüntü düzenleme aracı",
                "Grafik", "https://github.com/paintdotnet/release/releases/latest/download/paint.net.5.0.13.install.anycpu.web.zip",
                "/S", "5.0.13", "~8 MB", "🎨"),
            new OnlineApp("DaVinci Resolve", "Profesyonel video düzenleme yazılımı",
                "Grafik", "https://www.blackmagicdesign.com/api/support/us/downloads.json",
                "/S", "Son", "~4 GB", "🎬"),
            new OnlineApp("Canva Desktop", "Çevrimiçi tasarım aracının masaüstü uygulaması",
                "Grafik", "https://desktop.canva.com/windows/prod/Canva.exe",
                "/S", "Son", "~200 MB", "🎨"),
            new OnlineApp("Blender", "3D modelleme ve animasyon yazılımı",
                "Grafik", "https://download.blender.org/release/Blender4.1/blender-4.1.0-windows-x64.msi",
                "/quiet /norestart", "4.1.0", "~700 MB", "🦴"),

            // ── Sistem ───────────────────────────────────────────
            new OnlineApp(".NET Desktop Runtime 8", "Gerekli .NET kütüphaneleri",
                "Sistem", "https://download.visualstudio.microsoft.com/download/pr/49ebc904-8bde-40db-9fc0-097561deca8b/3028d0026eaf1cfdfbba8b79ebdefcd3/windowsdesktop-runtime-8.0.3-win-x64.exe",
                "/install /quiet /norestart", "8.0", "~55 MB", "⚙️"),
            new OnlineApp("DirectX", "Microsoft DirectX End-User Runtime",
                "Sistem", "https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe",
                "/Q", "Son", "~1 MB", "🎮"),
            new OnlineApp("Visual C++ Redistributable", "AIO Repack (Tüm VC++ sürümleri)",
                "Sistem", "https://github.com/abbodi1406/vcredist/releases/download/v0.79.0/VisualCppRedist_AIO_x86_x64.exe",
                "/y", "Son", "~30 MB", "🧩"),
        };

        // ─── Varsayılan (gömülü) kurulum profilleri ──────────────────────
        private static List<InstallProfile> DefaultProfiles() => new List<InstallProfile>
        {
            new InstallProfile { Name = "Temel", Icon = "🏠", Apps = new List<string> {
                "Google Chrome", "7-Zip", "VLC Media Player", "Foxit PDF Reader",
                "SumatraPDF", "Everything", "Visual C++ Redistributable", ".NET Desktop Runtime 8" } },
            new InstallProfile { Name = "Ofis", Icon = "📄", Apps = new List<string> {
                "Google Chrome", "LibreOffice", "OnlyOffice Desktop", "Foxit PDF Reader",
                "SumatraPDF", "7-Zip", "Zoom", "Microsoft Teams", "AnyDesk" } },
            new InstallProfile { Name = "Oyun", Icon = "🎮", Apps = new List<string> {
                "Google Chrome", "Steam", "Epic Games Launcher", "Discord", "OBS Studio",
                "Visual C++ Redistributable", "DirectX", "7-Zip" } },
            new InstallProfile { Name = "Geliştirici", Icon = "💻", Apps = new List<string> {
                "Google Chrome", "VS Code", "Git", "Node.js LTS", "Python 3",
                "Notepad++", "Windows Terminal", "DBeaver Community", "Docker Desktop", "7-Zip" } },
        };

        // ─── Constructor ────────────────────────────────────────────────
        public OnlineAppsForm()
        {
            // Uzak katalog (GitHub) → önbellek → gömülü liste sırasıyla denenir.
            var fallback = new CatalogData { Apps = BuildCatalogue(), Profiles = DefaultProfiles() };
            var data = CatalogService.Load(fallback);
            allApps = data.Apps;
            profiles = data.Profiles ?? new List<InstallProfile>();

            InitializeUi();
            if (listCategories.Items.Count > 0)
                listCategories.SelectedIndex = 0;
        }

        // ─── UI Setup ────────────────────────────────────────────────────
        private void InitializeUi()
        {
            this.SuspendLayout();
            this.Text          = "🌐 Online Program İndir";
            this.Size          = new Size(1000, 680);
            this.MinimumSize   = new Size(860, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor     = C(15, 23, 42);
            this.Font          = UiFont(9F);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            // ── Header ──────────────────────────────────────────────────
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 64,
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
                Text      = "Programları indirin, programlar klasörüne otomatik kaydedilir",
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
                Height    = 54,
                BackColor = C(30, 41, 59),
                Padding   = new Padding(12, 6, 12, 6),
                Visible   = false
            };
            lblStatus = new Label
            {
                AutoSize  = false,
                ForeColor = Color.White,
                Font      = UiFont(9F, FontStyle.Bold),
                Text      = "",
                TextAlign = ContentAlignment.MiddleLeft
            };
            progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value   = 0,
                Style   = ProgressBarStyle.Continuous
            };
            btnCancelDownload = new Button
            {
                Text      = "✕ Durdur",
                BackColor = C(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = UiFont(8.5F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Width     = 100
            };
            btnCancelDownload.FlatAppearance.BorderSize = 0;
            btnCancelDownload.Click += (s, e) => cts?.Cancel();

            // TableLayoutPanel: [status label | progress bar | cancel btn]
            var statusLayout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 1,
                BackColor   = Color.Transparent
            };
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            lblStatus.Dock     = DockStyle.Fill;
            progressBar.Dock   = DockStyle.Fill;
            btnCancelDownload.Dock = DockStyle.Fill;

            statusLayout.Controls.Add(lblStatus,        0, 0);
            statusLayout.Controls.Add(progressBar,      1, 0);
            statusLayout.Controls.Add(btnCancelDownload, 2, 0);

            panelStatus.Controls.Add(statusLayout);

            // ── Left: Categories ─────────────────────────────────────────
            var panelLeft = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 180,
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
                Dock          = DockStyle.Fill,
                BackColor     = C(30, 41, 59),
                ForeColor     = Color.White,
                BorderStyle   = BorderStyle.None,
                Font          = UiFont(10F, FontStyle.Bold),
                ItemHeight    = 36,
                SelectionMode = SelectionMode.One,
                DrawMode      = DrawMode.OwnerDrawFixed
            };
            listCategories.DrawItem             += ListCategories_DrawItem;
            listCategories.SelectedIndexChanged += (s, e) => RefreshAppList();

            var categories = allApps.Select(a => a.Category).Distinct().OrderBy(c => c).ToList();
            categories.Insert(0, "Tümü");
            foreach (var cat in categories)
                listCategories.Items.Add(cat);

            panelLeft.Controls.Add(listCategories);
            panelLeft.Controls.Add(lblCatTitle);

            // ── Right: Search + App Cards ─────────────────────────────────
            var panelRight = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = C(15, 23, 42)
            };

            var panelSearch = new Panel
            {
                Dock       = DockStyle.Top,
                Height     = 55,
                Padding    = new Padding(10, 15, 10, 5) // Sol, Üst, Sağ, Alt
            };
            
            txtSearch = new TextBox
            {
                Dock        = DockStyle.Fill,
                Font        = UiFont(12F),
                BackColor   = C(30, 41, 59),
                ForeColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            SendMessage(txtSearch.Handle, 0x1501, IntPtr.Zero, "🔍 Uygulama Ara...");
            txtSearch.TextChanged += (s, e) => RefreshAppList();
            
            panelSearch.Controls.Add(txtSearch);

            panelAppList = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(8)
            };

            // ── Profiller şeridi (hazır paketler) ────────────────────────
            var panelProfiles = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 48,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = C(30, 41, 59),
                Padding       = new Padding(10, 8, 10, 8)
            };
            var lblProfiles = new Label
            {
                Text      = "📦 PAKETLER:",
                ForeColor = C(148, 163, 184),
                Font      = UiFont(8.5F, FontStyle.Bold),
                AutoSize  = false,
                Width     = 90,
                Height    = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin    = new Padding(0, 0, 4, 0)
            };
            panelProfiles.Controls.Add(lblProfiles);

            foreach (var profile in profiles)
            {
                InstallProfile captured = profile;
                var pbtn = new Button
                {
                    Text      = $"{profile.Icon} {profile.Name}",
                    AutoSize  = true,
                    Height    = 30,
                    BackColor = C(234, 88, 12),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font      = UiFont(8.5F, FontStyle.Bold),
                    Cursor    = Cursors.Hand,
                    Margin    = new Padding(3, 0, 3, 0),
                    Padding   = new Padding(8, 0, 8, 0)
                };
                pbtn.FlatAppearance.BorderSize = 0;
                pbtn.FlatAppearance.MouseOverBackColor = C(249, 115, 22);
                pbtn.Click += async (s, e) => await DownloadProfileAsync(captured);
                panelProfiles.Controls.Add(pbtn);
            }

            panelRight.Controls.Add(panelAppList);
            panelRight.Controls.Add(panelSearch);
            if (profiles != null && profiles.Count > 0)
                panelRight.Controls.Add(panelProfiles);

            var splitter = new Splitter
            {
                Dock      = DockStyle.Left,
                Width     = 4,
                BackColor = C(51, 65, 85)
            };

            this.Controls.Add(panelRight);
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
            string text   = listCategories.Items[e.Index].ToString();
            bool selected = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.FillRectangle(
                new SolidBrush(selected ? C(59, 130, 246) : C(30, 41, 59)), e.Bounds);
            var sf = new System.Drawing.StringFormat
                { LineAlignment = System.Drawing.StringAlignment.Center };
            using (var font = UiFont(10F, selected ? FontStyle.Bold : FontStyle.Regular))
                e.Graphics.DrawString(text, font,
                    new SolidBrush(selected ? Color.White : C(203, 213, 225)),
                    new RectangleF(e.Bounds.X + 8, e.Bounds.Y,
                                   e.Bounds.Width - 8, e.Bounds.Height), sf);
        }

        // ─── Refresh right panel ──────────────────────────────────────────
        private void RefreshAppList()
        {
            string cat  = listCategories.SelectedItem?.ToString() ?? "Tümü";
            string search = txtSearch?.Text?.ToLowerInvariant() ?? "";
            
            var filtered = allApps.Where(a => 
                (cat == "Tümü" || a.Category == cat) &&
                (string.IsNullOrWhiteSpace(search) || a.Name.ToLowerInvariant().Contains(search) || a.Description.ToLowerInvariant().Contains(search))
            ).ToList();

            panelAppList.SuspendLayout();
            panelAppList.Controls.Clear();

            for (int i = filtered.Count - 1; i >= 0; i--)
                panelAppList.Controls.Add(BuildAppCard(filtered[i]));

            panelAppList.ResumeLayout(true);

            // Scroll sorununu düzelt: Eklenen son eleman odak alıp ekranı aşağı kaydırmasın
            if (this.IsHandleCreated)
            {
                this.BeginInvoke(new Action(() => {
                    panelAppList.AutoScrollPosition = new Point(0, 0);
                }));
            }
        }

        // ─── Build a single card ──────────────────────────────────────────
        private Panel BuildAppCard(OnlineApp app)
        {
            bool isVip = app.DownloadUrl?.IndexOf("vip.solidshare.net",
                StringComparison.OrdinalIgnoreCase) >= 0;

            var card = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = C(30, 41, 59),
                Padding   = new Padding(12, 8, 12, 8),
                Cursor    = Cursors.Default
            };

            var bar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 4,
                BackColor = isVip ? C(234, 179, 8) : C(59, 130, 246)
            };

            var lblIcon = new Label
            {
                Text      = app.IconEmoji,
                Font      = UiFont(20F),
                ForeColor = Color.White,
                Size      = new Size(44, 54),
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(10, 9)
            };

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
                Text      = isVip
                    ? $"⚡ SolidShare VIP  •  {app.SizeMB}"
                    : $"v{app.Version}  •  {app.SizeMB}",
                ForeColor = isVip ? C(234, 179, 8) : C(100, 116, 139),
                Font      = UiFont(8F),
                AutoSize  = true,
                Location  = new Point(61, 50)
            };

            var btn = new Button
            {
                Text      = isVip ? "🔐 VIP İndir" : "⬇ İndir",
                Size      = new Size(110, 38),
                BackColor = isVip ? C(161, 122, 6) : C(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = UiFont(9.5F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize          = 0;
            btn.FlatAppearance.MouseOverBackColor  = isVip ? C(234, 179, 8) : C(96, 165, 250);
            btn.FlatAppearance.MouseDownBackColor  = isVip ? C(120, 90, 4) : C(37, 99, 235);

            void PositionBtn() => btn.Location = new Point(
                card.ClientSize.Width - btn.Width - 14,
                (card.ClientSize.Height - btn.Height) / 2);
            card.Resize += (s, e) => PositionBtn();

            OnlineApp captured = app;
            btn.Click += async (s, e) => await DownloadToDatabaseAsync(captured, btn);

            card.Controls.Add(btn);
            card.Controls.Add(lblMeta);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblName);
            card.Controls.Add(lblIcon);
            card.Controls.Add(bar);
            card.PerformLayout();
            return card;
        }

        // ─── VIP Kimlik Bilgisi Diyaloğu ─────────────────────────────────
        private static bool EnsureVipCredentials()
        {
            if (!string.IsNullOrEmpty(_vipUser)) return true;

            using (var dlg = new Form())
            {
                dlg.Text             = "SolidShare VIP Kimlik Bilgileri";
                dlg.ClientSize       = new Size(320, 130);
                dlg.StartPosition    = FormStartPosition.CenterParent;
                dlg.FormBorderStyle  = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox      = false;
                dlg.MinimizeBox      = false;
                dlg.BackColor        = C(15, 23, 42);

                var lblU = new Label { Text = "Kullanıcı Adı:", ForeColor = Color.White,
                    Font = UiFont(9F), Location = new Point(12, 18), AutoSize = true };
                var txtU = new TextBox { Location = new Point(130, 15), Width = 170,
                    BackColor = C(30, 41, 59), ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle, Font = UiFont(9F) };
                var lblP = new Label { Text = "Şifre:", ForeColor = Color.White,
                    Font = UiFont(9F), Location = new Point(12, 50), AutoSize = true };
                var txtP = new TextBox { Location = new Point(130, 47), Width = 170,
                    BackColor = C(30, 41, 59), ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true,
                    Font = UiFont(9F) };
                var btnOk = new Button { Text = "Giriş", DialogResult = DialogResult.OK,
                    Location = new Point(130, 88), Size = new Size(80, 28),
                    BackColor = C(59, 130, 246), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = UiFont(9F) };
                var btnCan = new Button { Text = "İptal", DialogResult = DialogResult.Cancel,
                    Location = new Point(220, 88), Size = new Size(80, 28),
                    BackColor = C(71, 85, 105), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = UiFont(9F) };
                btnOk.FlatAppearance.BorderSize  = 0;
                btnCan.FlatAppearance.BorderSize = 0;
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCan;
                dlg.Controls.AddRange(new Control[] { lblU, txtU, lblP, txtP, btnOk, btnCan });

                if (dlg.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(txtU.Text))
                    return false;

                _vipUser = txtU.Text.Trim();
                _vipPass = txtP.Text;
                return true;
            }
        }

        // ─── Tek bir uygulamayı programlar klasörüne indirir (çekirdek) ────
        // Buton/panel/cts yönetimini YAPMAZ; çağıran taraf sarmalar. Başarıda true.
        private async Task<bool> DownloadCoreAsync(OnlineApp app, CancellationToken token)
        {
            if (!Directory.Exists(DatabaseDir))
                Directory.CreateDirectory(DatabaseDir);

            string url   = app.DownloadUrl;
            bool   isVip = url.IndexOf("vip.solidshare.net", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isVip && !EnsureVipCredentials())
                return false;

            string ext = ".exe";
            if (url.IndexOf(".msi", StringComparison.OrdinalIgnoreCase) >= 0)
                ext = ".msi";
            else if (url.IndexOf(".zip", StringComparison.OrdinalIgnoreCase) >= 0)
                ext = ".zip";

            string safeName = string.Join("_",
                app.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            string destFile = Path.Combine(DatabaseDir, safeName + ext);

            try
            {
                try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }

                using (var wc = new WebClient())
                {
                    if (isVip && !string.IsNullOrEmpty(_vipUser))
                        wc.Credentials = new System.Net.NetworkCredential(_vipUser, _vipPass);

                    wc.DownloadProgressChanged += (s, e) =>
                    {
                        if (!progressBar.IsHandleCreated) return;
                        progressBar.BeginInvoke((Action)(() =>
                        {
                            progressBar.Value = Math.Min(e.ProgressPercentage, 100);
                            long total = e.TotalBytesToReceive > 0
                                ? e.TotalBytesToReceive / (1024 * 1024) : 0;
                            long done  = e.BytesReceived / (1024 * 1024);
                            lblStatus.Text = total > 0
                                ? $"⬇ {app.Name}  —  {done} MB / {total} MB"
                                : $"⬇ {app.Name}  —  {done} MB";
                        }));
                    };

                    using (token.Register(() => wc.CancelAsync()))
                    {
                        await wc.DownloadFileTaskAsync(new Uri(url), destFile);
                    }
                }

                if (token.IsCancellationRequested)
                {
                    try { if (File.Exists(destFile)) File.Delete(destFile); } catch { }
                    return false;
                }
                return true;
            }
            catch (System.Net.WebException wex) when (
                wex.Response is System.Net.HttpWebResponse hr &&
                hr.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Hatalı VIP bilgileri — sıfırla, kullanıcı tekrar deneyebilir
                _vipUser = null;
                _vipPass = null;
                lblStatus.Text = $"❌ {app.Name}: VIP kullanıcı adı veya şifre hatalı.";
                try { if (File.Exists(destFile)) File.Delete(destFile); } catch { }
                return false;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                lblStatus.Text = $"❌ {app.Name}: {ex.Message}";
                try { if (File.Exists(destFile)) File.Delete(destFile); } catch { }
                return false;
            }
            catch
            {
                try { if (File.Exists(destFile)) File.Delete(destFile); } catch { }
                return false;
            }
        }

        // ─── Tek uygulama indir (kart butonundan) ─────────────────────────
        private async Task DownloadToDatabaseAsync(OnlineApp app, Button triggerBtn)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                MessageBox.Show("Başka bir indirme devam ediyor. Lütfen bekleyin veya durdurun.",
                    "Meşgul", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            cts = new CancellationTokenSource();
            SetAllDownloadButtonsEnabled(false);
            panelStatus.Visible = true;
            progressBar.Style   = ProgressBarStyle.Continuous;
            progressBar.Value   = 0;
            lblStatus.Text      = $"Hazırlanıyor: {app.Name}…";

            try
            {
                bool ok = await DownloadCoreAsync(app, cts.Token);
                if (ok)
                {
                    lblStatus.Text    = $"✅ {app.Name} → programlar klasörüne kaydedildi";
                    progressBar.Value = 100;
                    MessageBox.Show(
                        $"{app.Name} başarıyla indirildi!\n\nProgram 'programlar' klasörüne kaydedildi.",
                        "İndirme Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (cts.IsCancellationRequested)
                {
                    lblStatus.Text = "⛔ İndirme durduruldu.";
                }
                else
                {
                    MessageBox.Show(
                        $"{app.Name} indirilemedi. Link çalışmıyor olabilir.\n\n" +
                        "Not: Katalog linkleri GitHub'daki catalog.json dosyasından güncellenir.",
                        "İndirme Başarısız", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                cts?.Dispose();
                cts = null;
                progressBar.Style = ProgressBarStyle.Continuous;
                SetAllDownloadButtonsEnabled(true);
                await Task.Delay(3000);
                if (cts == null)
                    panelStatus.Visible = false;
            }
        }

        // ─── Profil (paket) indir: içindeki tüm uygulamaları sırayla indir ─
        private async Task DownloadProfileAsync(InstallProfile profile)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                MessageBox.Show("Başka bir indirme devam ediyor. Lütfen bekleyin veya durdurun.",
                    "Meşgul", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Profildeki adları katalogdaki uygulamalarla eşle
            var list = new List<OnlineApp>();
            foreach (var name in profile.Apps)
            {
                var match = allApps.FirstOrDefault(a =>
                    string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
                if (match != null && !list.Contains(match))
                    list.Add(match);
            }

            if (list.Count == 0)
            {
                MessageBox.Show("Bu pakette indirilebilecek uygulama bulunamadı.",
                    "Paket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string names = string.Join("\n", list.Select(a => $"• {a.Name}"));
            if (MessageBox.Show(
                    $"'{profile.Name}' paketi — {list.Count} uygulama 'programlar' klasörüne indirilecek:\n\n{names}\n\nDevam edilsin mi?",
                    $"{profile.Icon} {profile.Name} Paketi",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            cts = new CancellationTokenSource();
            SetAllDownloadButtonsEnabled(false);
            panelStatus.Visible = true;
            progressBar.Style   = ProgressBarStyle.Continuous;

            int ok = 0, fail = 0;
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (cts.IsCancellationRequested) break;
                    progressBar.Value = 0;
                    lblStatus.Text = $"({i + 1}/{list.Count}) {list[i].Name} indiriliyor…";
                    bool r = await DownloadCoreAsync(list[i], cts.Token);
                    if (r) ok++; else fail++;
                }

                if (cts.IsCancellationRequested)
                {
                    lblStatus.Text = $"⛔ Paket indirme durduruldu. ({ok} indirildi)";
                }
                else
                {
                    lblStatus.Text = $"✅ {profile.Name} paketi: {ok} indirildi, {fail} atlandı.";
                    MessageBox.Show(
                        $"'{profile.Name}' paketi tamamlandı.\n\nİndirilen: {ok}\nBaşarısız/atlanan: {fail}\n\n" +
                        "Programlar 'programlar' klasörüne eklendi; kurmak için ana penceredeki 'Tam Kurulum' veya 'Seçilenleri Kur' kullanabilirsiniz.",
                        "Paket İndirme Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            finally
            {
                cts?.Dispose();
                cts = null;
                progressBar.Style = ProgressBarStyle.Continuous;
                SetAllDownloadButtonsEnabled(true);
                await Task.Delay(3000);
                if (cts == null)
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
