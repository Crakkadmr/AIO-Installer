using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AIOInstaller
{
    /// <summary>netsh wlan üzerinden ağ tarama ve kayıtlı ağlara bağlanma.</summary>
    public static class WifiService
    {
        private static string RunNetsh(string args, int timeoutMs = 20000)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh.exe", args)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage)
                };
                using (var p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(timeoutMs);
                    return output ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        private static string Quote(string s)
        {
            return "\"" + (s ?? "").Replace("\"", "") + "\"";
        }

        /// <summary>Çevredeki görünür ağların adları (boş adlar hariç, tekrarsız).</summary>
        public static List<string> ScanSsids()
        {
            string output = RunNetsh("wlan show networks");
            var list = new List<string>();
            foreach (Match m in Regex.Matches(output, @"^\s*SSID\s+\d+\s*:\s*(.*?)\s*$", RegexOptions.Multiline))
            {
                string ssid = m.Groups[1].Value;
                if (ssid.Length > 0 && !list.Contains(ssid)) list.Add(ssid);
            }
            return list;
        }

        /// <summary>Şu an bağlı olunan ağın adı; bağlı değilse null.</summary>
        public static string ConnectedSsid()
        {
            string output = RunNetsh("wlan show interfaces");
            var m = Regex.Match(output, @"^\s*SSID\s*:\s*(.*?)\s*$", RegexOptions.Multiline);
            return m.Success && m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : null;
        }

        private static string BuildProfileXml(WifiNetwork net)
        {
            string ssid = SecurityElement.Escape(net.Ssid ?? "");
            string sec = (net.Security ?? "WPA2").ToUpperInvariant();

            string auth = sec == "WPA3" ? "WPA3SAE" : sec == "OPEN" ? "open" : "WPA2PSK";
            string enc = sec == "OPEN" ? "none" : "AES";
            string sharedKey = sec == "OPEN" ? "" :
                "<sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>" +
                SecurityElement.Escape(net.Password ?? "") + "</keyMaterial></sharedKey>";

            return "<?xml version=\"1.0\"?>" +
                   "<WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\">" +
                   "<name>" + ssid + "</name>" +
                   "<SSIDConfig><SSID><name>" + ssid + "</name></SSID></SSIDConfig>" +
                   "<connectionType>ESS</connectionType><connectionMode>auto</connectionMode>" +
                   "<MSM><security><authEncryption>" +
                   "<authentication>" + auth + "</authentication><encryption>" + enc + "</encryption>" +
                   "<useOneX>false</useOneX></authEncryption>" + sharedKey + "</security></MSM>" +
                   "</WLANProfile>";
        }

        /// <summary>Tek bir ağa bağlanmayı dener; bağlanırsa true.</summary>
        public static bool Connect(WifiNetwork net, int waitSeconds = 12)
        {
            if (net == null || string.IsNullOrWhiteSpace(net.Ssid)) return false;

            string tmp = Path.Combine(Path.GetTempPath(), "aio_wifi_" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                File.WriteAllText(tmp, BuildProfileXml(net), new UTF8Encoding(false));
                RunNetsh("wlan add profile filename=" + Quote(tmp) + " user=all");
                RunNetsh("wlan connect name=" + Quote(net.Ssid) + " ssid=" + Quote(net.Ssid));

                for (int i = 0; i < waitSeconds; i++)
                {
                    Thread.Sleep(1000);
                    if (string.Equals(ConnectedSsid(), net.Ssid, StringComparison.Ordinal)) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }

        /// <summary>
        /// Kayıtlı ağları sırayla dener. Önce çevrede görünenleri dener; tarama sonuç
        /// vermezse (ör. tarama izni yok) listedeki her ağı dener. Bağlanılan ağın adını
        /// döndürür, hiçbirine bağlanamazsa null.
        /// </summary>
        public static string ConnectToFirstAvailable(IList<WifiNetwork> networks, Action<string> report)
        {
            if (networks == null || networks.Count == 0) return null;

            string already = ConnectedSsid();
            if (already != null && networks.Any(n => n.Ssid == already)) return already;

            var visible = ScanSsids();
            var candidates = visible.Count > 0
                ? networks.Where(n => visible.Contains(n.Ssid)).ToList()
                : networks.ToList();

            foreach (var net in candidates)
            {
                if (report != null) report(net.Ssid);
                if (Connect(net)) return net.Ssid;
            }
            return null;
        }
    }
}
