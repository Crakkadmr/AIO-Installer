using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AIOInstaller
{
    /// <summary>Kayıtlı bir Wi-Fi ağı. Tam Kurulum bu listeyi sırayla dener.</summary>
    [DataContract]
    public class WifiNetwork
    {
        [DataMember(Order = 1)] public string Ssid { get; set; }
        [DataMember(Order = 2)] public string Password { get; set; }

        /// <summary>"WPA2", "WPA3" veya "OPEN".</summary>
        [DataMember(Order = 3)] public string Security { get; set; }

        public WifiNetwork()
        {
            Security = "WPA2";
        }

        public string SecurityLabel
        {
            get
            {
                switch ((Security ?? "").ToUpperInvariant())
                {
                    case "WPA3": return "WPA3-Kişisel";
                    case "OPEN": return "Açık (şifresiz)";
                    default: return "WPA2-Kişisel";
                }
            }
        }
    }

    /// <summary>
    /// Kalıcı uygulama ayarları. Programın bulunduğu klasördeki <c>ayarlar.json</c>
    /// dosyasında düz metin olarak saklanır (USB'de taşınabilir olsun diye).
    /// </summary>
    [DataContract]
    public class AppSettings
    {
        public const string FileName = "ayarlar.json";

        [DataMember(Order = 1)] public List<WifiNetwork> WifiNetworks { get; set; }

        /// <summary>EBYS kurulum dosyasına verilecek ek parametreler (boşsa pencereli kurulum).</summary>
        [DataMember(Order = 2)] public string EbysArguments { get; set; }

        public AppSettings()
        {
            WifiNetworks = new List<WifiNetwork>();
            EbysArguments = "";
        }

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName); }
        }

        private static AppSettings current;

        /// <summary>Bellekteki ayarlar; ilk erişimde diskten okunur.</summary>
        public static AppSettings Current
        {
            get
            {
                if (current == null) current = Load();
                return current;
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    using (var fs = File.OpenRead(FilePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(AppSettings));
                        var s = (AppSettings)ser.ReadObject(fs);
                        if (s.WifiNetworks == null) s.WifiNetworks = new List<WifiNetwork>();
                        if (s.EbysArguments == null) s.EbysArguments = "";
                        return s;
                    }
                }
            }
            catch
            {
                // Bozuk dosya: varsayılanlarla devam et (dosyanın üstüne ancak kaydedilince yazılır)
            }
            return new AppSettings();
        }

        /// <summary>Ayarları diske yazar. Başarısızsa hata mesajını döndürür, başarılıysa null.</summary>
        public string Save()
        {
            try
            {
                string tmp = FilePath + ".tmp";
                using (var fs = File.Create(tmp))
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fs, new UTF8Encoding(false), true, true, "  "))
                {
                    new DataContractJsonSerializer(typeof(AppSettings)).WriteObject(writer, this);
                    writer.Flush();
                }
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
