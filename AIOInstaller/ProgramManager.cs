using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace AIOInstaller
{
    /// <summary>
    /// Program yönetimi için yardımcı sınıf
    /// </summary>
    public class ProgramManager
    {
        public static string DatabaseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
        private static string ProgramsXmlFile = Path.Combine(DatabaseFolder, "Programs.xml");
        private static string ProgramsListFile = Path.Combine(DatabaseFolder, "ProgramsList.xml");

        /// <summary>
        /// Database klasörünü oluşturur
        /// </summary>
        public static void InitializeDatabase()
        {
            if (!Directory.Exists(DatabaseFolder))
            {
                Directory.CreateDirectory(DatabaseFolder);
            }
        }

        /// <summary>
        /// Tüm programları yükler
        /// </summary>
        public static List<ProgramInfo> LoadPrograms()
        {
            InitializeDatabase();
            List<ProgramInfo> programs = new List<ProgramInfo>();

            if (File.Exists(ProgramsListFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<ProgramInfo>));
                    using (FileStream fileStream = new FileStream(ProgramsListFile, FileMode.Open))
                    {
                        programs = (List<ProgramInfo>)serializer.Deserialize(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show($"Programlar yüklenirken hata oluştu: {ex.Message}", 
                        "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
            }

            // Otomatik senkronizasyon: Disk ile XML listesini eşle
            bool changed = false;

            // Diskteki kurulum dosyalarını al (.exe, .msi, .zip)
            var installerExtensions = new[] { "*.exe", "*.msi", "*.zip" };
            var diskFiles = Directory.Exists(DatabaseFolder)
                ? installerExtensions.SelectMany(ext => Directory.GetFiles(DatabaseFolder, ext)).ToArray()
                : new string[0];
            var filesOnDisk = diskFiles.Select(Path.GetFileName).ToList();

            // XML'de olup diskte olmayan kayıtları kaldır
            var toRemove = programs.Where(p => !filesOnDisk.Contains(p.FileName, StringComparer.OrdinalIgnoreCase)).ToList();
            foreach (var p in toRemove)
            {
                programs.Remove(p);
                changed = true;
            }

            // Diskte olup XML'de olmayan dosyaları ekle
            var filesInList = programs.Select(p => p.FileName).ToList();
            foreach (var file in filesOnDisk)
            {
                if (!filesInList.Contains(file, StringComparer.OrdinalIgnoreCase))
                {
                    string filePath = Path.Combine(DatabaseFolder, file);
                    string programName = Path.GetFileNameWithoutExtension(file);
                    ProgramInfo newProg = new ProgramInfo(programName, file);
                    FileInfo fileInfo = new FileInfo(filePath);
                    newProg.FileSize = fileInfo.Length;
                    programs.Add(newProg);
                    changed = true;
                }
            }

            if (changed)
            {
                SavePrograms(programs);
            }

            return programs;
        }

        /// <summary>
        /// Programları kaydeder
        /// </summary>
        public static void SavePrograms(List<ProgramInfo> programs)
        {
            InitializeDatabase();
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ProgramInfo>));
                using (FileStream fileStream = new FileStream(ProgramsListFile, FileMode.Create))
                {
                    serializer.Serialize(fileStream, programs);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Programlar kaydedilirken hata oluştu: {ex.Message}", 
                    "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Yeni program ekler
        /// </summary>
        public static bool AddProgram(string exeFilePath, string name, string description = "", string version = "1.0")
        {
            InitializeDatabase();

            if (!File.Exists(exeFilePath))
            {
                System.Windows.Forms.MessageBox.Show("Seçilen dosya bulunamadı!", 
                    "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }

            string lowerPath = exeFilePath.ToLower();
            if (!lowerPath.EndsWith(".exe") && !lowerPath.EndsWith(".msi") && !lowerPath.EndsWith(".zip"))
            {
                System.Windows.Forms.MessageBox.Show("Sadece .exe, .msi veya .zip dosyaları eklenebilir!", 
                    "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }

            try
            {
                string fileName = Path.GetFileName(exeFilePath);
                string destinationPath = Path.Combine(DatabaseFolder, fileName);

                // Dosya zaten varsa, yeni isim ver
                if (File.Exists(destinationPath))
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    int counter = 1;
                    do
                    {
                        fileName = $"{nameWithoutExt}_{counter}{extension}";
                        destinationPath = Path.Combine(DatabaseFolder, fileName);
                        counter++;
                    } while (File.Exists(destinationPath));
                }

                File.Copy(exeFilePath, destinationPath, false);

                ProgramInfo programInfo = new ProgramInfo(name, fileName, description, version);
                FileInfo fileInfo = new FileInfo(destinationPath);
                programInfo.FileSize = fileInfo.Length;

                List<ProgramInfo> programs = LoadPrograms();
                programs.Add(programInfo);
                SavePrograms(programs);

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Program eklenirken hata oluştu: {ex.Message}", 
                    "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Çoklu program ekleme (Asenkron + İlerleme Bildirimi)
        /// </summary>
        public static async System.Threading.Tasks.Task<(int added, int total, List<string> duplicates)> AddProgramsAsync(string[] exeFilePaths, IProgress<(int current, string fileName)> progress = null)
        {
            InitializeDatabase();
            List<ProgramInfo> existingPrograms = LoadPrograms();
            int addedCount = 0;
            int total = exeFilePaths.Length;
            var duplicates = new List<string>();

            await System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < total; i++)
                {
                    string filePath = exeFilePaths[i];
                    string fp = filePath.ToLower();
                    if (!File.Exists(filePath) || (!fp.EndsWith(".exe") && !fp.EndsWith(".msi") && !fp.EndsWith(".zip"))) continue;

                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        string programName = Path.GetFileNameWithoutExtension(fileName);
                        string destinationPath = Path.Combine(DatabaseFolder, fileName);

                        // Aynı dosya adı zaten var mı? (büyük/küçük harf duyarsız)
                        bool alreadyExists = existingPrograms.Any(p =>
                            string.Equals(p.FileName, fileName, StringComparison.OrdinalIgnoreCase))
                            || File.Exists(destinationPath);

                        if (alreadyExists)
                        {
                            duplicates.Add(programName);
                            progress?.Report((i + 1, programName));
                            continue; // Atla, diğer programlara devam et
                        }

                        // İlerlemeyi bildir
                        progress?.Report((i + 1, programName));

                        File.Copy(filePath, destinationPath, false);

                        ProgramInfo programInfo = new ProgramInfo(programName, fileName);
                        FileInfo fileInfo = new FileInfo(destinationPath);
                        programInfo.FileSize = fileInfo.Length;

                        existingPrograms.Add(programInfo);
                        addedCount++;
                    }
                    catch { /* Hata durumunda atla */ }
                }

                if (addedCount > 0)
                {
                    SavePrograms(existingPrograms);
                }
            });

            return (addedCount, total, duplicates);
        }

        /// <summary>
        /// Programı siler
        /// </summary>
        public static bool RemoveProgram(ProgramInfo program)
        {
            try
            {
                string filePath = Path.Combine(DatabaseFolder, program.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                List<ProgramInfo> programs = LoadPrograms();
                programs.RemoveAll(p => p.Id == program.Id);
                SavePrograms(programs);

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Program silinirken hata oluştu: {ex.Message}", 
                    "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Programı çalıştırır/yükler
        /// </summary>
        public static bool InstallProgram(ProgramInfo program)
        {
            int _;
            return InstallProgram(program, waitForExit: false, exitCode: out _);
        }

        /// <summary>
        /// Programı çalıştırır/yükler. waitForExit=true ise işlem bitene kadar bekler.
        /// </summary>
        public static bool InstallProgram(ProgramInfo program, bool waitForExit, out int exitCode)
        {
            exitCode = -1;
            try
            {
                string filePath = Path.Combine(DatabaseFolder, program.FileName);
                if (!File.Exists(filePath))
                {
                    System.Windows.Forms.MessageBox.Show("Program dosyası bulunamadı!", 
                        "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return false;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "runas" // Yönetici yetkisi ile çalıştır
                };

                var proc = Process.Start(startInfo);

                if (waitForExit && proc != null)
                {
                    proc.WaitForExit();
                    exitCode = proc.ExitCode;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Program çalıştırılırken hata oluştu: {ex.Message}", 
                    "Hata", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Kurulum exe'sini başlatır ve Process döndürür (takip edebilmek için).
        /// </summary>
        public static bool StartInstallProcess(ProgramInfo program, out Process process)
        {
            process = null;
            try
            {
                string filePath = Path.Combine(DatabaseFolder, program.FileName);
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Program dosyası bulunamadı!",
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                process = Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Program çalıştırılırken hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Dosya boyutunu okunabilir formata çevirir
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Database klasörünün yolunu döndürür
        /// </summary>
        public static string GetDatabaseFolder()
        {
            return DatabaseFolder;
        }

        /// <summary>
        /// Database klasörünü tarar ve XML listesiyle senkronize eder
        /// </summary>
        public static (int added, int removed) SyncDatabase()
        {
            InitializeDatabase();
            List<ProgramInfo> existingPrograms = LoadPrograms();
            
            // Eğer LoadPrograms hata verirse boş liste döner, 
            // ama fiziksel dosyalar varsa silinmesinler diye dikkatli olmalıyız.
            // Bu yüzden LoadPrograms'ın hata durumunu kontrol etmek iyi olurdu 
            // ama mevcut yapı mesaj kutusu gösteriyor.

            var installerExts = new[] { "*.exe", "*.msi", "*.zip" };
            string[] exeFiles = installerExts.SelectMany(ext => Directory.GetFiles(DatabaseFolder, ext)).ToArray();
            
            int addedCount = 0;
            int removedCount = 0;

            // Mevcut listede olup fiziksel olarak silinmiş olanları kaldır
            var filesOnDisk = exeFiles.Select(Path.GetFileName).ToList();
            var toRemove = existingPrograms.Where(p => !filesOnDisk.Contains(p.FileName, StringComparer.OrdinalIgnoreCase)).ToList();
            foreach (var p in toRemove)
            {
                existingPrograms.Remove(p);
                removedCount++;
            }

            // Fiziksel olarak var olup listede olmayanları ekle
            var filesInList = existingPrograms.Select(p => p.FileName).ToList();
            foreach (var file in filesOnDisk)
            {
                if (!filesInList.Contains(file, StringComparer.OrdinalIgnoreCase))
                {
                    string filePath = Path.Combine(DatabaseFolder, file);
                    string programName = Path.GetFileNameWithoutExtension(file);
                    ProgramInfo newProg = new ProgramInfo(programName, file);
                    FileInfo fileInfo = new FileInfo(filePath);
                    newProg.FileSize = fileInfo.Length;
                    
                    existingPrograms.Add(newProg);
                    addedCount++;
                }
            }

            if (addedCount > 0 || removedCount > 0)
            {
                SavePrograms(existingPrograms);
            }

            return (addedCount, removedCount);
        }
    }
}
