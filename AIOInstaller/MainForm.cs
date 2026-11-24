using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AIOInstaller.Ui;

namespace AIOInstaller
{
    public partial class MainForm : Form
    {
        private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);

        private List<ProgramInfo> programs;
        private DataGridView gridPrograms;
        private ModernButton btnSelectAll;
        private ModernButton btnInstallSelected;
        private ModernButton btnDeselectAll;

        private Card installStatusPanel;
        private ModernProgress progressBarInstall;
        private Label lblInstallStatus;
        private bool isInstalling;
        private CancellationTokenSource installCts;
        private ModernButton btnStopInstall;

        private ModernButton btnFullSetup;
        private ModernButton btnAdd;
        private ModernButton btnRemove;
        private ModernButton btnOpenDatabase;
        private ModernButton btnSync;
        private ModernButton btnActivate;
        private ModernButton btnPowerShell;
        private ModernButton btnDefenderOff;
        private ModernButton btnDefenderOn;
        private ModernButton btnUpdateBlock;
        private ModernButton btnUpdateAllow;
        private ModernButton btnBloatware;
        private Label lblTotalCount;
        private Label lblSelection;
        private DarkTextBox txtSearch;
        private Panel emptyState;

        // Kabuk (sol menü + sayfalar)
        private Panel sidebar;
        private Panel brandPanel;
        private Label lblCredit;
        private NavItem navPrograms;
        private NavItem navOnline;
        private NavItem navTools;
        private NavItem navSettings;
        private Panel pagePrograms;
        private Panel pageTools;
        private SettingsPage pageSettings;
        private bool sidebarCompact;
        private readonly ToolTip navTips = new ToolTip();

        private const string ColSelect = "colSelect";
        private const string ColName = "colName";
        private const string ColSize = "colSize";
        private const string ColAdded = "colAdded";
        private const string ColStatus = "colStatus";

        private const string StatusWaiting = "Bekliyor";

        public MainForm()
        {
            InitializeComponent();
            LoadPrograms();
            ShowPage(pagePrograms);
            Shown += (s, e) => { gridPrograms.ClearSelection(); gridPrograms.CurrentCell = null; };
        }

        // ══════════════════════════════════════════════════════════════════
        // ARAYÜZ
        // ══════════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "AIO Yazılım Yükleyici";
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(1200, 780);
            MinimumSize = new Size(880, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body(9.75f);
            DoubleBuffered = true;
            Theme.ApplyDarkTitleBar(this);
            try { Icon = CreateAppIcon(); } catch { }

            BuildSidebar();

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Bg,
                Padding = new Padding(32, 26, 32, 22)
            };

            BuildStatusDock();

            var pagesHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
            pagePrograms = BuildProgramsPage();
            pageTools = BuildToolsPage();
            pageSettings = new SettingsPage { Dock = DockStyle.Fill, Visible = false };
            pagesHost.Controls.Add(pagePrograms);
            pagesHost.Controls.Add(pageTools);
            pagesHost.Controls.Add(pageSettings);

            content.Controls.Add(pagesHost);          // Fill
            content.Controls.Add(installStatusPanel); // Bottom

            Controls.Add(content);   // Fill
            Controls.Add(sidebar);   // Left

            Resize += (s, e) => ApplySidebarMode(ClientSize.Width < 1040);
            ApplySidebarMode(ClientSize.Width < 1040);

            KeyPreview = true;
            KeyDown += MainForm_KeyDown;

