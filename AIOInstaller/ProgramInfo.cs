using System;

namespace AIOInstaller
{
    /// <summary>
    /// Program bilgilerini tutan sınıf
    /// </summary>
    [Serializable]
    public class ProgramInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string Version { get; set; }
        public DateTime AddedDate { get; set; }
        public long FileSize { get; set; }
        /// <summary>
        /// Sessiz kurulum argümanları (katalogdan indirilen uygulamalarda doldurulur).
        /// Boşsa .exe etkileşimli çalışır; .msi her durumda msiexec ile sessiz kurulur.
        /// </summary>
        public string SilentArgs { get; set; }

        public ProgramInfo()
        {
            Id = Guid.NewGuid().ToString();
            AddedDate = DateTime.Now;
        }

        public ProgramInfo(string name, string fileName, string description = "", string version = "1.0")
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            FileName = fileName;
            Description = description;
            Version = version;
            AddedDate = DateTime.Now;
        }
    }
}
