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
        public static string DatabaseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "programlar");
        private static string ProgramsXmlFile = Path.Combine(DatabaseFolder, "Programs.xml");
        private static string ProgramsListFile = Path.Combine(DatabaseFolder, "ProgramsList.xml");

        /// <summary>
        /// programlar klasörünü oluşturur
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

                var proc = Process.Start(BuildInstallStartInfo(filePath, program.SilentArgs));

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

                process = Process.Start(BuildInstallStartInfo(filePath, program.SilentArgs));
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Program çalıştırılırken hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>MSI için argüman verilmemişse kullanılan varsayılan sessiz parametreler.</summary>
        public const string DefaultMsiArgs = "/qn /norestart";

        /// <summary>
        /// Kurulum dosyasının tipine göre başlatma bilgisini üretir:
        /// .msi → msiexec /i "dosya" + (SilentArgs veya /qn /norestart),
        /// .exe → dosya + SilentArgs (boşsa etkileşimli), diğerleri (ör. .zip) → kabukla aç.
        /// </summary>
        public static ProcessStartInfo BuildInstallStartInfo(string filePath, string silentArgs)
        {
            string ext = (Path.GetExtension(filePath) ?? "").ToLowerInvariant();
            string args = (silentArgs ?? "").Trim();

            var psi = new ProcessStartInfo
            {
                UseShellExecute = true,
                Verb = "runas", // Yönetici yetkisi ile çalıştır
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? ""
            };

            if (ext == ".msi")
            {
                psi.FileName = "msiexec.exe";
                psi.Arguments = $"/i \"{filePath}\" {(args.Length > 0 ? args : DefaultMsiArgs)}";
            }
            else
            {
                psi.FileName = filePath;
                if (ext == ".exe" && args.Length > 0)
                    psi.Arguments = args;
            }
            return psi;
        }

        public enum InstallOutcome { Success, RebootRequired, Cancelled, Busy, Failed }

        /// <summary>
        /// Kurulum çıkış kodunu yorumlar (MSI/Windows Installer standart kodları + genel 0/≠0).
        /// </summary>
        public static (InstallOutcome outcome, string text) DescribeExitCode(int code)
        {
            switch (code)
            {
                case 0:    return (InstallOutcome.Success, "Tamamlandı");
                case 3010: return (InstallOutcome.RebootRequired, "Tamamlandı — yeniden başlatma gerekli");
                case 1641: return (InstallOutcome.RebootRequired, "Tamamlandı — yeniden başlatma başlatıldı");
                case 1602: return (InstallOutcome.Cancelled, "Kullanıcı iptal etti (1602)");
                case 1618: return (InstallOutcome.Busy, "Başka bir kurulum sürüyor (1618)");
                case 1603: return (InstallOutcome.Failed, "Kurulum hatası (1603)");
                case 1638: return (InstallOutcome.Failed, "Başka bir sürümü zaten kurulu (1638)");
                default:   return (InstallOutcome.Failed, $"Hata (Çıkış: {code})");
            }
        }

        /// <summary>
        /// Katalogdan indirilen dosyanın kaydına sessiz kurulum argümanlarını yazar.
        /// Dosya henüz listede değilse LoadPrograms senkronizasyonu onu ekler.
        /// </summary>
        public static void SetSilentArgs(string fileName, string silentArgs)
        {
            var programs = LoadPrograms();
            var prog = programs.FirstOrDefault(p =>
                string.Equals(p.FileName, fileName, StringComparison.OrdinalIgnoreCase));
            if (prog == null) return;

            string args = string.IsNullOrWhiteSpace(silentArgs) ? null : silentArgs.Trim();
            if (prog.SilentArgs == args) return;
            prog.SilentArgs = args;
            SavePrograms(programs);
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
        /// programlar klasörünün yolunu döndürür
        /// </summary>
        public static string GetDatabaseFolder()
        {
            return DatabaseFolder;
        }

        /// <summary>
        /// programlar klasörünü tarar ve XML listesiyle senkronize eder
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

        /// <summary>
        /// Bir PowerShell betiğini (uygulama zaten yönetici olduğu için ek UAC istemeden)
        /// çalıştırır, pencere açmaz ve stdout+stderr çıktısını metin olarak döndürür.
        /// Tam Kurulum orkestrasyonunda adımların sonucunu okumak için kullanılır.
        /// </summary>
        public static string RunPowerShellScript(string scriptContent, int timeoutMs = 180000)
        {
            string tmpFile = null;
            try
            {
                tmpFile = Path.Combine(Path.GetTempPath(), $"aio_{Guid.NewGuid():N}.ps1");
                File.WriteAllText(tmpFile, scriptContent, new System.Text.UTF8Encoding(false));

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tmpFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(startInfo))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    if (!proc.WaitForExit(timeoutMs))
                    {
                        try { proc.Kill(); } catch { }
                    }
                    return ((output ?? "") + (error ?? "")).Trim();
                }
            }
            catch (Exception ex)
            {
                return "ERR:" + ex.Message;
            }
            finally
            {
                try { if (tmpFile != null) File.Delete(tmpFile); } catch { }
            }
        }

        /// <summary>
        /// Windows Defender gerçek zamanlı korumasının açık olup olmadığını döndürür.
        /// true=açık, false=kapalı, null=durum okunamadı.
        /// </summary>
        public static bool? IsDefenderRealtimeOn()
        {
            string result = RunPowerShellScript(
                "try { if ((Get-MpComputerStatus).RealTimeProtectionEnabled) { 'ON' } else { 'OFF' } } catch { 'UNK' }",
                30000);

            if (result.IndexOf("ON", StringComparison.OrdinalIgnoreCase) >= 0 &&
                result.IndexOf("OFF", StringComparison.OrdinalIgnoreCase) < 0)
                return true;
            if (result.IndexOf("OFF", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return null;
        }

        /// <summary>
        /// Sistemde bir kablosuz (Wi-Fi) ağ arayüzü var mı?
        /// </summary>
        public static bool HasWifiAdapter()
        {
            string result = RunPowerShellScript(
                "try { if (Get-NetAdapter -ErrorAction Stop | Where-Object { $_.PhysicalMediaType -like '*802.11*' -or $_.InterfaceDescription -like '*Wireless*' -or $_.Name -like '*Wi-Fi*' }) { 'YES' } else { 'NO' } } catch { if (netsh wlan show interfaces 2>$null | Select-String 'SSID') { 'YES' } else { 'NO' } }",
                30000);
            return result.IndexOf("YES", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Herhangi bir bağlantı (kablolu veya Wi-Fi) üzerinden internete erişim var mı?
        /// Önce Windows ağ profillerine (NCSI) bakar, sonra msftconnecttest.com'a HTTP isteği atar.
        /// Bağlantı yeni kurulduysa NCSI geç güncellenebildiği için birkaç kez dener.
        /// </summary>
        public static bool HasInternetConnection()
        {
            string result = RunPowerShellScript(
                "$ErrorActionPreference='SilentlyContinue'\r\n" +
                "for ($i = 0; $i -lt 3; $i++) {\r\n" +
                "  if (Get-NetConnectionProfile | Where-Object { $_.IPv4Connectivity -eq 'Internet' -or $_.IPv6Connectivity -eq 'Internet' }) { 'NET_OK'; return }\r\n" +
                "  try {\r\n" +
                "    $r = Invoke-WebRequest -Uri 'http://www.msftconnecttest.com/connecttest.txt' -UseBasicParsing -TimeoutSec 5\r\n" +
                "    if ($r.Content -like '*Microsoft Connect Test*') { 'NET_OK'; return }\r\n" +
                "  } catch {}\r\n" +
                "  Start-Sleep -Seconds 3\r\n" +
                "}\r\n" +
                "'NET_NO'",
                60000);
            return result.IndexOf("NET_OK", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
