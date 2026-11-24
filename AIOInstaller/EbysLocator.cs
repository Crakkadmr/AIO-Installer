using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AIOInstaller
{
    /// <summary>
    /// MEB EBYS (Belgenet) kurulum dosyasını programın bulunduğu klasörde bulur.
    /// Dosya "programlar" klasörüne konmaz; güncelleme gelince yeni EBYS*.exe
    /// programın yanına atılır, en yeni dosya kullanılır.
    /// </summary>
    public static class EbysLocator
    {
        public class EbysInstaller
        {
            public string FilePath { get; set; }
            public string FileName { get { return Path.GetFileName(FilePath); } }
            public string Version { get; set; }
            public long Size { get; set; }
            public DateTime Modified { get; set; }

            /// <summary>Kullanıcıya gösterilecek kısa açıklama, ör. "EBYS Belgenet 25.06.2026".</summary>
            public string DisplayName
            {
                get { return string.IsNullOrEmpty(Version) ? FileName : "EBYS Belgenet " + Version; }
            }
        }

        public static string SearchFolder
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        /// <summary>Bulunamazsa null döner.</summary>
        public static EbysInstaller Find()
        {
            try
            {
                var file = new DirectoryInfo(SearchFolder)
                    .GetFiles("EBYS*.exe", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();
                if (file == null) return null;

                return new EbysInstaller
                {
                    FilePath = file.FullName,
                    Size = file.Length,
                    Modified = file.LastWriteTime,
                    Version = ReadVersion(file.FullName)
                };
            }
            catch
            {
                return null;
            }
        }

        private static string ReadVersion(string path)
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(path);
                // Paket açıklaması "EBYS Belgenet Kurulum 25.06.2026 Installation" biçiminde geliyor
                var m = Regex.Match(info.FileDescription ?? "", @"\d{1,2}\.\d{1,2}\.\d{4}");
                if (m.Success) return m.Value;
                string v = (info.FileVersion ?? "").Trim();
                return v.Length > 0 ? v : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
