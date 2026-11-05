using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AIOInstaller
{
    public partial class MainForm : Form
    {
        private static Font UiFont(float size, FontStyle style = FontStyle.Regular)
        {
            string[] preferred =
            {
                "Bahnschrift",
                "Segoe UI Variable",
                "Segoe UI",
                "Calibri"
            };

            foreach (var name in preferred)
            {
                try { return new Font(name, size, style, GraphicsUnit.Point); }
                catch { /* font yoksa denemeye devam */ }
            }

            return new Font(SystemFonts.MessageBoxFont.FontFamily, size, style, GraphicsUnit.Point);
        }

        private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);

        private List<ProgramInfo> programs;
        private DataGridView gridPrograms;
        private Button btnSelectAll;
        private Button btnInstallSelected;
        private Button btnDeselectAll;

        private Panel installStatusPanel;
        private ProgressBar progressBarInstall;
        private Label lblInstallStatus;
        private bool isInstalling;
        private CancellationTokenSource installCts;
        private Button btnStopInstall;

        private Button btnFullSetup;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnOpenDatabase;
        private Button btnSync;
        private Button btnDownload;
        private Button btnActivate;
        private Button btnPowerShell;
        private Button btnDefenderOff;
        private Button btnDefenderOn;
        private Button btnUpdateBlock;
        private Button btnUpdateAllow;
        private Label lblCredit;
        private Label lblTotalCount;

        private const string ColSelect = "colSelect";
        private const string ColName = "colName";
        private const string ColDescription = "colDescription";
        private const string ColVersion = "colVersion";
        private const string ColSize = "colSize";
        private const string ColAdded = "colAdded";
        private const string ColStatus = "colStatus";
        private Label lblTitle;

        public MainForm()
        {
            InitializeComponent();
            LoadPrograms();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form ayarları - Modern görünüm
            this.Text = "AIO INSTALLER V1.0";
            this.Size = new Size(932, 821);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(750, 600);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = C(15, 23, 42);
            this.Font = UiFont(9F, FontStyle.Regular);
            // Programatik icon oluştur
            try { this.Icon = CreateAppIcon(); } catch { }

            // Başlık paneli
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = C(15, 23, 42),
                Padding = new Padding(20, 0, 20, 0)
            };

            // Başlık
            lblTitle = new Label
            {
                Text = "AIO Yazılım Yükleyici",
                ForeColor = Color.White,
                Font = UiFont(18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 10)
            };

            lblCredit = new Label
            {
                Text = "made by demircan",
                ForeColor = C(59, 130, 246),
                Font = UiFont(9F, FontStyle.Italic | FontStyle.Bold),
                AutoSize = true,
                Location = new Point(23, 44)
            };

            // Sağ kısım: Toplam Sayı
            lblTotalCount = new Label
            {
                Text = "Toplam: 0 Program",
                ForeColor = C(148, 163, 184),
                Font = UiFont(11F, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(800, 25)
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblCredit);
            headerPanel.Controls.Add(lblTotalCount);

            // Resize event to reposition lblTotalCount
            headerPanel.Resize += (s, ev) =>
            {
                lblTotalCount.Location = new Point(
                    headerPanel.ClientSize.Width - lblTotalCount.Width - 20,
                    25);
            };

            // DataGridView - Modern görünüm
            gridPrograms = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = C(30, 41, 59),
                GridColor = C(51, 65, 85),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                EnableHeadersVisualStyles = false,
                Font = UiFont(10F),
                ForeColor = C(226, 232, 240),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = C(30, 41, 59), ForeColor = C(226, 232, 240) },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = C(15, 23, 42),
                    ForeColor = C(226, 232, 240),
                    SelectionBackColor = C(37, 99, 235),
                    SelectionForeColor = Color.White,
                    Padding = new Padding(6, 0, 6, 0)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = C(30, 41, 59),
                    ForeColor = C(148, 163, 184),
                    Font = UiFont(9F, FontStyle.Bold),
                    SelectionBackColor = C(30, 41, 59),
                    SelectionForeColor = C(148, 163, 184),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(6, 0, 6, 0)
                },
                ColumnHeadersHeight = 42,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EditMode = DataGridViewEditMode.EditOnEnter,
                RowTemplate = { Height = 42 },
                ShowCellToolTips = true,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };

            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = ColSelect,
                HeaderText = "",
                Width = 44,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colName = new DataGridViewTextBoxColumn
            {
                Name = ColName,
                HeaderText = "💻 Program Adı",
                Width = 320,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };



            var colSize = new DataGridViewTextBoxColumn
            {
                Name = ColSize,
                HeaderText = "💾 Boyut",
                Width = 90,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colAdded = new DataGridViewTextBoxColumn
            {
                Name = ColAdded,
                HeaderText = "📅 Tarih",
                Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = ColStatus,
                HeaderText = "✓ Durum",
                Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            gridPrograms.Columns.AddRange(new DataGridViewColumn[]
            {
                colSelect, colName, colSize, colAdded, colStatus
            });

            gridPrograms.CurrentCellDirtyStateChanged += GridPrograms_CurrentCellDirtyStateChanged;
            gridPrograms.CellDoubleClick += GridPrograms_CellDoubleClick;

            // Kurulum durum paneli (progress bar)
            installStatusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                Padding = new Padding(20, 12, 20, 12),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Visible = false
            };

            lblInstallStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 18,
                Text = "",
                ForeColor = C(15, 23, 42),
                Font = UiFont(9F, FontStyle.Bold)
            };

            progressBarInstall = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Height = 16,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            btnStopInstall = new Button
            {
                Text = "Durdur",
                Size = new Size(110, 28),
                BackColor = C(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = UiFont(9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnStopInstall.FlatAppearance.BorderSize = 0;
            btnStopInstall.Click += BtnStopInstall_Click;

            var installLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            installLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            installLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            installLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            installLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            lblInstallStatus.Dock = DockStyle.Fill;
            btnStopInstall.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            installLayout.Controls.Add(lblInstallStatus, 0, 0);
            installLayout.Controls.Add(btnStopInstall, 1, 0);
            installLayout.SetColumnSpan(progressBarInstall, 2);
            installLayout.Controls.Add(progressBarInstall, 0, 1);

            installStatusPanel.Controls.Add(installLayout);

            // ══════════════════════════════════════════════════════════════════
            // Sağ taraf: Kategorili buton paneli
            // ══════════════════════════════════════════════════════════════════
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 175,
                BackColor = C(20, 27, 45),
                Padding = new Padding(0)
            };

            // İnce ayırıcı çizgi (sol kenar)
            var separator = new Panel
            {
                Dock = DockStyle.Left,
                Width = 2,
                BackColor = C(59, 130, 246)
            };

            // Butonların yerleşeceği FlowLayoutPanel (dikey)
            var sideFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 8, 10, 8)
            };

            // ── Yardımcı: Kategori başlığı oluştur
            Func<string> lastCat = () => "";
            Action<FlowLayoutPanel, string> addCategoryLabel = (flow, text) =>
            {
                var lbl = new Label
                {
                    Text = text.ToUpperInvariant(),
                    ForeColor = C(100, 116, 139),
                    Font = UiFont(7.5F, FontStyle.Bold),
                    AutoSize = false,
                    Width = 150,
                    Height = 22,
                    TextAlign = ContentAlignment.BottomLeft,
                    Margin = new Padding(0, 6, 0, 2)
                };
                flow.Controls.Add(lbl);

                // İnce ayırıcı
                var line = new Panel
                {
                    Width = 150,
                    Height = 1,
                    BackColor = C(51, 65, 85),
                    Margin = new Padding(0, 0, 0, 4)
                };
                flow.Controls.Add(line);
            };

            // ── Yardımcı: Yan panel butonu oluştur
            Func<string, Color, Button> createSideButton = (text, color) =>
            {
                var btn = new Button
                {
                    Text = text,
                    Size = new Size(150, 34),
                    BackColor = color,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = UiFont(8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 2, 0, 2),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                    Math.Min(255, color.R + 30),
                    Math.Min(255, color.G + 30),
                    Math.Min(255, color.B + 30));
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                    Math.Max(0, color.R - 30),
                    Math.Max(0, color.G - 30),
                    Math.Max(0, color.B - 30));
                return btn;
            };

            // ── Kategori: OTOMATİK (en üstte, öne çıkan) ──
            addCategoryLabel(sideFlow, "🚀 Otomatik");

            btnFullSetup = createSideButton("🚀 TAM KURULUM", C(234, 88, 12));
            btnFullSetup.Size = new Size(150, 44);
            btnFullSetup.Font = UiFont(9.5F, FontStyle.Bold);
            btnFullSetup.Click += BtnFullSetup_Click;
            sideFlow.Controls.Add(btnFullSetup);

            // ── Kategori: SEÇİM ──
            addCategoryLabel(sideFlow, "📋 Seçim");

            btnSelectAll = createSideButton("✔ Tümünü Seç", C(16, 185, 129));
            btnSelectAll.Click += BtnSelectAll_Click;
            sideFlow.Controls.Add(btnSelectAll);

            btnDeselectAll = createSideButton("✕ Seçimi Kaldır", C(100, 116, 139));
            btnDeselectAll.Click += BtnDeselectAll_Click;
            sideFlow.Controls.Add(btnDeselectAll);

            btnInstallSelected = createSideButton("▶ Seçilenleri Kur", C(59, 130, 246));
            btnInstallSelected.Click += BtnInstallSelected_Click;
            sideFlow.Controls.Add(btnInstallSelected);

            // ── Kategori: PROGRAM ──
            addCategoryLabel(sideFlow, "💻 Program");

            btnAdd = createSideButton("➕ Program Ekle", C(16, 185, 129));
            btnAdd.Click += BtnAdd_Click;
            sideFlow.Controls.Add(btnAdd);

            btnRemove = createSideButton("🗑 Sil", C(239, 68, 68));
            btnRemove.Click += BtnRemove_Click;
            sideFlow.Controls.Add(btnRemove);

            // ── Kategori: DOSYA ──
            addCategoryLabel(sideFlow, "📁 Dosya");

            btnOpenDatabase = createSideButton("📂 Klasör Aç", C(124, 58, 237));
            btnOpenDatabase.Click += BtnOpenDatabase_Click;
            sideFlow.Controls.Add(btnOpenDatabase);

            btnSync = createSideButton("🔄 Yenile", C(245, 158, 11));
            btnSync.Click += BtnSync_Click;
            sideFlow.Controls.Add(btnSync);

            btnDownload = createSideButton("🌐 İndir", C(124, 58, 237));
            btnDownload.Click += BtnDownload_Click;
            sideFlow.Controls.Add(btnDownload);

            // ── Kategori: ARAÇLAR ──
            addCategoryLabel(sideFlow, "🔧 Araçlar");

            btnActivate = createSideButton("🔑 Etkinleştir", C(220, 38, 38));
            btnActivate.Click += BtnActivate_Click;
            sideFlow.Controls.Add(btnActivate);

            btnPowerShell = createSideButton("⚡ PowerShell", C(0, 120, 212));
            btnPowerShell.Click += BtnPowerShell_Click;
            sideFlow.Controls.Add(btnPowerShell);

            // ── Kategori: GÜVENLİK ──
            addCategoryLabel(sideFlow, "🛡️ Güvenlik");

            btnDefenderOff = createSideButton("🛡️ Defender Kapat", C(180, 30, 30));
            btnDefenderOff.Click += BtnDefenderOff_Click;
            sideFlow.Controls.Add(btnDefenderOff);

            btnDefenderOn = createSideButton("✅ Defender Aç", C(16, 130, 60));
            btnDefenderOn.Click += BtnDefenderOn_Click;
            sideFlow.Controls.Add(btnDefenderOn);

            // ── Kategori: WINDOWS UPDATE ──
            addCategoryLabel(sideFlow, "🔄 Windows Update");

            btnUpdateBlock = createSideButton("⛔ Update Engelle", C(180, 80, 0));
            btnUpdateBlock.Click += BtnUpdateBlock_Click;
            sideFlow.Controls.Add(btnUpdateBlock);

            btnUpdateAllow = createSideButton("✅ Update Aç", C(16, 130, 60));
            btnUpdateAllow.Click += BtnUpdateAllow_Click;
            sideFlow.Controls.Add(btnUpdateAllow);

            rightPanel.Controls.Add(sideFlow);
            rightPanel.Controls.Add(separator);

            // WinForms Dock kuralı: Right panel, Bottom paneller, Top paneller, Fill en son
            this.Controls.Add(gridPrograms);        // Fill
            this.Controls.Add(headerPanel);         // Top
            this.Controls.Add(installStatusPanel);  // Bottom
            this.Controls.Add(rightPanel);          // Right

            this.ResumeLayout(false);
        }

        // Programatik icon oluşturma
        private Icon CreateAppIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Koyu arka plan
                g.FillRectangle(new SolidBrush(C(15, 23, 42)), 0, 0, 32, 32);
                // Mavi daire
                g.FillEllipse(new SolidBrush(C(37, 99, 235)), 3, 3, 26, 26);
                // Beyaz "A" harfi
                using (var font = new Font("Arial", 14, FontStyle.Bold, GraphicsUnit.Point))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("A", font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);
                }
                // Bitmap'i icon'a çevir
                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        private void LoadPrograms()
        {
            programs = ProgramManager.LoadPrograms();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            gridPrograms.SuspendLayout();
            gridPrograms.Rows.Clear();
            lblTotalCount.Text = $"Toplam: {programs.Count} Program";

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
                        fileExists ? "Bekliyor" : "⚠ Dosya Yok"
                    );

                    var row = gridPrograms.Rows[idx];
                    row.Tag = program;

                    if (!fileExists)
                    {
                        row.DefaultCellStyle.ForeColor = C(239, 68, 68);
                        row.DefaultCellStyle.BackColor = C(30, 41, 59);
                    }
                    else
                    {
                        row.DefaultCellStyle.ForeColor = C(226, 232, 240);
                        row.DefaultCellStyle.BackColor = C(15, 23, 42);
                    }
                }
                catch { /* Satır eklenirken hata olursa atla */ }
            }

            if (programs.Count == 0)
            {
                int idx = gridPrograms.Rows.Add(false, "Henüz program eklenmemiş", "", "", "");
                var row = gridPrograms.Rows[idx];
                row.ReadOnly = true;
                row.DefaultCellStyle.ForeColor = C(100, 116, 139);
                row.DefaultCellStyle.BackColor = C(30, 41, 59);
            }

            gridPrograms.ResumeLayout();
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
                            MessageBox.Show(messages.ToString().Trim(),
                                result.duplicates.Count > 0 ? "Ekleme Tamamlandı (Uyarılar Var)" : "Başarılı",
                                MessageBoxButtons.OK,
                                result.duplicates.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", 
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
                if (row.Tag != null)
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
                MessageBox.Show("Lütfen yüklenecek bir program seçin!", "Uyarı", 
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
                DialogResult result = MessageBox.Show(
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
                MessageBox.Show("Lütfen yüklenecek programları seçin!", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var checkedPrograms = plannedRows.Select(r => (ProgramInfo)r.Tag).ToList();
            string programList = string.Join("\n", checkedPrograms.Select(p => $"• {p.Name}"));
            DialogResult result = MessageBox.Show(
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
                    lblInstallStatus.Text = $"Bitti: {successCount}/{plannedRows.Count} program işlendi.";
                    MessageBox.Show($"{successCount} program sırayla işlendi. Detay için 'Durum' sütununa bakabilirsiniz.", "Bilgi",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        /// <summary>
        /// Verilen satırları yukarıdan aşağı sırayla kurar. Çağıran taraf isInstalling,
        /// installCts ve UI durumunu (panel/buton) ayarlamış olmalıdır. İşlenen (başlatılan)
        /// program sayısını döndürür. Hem "Seçilenleri Kur" hem de "Tam Kurulum" bunu kullanır.
        /// </summary>
        private async Task<int> RunInstallLoopAsync(List<DataGridViewRow> plannedRows)
        {
            progressBarInstall.Style = ProgressBarStyle.Continuous;
            progressBarInstall.Minimum = 0;
            progressBarInstall.Maximum = plannedRows.Count;
            progressBarInstall.Value = 0;

            int successCount = 0;
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
                    SetRowStatus(row, "Atlandı", C(238, 238, 238));
                    progressBarInstall.Value = i + 1;
                    continue;
                }

                SetRowStatus(row, "Kuruluyor...", C(255, 249, 196));
                lblInstallStatus.Text = $"({i + 1}/{plannedRows.Count}) Kuruluyor: {program.Name}";
                progressBarInstall.Value = i;

                Process proc;
                bool started = ProgramManager.StartInstallProcess(program, out proc);
                if (!started)
                {
                    SetRowStatus(row, "Hata", C(255, 205, 210));
                }
                else
                {
                    successCount++;

                    // Process nesnesi alınamadıysa takip edemeyiz: sadece başlatıldı say
                    if (proc == null)
                    {
                        SetRowStatus(row, "Başlatıldı", C(200, 230, 201));
                    }
                    else
                    {
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
                            if (proc.HasExited)
                            {
                                SetRowStatus(row, $"Durduruldu (Çıkış: {proc.ExitCode})", C(255, 205, 210));
                            }
                            else
                            {
                                SetRowStatus(row, "Durduruldu", C(255, 205, 210));
                            }
                            break;
                        }

                        SetRowStatus(row, $"Tamamlandı (Çıkış: {proc.ExitCode})", C(200, 230, 201));
                    }
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
                            SetRowStatus(row, "İptal edildi", C(245, 245, 245));
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
            btnDownload.Enabled = enabled;
            btnActivate.Enabled = enabled;
            btnPowerShell.Enabled = enabled;
            btnDefenderOff.Enabled = enabled;
            btnDefenderOn.Enabled = enabled;
            btnUpdateBlock.Enabled = enabled;
            btnUpdateAllow.Enabled = enabled;
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

        private void SetRowStatus(DataGridViewRow row, string status, Color backColor)
        {
            row.Cells[ColStatus].Value = status;
            row.DefaultCellStyle.BackColor = backColor;
            row.DefaultCellStyle.SelectionBackColor = backColor;
            
            // Alternating rows için de rengi zorla
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.Style.BackColor = backColor;
            }
            
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
                    MessageBox.Show("Lütfen silmek istediğiniz program(lar)ı işaretleyin veya satıra tıklayın!",
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var toDelete = checkedRows.Select(r => (ProgramInfo)r.Tag).ToList();
            string programList = string.Join("\n", toDelete.Select(p => $"• {p.Name}"));

            DialogResult confirm = MessageBox.Show(
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
                MessageBox.Show($"{deleted} program başarıyla silindi!", "Başarılı",
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
                MessageBox.Show("programlar klasörü bulunamadı!", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
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
                    MessageBox.Show($"Etkinleştirme başlatılamadı: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnPowerShell_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
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
            if (MessageBox.Show(
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
            if (MessageBox.Show(
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
            if (MessageBox.Show(
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
            if (MessageBox.Show(
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

            DialogResult confirm = MessageBox.Show(
                "TAM KURULUM başlatılacak. Sırasıyla şunlar yapılacak:\n\n" +
                "1) UAC (Kullanıcı Hesabı Denetimi) kapatılacak\n" +
                "2) Windows Defender kapatılacak (kapanmazsa elle kapatmanız istenir)\n" +
                "3) Wi-Fi'ye bağlanmak isteyip istemediğiniz sorulacak (TNC_AP)\n" +
                "4) Wi-Fi bağlanırsa Windows Update başlatılacak\n" +
                "5) Listedeki TÜM programlar sırayla kurulacak\n" +
                "6) PowerShell (SMB) ayarları uygulanacak\n" +
                "7) Word/Excel/PowerPoint/Outlook masaüstü kısayolları oluşturulacak\n\n" +
                "Devam etmek istiyor musunuz?",
                "🚀 Tam Kurulum",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            await RunFullSetupAsync();
        }

        private async Task RunFullSetupAsync()
        {
            bool uacDisabled = false;
            try
            {
                isInstalling = true;
                installCts = new CancellationTokenSource();
                SetUiEnabled(false);
                installStatusPanel.Visible = true;
                btnStopInstall.Enabled = false; // sistem adımlarında durdurma yok
                progressBarInstall.Style = ProgressBarStyle.Marquee;

                // ── 1) UAC kapat ──────────────────────────────────────────
                lblInstallStatus.Text = "1/7 — UAC kapatılıyor...";
                string uacResult = await Task.Run(() => ProgramManager.RunPowerShellScript(
                    "$ErrorActionPreference='SilentlyContinue'\r\n" +
                    "$p='HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System'\r\n" +
                    "if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }\r\n" +
                    "Set-ItemProperty -Path $p -Name 'ConsentPromptBehaviorAdmin' -Value 0 -Type DWord -Force\r\n" +
                    "Set-ItemProperty -Path $p -Name 'PromptOnSecureDesktop' -Value 0 -Type DWord -Force\r\n" +
                    "Set-ItemProperty -Path $p -Name 'EnableLUA' -Value 0 -Type DWord -Force\r\n" +
                    "'OK'"));
                uacDisabled = uacResult.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0;

                // ── 2) Defender kapat + doğrulama döngüsü ─────────────────
                lblInstallStatus.Text = "2/7 — Windows Defender kapatılıyor...";
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

                    DialogResult r = MessageBox.Show(
                        "Windows Defender otomatik kapatılamadı (büyük olasılıkla 'Kurcalama Koruması/Tamper Protection' açık).\n\n" +
                        "Lütfen açılan Windows Güvenliği penceresinden:\n" +
                        "  • Virüs ve tehdit koruması → Ayarları yönet\n" +
                        "  • 'Gerçek zamanlı koruma'yı ELLE kapatın\n\n" +
                        "Kapattıktan sonra 'Tamam'a basın (tekrar kontrol edilecek).\n" +
                        "'İptal' derseniz bu adım atlanır ve kuruluma devam edilir.",
                        "Defender elle kapatılmalı",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning);

                    if (r == DialogResult.Cancel)
                    {
                        break;
                    }

                    lblInstallStatus.Text = "2/7 — Defender durumu kontrol ediliyor...";
                    defState = await Task.Run(() => ProgramManager.IsDefenderRealtimeOn());
                }

                // ── 3) Wi-Fi sorusu ───────────────────────────────────────
                bool wifiConnected = false;
                lblInstallStatus.Text = "3/7 — Wi-Fi kartı kontrol ediliyor...";
                bool hasWifi = await Task.Run(() => ProgramManager.HasWifiAdapter());

                if (hasWifi)
                {
                    DialogResult wifiAns = MessageBox.Show(
                        "Wi-Fi kartı bulundu.\n\n'TNC_AP' ağına bağlanılsın mı?",
                        "Wi-Fi Bağlantısı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (wifiAns == DialogResult.Yes)
                    {
                        lblInstallStatus.Text = "3/7 — TNC_AP ağına bağlanılıyor...";
                        string wifiResult = await Task.Run(() => ProgramManager.RunPowerShellScript(
                            "$ssid='TNC_AP'; $key='tekno.2453'\r\n" +
                            "$xml = '<?xml version=''1.0''?><WLANProfile xmlns=''http://www.microsoft.com/networking/WLAN/profile/v1''><name>' + $ssid + '</name><SSIDConfig><SSID><name>' + $ssid + '</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>auto</connectionMode><MSM><security><authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>' + $key + '</keyMaterial></sharedKey></security></MSM></WLANProfile>'\r\n" +
                            "$f = Join-Path $env:TEMP 'aio_wifi.xml'\r\n" +
                            "$xml | Out-File -FilePath $f -Encoding ASCII\r\n" +
                            "netsh wlan add profile filename=\"$f\" user=all | Out-Null\r\n" +
                            "netsh wlan connect name=\"$ssid\" ssid=\"$ssid\" | Out-Null\r\n" +
                            "Start-Sleep -Seconds 7\r\n" +
                            "$st = (netsh wlan show interfaces | Out-String)\r\n" +
                            "Remove-Item $f -ErrorAction SilentlyContinue\r\n" +
                            "if ($st -match 'TNC_AP') { 'CONNECTED' } else { 'NOTCONN' }",
                            60000));

                        wifiConnected = wifiResult.IndexOf("CONNECTED", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (!wifiConnected)
                        {
                            MessageBox.Show(
                                "TNC_AP ağına bağlanılamadı. (Ağ kapsama alanında olmayabilir veya şifre değişmiş olabilir.)\n" +
                                "Windows Update adımı atlanacak, kuruluma devam edilecek.",
                                "Wi-Fi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                else
                {
                    lblInstallStatus.Text = "3/7 — Wi-Fi kartı bulunamadı, atlanıyor...";
                }

                // ── 4) Windows Update (yalnızca Wi-Fi bağlıysa) ───────────
                if (wifiConnected)
                {
                    lblInstallStatus.Text = "4/7 — Windows Update başlatılıyor (arka planda)...";
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
                }

                // ── 5) Tüm programları sırayla kur ────────────────────────
                var plannedRows = new List<DataGridViewRow>();
                foreach (DataGridViewRow row in gridPrograms.Rows)
                {
                    if (row.Tag == null) continue;
                    var prog = (ProgramInfo)row.Tag;
                    bool fileExists = File.Exists(
                        System.IO.Path.Combine(ProgramManager.DatabaseFolder, prog.FileName ?? ""));
                    if (fileExists)
                    {
                        row.Cells[ColSelect].Value = true; // Tam Kurulum: hepsini işaretle
                        plannedRows.Add(row);
                    }
                }

                if (plannedRows.Count > 0)
                {
                    btnStopInstall.Enabled = true; // kurulum aşamasında durdurulabilir
                    lblInstallStatus.Text = $"5/7 — {plannedRows.Count} program kuruluyor...";
                    int sc = await RunInstallLoopAsync(plannedRows);
                    btnStopInstall.Enabled = false;

                    if (installCts.IsCancellationRequested)
                    {
                        MessageBox.Show("Program kurulumu durduruldu. Tam Kurulum iptal edildi.",
                            "İptal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                else
                {
                    lblInstallStatus.Text = "5/7 — Kurulacak program yok, atlanıyor...";
                }

                progressBarInstall.Style = ProgressBarStyle.Marquee;

                // ── 6) PowerShell (SMB) ayarları ──────────────────────────
                lblInstallStatus.Text = "6/7 — SMB ayarları uygulanıyor...";
                await Task.Run(() => ProgramManager.RunPowerShellScript(
                    "$ErrorActionPreference='SilentlyContinue'\r\n" +
                    "Set-SmbClientConfiguration -EnableInsecureGuestLogons $true -Force\r\n" +
                    "Set-SmbClientConfiguration -RequireSecuritySignature $false -Force\r\n" +
                    "Set-SmbServerConfiguration -RequireSecuritySignature $false -Force\r\n" +
                    "'OK'"));

                // ── 7) Office masaüstü kısayolları ────────────────────────
                lblInstallStatus.Text = "7/7 — Office kısayolları oluşturuluyor...";
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

                // ── Bitti ─────────────────────────────────────────────────
                lblInstallStatus.Text = "Tam Kurulum tamamlandı.";

                string officeInfo = string.IsNullOrWhiteSpace(officeResult) || officeResult.StartsWith("ERR")
                    ? "⚠ Office bulunamadı — kısayol oluşturulamadı."
                    : $"✅ Office kısayolları oluşturuldu: {officeResult}";

                string uacInfo = uacDisabled
                    ? "✅ UAC kapatıldı. (TAM etki için bilgisayarı yeniden başlatın.)"
                    : "⚠ UAC kapatılamadı.";

                MessageBox.Show(
                    "🚀 TAM KURULUM TAMAMLANDI!\n\n" +
                    uacInfo + "\n" +
                    officeInfo + "\n\n" +
                    "Not: UAC değişikliğinin tam olarak etkin olması için bilgisayarı yeniden başlatmanız önerilir.",
                    "İşlem Bitti",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tam Kurulum sırasında hata oluştu:\n{ex.Message}",
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

                MessageBox.Show($"{actionName} işlemi tamamlandı!", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Komut çalıştırılamadı: {ex.Message}", "Hata",
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
                MessageBox.Show($"Senkronizasyon tamamlandı!\n\nYol: {dbPath}\nEklenen: {result.added}\nKaldırılan: {result.removed}", 
                    "Veritabanı Güncellendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Veritabanı tarandı ve liste güncellendi.\nYol: {dbPath}", 
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
