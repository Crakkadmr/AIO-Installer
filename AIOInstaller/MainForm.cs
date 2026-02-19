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

        private Button btnAdd;
        private Button btnInstall;
        private Button btnRemove;
        private Button btnOpenDatabase;
        private Button btnSync;
        private Button btnDownload;
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
            this.Size = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(900, 550);
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
                Width = 200,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colDescription = new DataGridViewTextBoxColumn
            {
                Name = ColDescription,
                HeaderText = "Açıklama",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 100,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colVersion = new DataGridViewTextBoxColumn
            {
                Name = ColVersion,
                HeaderText = "Versiyon",
                Width = 75,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
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
                colSelect, colName, colDescription, colVersion, colSize, colAdded, colStatus
            });

            gridPrograms.CurrentCellDirtyStateChanged += GridPrograms_CurrentCellDirtyStateChanged;
            gridPrograms.CellDoubleClick += GridPrograms_CellDoubleClick;

            // Seçim butonları paneli - Dark modern
            Panel selectionPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = C(30, 41, 59),
                Padding = new Padding(15, 10, 15, 10)
            };

            // Seçim butonları için FlowLayout
            var selectionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            btnSelectAll = CreateToolButton("✔ Tümünü Seç", C(16, 185, 129));
            btnSelectAll.Click += BtnSelectAll_Click;

            btnDeselectAll = CreateToolButton("✕ Seçimi Kaldır", C(100, 116, 139));
            btnDeselectAll.Click += BtnDeselectAll_Click;

            btnInstallSelected = CreateToolButton("▶ Seçilenleri Kur", C(59, 130, 246));
            btnInstallSelected.Click += BtnInstallSelected_Click;

            selectionFlow.Controls.Add(btnSelectAll);
            selectionFlow.Controls.Add(btnDeselectAll);
            selectionFlow.Controls.Add(btnInstallSelected);
            selectionPanel.Controls.Add(selectionFlow);

            // Kurulum durum paneli (progress bar) - Yükseklik artırıldı, boşluklar düzenlendi
            installStatusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85, // 52'den 85'e çıkarıldı
                Padding = new Padding(20, 12, 20, 12),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None, // Daha temiz görünüm için border kaldırıldı
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
            installLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // Buton genişliği
            installLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // Label satırı
            installLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // ProgressBar satırı

            lblInstallStatus.Dock = DockStyle.Fill;
            btnStopInstall.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            installLayout.Controls.Add(lblInstallStatus, 0, 0);
            installLayout.Controls.Add(btnStopInstall, 1, 0);
            installLayout.SetColumnSpan(progressBarInstall, 2);
            installLayout.Controls.Add(progressBarInstall, 0, 1);

            installStatusPanel.Controls.Add(installLayout);

            // Butonlar için Panel - Dark modern
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = C(15, 23, 42),
                Padding = new Padding(0)
            };

            // FlowLayoutPanel ile butonları otomatik hizala
            var buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 10, 10, 10),
                AutoScroll = false
            };

            btnAdd = CreateModernButton("➕ Program Ekle", C(16, 185, 129));
            btnAdd.Click += BtnAdd_Click;

            btnInstall = CreateModernButton("▶ Kur", C(59, 130, 246));
            btnInstall.Click += BtnInstall_Click;

            btnRemove = CreateModernButton("🗑 Sil", C(239, 68, 68));
            btnRemove.Click += BtnRemove_Click;

            btnOpenDatabase = CreateModernButton("📂 Klasör", C(124, 58, 237));
            btnOpenDatabase.Click += BtnOpenDatabase_Click;

            btnSync = CreateModernButton("🔄 Yenile", C(245, 158, 11));
            btnSync.Click += BtnSync_Click;

            btnDownload = CreateModernButton("🌐 İndir", C(124, 58, 237));
            btnDownload.Click += BtnDownload_Click;

            buttonFlow.Controls.Add(btnAdd);
            buttonFlow.Controls.Add(btnInstall);
            buttonFlow.Controls.Add(btnRemove);
            buttonFlow.Controls.Add(btnOpenDatabase);
            buttonFlow.Controls.Add(btnSync);
            buttonFlow.Controls.Add(btnDownload);

            buttonPanel.Controls.Add(buttonFlow);

            // WinForms Dock kuralı: Bottom paneller önce, sonra Top paneller ters sırayla, Fill en son
            // Bu sıra kesinlikle değiştirilmemeli!
            this.Controls.Add(gridPrograms);        // Fill - en son eklenen Fill en önce işlenir
            this.Controls.Add(selectionPanel);      // Top - 2. sıradan eklenen
            this.Controls.Add(headerPanel);         // Top - son eklenen Top en üste yerleşir
            this.Controls.Add(installStatusPanel);  // Bottom
            this.Controls.Add(buttonPanel);         // Bottom - son eklenen Bottom en alta yerleşir

            this.ResumeLayout(false);
        }

        private Button CreateModernButton(string text, Color backColor)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(130, 42),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = UiFont(9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 4, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 30),
                Math.Min(255, backColor.G + 30),
                Math.Min(255, backColor.B + 30)
            );
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, backColor.R - 30),
                Math.Max(0, backColor.G - 30),
                Math.Max(0, backColor.B - 30)
            );
            return btn;
        }

        private Button CreateToolButton(string text, Color backColor)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(145, 33),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = UiFont(9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 30),
                Math.Min(255, backColor.G + 30),
                Math.Min(255, backColor.B + 30)
            );
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, backColor.R - 30),
                Math.Max(0, backColor.G - 30),
                Math.Max(0, backColor.B - 30)
            );
            return btn;
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
                        program.Description ?? "",
                        program.Version ?? "",
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
                int idx = gridPrograms.Rows.Add(false, "Henüz program eklenmemiş", "Program eklemek için 'Program Ekle' butonuna tıklayın", "", "", "", "");
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

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            gridPrograms.EndEdit();
            
            // Eğer tikli olan varsa toplu kurulumu başlat
            if (gridPrograms.Rows.Cast<DataGridViewRow>().Any(r => IsRowChecked(r)))
            {
                _ = InstallSelectedProgramsAsync();
                return;
            }

            // Tikli yoksa ama seçili/vurgulu olan varsa onu kur
            if (gridPrograms.SelectedRows.Count > 0)
            {
                InstallSelectedProgram();
            }
            else
            {
                MessageBox.Show("Lütfen kurulacak programları yanındaki kutucuktan seçin veya satıra tıklayın.", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                    lblInstallStatus.Text = "Kurulum durduruldu. (Sıradakiler iptal edildi)";
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

        private void SetUiEnabled(bool enabled)
        {
            btnSelectAll.Enabled = enabled;
            btnDeselectAll.Enabled = enabled;
            btnInstallSelected.Enabled = enabled;

            btnAdd.Enabled = enabled;
            btnInstall.Enabled = enabled;
            btnRemove.Enabled = enabled;
            btnOpenDatabase.Enabled = enabled;
            btnSync.Enabled = enabled;
            btnDownload.Enabled = enabled;
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
                MessageBox.Show("Database klasörü bulunamadı!", "Hata", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSync_Click(object sender, EventArgs e)
        {
            var result = ProgramManager.SyncDatabase();
            string dbPath = ProgramManager.GetDatabaseFolder();
            
            if (result.added > 0 || result.removed > 0)
            {
                MessageBox.Show($"Senkronizasyon tamamlandı!\n\nYol: {dbPath}\nEklenen: {result.added}\nKaldırılan: {result.removed}", 
                    "Veritabanı Güncellendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadPrograms();
            }
            else
            {
                MessageBox.Show($"Veritabanı zaten güncel.\nYol: {dbPath}\n\nHiçbir değişiklik bulunmadı.", 
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
