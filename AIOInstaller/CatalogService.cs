using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AIOInstaller
{
    /// <summary>
    /// Bir kurulum profili: ilgili uygulamaların adlarını tutar (katalogdaki Name ile eşleşir).
    /// </summary>
    public class InstallProfile
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public List<string> Apps { get; set; }

        public InstallProfile()
        {
            Apps = new List<string>();
        }
    }

    /// <summary>
    /// Uzaktan/gömülü katalog verisi: uygulama listesi + profiller.
    /// </summary>
    public class CatalogData
    {
        public List<OnlineApp> Apps { get; set; }
        public List<InstallProfile> Profiles { get; set; }

        public CatalogData()
        {
            Apps = new List<OnlineApp>();
            Profiles = new List<InstallProfile>();
        }
    }

    /// <summary>
    /// Online uygulama kataloğunu yönetir. Önce GitHub'daki uzak catalog.json dosyasını
    /// dener (linkler derlemeden güncellenebilsin diye), başarılı olursa yerel önbelleğe
    /// yazar. İnternet yoksa önbelleği, o da yoksa uygulamaya gömülü listeyi kullanır.
    /// </summary>
    public static class CatalogService
    {
        // Katalog burada tutulur; bir linki düzeltmek için SADECE bu dosyayı güncelleyip
        // push etmek yeterli — uygulamayı yeniden derlemeye gerek yok.
        public const string RemoteUrl =
            "https://raw.githubusercontent.com/Crakkadmr/AIO-Installer/main/catalog.json";

        private static string CacheFile =>
            Path.Combine(ProgramManager.DatabaseFolder, "catalog.cache.json");

        /// <summary>
        /// Katalogu yükler. <paramref name="fallback"/> uzak kaynak ve önbellek
        /// başarısız olursa döndürülecek gömülü listedir. Asla null dönmez.
        /// </summary>
        public static CatalogData Load(CatalogData fallback)
        {
            // 1) Uzak kaynak (GitHub)
            try
            {
                string json = HttpGet(RemoteUrl, 8000);
                CatalogData data = Parse(json);
                if (IsValid(data))
                {
                    try
                    {
                        ProgramManager.InitializeDatabase();
                        File.WriteAllText(CacheFile, json, new UTF8Encoding(false));
                    }
                    catch { /* önbelleğe yazılamazsa sorun değil */ }
                    return Normalize(data);
                }
            }
            catch { /* internet yok / sunucu hatası → sonraki adıma düş */ }

            // 2) Yerel önbellek (en son başarılı indirme)
            try
            {
                if (File.Exists(CacheFile))
                {
                    CatalogData data = Parse(File.ReadAllText(CacheFile));
                    if (IsValid(data))
                        return Normalize(data);
                }
            }
            catch { }

            // 3) Gömülü liste
            return fallback ?? new CatalogData();
        }

        private static bool IsValid(CatalogData d)
        {
            return d != null && d.Apps != null && d.Apps.Count > 0;
        }

        private static CatalogData Normalize(CatalogData d)
        {
            if (d.Profiles == null) d.Profiles = new List<InstallProfile>();
            foreach (var a in d.Apps)
            {
                if (string.IsNullOrEmpty(a.Version)) a.Version = "Son";
                if (string.IsNullOrEmpty(a.SizeMB)) a.SizeMB = "?";
                if (string.IsNullOrEmpty(a.IconEmoji)) a.IconEmoji = "💿";
            }
            return d;
        }

        private static string HttpGet(string url, int timeoutMs)
        {
            // raw.githubusercontent TLS 1.2 ister
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch { }

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.UserAgent = "AIOInstaller";
            req.CachePolicy = new System.Net.Cache.RequestCachePolicy(
                System.Net.Cache.RequestCacheLevel.NoCacheNoStore);

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var stream = resp.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static CatalogData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var ser = new DataContractJsonSerializer(typeof(CatalogData));
                return (CatalogData)ser.ReadObject(ms);
            }
        }
    }
}