            ResumeLayout(false);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+F: aramaya odaklan
            if (e.Control && e.KeyCode == Keys.F && pagePrograms.Visible)
            {
                txtSearch.Inner.Focus();
                e.SuppressKeyPress = true;
            }
        }

        // ── Sol menü ─────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 248,
                BackColor = Theme.Sidebar,
                Padding = new Padding(0, 0, 1, 16) // sağdaki 1px kenar çizgisi görünsün
            };
            sidebar.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border))
                    e.Graphics.DrawLine(p, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };

            brandPanel = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Theme.Sidebar };
            brandPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Theme.HighQuality(g);
                int x = sidebarCompact ? (brandPanel.Width - 40) / 2 : 22;
                var logo = new RectangleF(x, 26, 40, 40);
                using (var path = Theme.RoundRect(logo, 11))
                using (var lg = new System.Drawing.Drawing2D.LinearGradientBrush(logo, Theme.Accent, Theme.HeroFrom, 45f))
                    g.FillPath(lg, path);
                Theme.DrawText(g, "A", Theme.Heading(15f), Rectangle.Round(logo), Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                if (!sidebarCompact)
                {
                    Theme.DrawText(g, "AIO", Theme.Heading(15f), new Rectangle(74, 24, 150, 24), Theme.Text);
                    Theme.DrawText(g, "Yazılım Yükleyici", Theme.Body(8.75f), new Rectangle(74, 47, 160, 20), Theme.TextMuted);
                }
            };

            var navSection = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "MENÜ",
                Font = Theme.Body(7.75f, FontStyle.Bold),
                ForeColor = Theme.TextDim,
                Padding = new Padding(24, 0, 0, 4),
                TextAlign = ContentAlignment.BottomLeft
            };

            navPrograms = MakeNav("Programlar", Theme.Glyph.Programs);
            navOnline = MakeNav("Online İndir", Theme.Glyph.Download);
            navOnline.ShowExternal = true;
            navTools = MakeNav("Sistem Araçları", Theme.Glyph.Tools);
            navSettings = MakeNav("Ayarlar", Theme.Glyph.Settings);

            navPrograms.Click += (s, e) => ShowPage(pagePrograms);
            navTools.Click += (s, e) => ShowPage(pageTools);
            navSettings.Click += (s, e) => ShowPage(pageSettings);
            navOnline.Click += BtnDownload_Click;

            // Alt kısım: Tam Kurulum (her sayfadan erişilir)
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 118, BackColor = Theme.Sidebar, Padding = new Padding(14, 0, 14, 0) };
            btnFullSetup = new ModernButton
            {
                Kind = ButtonKind.Hero,
                Text = "Tam Kurulum",
                Subtitle = "Bilgisayarı tek tıkla hazırla",
                Glyph = Theme.Glyph.Rocket,
                Font = Theme.Heading(12f),
                CornerRadius = 12,
                Dock = DockStyle.Top,
                Height = 66
            };
            btnFullSetup.Click += BtnFullSetup_Click;

            lblCredit = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                Text = "made by demircan",
                Font = Theme.Body(8f, FontStyle.Italic),
                ForeColor = Theme.TextDim,
                TextAlign = ContentAlignment.BottomCenter
            };
            bottom.Controls.Add(btnFullSetup);
            bottom.Controls.Add(lblCredit);

            // Dock sırası: son eklenen en üstte
            sidebar.Controls.Add(bottom);
            sidebar.Controls.Add(navSettings);
            sidebar.Controls.Add(navTools);
            sidebar.Controls.Add(navOnline);
            sidebar.Controls.Add(navPrograms);
            sidebar.Controls.Add(navSection);
            sidebar.Controls.Add(brandPanel);
        }

        private NavItem MakeNav(string text, string glyph)
        {
            return new NavItem { Text = text, Glyph = glyph, Dock = DockStyle.Top, Height = 46 };
        }

        private void ApplySidebarMode(bool compact)
        {
            if (sidebar == null || (compact == sidebarCompact && sidebar.Width > 0 && sidebar.Tag != null)) return;
            sidebar.Tag = true;
            sidebarCompact = compact;
            sidebar.Width = compact ? 84 : 248;
            foreach (var nav in new[] { navPrograms, navOnline, navTools, navSettings })
            {
                nav.Compact = compact;
                navTips.SetToolTip(nav, compact ? nav.Text : null);
            }
            navTips.SetToolTip(btnFullSetup, compact ? "Tam Kurulum" : null);

            btnFullSetup.Text = compact ? "" : "Tam Kurulum";
            btnFullSetup.Subtitle = compact ? null : "Bilgisayarı tek tıkla hazırla";
            lblCredit.Visible = !compact;
            sidebar.Controls[0].Padding = compact ? new Padding(12, 0, 12, 0) : new Padding(14, 0, 14, 0);
            brandPanel.Invalidate();
            sidebar.Invalidate();
        }

        private void ShowPage(Control page)
        {
            foreach (var p in new Control[] { pagePrograms, pageTools, pageSettings })
                p.Visible = p == page;

            navPrograms.Selected = page == pagePrograms;
            navTools.Selected = page == pageTools;
            navSettings.Selected = page == pageSettings;

            if (page == pageSettings) pageSettings.RefreshEbys();
        }

        /// <summary>Sayfa başlığı: büyük başlık + açıklama, sağda isteğe bağlı kontrol.</summary>
        private static Panel BuildPageHeader(string title, string subtitle, Control right)
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Theme.Bg };
            var lblTitle = UiFactory.Label(title, Theme.Heading(22f), Theme.Text);
            lblTitle.Location = new Point(0, 0);
            var lblSub = UiFactory.Label(subtitle, Theme.Body(10f), Theme.TextMuted);
            lblSub.Location = new Point(2, 40);
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSub);

            if (right != null)
            {
                right.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                header.Controls.Add(right);
                header.Layout += (s, e) => right.Location = new Point(header.Width - right.Width, 6);
            }
            return header;
        }

        // ── Kurulum durum çubuğu (her sayfanın altında) ─────────────────────
        private void BuildStatusDock()
        {
            installStatusPanel = new Card
            {
                Dock = DockStyle.Bottom,
                Height = 92,
                Padding = new Padding(22, 16, 18, 16),
                Visible = false,
                FillColor = Theme.Surface,
                BorderColor = Theme.BorderStrong
            };

            var spacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Theme.Bg };

            lblInstallStatus = new Label
            {
                Font = Theme.Body(10.5f, FontStyle.Bold),
                ForeColor = Theme.Text,
                BackColor = Theme.Surface,
                AutoEllipsis = true,
                UseMnemonic = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            progressBarInstall = new ModernProgress { Height = 8, BackColor = Theme.Surface };

            btnStopInstall = new ModernButton
            {
                Text = "Durdur",
                Glyph = Theme.Glyph.Stop,
                Kind = ButtonKind.Danger,
                Size = new Size(118, 40),
                Enabled = false
            };
            btnStopInstall.Click += BtnStopInstall_Click;

            installStatusPanel.Controls.Add(lblInstallStatus);
            installStatusPanel.Controls.Add(progressBarInstall);
            installStatusPanel.Controls.Add(btnStopInstall);
            installStatusPanel.Layout += (s, e) =>
            {
                var r = installStatusPanel.DisplayRectangle;
                btnStopInstall.Location = new Point(r.Right - btnStopInstall.Width, r.Top + (r.Height - btnStopInstall.Height) / 2);
                int w = btnStopInstall.Left - r.Left - 20;
                lblInstallStatus.SetBounds(r.Left, r.Top, w, 26);
                progressBarInstall.SetBounds(r.Left, r.Bottom - progressBarInstall.Height - 4, w, progressBarInstall.Height);
            };
            installStatusPanel.VisibleChanged += (s, e) => spacer.Visible = installStatusPanel.Visible;
        }

        // ── Programlar sayfası ─────────────────────────────────────────────
        private Panel BuildProgramsPage()
        {
            var page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };

            lblTotalCount = new Label
            {
                AutoSize = false,
                Size = new Size(150, 34),
                Font = Theme.Body(9.5f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Theme.Bg
            };
            var header = BuildPageHeader("Programlar",
                "programlar klasöründeki kurulum dosyaları — işaretle, sırayla kur.", lblTotalCount);

            // Araç çubuğu: arama + işlemler
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Theme.Bg };
            txtSearch = new DarkTextBox(Theme.Glyph.Search)
            {
                Placeholder = "Program ara…  (Ctrl+F)",
                Size = new Size(300, 40),
                Location = new Point(0, 2)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            btnAdd = new ModernButton { Text = "Program Ekle", Glyph = Theme.Glyph.Add, Kind = ButtonKind.Primary, Height = 40 }.AutoWidth();
            btnAdd.Click += BtnAdd_Click;
            btnRemove = new ModernButton { Text = "Sil", Glyph = Theme.Glyph.Delete, Kind = ButtonKind.Danger, Height = 40 }.AutoWidth(84);
            btnRemove.Click += BtnRemove_Click;
            btnOpenDatabase = new ModernButton { Text = "Klasör", Glyph = Theme.Glyph.Folder, Height = 40 }.AutoWidth();
            btnOpenDatabase.Click += BtnOpenDatabase_Click;
            btnSync = new ModernButton { Glyph = Theme.Glyph.Refresh, Height = 40, Width = 44 };
            btnSync.Click += BtnSync_Click;
            new ToolTip().SetToolTip(btnSync, "Klasörü tara ve listeyi yenile");

            var toolButtons = new ModernButton[] { btnSync, btnOpenDatabase, btnRemove, btnAdd };
            toolbar.Controls.Add(txtSearch);
            foreach (var b in toolButtons) toolbar.Controls.Add(b);
            toolbar.Layout += (s, e) =>
            {
                int x = toolbar.Width;
                foreach (var b in toolButtons)
                {
                    x -= b.Width;
                    b.Location = new Point(x, 2);
                    x -= 8;
                }
                txtSearch.Width = Math.Max(180, Math.Min(340, x - 16));
            };

            // Liste kartı
            var gridCard = new Card { Dock = DockStyle.Fill, Padding = new Padding(1, 6, 1, 6) };
            BuildGrid();
            emptyState = BuildEmptyState();
            gridCard.Controls.Add(gridPrograms);
            gridCard.Controls.Add(emptyState);

            // Alt eylem çubuğu
            var actionBar = new Panel { Dock = DockStyle.Bottom, Height = 66, BackColor = Theme.Bg };
            btnSelectAll = new ModernButton { Text = "Tümünü Seç", Glyph = Theme.Glyph.SelectAll, Kind = ButtonKind.Ghost, Height = 40 }.AutoWidth();
            btnSelectAll.Click += BtnSelectAll_Click;
            btnDeselectAll = new ModernButton { Text = "Seçimi Kaldır", Glyph = Theme.Glyph.Close, Kind = ButtonKind.Ghost, Height = 40 }.AutoWidth();
            btnDeselectAll.Click += BtnDeselectAll_Click;
            lblSelection = new Label
            {
                AutoSize = false,
                Size = new Size(200, 40),
                Font = Theme.Body(9.5f),
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.Bg
            };
            btnInstallSelected = new ModernButton
            {
                Text = "Seçilenleri Kur",
                Glyph = Theme.Glyph.Play,
                Kind = ButtonKind.Primary,
                Height = 44,
                Font = Theme.Body(10.5f, FontStyle.Bold)
            }.AutoWidth(190);
            btnInstallSelected.Click += BtnInstallSelected_Click;

            actionBar.Controls.AddRange(new Control[] { btnSelectAll, btnDeselectAll, lblSelection, btnInstallSelected });
            actionBar.Layout += (s, e) =>
            {
                int y = 16;
                btnSelectAll.Location = new Point(0, y + 2);
                btnDeselectAll.Location = new Point(btnSelectAll.Right + 6, y + 2);
                lblSelection.Location = new Point(btnDeselectAll.Right + 14, y + 2);
                btnInstallSelected.Location = new Point(actionBar.Width - btnInstallSelected.Width, y);
                lblSelection.Width = Math.Max(0, btnInstallSelected.Left - lblSelection.Left - 10);
            };

            page.Controls.Add(gridCard);   // Fill
            page.Controls.Add(actionBar);  // Bottom
            page.Controls.Add(toolbar);    // Top (alt)
            page.Controls.Add(header);     // Top (üst)
            return page;
        }

        private Panel BuildEmptyState()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Visible = false };
            var badge = new IconBadge(Theme.Glyph.Programs, Theme.Accent) { Size = new Size(64, 64) };
            var title = UiFactory.Label("Henüz program yok", Theme.Heading(15f), Theme.Text);
            var sub = UiFactory.Label("Kurulum dosyalarını (.exe / .msi) eklemek için \"Program Ekle\"ye tıkla\nveya dosyaları programlar klasörüne kopyalayıp yenile.",
                Theme.Body(10f), Theme.TextMuted);
            sub.TextAlign = ContentAlignment.MiddleCenter;
            var add = new ModernButton { Text = "Program Ekle", Glyph = Theme.Glyph.Add, Kind = ButtonKind.Primary, Height = 40 }.AutoWidth();
            add.Click += BtnAdd_Click;
            panel.Controls.AddRange(new Control[] { badge, title, sub, add });
            panel.Layout += (s, e) =>
            {
                int cx = panel.Width / 2;
                int top = Math.Max(20, panel.Height / 2 - 110);
                badge.Location = new Point(cx - badge.Width / 2, top);
                title.Location = new Point(cx - title.Width / 2, badge.Bottom + 18);
                sub.Location = new Point(cx - sub.Width / 2, title.Bottom + 8);
                add.Location = new Point(cx - add.Width / 2, sub.Bottom + 20);
            };
            return panel;
        }

        private void BuildGrid()
        {
            gridPrograms = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                GridColor = Theme.Border,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                EnableHeadersVisualStyles = false,
                Font = Theme.Body(10f),
                ForeColor = Theme.Text,
                ColumnHeadersHeight = 42,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EditMode = DataGridViewEditMode.EditOnEnter,
                ShowCellToolTips = true,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                StandardTab = true
            };
            gridPrograms.RowTemplate.Height = 48;
            gridPrograms.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                SelectionBackColor = Theme.SurfaceHover,
                SelectionForeColor = Theme.Text,
                Padding = new Padding(10, 0, 10, 0)
            };
            gridPrograms.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Surface };
            gridPrograms.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextDim,
                Font = Theme.Body(8.25f, FontStyle.Bold),
                SelectionBackColor = Theme.Surface,
                SelectionForeColor = Theme.TextDim,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };

            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = ColSelect, HeaderText = "", Width = 52,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, SortMode = DataGridViewColumnSortMode.NotSortable
            };
            var colName = new DataGridViewTextBoxColumn
            {
                Name = ColName, HeaderText = "PROGRAM", MinimumWidth = 160,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
            };
            var colSize = new DataGridViewTextBoxColumn
            {
                Name = ColSize, HeaderText = "BOYUT", Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
            };
            var colAdded = new DataGridViewTextBoxColumn
            {
                Name = ColAdded, HeaderText = "EKLENME", Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
            };
            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = ColStatus, HeaderText = "DURUM", Width = 210,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colSize.DefaultCellStyle.ForeColor = Theme.TextMuted;
            colSize.DefaultCellStyle.SelectionForeColor = Theme.TextMuted;
            colAdded.DefaultCellStyle.ForeColor = Theme.TextMuted;
            colAdded.DefaultCellStyle.SelectionForeColor = Theme.TextMuted;

            gridPrograms.Columns.AddRange(new DataGridViewColumn[] { colSelect, colName, colSize, colAdded, colStatus });

            gridPrograms.CurrentCellDirtyStateChanged += GridPrograms_CurrentCellDirtyStateChanged;
            gridPrograms.CellDoubleClick += GridPrograms_CellDoubleClick;
            gridPrograms.CellPainting += GridPrograms_CellPainting;
            gridPrograms.RowPostPaint += (s, e) =>
            {
                // İnce satır ayırıcı (kart kenarlarına değmeden)
                int y = e.RowBounds.Bottom - 1;
                using (var p = new Pen(Theme.Border))
                    e.Graphics.DrawLine(p, e.RowBounds.Left + 14, y, e.RowBounds.Right - 14, y);
            };
            gridPrograms.CellValueChanged += (s, e) => { if (e.ColumnIndex == 0) UpdateSelectionInfo(); };
            gridPrograms.Resize += (s, e) => UpdateResponsiveColumns();
            Theme.DarkScrollbars(gridPrograms);
        }

        private void UpdateResponsiveColumns()
        {
            // Dar pencerede tarih sütununu gizle ki program adı sığsın
            gridPrograms.Columns[ColAdded].Visible = gridPrograms.Width > 760;
            gridPrograms.Columns[ColSize].Visible = gridPrograms.Width > 560;
        }

        private void GridPrograms_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var g = e.Graphics;
            string colName = gridPrograms.Columns[e.ColumnIndex].Name;

            if (colName == ColSelect)
            {
                e.PaintBackground(e.ClipBounds, true);
                Theme.HighQuality(g);
                bool on = IsRowChecked(gridPrograms.Rows[e.RowIndex]);
                var box = new RectangleF(e.CellBounds.X + (e.CellBounds.Width - 20) / 2f,
                    e.CellBounds.Y + (e.CellBounds.Height - 20) / 2f, 20, 20);
                using (var path = Theme.RoundRect(box, 6))
                {
                    using (var b = new SolidBrush(on ? Theme.Accent : Theme.Bg)) g.FillPath(b, path);
                    using (var p = new Pen(on ? Theme.Accent : Theme.BorderStrong, 1.5f)) g.DrawPath(p, path);
                }
                if (on)
                {
                    Theme.DrawText(g, Theme.Glyph.Check, Theme.Icon(9f), Rectangle.Round(box), Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                e.Handled = true;
                return;
            }

            if (colName == ColStatus)
            {
                e.PaintBackground(e.ClipBounds, true);
                string text = Convert.ToString(e.Value ?? "");
                if (text.Length == 0) { e.Handled = true; return; }

                Theme.HighQuality(g);
                var cell = gridPrograms.Rows[e.RowIndex].Cells[e.ColumnIndex];
                Color tone = cell.Tag is Color ? (Color)cell.Tag : Theme.TextDim;
                var font = Theme.Body(8.75f, FontStyle.Bold);
                Size sz = TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding);
                int maxW = e.CellBounds.Width - 20;
                int w = Math.Min(maxW, sz.Width + 30);
                var pill = new RectangleF(e.CellBounds.X + 10, e.CellBounds.Y + (e.CellBounds.Height - 26) / 2f, w, 26);
                using (var path = Theme.RoundRect(pill, 13))
                using (var b = new SolidBrush(Theme.WithAlpha(tone, 34)))
                    g.FillPath(b, path);
                using (var dot = new SolidBrush(tone))
                    g.FillEllipse(dot, pill.X + 10, pill.Y + 10, 6, 6);
                Theme.DrawText(g, text, font, new Rectangle((int)pill.X + 21, (int)pill.Y, (int)pill.Width - 26, (int)pill.Height), tone,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                e.Handled = true;
            }
        }

        // Programatik icon oluşturma
        private Icon CreateAppIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                Theme.HighQuality(g);
                using (var path = Theme.RoundRect(new RectangleF(1, 1, 30, 30), 8))
                using (var lg = new System.Drawing.Drawing2D.LinearGradientBrush(new RectangleF(0, 0, 32, 32), Theme.Accent, Theme.HeroFrom, 45f))
                    g.FillPath(lg, path);
                using (var font = new Font("Arial", 14, FontStyle.Bold, GraphicsUnit.Point))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("A", font, Brushes.White, new RectangleF(0, 1, 32, 32), sf);
                }
                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        // ── Sistem Araçları sayfası ───────────────────────────────────────
        private Panel BuildToolsPage()
        {
            var page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Visible = false };
            var header = BuildPageHeader("Sistem Araçları",
                "Tek seferlik sistem ayarları. Hepsi yönetici yetkisiyle çalışır ve önce onay ister.", null);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Bg,
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnDefenderOff = new ModernButton { Text = "Kapat", Glyph = Theme.Glyph.Close, Kind = ButtonKind.Danger, Height = 38 }.AutoWidth(110);
            btnDefenderOff.Click += BtnDefenderOff_Click;
            btnDefenderOn = new ModernButton { Text = "Aç", Glyph = Theme.Glyph.Check, Kind = ButtonKind.Success, Height = 38 }.AutoWidth(110);
            btnDefenderOn.Click += BtnDefenderOn_Click;

            btnUpdateBlock = new ModernButton { Text = "Engelle", Glyph = Theme.Glyph.Close, Kind = ButtonKind.Danger, Height = 38 }.AutoWidth(110);
            btnUpdateBlock.Click += BtnUpdateBlock_Click;
            btnUpdateAllow = new ModernButton { Text = "Aç", Glyph = Theme.Glyph.Check, Kind = ButtonKind.Success, Height = 38 }.AutoWidth(110);
            btnUpdateAllow.Click += BtnUpdateAllow_Click;

            btnBloatware = new ModernButton { Text = "Temizle", Glyph = Theme.Glyph.Broom, Kind = ButtonKind.Primary, Height = 38 }.AutoWidth(120);
            btnBloatware.Click += BtnBloatware_Click;

            btnPowerShell = new ModernButton { Text = "Uygula", Glyph = Theme.Glyph.Terminal, Kind = ButtonKind.Primary, Height = 38 }.AutoWidth(120);
            btnPowerShell.Click += BtnPowerShell_Click;

            btnActivate = new ModernButton { Text = "Çalıştır", Glyph = Theme.Glyph.Play, Kind = ButtonKind.Secondary, Height = 38 }.AutoWidth(120);
            btnActivate.Click += BtnActivate_Click;

            flow.Controls.Add(ToolCard(Theme.Glyph.Shield, Theme.Danger, "Windows Defender",
                "Gerçek zamanlı korumayı kapat veya geri aç. Kurulumlarda engellemeyi önler.",
                btnDefenderOff, btnDefenderOn));
            flow.Controls.Add(ToolCard(Theme.Glyph.Update, Theme.Warning, "Windows Update",
                "Güncelleme servislerini (wuauserv, UsoSvc, BITS) durdur ya da yeniden başlat.",
                btnUpdateBlock, btnUpdateAllow));
            flow.Controls.Add(ToolCard(Theme.Glyph.Broom, Color.FromArgb(192, 132, 252), "Bloatware Temizle",
                "Xbox, Haritalar, Skype, Solitaire gibi gereksiz uygulamaları kaldırır. Mağaza ve Hesap Makinesi kalır.",
                btnBloatware));
            flow.Controls.Add(ToolCard(Theme.Glyph.Share, Theme.Info, "Ağ Paylaşımı (SMB)",
                "Misafir erişimine izin verir, SMB imza zorunluluğunu kaldırır. Eski NAS ve yazıcılar için.",
                btnPowerShell));
            flow.Controls.Add(ToolCard(Theme.Glyph.Key, Theme.Success, "Windows / Office Etkinleştir",
                "Microsoft Activation Scripts (massgrave.dev) yönetici PowerShell penceresinde açılır.",
                btnActivate));

            Theme.DarkScrollbars(flow);
            page.Controls.Add(flow);
            page.Controls.Add(header);
            return page;
        }

        private static Card ToolCard(string glyph, Color tint, string title, string description, params ModernButton[] actions)
        {
            var card = new Card { Size = new Size(352, 214), Margin = new Padding(0, 0, 18, 18), Padding = new Padding(22) };
            var badge = new IconBadge(glyph, tint) { Location = new Point(22, 22), Size = new Size(44, 44) };
            var lblTitle = UiFactory.Label(title, Theme.Heading(13f), Theme.Text);
            lblTitle.Location = new Point(80, 32);
            var lblDesc = new Label
            {
                Text = description,
                Font = Theme.Body(9.5f),
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Surface,
                UseMnemonic = false,
                AutoSize = false,
                Location = new Point(22, 80),
                Size = new Size(308, 62)
            };
            card.Controls.Add(badge);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblDesc);

            int x = 22;
            foreach (var b in actions)
            {
                b.Location = new Point(x, 156);
                card.Controls.Add(b);
                x += b.Width + 10;
            }
            return card;
        }

        // ══════════════════════════════════════════════════════════════════
        // LİSTE
        // ══════════════════════════════════════════════════════════════════

        private void LoadPrograms()
        {
            programs = ProgramManager.LoadPrograms();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            gridPrograms.SuspendLayout();
            gridPrograms.Rows.Clear();
            lblTotalCount.Text = programs.Count + " program";

            foreach (var program in programs.OrderBy(p => p.Name))
            {
                try
                {
                    bool fileExists = File.Exists(
                        System.IO.Path.Combine(ProgramManager.DatabaseFolder, program.FileName ?? ""));

                    int idx = gridPrograms.Rows.Add(
                        false,
                        program.Name ?? "(isimsiz)",
                        ProgramManager.FormatFileSize(program.FileSize),
                        program.AddedDate.ToString("dd.MM.yyyy HH:mm"),
                        fileExists ? StatusWaiting : "Dosya yok"
                    );

                    var row = gridPrograms.Rows[idx];
                    row.Tag = program;
                    row.Cells[ColStatus].Tag = fileExists ? Theme.TextDim : Theme.Danger;
                    row.Cells[ColName].ToolTipText = program.FileName;
                }
                catch { /* Satır eklenirken hata olursa atla */ }
            }

            gridPrograms.ResumeLayout();
            emptyState.Visible = programs.Count == 0;
            gridPrograms.Visible = programs.Count > 0;
            ApplyFilter();
            UpdateSelectionInfo();
        }

        private void ApplyFilter()
        {
            if (gridPrograms == null) return;
            string q = (txtSearch.Text ?? "").Trim();
            var culture = new CultureInfo("tr-TR");
            gridPrograms.CurrentCell = null;
            foreach (DataGridViewRow row in gridPrograms.Rows)
            {
                string name = Convert.ToString(row.Cells[ColName].Value ?? "");
                row.Visible = q.Length == 0 || culture.CompareInfo.IndexOf(name, q, CompareOptions.IgnoreCase) >= 0;
            }
        }

        private void UpdateSelectionInfo()
        {
            if (lblSelection == null) return;
            int count = gridPrograms.Rows.Cast<DataGridViewRow>().Count(r => r.Tag != null && IsRowChecked(r));
            lblSelection.Text = count == 0 ? "Kurmak için programları işaretle" : count + " program seçildi";
            lblSelection.ForeColor = count == 0 ? Theme.TextDim : Theme.Text;
            btnInstallSelected.Text = count == 0 ? "Seçilenleri Kur" : "Seçilenleri Kur (" + count + ")";
            btnInstallSelected.AutoWidth(190);
            btnInstallSelected.Parent?.PerformLayout();
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SetUiEnabled(false);
                        installStatusPanel.Visible = true;
                        btnStopInstall.Enabled = false; // Ekleme sırasında durdurma şimdilik yok
                        progressBarInstall.Minimum = 0;
                        progressBarInstall.Maximum = openFileDialog.FileNames.Length;
                        progressBarInstall.Value = 0;

                        var progress = new Progress<(int current, string fileName)>(p =>
                        {
                            lblInstallStatus.Text = $"({p.current}/{openFileDialog.FileNames.Length}) Ekleniyor: {p.fileName}";
                            progressBarInstall.Value = p.current;
                        });

                        var result = await ProgramManager.AddProgramsAsync(openFileDialog.FileNames, progress);

                        // Önce yeni eklenenler için listeyi yenile
                        if (result.added > 0)
                        {
                            LoadPrograms();
                        }

                        // Mesajları oluştur
                        var messages = new System.Text.StringBuilder();

                        if (result.added > 0)
                            messages.AppendLine($"✅ {result.added} program başarıyla eklendi.");

                        if (result.duplicates.Count > 0)
                        {
                            messages.AppendLine($"\n⚠ {result.duplicates.Count} program zaten mevcut (atlandı):");
                            foreach (var dup in result.duplicates)
                                messages.AppendLine($"  • {dup}");
                        }

                        if (messages.Length > 0)
                        {
                            Dialogs.Show(messages.ToString().Trim(),
                                result.duplicates.Count > 0 ? "Ekleme Tamamlandı (Uyarılar Var)" : "Başarılı",
                                MessageBoxButtons.OK,
                                result.duplicates.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        Dialogs.Show($"Hata oluştu: {ex.Message}", "Hata", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        installStatusPanel.Visible = false;
                        SetUiEnabled(true);
                    }
                }
            }
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            gridPrograms.EndEdit();
            foreach (DataGridViewRow row in gridPrograms.Rows)
            {
                if (row.Tag != null && row.Visible)
                {
                    row.Cells[ColSelect].Value = true;
                }
            }
        }

        private void BtnDeselectAll_Click(object sender, EventArgs e)
        {
            gridPrograms.EndEdit();
            foreach (DataGridViewRow row in gridPrograms.Rows)
            {
                row.Cells[ColSelect].Value = false;
            }
        }

        private void BtnInstallSelected_Click(object sender, EventArgs e)
        {
            _ = InstallSelectedProgramsAsync();
        }

        private void GridPrograms_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridPrograms.SelectedRows.Count > 0)
            {
                InstallSelectedProgram();
            }
        }

        private void InstallSelectedProgram()
        {
            if (gridPrograms.SelectedRows.Count == 0)
            {
                Dialogs.Show("Lütfen yüklenecek bir program seçin!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = gridPrograms.SelectedRows[0];
            if (selectedRow.Tag == null)
            {
                return;
            }

            ProgramInfo program = selectedRow.Tag as ProgramInfo;
            if (program != null)
            {
                DialogResult result = Dialogs.Show(
                    $"{program.Name} programını yüklemek istediğinizden emin misiniz?",
                    "Onay",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    ProgramManager.InstallProgram(program);
                }
            }
        }

        private async Task InstallSelectedProgramsAsync()
        {
            if (isInstalling)
            {
                return;
            }

            gridPrograms.EndEdit();

            // Yukarıdan aşağı (gridde göründüğü sırayla) seçilenleri al (snapshot)
            var plannedRows = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in gridPrograms.Rows)
            {
                if (row.Tag == null) continue;
                if (IsRowChecked(row)) plannedRows.Add(row);
            }

            if (plannedRows.Count == 0)
            {
                Dialogs.Show("Lütfen yüklenecek programları seçin!", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var checkedPrograms = plannedRows.Select(r => (ProgramInfo)r.Tag).ToList();
            string programList = string.Join("\n", checkedPrograms.Select(p => $"• {p.Name}"));
            DialogResult result = Dialogs.Show(
                $"Seçilen {checkedPrograms.Count} programı yukarıdan aşağı sırayla yüklemek istediğinizden emin misiniz?\n\n{programList}\n\nNot: Kurulum sırasında tik kaldırırsanız o program atlanır.",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
            {
                return;
            }

            // UI'ı hemen güncellemek için DoEvents
            Application.DoEvents();

            try
            {
                isInstalling = true;
                installCts = new CancellationTokenSource();
                SetUiEnabled(false);

                installStatusPanel.Visible = true;
                btnStopInstall.Enabled = true;

                int successCount = await RunInstallLoopAsync(plannedRows);

                if (installCts.IsCancellationRequested)
                {
                    lblInstallStatus.Text = "Kurulum durduruldu. (Sıradakiler iptal edildi)";
                }
                else
                {
                    lblInstallStatus.Text = $"Bitti: {successCount}/{plannedRows.Count} program başarıyla kuruldu.";
                    Dialogs.Show(
                        $"{successCount}/{plannedRows.Count} program başarıyla kuruldu. Detay için 'Durum' sütununa bakabilirsiniz." +
                        (rebootRequired ? "\n\nBazı programlar için bilgisayarın yeniden başlatılması gerekiyor." : ""),
                        "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            finally
            {
                isInstalling = false;
                if (installCts != null)
                {
                    installCts.Dispose();
                    installCts = null;
                }
                btnStopInstall.Enabled = false;
                SetUiEnabled(true);
            }
        }

        /// <summary>Son kurulum turunda en az bir program yeniden başlatma istedi mi (3010/1641).</summary>
        private bool rebootRequired;

        /// <summary>
        /// Verilen satırları yukarıdan aşağı sırayla kurar. Çağıran taraf isInstalling,
        /// installCts ve UI durumunu (panel/buton) ayarlamış olmalıdır. Başarıyla kurulan
        /// (çıkış kodu 0/3010/1641 veya takip edilemeyen) program sayısını döndürür. Hem "Seçilenleri Kur" hem de "Tam Kurulum" bunu kullanır.
        /// </summary>
        private async Task<int> RunInstallLoopAsync(List<DataGridViewRow> plannedRows, string statusPrefix = "")
        {
            progressBarInstall.Style = ProgressBarStyle.Continuous;
            progressBarInstall.Minimum = 0;
            progressBarInstall.Maximum = plannedRows.Count;
            progressBarInstall.Value = 0;

            int successCount = 0;
            rebootRequired = false;
            for (int i = 0; i < plannedRows.Count; i++)
            {
                if (installCts.IsCancellationRequested)
                {
                    break;
                }

                var row = plannedRows[i];
                var program = (ProgramInfo)row.Tag;

                // Kullanıcı sıradaki programın tikini kaldırdıysa es geç
                if (!IsRowChecked(row))
                {
                    SetRowStatus(row, "Atlandı", Theme.TextDim);
                    progressBarInstall.Value = i + 1;
                    continue;
                }

                SetRowStatus(row, "Kuruluyor...", Theme.Accent);
                lblInstallStatus.Text = statusPrefix + $"({i + 1}/{plannedRows.Count}) Kuruluyor: {program.Name}";
                progressBarInstall.Value = i;

                // 1618 (başka kurulum sürüyor — ör. arka planda Windows Update) için birkaç kez bekleyip tekrar dene
                const int maxBusyRetries = 3;
                bool stopped = false;
                for (int attempt = 0; ; attempt++)
                {
                    Process proc;
                    bool started = ProgramManager.StartInstallProcess(program, out proc);
                    if (!started)
                    {
                        SetRowStatus(row, "Hata", Theme.Danger);
                        break;
                    }

                    // Process nesnesi alınamadıysa takip edemeyiz: sadece başlatıldı say
                    if (proc == null)
                    {
                        successCount++;
                        SetRowStatus(row, "Başlatıldı", Theme.Success);
                        break;
                    }

                    // İşlem bitene kadar bekle (iptal olursa best-effort durdur)
                    while (!proc.HasExited)
                    {
                        if (installCts.IsCancellationRequested)
                        {
                            TryStopProcess(proc);
                            break;
                        }

                        await Task.Delay(500);
                    }

                    if (installCts.IsCancellationRequested)
                    {
                        SetRowStatus(row, proc.HasExited ? $"Durduruldu (Çıkış: {proc.ExitCode})" : "Durduruldu",
                            Theme.Danger);
                        stopped = true;
                        break;
                    }

                    var (outcome, text) = ProgramManager.DescribeExitCode(proc.ExitCode);

                    if (outcome == ProgramManager.InstallOutcome.Busy && attempt < maxBusyRetries)
                    {
                        SetRowStatus(row, $"Başka kurulum sürüyor, bekleniyor ({attempt + 1}/{maxBusyRetries})...",
                            Theme.Warning);
                        lblInstallStatus.Text = statusPrefix + $"({i + 1}/{plannedRows.Count}) {program.Name}: başka bir kurulum bitene kadar bekleniyor...";
                        for (int w = 0; w < 60 && !installCts.IsCancellationRequested; w++)
                        {
                            await Task.Delay(500); // ~30 sn
                        }
                        if (installCts.IsCancellationRequested)
                        {
                            SetRowStatus(row, "Durduruldu", Theme.Danger);
                            stopped = true;
                            break;
                        }
                        continue;
                    }

                    switch (outcome)
                    {
                        case ProgramManager.InstallOutcome.Success:
                            successCount++;
                            SetRowStatus(row, text, Theme.Success);
                            break;
                        case ProgramManager.InstallOutcome.RebootRequired:
                            successCount++;
                            rebootRequired = true;
                            SetRowStatus(row, text, Theme.Success);
                            break;
                        case ProgramManager.InstallOutcome.Cancelled:
                            SetRowStatus(row, text, Theme.Warning);
                            break;
                        default:
                            SetRowStatus(row, text, Theme.Danger);
                            break;
                    }
                    break;
                }

                if (stopped)
                {
                    break;
                }

                progressBarInstall.Value = i + 1;
            }

            if (installCts.IsCancellationRequested)
            {
                // Kalanları "İptal edildi" olarak işaretle (hala tikliyse)
                foreach (var row in plannedRows)
                {
                    if (row.Tag != null && IsRowChecked(row))
                    {
                        string status = Convert.ToString(row.Cells[ColStatus].Value ?? "");
                        if (status == "Bekliyor")
                        {
                            SetRowStatus(row, "İptal edildi", Theme.TextDim);
                        }
                    }
                }
            }

            return successCount;
        }

        private void SetUiEnabled(bool enabled)
        {
            btnFullSetup.Enabled = enabled;

            btnSelectAll.Enabled = enabled;
            btnDeselectAll.Enabled = enabled;
            btnInstallSelected.Enabled = enabled;

            btnAdd.Enabled = enabled;
            btnRemove.Enabled = enabled;
            btnOpenDatabase.Enabled = enabled;
            btnSync.Enabled = enabled;
            navOnline.Enabled = enabled;
            txtSearch.Enabled = enabled;
            pageSettings.SetBusy(!enabled);
            btnActivate.Enabled = enabled;
            btnPowerShell.Enabled = enabled;
            btnDefenderOff.Enabled = enabled;
            btnDefenderOn.Enabled = enabled;
            btnUpdateBlock.Enabled = enabled;
            btnUpdateAllow.Enabled = enabled;
            btnBloatware.Enabled = enabled;
        }

        private void BtnDownload_Click(object sender, EventArgs e)
        {
            using (var form = new OnlineAppsForm())
            {
                form.ShowDialog(this);
            }
        }

        private void BtnStopInstall_Click(object sender, EventArgs e)
        {
            if (installCts == null || installCts.IsCancellationRequested)
            {
                return;
            }

            installCts.Cancel();
            btnStopInstall.Enabled = false;
            lblInstallStatus.Text = "Durduruluyor... (mevcut kurulum kapatılmaya çalışılıyor)";
        }

        private void TryStopProcess(Process proc)
        {
            try
            {
                if (proc == null || proc.HasExited)
                {
                    return;
                }

                // Önce nazikçe kapatmayı dene
                try
                {
                    proc.CloseMainWindow();
                }
                catch
                {
                    // ignore
                }

                // Kısa süre bekle, kapanmazsa kill dene
                try
                {
                    if (!proc.WaitForExit(1500))
                    {
                        proc.Kill();
                    }
                }
                catch
                {
                    // Bazı installer'lar (runas) kill'e izin vermeyebilir
                }
            }
            catch
            {
                // ignore
            }
        }

        private bool IsRowChecked(DataGridViewRow row)
        {
            try
            {
                var v = row.Cells[ColSelect].Value;
                return v != null && v != DBNull.Value && Convert.ToBoolean(v);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Satırın durum rozetini günceller; tone rozetin rengidir.</summary>
        private void SetRowStatus(DataGridViewRow row, string status, Color tone)
        {
            row.Cells[ColStatus].Value = status;
            row.Cells[ColStatus].Tag = tone;
            row.Cells[ColStatus].ToolTipText = status;
            gridPrograms.InvalidateRow(row.Index);
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            gridPrograms.EndEdit();

            // Önce checkbox'larla seçilenleri topla
            var checkedRows = gridPrograms.Rows
                .Cast<DataGridViewRow>()
                .Where(r => r.Tag != null && IsRowChecked(r))
                .ToList();

            // Checkbox seçimi yoksa, satır seçimine bak
            if (checkedRows.Count == 0)
            {
                if (gridPrograms.SelectedRows.Count > 0 && gridPrograms.SelectedRows[0].Tag != null)
                {
                    checkedRows.Add(gridPrograms.SelectedRows[0]);
                }
                else
                {
                    Dialogs.Show("Lütfen silmek istediğiniz program(lar)ı işaretleyin veya satıra tıklayın!",
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var toDelete = checkedRows.Select(r => (ProgramInfo)r.Tag).ToList();
            string programList = string.Join("\n", toDelete.Select(p => $"• {p.Name}"));

            DialogResult confirm = Dialogs.Show(
                $"{toDelete.Count} program silinecek:\n\n{programList}\n\nBu işlem dosyaları da silecektir!",
                "Silme Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes) return;

            int deleted = 0;
            foreach (var program in toDelete)
            {
                if (ProgramManager.RemoveProgram(program))
                    deleted++;
            }

            if (deleted > 0)
            {
                Dialogs.Show($"{deleted} program başarıyla silindi!", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadPrograms();
            }
        }

        private void BtnOpenDatabase_Click(object sender, EventArgs e)
        {
            string databaseFolder = ProgramManager.GetDatabaseFolder();
            if (Directory.Exists(databaseFolder))
            {
                System.Diagnostics.Process.Start("explorer.exe", databaseFolder);
            }
            else
            {
                Dialogs.Show("programlar klasörü bulunamadı!", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            DialogResult result = Dialogs.Show(
                "Microsoft Activation Scripts (MAS) çalıştırılacak.\n\n" +
                "Bu araç Windows ve Office etkinleştirmesi yapar.\n" +
                "Kaynak: massgrave.dev\n\n" +
                "Devam etmek istiyor musunuz?",
                "Windows/Office Etkinleştirme",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-ExecutionPolicy Bypass -Command \"irm https://get.activated.win | iex\"",
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Dialogs.Show($"Etkinleştirme başlatılamadı: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnPowerShell_Click(object sender, EventArgs e)
        {
            if (Dialogs.Show(
                "Aşağıdaki SMB yapılandırma komutları yönetici olarak çalıştırılacak:\n\n" +
                "• Set-SmbClientConfiguration -EnableInsecureGuestLogons $true\n" +
                "• Set-SmbClientConfiguration -RequireSecuritySignature $false\n" +
                "• Set-SmbServerConfiguration -RequireSecuritySignature $false\n\n" +
                "Devam etmek istiyor musunuz?",
                "PowerShell SMB Ayarları",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                RunPowerShellAsAdmin(
                    "Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force\r\n" +
                    "Set-SmbClientConfiguration -RequireSecuritySignature $false -Force\r\n" +
                    "Set-SmbServerConfiguration -RequireSecuritySignature $false -Force\r\n" +
                    "Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force\r\n" +
                    "Set-SmbClientConfiguration -RequireSecuritySignature $false -Force\r\n" +
                    "Set-SmbServerConfiguration -RequireSecuritySignature $false -Force",
                    "SMB ayarları uygulama");
            }
        }

        private void BtnDefenderOff_Click(object sender, EventArgs e)
        {
            if (Dialogs.Show(
                "Windows Defender Gerçek Zamanlı Koruma KAPATILACAK.\n\nDevam etmek istiyor musunuz?",
                "Defender Kapat", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                RunPowerShellAsAdmin(
                    "Set-MpPreference -DisableRealtimeMonitoring $true -Force\r\n" +
                    "Set-MpPreference -DisableBehaviorMonitoring $true -Force\r\n" +
                    "Set-MpPreference -DisableIOAVProtection $true -Force\r\n" +
                    "Set-MpPreference -DisablePrivacyMode $true -Force\r\n" +
                    "Set-MpPreference -SignatureDisableUpdateOnStartupWithoutEngine $true -Force\r\n" +
                    "Set-MpPreference -DisableArchiveScanning $true -Force\r\n" +
                    "Set-MpPreference -DisableIntrusionPreventionSystem $true -Force\r\n" +
                    "Set-MpPreference -DisableScriptScanning $true -Force",
                    "Defender kapatma");
            }
        }

        private void BtnDefenderOn_Click(object sender, EventArgs e)
        {
            if (Dialogs.Show(
                "Windows Defender Gerçek Zamanlı Koruma AÇILACAK.\n\nDevam etmek istiyor musunuz?",
                "Defender Aç", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                RunPowerShellAsAdmin(
                    "Set-MpPreference -DisableRealtimeMonitoring $false -Force\r\n" +
                    "Set-MpPreference -DisableBehaviorMonitoring $false -Force\r\n" +
                    "Set-MpPreference -DisableIOAVProtection $false -Force\r\n" +
                    "Set-MpPreference -DisablePrivacyMode $false -Force\r\n" +
                    "Set-MpPreference -SignatureDisableUpdateOnStartupWithoutEngine $false -Force\r\n" +
                    "Set-MpPreference -DisableArchiveScanning $false -Force\r\n" +
                    "Set-MpPreference -DisableIntrusionPreventionSystem $false -Force\r\n" +
                    "Set-MpPreference -DisableScriptScanning $false -Force",
                    "Defender açma");
            }
        }

        private void BtnUpdateBlock_Click(object sender, EventArgs e)
        {
            if (Dialogs.Show(
                "Windows Update ENGELLENECEK.\nGüncellemeler devre dışı bırakılacak.\n\nDevam etmek istiyor musunuz?",
                "Update Engelle", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                RunPowerShellAsAdmin(
                    "Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue\r\n" +
                    "Set-Service -Name wuauserv -StartupType Disabled\r\n" +
                    "Stop-Service -Name UsoSvc -Force -ErrorAction SilentlyContinue\r\n" +
                    "Set-Service -Name UsoSvc -StartupType Disabled\r\n" +
                    "Stop-Service -Name bits -Force -ErrorAction SilentlyContinue\r\n" +
                    "Set-Service -Name bits -StartupType Disabled",
                    "Update engelleme");
            }
        }

        private void BtnUpdateAllow_Click(object sender, EventArgs e)
        {
            if (Dialogs.Show(
                "Windows Update ETKINLEŞTIRILECEK.\nGüncellemeler tekrar çalışmaya başlayacak.\n\nDevam etmek istiyor musunuz?",
                "Update Aç", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                RunPowerShellAsAdmin(
                    "Set-Service -Name wuauserv -StartupType Automatic\r\n" +
                    "Start-Service -Name wuauserv -ErrorAction SilentlyContinue\r\n" +
                    "Set-Service -Name UsoSvc -StartupType Automatic\r\n" +
                    "Start-Service -Name UsoSvc -ErrorAction SilentlyContinue\r\n" +
                    "Set-Service -Name bits -StartupType Automatic\r\n" +
                    "Start-Service -Name bits -ErrorAction SilentlyContinue",
                    "Update açma");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // TAM KURULUM — tek tuşla komple sistem hazırlama akışı
        // ══════════════════════════════════════════════════════════════════
        private async void BtnFullSetup_Click(object sender, EventArgs e)
        {
            if (isInstalling)
            {
                return;
            }

            int programCount = gridPrograms.Rows.Cast<DataGridViewRow>().Count(r =>
                r.Tag != null && File.Exists(Path.Combine(ProgramManager.DatabaseFolder, ((ProgramInfo)r.Tag).FileName ?? "")));
            var ebys = EbysLocator.Find();

            FullSetupOptions options;
            using (var dlg = new FullSetupDialog(programCount, ebys, AppSettings.Current.WifiNetworks))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                options = dlg.Options;
            }

            ShowPage(pagePrograms);
            await RunFullSetupAsync(options, ebys);
        }

        /// <summary>
        /// Tam Kurulum: tüm sorular baştaki seçenek penceresinde sorulur, burada
        /// yalnızca seçilen adımlar sırayla ve sorusuz çalışır (Defender elle
        /// kapatılması gereken durum hariç).
        /// </summary>
        private async Task RunFullSetupAsync(FullSetupOptions options, EbysLocator.EbysInstaller ebys)
        {
            bool uacDisabled = false;
            var summary = new List<string>();
            int total = options.StepCount;
            int stepNo = 0;

            void Step(string text)
            {
                stepNo++;
                lblInstallStatus.Text = $"{stepNo}/{total} — {text}";
            }

            void Status(string text)
            {
                lblInstallStatus.Text = $"{stepNo}/{total} — {text}";
            }

            try
            {
                isInstalling = true;
                installCts = new CancellationTokenSource();
                SetUiEnabled(false);
                installStatusPanel.Visible = true;
                btnStopInstall.Enabled = false; // sistem adımlarında durdurma yok
                progressBarInstall.Style = ProgressBarStyle.Marquee;
                rebootRequired = false;

                // ── UAC kapat ──────────────────────────────────────────────
                if (options.DisableUac)
                {
                    Step("UAC kapatılıyor...");
                    string uacResult = await Task.Run(() => ProgramManager.RunPowerShellScript(
                        "$ErrorActionPreference='SilentlyContinue'\r\n" +
                        "$p='HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System'\r\n" +
                        "if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }\r\n" +
                        "Set-ItemProperty -Path $p -Name 'ConsentPromptBehaviorAdmin' -Value 0 -Type DWord -Force\r\n" +
                        "Set-ItemProperty -Path $p -Name 'PromptOnSecureDesktop' -Value 0 -Type DWord -Force\r\n" +
                        "Set-ItemProperty -Path $p -Name 'EnableLUA' -Value 0 -Type DWord -Force\r\n" +
                        "'OK'"));
                    uacDisabled = uacResult.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0;
                    summary.Add(uacDisabled ? "✓ UAC kapatıldı (yeniden başlatınca tam etkin olur)." : "✗ UAC kapatılamadı.");
                }

                // ── Defender kapat + doğrulama döngüsü ─────────────────────
                if (options.DisableDefender)
                {
                    Step("Windows Defender kapatılıyor...");
                    await Task.Run(() => ProgramManager.RunPowerShellScript(
                        "$ErrorActionPreference='SilentlyContinue'\r\n" +
                        "Set-MpPreference -DisableRealtimeMonitoring $true\r\n" +
                        "Set-MpPreference -DisableBehaviorMonitoring $true\r\n" +
                        "Set-MpPreference -DisableIOAVProtection $true\r\n" +
                        "Set-MpPreference -DisableScriptScanning $true\r\n" +
                        "Set-MpPreference -DisableArchiveScanning $true\r\n" +
                        "Set-MpPreference -DisableIntrusionPreventionSystem $true\r\n" +
                        "'OK'"));

                    bool? defState = await Task.Run(() => ProgramManager.IsDefenderRealtimeOn());
                    bool defenderOpened = false;
                    while (defState == true) // hâlâ açık
                    {
                        if (!defenderOpened)
                        {
                            await Task.Run(() => ProgramManager.RunPowerShellScript(
                                "Start-Process 'windowsdefender:' -ErrorAction SilentlyContinue", 15000));
                            defenderOpened = true;
                        }

                        DialogResult r = Dialogs.Show(
                            "Windows Defender otomatik kapatılamadı (büyük olasılıkla 'Kurcalama Koruması' açık).\n\n" +
                            "Açılan Windows Güvenliği penceresinde:\n" +
                            "  • Virüs ve tehdit koruması → Ayarları yönet\n" +
                            "  • 'Gerçek zamanlı koruma'yı elle kapatın\n\n" +
                            "Kapattıktan sonra 'Tamam'a basın, tekrar kontrol edilecek.\n" +
                            "'İptal' derseniz bu adım atlanır.",
                            "Defender elle kapatılmalı",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Warning);

                        if (r == DialogResult.Cancel)
                        {
                            break;
                        }

                        Status("Defender durumu kontrol ediliyor...");
                        defState = await Task.Run(() => ProgramManager.IsDefenderRealtimeOn());
                    }
                    summary.Add(defState == true ? "✗ Defender açık kaldı (adım atlandı)." : "✓ Defender kapatıldı.");
                }

                // ── Wi-Fi: kayıtlı ağları sırayla dene ─────────────────────
                if (options.ConnectWifi)
                {
                    Step("Wi-Fi kartı kontrol ediliyor...");
                    bool hasWifi = await Task.Run(() => ProgramManager.HasWifiAdapter());
                    var networks = AppSettings.Current.WifiNetworks.ToList();

                    if (!hasWifi)
                    {
                        summary.Add("• Wi-Fi kartı yok, kablolu bağlantıyla devam edildi.");
                    }
                    else if (networks.Count == 0)
                    {
                        summary.Add("• Kayıtlı Wi-Fi ağı yok (Ayarlar'dan ekleyebilirsiniz).");
                    }
                    else
                    {
                        string connected = await Task.Run(() => WifiService.ConnectToFirstAvailable(networks,
                            ssid => BeginInvoke((Action)(() => Status($"'{ssid}' ağına bağlanılıyor...")))));
                        summary.Add(connected != null
                            ? $"✓ Wi-Fi: '{connected}' ağına bağlanıldı."
                            : "✗ Kayıtlı Wi-Fi ağlarının hiçbirine bağlanılamadı.");
                    }
                }

                // ── Windows Update (internet varsa) ────────────────────────
                if (options.WindowsUpdate)
                {
                    Step("İnternet bağlantısı kontrol ediliyor...");
                    bool internetAvailable = await Task.Run(() => ProgramManager.HasInternetConnection());

                    if (!internetAvailable)
                    {
                        summary.Add("✗ İnternet yok, Windows Update atlandı.");
                    }
                    else
                    {
                        Status("Windows Update başlatılıyor (arka planda)...");
                        await Task.Run(() => ProgramManager.RunPowerShellScript(
                            "$ErrorActionPreference='SilentlyContinue'\r\n" +
                            "Set-Service -Name wuauserv -StartupType Automatic\r\n" +
                            "Start-Service -Name wuauserv\r\n" +
                            "Set-Service -Name UsoSvc -StartupType Automatic\r\n" +
                            "Start-Service -Name UsoSvc\r\n" +
                            "Set-Service -Name bits -StartupType Automatic\r\n" +
                            "Start-Service -Name bits\r\n" +
                            "try { (New-Object -ComObject Microsoft.Update.AutoUpdate).DetectNow() } catch {}\r\n" +
                            "Start-Process 'UsoClient.exe' -ArgumentList 'StartInteractiveScan' -WindowStyle Hidden -ErrorAction SilentlyContinue\r\n" +
                            "Start-Process 'ms-settings:windowsupdate' -ErrorAction SilentlyContinue\r\n" +
                            "'OK'"));
                        summary.Add("✓ Windows Update arka planda başlatıldı.");
                    }
                }

                // ── Listedeki tüm programlar ───────────────────────────────
                if (options.InstallPrograms)
                {
                    Step("Programlar hazırlanıyor...");
                    var plannedRows = new List<DataGridViewRow>();
                    foreach (DataGridViewRow row in gridPrograms.Rows)
                    {
                        if (row.Tag == null) continue;
                        var prog = (ProgramInfo)row.Tag;
                        if (File.Exists(Path.Combine(ProgramManager.DatabaseFolder, prog.FileName ?? "")))
                        {
                            row.Cells[ColSelect].Value = true; // Tam Kurulum: hepsini işaretle
                            plannedRows.Add(row);
                        }
                    }

                    if (plannedRows.Count > 0)
                    {
                        btnStopInstall.Enabled = true; // kurulum aşamasında durdurulabilir
                        int stepAtStart = stepNo;
                        int programsOk = await RunInstallLoopAsync(plannedRows, $"{stepAtStart}/{total} — ");
                        btnStopInstall.Enabled = false;

                        if (installCts.IsCancellationRequested)
                        {
                            Dialogs.Show("Program kurulumu durduruldu. Tam Kurulum iptal edildi.",
                                "İptal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        summary.Add((programsOk == plannedRows.Count ? "✓ " : "✗ ") +
                            $"Programlar: {programsOk}/{plannedRows.Count} başarıyla kuruldu.");
                    }
                    else
                    {
                        summary.Add("• Kurulacak program yoktu.");
                    }
                    progressBarInstall.Style = ProgressBarStyle.Marquee;
                }

                // ── MEB EBYS (programlardan sonra, pencereli) ──────────────
                if (options.InstallEbys)
                {
                    Step("MEB EBYS kuruluyor — açılan kurulum penceresini takip edin...");
                    string ebysResult = await InstallEbysAsync(ebys);
                    if (installCts.IsCancellationRequested)
                    {
                        Dialogs.Show("EBYS kurulumu durduruldu. Tam Kurulum iptal edildi.",
                            "İptal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    summary.Add(ebysResult);
                }

                // ── SMB ayarları ───────────────────────────────────────────
                if (options.ApplySmb)
                {
                    Step("SMB ayarları uygulanıyor...");
                    await Task.Run(() => ProgramManager.RunPowerShellScript(
                        "$ErrorActionPreference='SilentlyContinue'\r\n" +
                        "Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force\r\n" +
                        "Set-SmbClientConfiguration -RequireSecuritySignature $false -Force\r\n" +
                        "Set-SmbServerConfiguration -RequireSecuritySignature $false -Force\r\n" +
                        "'OK'"));
                    summary.Add("✓ SMB ayarları uygulandı.");
                }

                // ── Office masaüstü kısayolları ────────────────────────────
                if (options.OfficeShortcuts)
                {
                    Step("Office kısayolları oluşturuluyor...");
                    string officeResult = await Task.Run(() => ProgramManager.RunPowerShellScript(
                        "$ErrorActionPreference='SilentlyContinue'\r\n" +
                        "$apps = [ordered]@{ 'Word'='WINWORD.EXE'; 'Excel'='EXCEL.EXE'; 'PowerPoint'='POWERPNT.EXE'; 'Outlook'='OUTLOOK.EXE' }\r\n" +
                        "$ws = New-Object -ComObject WScript.Shell\r\n" +
                        "$desk = [Environment]::GetFolderPath('CommonDesktopDirectory')\r\n" +
                        "if (-not $desk) { $desk = [Environment]::GetFolderPath('Desktop') }\r\n" +
                        "$made = @()\r\n" +
                        "foreach ($name in $apps.Keys) {\r\n" +
                        "  $exe = $apps[$name]; $path = $null\r\n" +
                        "  foreach ($root in @(('HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\' + $exe), ('HKLM:\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\App Paths\\' + $exe))) {\r\n" +
                        "    $v = (Get-ItemProperty -Path $root -ErrorAction SilentlyContinue).'(default)'\r\n" +
                        "    if ($v -and (Test-Path $v)) { $path = $v; break }\r\n" +
                        "  }\r\n" +
                        "  if ($path) {\r\n" +
                        "    $lnk = $ws.CreateShortcut((Join-Path $desk ($name + '.lnk')))\r\n" +
                        "    $lnk.TargetPath = $path; $lnk.Save(); $made += $name\r\n" +
                        "  }\r\n" +
                        "}\r\n" +
                        "($made -join ',')"));

                    summary.Add(string.IsNullOrWhiteSpace(officeResult) || officeResult.StartsWith("ERR")
                        ? "✗ Office bulunamadı, kısayol oluşturulamadı."
                        : $"✓ Office kısayolları: {officeResult}");
                }

                // ── Bitti ──────────────────────────────────────────────────
                lblInstallStatus.Text = "Tam Kurulum tamamlandı.";
                progressBarInstall.Style = ProgressBarStyle.Continuous;
                progressBarInstall.Value = progressBarInstall.Maximum;

                string restartNote = uacDisabled || rebootRequired
                    ? "\n\nDeğişikliklerin tam etkin olması için bilgisayarı yeniden başlatın."
                    : "";

                Dialogs.Show(
                    (summary.Count == 0 ? "Hiçbir adım seçilmemişti." : string.Join("\n", summary)) + restartNote,
                    "Tam Kurulum tamamlandı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Dialogs.Show($"Tam Kurulum sırasında hata oluştu:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isInstalling = false;
                if (installCts != null)
                {
                    installCts.Dispose();
                    installCts = null;
                }
                btnStopInstall.Enabled = false;
                progressBarInstall.Style = ProgressBarStyle.Continuous;
                SetUiEnabled(true);
            }
        }

        /// <summary>
        /// EBYS kurulum dosyasını pencereli çalıştırır ve bitmesini bekler.
        /// Özet satırını döndürür. Durdur butonu bu aşamada aktiftir.
        /// </summary>
        private async Task<string> InstallEbysAsync(EbysLocator.EbysInstaller ebys)
        {
            if (ebys == null || !File.Exists(ebys.FilePath))
            {
                return "✗ EBYS kurulum dosyası bulunamadı (programın yanına EBYS*.exe koyun).";
            }

            Process proc;
            try
            {
                proc = Process.Start(new ProcessStartInfo
                {
                    FileName = ebys.FilePath,
                    Arguments = (AppSettings.Current.EbysArguments ?? "").Trim(),
                    WorkingDirectory = Path.GetDirectoryName(ebys.FilePath),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                return "✗ EBYS başlatılamadı: " + ex.Message;
            }

            if (proc == null)
            {
                return "✓ EBYS kurulumu başlatıldı (takip edilemedi).";
            }

            btnStopInstall.Enabled = true;
            try
            {
                while (!proc.HasExited)
                {
                    if (installCts.IsCancellationRequested)
                    {
                        TryStopProcess(proc);
                        return "✗ EBYS kurulumu durduruldu.";
                    }
                    await Task.Delay(500);
                }
            }
            finally
            {
                btnStopInstall.Enabled = false;
            }

            var (outcome, text) = ProgramManager.DescribeExitCode(proc.ExitCode);
            if (outcome == ProgramManager.InstallOutcome.RebootRequired) rebootRequired = true;
            bool ok = outcome == ProgramManager.InstallOutcome.Success || outcome == ProgramManager.InstallOutcome.RebootRequired;
            return (ok ? "✓ " : "✗ ") + $"{ebys.DisplayName}: {text}";
        }

        private async void BtnBloatware_Click(object sender, EventArgs e)
        {
            if (isInstalling)
            {
                return;
            }

            DialogResult confirm = Dialogs.Show(
                "Gereksiz Windows uygulamaları (bloatware) kaldırılacak:\n\n" +
                "• Xbox uygulamaları, Haritalar, Hava Durumu, Haberler\n" +
                "• Skype, People, Your Phone, Clipchamp, To-Do\n" +
                "• 3D Builder/Viewer, Solitaire, Groove Müzik, Film-TV\n" +
                "• Feedback Hub, Get Help, Teams (tüketici) ve benzerleri\n\n" +
                "Mağaza, Terminal, Hesap Makinesi, Fotoğraflar gibi faydalı\n" +
                "uygulamalara DOKUNULMAZ.\n\nDevam edilsin mi?",
                "🧹 Bloatware Temizle",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                SetUiEnabled(false);
                installStatusPanel.Visible = true;
                btnStopInstall.Enabled = false;
                progressBarInstall.Style = ProgressBarStyle.Marquee;
                lblInstallStatus.Text = "Gereksiz uygulamalar kaldırılıyor...";

                string result = await Task.Run(() => ProgramManager.RunPowerShellScript(
                    "$ErrorActionPreference='SilentlyContinue'\r\n" +
                    "$apps = @(\r\n" +
                    "  'Microsoft.3DBuilder','Microsoft.Microsoft3DViewer','Microsoft.BingWeather',\r\n" +
                    "  'Microsoft.BingNews','Microsoft.BingSearch','Microsoft.GetHelp','Microsoft.Getstarted',\r\n" +
                    "  'Microsoft.MicrosoftOfficeHub','Microsoft.MicrosoftSolitaireCollection',\r\n" +
                    "  'Microsoft.MixedReality.Portal','Microsoft.People','Microsoft.SkypeApp',\r\n" +
                    "  'Microsoft.WindowsFeedbackHub','Microsoft.WindowsMaps','Microsoft.ZuneMusic',\r\n" +
                    "  'Microsoft.ZuneVideo','Microsoft.YourPhone','Microsoft.Clipchamp','Microsoft.Todos',\r\n" +
                    "  'Microsoft.PowerAutomateDesktop','MicrosoftTeams','Microsoft.549981C3F5F10',\r\n" +
                    "  'Microsoft.Xbox.TCUI','Microsoft.XboxApp','Microsoft.XboxGameOverlay',\r\n" +
                    "  'Microsoft.XboxGamingOverlay','Microsoft.XboxIdentityProvider',\r\n" +
                    "  'Microsoft.XboxSpeechToTextOverlay','Microsoft.GamingApp'\r\n" +
                    ")\r\n" +
                    "$removed = 0\r\n" +
                    "foreach ($a in $apps) {\r\n" +
                    "  $pkg = Get-AppxPackage -Name $a -AllUsers -ErrorAction SilentlyContinue\r\n" +
                    "  if ($pkg) {\r\n" +
                    "    $pkg | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\r\n" +
                    "    $removed++\r\n" +
                    "  }\r\n" +
                    "  Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue |\r\n" +
                    "    Where-Object { $_.DisplayName -eq $a } |\r\n" +
                    "    Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Out-Null\r\n" +
                    "}\r\n" +
                    "\"REMOVED:$removed\""));

                lblInstallStatus.Text = "Bloatware temizleme tamamlandı.";

                int removed = 0;
                int idx = result.IndexOf("REMOVED:", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string num = new string(result.Substring(idx + 8)
                        .TrimStart().TakeWhile(char.IsDigit).ToArray());
                    int.TryParse(num, out removed);
                }

                Dialogs.Show(
                    $"Temizlik tamamlandı.\n\nKaldırılan uygulama sayısı: {removed}\n\n" +
                    "(Zaten kurulu olmayan uygulamalar sayılmaz.)",
                    "Bloatware Temizlendi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Dialogs.Show($"Bloatware temizleme sırasında hata: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                installStatusPanel.Visible = false;
                progressBarInstall.Style = ProgressBarStyle.Continuous;
                SetUiEnabled(true);
            }
        }

        private void RunPowerShellAsAdmin(string scriptContent, string actionName)
        {
            string tmpFile = null;
            try
            {
                // Komutları geçici bir .ps1 dosyasına yaz
                tmpFile = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"aio_{Guid.NewGuid():N}.ps1");
                System.IO.File.WriteAllText(tmpFile, scriptContent, System.Text.Encoding.UTF8);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -NonInteractive -File \"{tmpFile}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                var proc = Process.Start(startInfo);
                proc?.WaitForExit();

                Dialogs.Show($"{actionName} işlemi tamamlandı!", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Dialogs.Show($"Komut çalıştırılamadı: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Geçici dosyayı temizle
                try { if (tmpFile != null) System.IO.File.Delete(tmpFile); } catch { }
            }
        }

        private void BtnSync_Click(object sender, EventArgs e)
        {
            var result = ProgramManager.SyncDatabase();
            string dbPath = ProgramManager.GetDatabaseFolder();

            // Her zaman listeyi yenile (değişiklik olmasa dahi görünüm güncel kalmali)
            LoadPrograms();

            if (result.added > 0 || result.removed > 0)
            {
                Dialogs.Show($"Senkronizasyon tamamlandı!\n\nYol: {dbPath}\nEklenen: {result.added}\nKaldırılan: {result.removed}", 
                    "Veritabanı Güncellendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Dialogs.Show($"Veritabanı tarandı ve liste güncellendi.\nYol: {dbPath}", 
                    "Yenilendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void GridPrograms_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (gridPrograms.IsCurrentCellDirty)
            {
                gridPrograms.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
    }
}
