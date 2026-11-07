using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIOInstaller
{
    /// <summary>
    /// SolidShare VIP dizinlerinin (Basic auth) listesini çekip içindeki dosyaları
    /// OnlineApp olarak döndürür. Son başarılı liste programlar/solidshare.cache.json'a
    /// yazılır; böylece pencere girişsiz de açılır.
    /// </summary>
    public static class SolidShareService
    {
        public const string Category    = "SolidShare Katılımsız";
        public const string IsoCategory = "ISOlar";
        public const string ListingSource = "solidshare-listing";

        /// <summary>Taranan bir SolidShare dizini.</summary>
        public class SourceDir
        {
            public string   Url;
            public string   Category;
            public string   Icon;
            public string   DescPrefix;
            public string[] Exts;
        }

        public static readonly SourceDir[] Sources =
        {
            new SourceDir
            {
                Url = "https://vip.solidshare.net/katilimsiz/", Category = Category, Icon = "📦",
                DescPrefix = "SolidShare katılımsız paket",
                Exts = new[] { ".zip", ".exe", ".msi" }
            },
            new SourceDir
            {
                Url = "https://vip.solidshare.net/sistem/", Category = IsoCategory, Icon = "💿",
                DescPrefix = "SolidShare sistem dosyası",
                Exts = new[] { ".iso", ".img", ".zip", ".7z", ".rar", ".exe", ".msi", ".esd", ".wim" }
            },
        };

        private const int MaxDepth       = 2;     // alt klasörlere en fazla bu kadar in
        private const int MaxParallel    = 6;     // aynı anda en fazla bu kadar istek
        private const int RequestTimeout = 20000; // ms

        private static string CacheFile => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "programlar", "solidshare.cache.json");

        /// <summary>Kullanıcı adı / şifre hatalıysa fırlatılır.</summary>
        public class UnauthorizedException : Exception
        {
            public UnauthorizedException() : base("VIP kullanıcı adı veya şifre hatalı.") { }
        }

        public class FetchResult
        {
            public List<OnlineApp> Apps   = new List<OnlineApp>();
            /// <summary>Okunamayan dizinler (ör. "sistem: erişim yok").</summary>
            public List<string>    Errors = new List<string>();
        }

        // ─── Sunucudan çek ────────────────────────────────────────────────
        /// <summary>
        /// Tüm kaynak dizinleri paralel tarar. Her kaynak kökünde 401 alınırsa
        /// UnauthorizedException; yalnızca bazıları okunamazsa Errors'a yazılır.
        /// </summary>
        public static FetchResult FetchAll(string user, string pass)
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            if (ServicePointManager.DefaultConnectionLimit < MaxParallel * 2)
                ServicePointManager.DefaultConnectionLimit = MaxParallel * 2; // varsayılan 2 → istekler sıraya giriyordu

            string auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes((user ?? "") + ":" + (pass ?? "")));

            var perSource = new FetchResult[Sources.Length];
            var unauthorized = new bool[Sources.Length];

            Parallel.For(0, Sources.Length, i =>
            {
                perSource[i] = new FetchResult();
                try
                {
                    perSource[i].Apps = CrawlSource(Sources[i], auth);
                }
                catch (UnauthorizedException)
                {
                    unauthorized[i] = true;
                    perSource[i].Errors.Add($"{Sources[i].Category}: erişim yok (401)");
                }
                catch (Exception ex)
                {
                    perSource[i].Errors.Add($"{Sources[i].Category}: {ex.Message}");
                }
            });

            if (unauthorized.All(u => u)) throw new UnauthorizedException();

            var result = new FetchResult();
            foreach (var r in perSource)
            {
                result.Apps.AddRange(r.Apps);
                result.Errors.AddRange(r.Errors);
            }
            result.Apps = result.Apps
                .GroupBy(a => a.DownloadUrl, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            return result;
        }

        /// <summary>Bir kaynağı seviye seviye (BFS) tarar; her seviyedeki klasörler paralel çekilir.</summary>
        private static List<OnlineApp> CrawlSource(SourceDir src, string auth)
        {
            var root    = new Uri(src.Url);
            var apps    = new ConcurrentBag<OnlineApp>();
            var visited = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            // Kök: hata olursa yukarı fırlat (401 → UnauthorizedException)
            visited[root.AbsoluteUri] = true;
            var rootListing = ParseListing(GetString(root, auth), root, root, src.Exts);
            foreach (var f in rootListing.Files) apps.Add(ToApp(src, f.Url, f.Size));

            var level = rootListing.Dirs;
            for (int depth = 1; depth <= MaxDepth && level.Count > 0; depth++)
            {
                var next = new ConcurrentBag<Uri>();
                Parallel.ForEach(level, new ParallelOptions { MaxDegreeOfParallelism = MaxParallel }, dir =>
                {
                    if (!visited.TryAdd(dir.AbsoluteUri, true)) return;
                    string html;
                    try { html = GetString(dir, auth); }
                    catch { return; } // alt klasör okunamadı → atla
                    var l = ParseListing(html, dir, root, src.Exts);
                    foreach (var f in l.Files) apps.Add(ToApp(src, f.Url, f.Size));
                    foreach (var d in l.Dirs) next.Add(d);
                });
                level = next.Distinct().ToList();
            }
            return apps.ToList();
        }

        /// <summary>
        /// Tek istek: Basic auth başlığı peşin gönderilir (401 → tekrar turu yok),
        /// gzip açık, zaman aşımı var.
        /// </summary>
        private static string GetString(Uri url, string auth)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Headers[HttpRequestHeader.Authorization] = auth;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.Timeout = RequestTimeout;
            req.ReadWriteTimeout = RequestTimeout;
            req.UserAgent = "AIOInstaller";

            try
            {
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var stream = resp.GetResponseStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
            catch (WebException wex) when (
                wex.Response is HttpWebResponse hr && hr.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException();
            }
        }

        // ─── HTML dizin listesini ayrıştır (Apache/nginx/LiteSpeed autoindex) ──
        public class Listing
        {
            public List<(Uri Url, string Size)> Files = new List<(Uri, string)>();
            public List<Uri> Dirs = new List<Uri>();
        }

        private static readonly Regex AnchorRx = new Regex(
            @"<a\s[^>]*?href\s*=\s*[""']([^""'#]+)[""'][^>]*>.*?</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex TagRx = new Regex(@"<[^>]+>", RegexOptions.Compiled);

        // "12M", "1.5G", "834K", "12345678", "12.3 MB"
        private static readonly Regex SizeRx = new Regex(
            @"(?<![\d:.\-/])(\d+(?:[.,]\d+)?)\s*([KMGT])?(?:i?B)?(?![\d:\-/])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Listing ParseListing(string html, Uri pageUri, Uri root, string[] exts)
        {
            var listing = new Listing();
            if (string.IsNullOrEmpty(html)) return listing;

            var matches = AnchorRx.Matches(html);
            for (int i = 0; i < matches.Count; i++)
            {
                string href = WebUtility.HtmlDecode(matches[i].Groups[1].Value).Trim();
                if (href.Length == 0 || href.StartsWith("?") ||
                    href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!Uri.TryCreate(pageUri, href, out Uri abs)) continue;

                // Yalnızca aynı sunucu ve kaynak klasörünün altı (üst dizin linklerini ele)
                if (!string.Equals(abs.Host, root.Host, StringComparison.OrdinalIgnoreCase)) continue;
                string path = abs.AbsolutePath;
                if (!path.StartsWith(root.AbsolutePath, StringComparison.OrdinalIgnoreCase)) continue;
                if (path.Length <= pageUri.AbsolutePath.Length) continue; // kendisi veya üst dizin

                abs = new UriBuilder(abs) { Query = "", Fragment = "" }.Uri;

                if (path.EndsWith("/"))
                {
                    listing.Dirs.Add(abs);
                    continue;
                }

                string ext = Path.GetExtension(Uri.UnescapeDataString(path)).ToLowerInvariant();
                if (!exts.Contains(ext)) continue;

                // Boyut: linkten sonra, bir sonraki linke kadar olan metin
                int start = matches[i].Index + matches[i].Length;
                int end   = i + 1 < matches.Count ? matches[i + 1].Index : html.Length;
                string tail = TagRx.Replace(html.Substring(start, Math.Min(end - start, 400)), " ");
                listing.Files.Add((abs, ParseSize(WebUtility.HtmlDecode(tail))));
            }
            return listing;
        }

        /// <summary>Satırdaki son boyut ifadesini "~N MB" biçimine çevirir; bulamazsa "?".</summary>
        public static string ParseSize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "?";

            Match last = null;
            foreach (Match m in SizeRx.Matches(text))
            {
                // Tarih/saat parçalarını değil, birimli ya da bayt sayısı gibi duranları al
                bool hasUnit = m.Groups[2].Success;
                bool looksLikeBytes = !hasUnit && m.Groups[1].Value.Length >= 4 && !m.Groups[1].Value.Contains(".");
                if (hasUnit || looksLikeBytes) last = m;
            }
            if (last == null) return "?";

            if (!double.TryParse(last.Groups[1].Value.Replace(',', '.'),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double n))
                return "?";

            double mb;
            switch (last.Groups[2].Success ? char.ToUpperInvariant(last.Groups[2].Value[0]) : 'B')
            {
                case 'K': mb = n / 1024; break;
                case 'M': mb = n; break;
                case 'G': mb = n * 1024; break;
                case 'T': mb = n * 1024 * 1024; break;
                default:  mb = n / (1024 * 1024); break;
            }
            if (mb < 1) return "<1 MB";
            return mb >= 1024 ? $"~{mb / 1024:0.#} GB" : $"~{Math.Round(mb):0} MB";
        }

        /// <summary>"Adobe.Photoshop.2026.zip" → "Adobe Photoshop 2026" (sürüm noktaları korunur).</summary>
        public static string NameFromFile(string fileName)
        {
            string n = Path.GetFileNameWithoutExtension(Uri.UnescapeDataString(fileName ?? ""));
            n = Regex.Replace(n, @"(?<!\d)\.|\.(?!\d)", " ");
            n = n.Replace('_', ' ');
            n = Regex.Replace(n, @"\s+", " ").Trim();
            return n.Length > 0 ? n : fileName;
        }

        /// <summary>Bu girdi dizin listesinden mi üretildi (katalogda elle yazılmış değil)?</summary>
        public static bool IsFromListing(OnlineApp a) =>
            a != null && a.Source == ListingSource;

        private static OnlineApp ToApp(SourceDir src, Uri url, string size)
        {
            string file = url.Segments.Last();
            return new OnlineApp(
                NameFromFile(file),
                src.DescPrefix + " (" + Uri.UnescapeDataString(file) + ")",
                src.Category,
                url.AbsoluteUri,
                "",
                "Son",
                size,
                src.Icon)
            { Source = ListingSource };
        }

        // ─── Katalogla birleştir ──────────────────────────────────────────
        /// <summary>
        /// Listedeki uygulamaları, aynı indirme linki veya aynı ad zaten yoksa ekler.
        /// Katalogdaki elle yazılmış girdiler (açıklama/ikon) öncelikli kalır.
        /// </summary>
        public static int MergeInto(List<OnlineApp> apps, IEnumerable<OnlineApp> listed)
        {
            if (listed == null) return 0;
            var urls  = new HashSet<string>(apps.Select(a => NormalizeUrl(a.DownloadUrl)), StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(apps.Select(a => a.Name ?? ""), StringComparer.OrdinalIgnoreCase);

            int added = 0;
            foreach (var a in listed)
            {
                if (urls.Contains(NormalizeUrl(a.DownloadUrl)) || names.Contains(a.Name ?? "")) continue;
                apps.Add(a);
                urls.Add(NormalizeUrl(a.DownloadUrl));
                names.Add(a.Name ?? "");
                added++;
            }
            return added;
        }

        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            try { return Uri.UnescapeDataString(new Uri(url).AbsoluteUri); } catch { return url; }
        }

        // ─── Önbellek ─────────────────────────────────────────────────────
        public static void SaveCache(List<OnlineApp> apps)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CacheFile));
                using (var ms = new MemoryStream())
                {
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(List<OnlineApp>));
                    ser.WriteObject(ms, apps);
                    File.WriteAllBytes(CacheFile, ms.ToArray());
                }
            }
            catch { }
        }

        public static List<OnlineApp> LoadCache()
        {
            try
            {
                if (!File.Exists(CacheFile)) return new List<OnlineApp>();
                using (var fs = File.OpenRead(CacheFile))
                {
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(List<OnlineApp>));
                    var list = (List<OnlineApp>)ser.ReadObject(fs) ?? new List<OnlineApp>();
                    // Eski sürüm önbelleğinde Source alanı yoktu
                    foreach (var a in list) a.Source = ListingSource;
                    return list;
                }
            }
            catch { return new List<OnlineApp>(); }
        }
    }
}
