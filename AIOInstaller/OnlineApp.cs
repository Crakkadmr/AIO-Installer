using System;

namespace AIOInstaller
{
    /// <summary>
    /// İnternetten indirilebilecek bir uygulamanın bilgilerini tutar.
    /// </summary>
    public class OnlineApp
    {
        public string Name        { get; set; }
        public string Description { get; set; }
        public string Category    { get; set; }
        public string DownloadUrl { get; set; }
        /// <summary>Sessiz kurulum için kullanılacak argümanlar.</summary>
        public string SilentArgs  { get; set; }
        public string Version     { get; set; }
        public string SizeMB      { get; set; }
        public string IconEmoji   { get; set; }

        public OnlineApp() { }

        public OnlineApp(string name, string description, string category,
                         string downloadUrl, string silentArgs,
                         string version = "Son", string sizeMB = "?",
                         string iconEmoji = "💿")
        {
            Name        = name;
            Description = description;
            Category    = category;
            DownloadUrl = downloadUrl;
            SilentArgs  = silentArgs;
            Version     = version;
            SizeMB      = sizeMB;
            IconEmoji   = iconEmoji;
        }
    }
}
