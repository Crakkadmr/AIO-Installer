<div align="center">

# 🖥️ AIO Yazılım Yükleyici

**Hepsi Bir Arada — Windows Program Yöneticisi**

![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/.NET_Framework-4.8-512BD4?style=for-the-badge&logo=dotnet)
![Language](https://img.shields.io/badge/C%23-WinForms-239120?style=for-the-badge&logo=csharp)
![Made by](https://img.shields.io/badge/Made_by-Demircan-3B82F6?style=for-the-badge)

> Birden fazla kurulum dosyasını tek tıkla toplu yükleyin. Yerel ve çevrimiçi program yönetimini tek bir arayüzde birleştiren modern, dark temalı bir Windows uygulaması.

</div>

---

## ✨ Özellikler

### 📦 Yerel Program Yönetimi
| Özellik | Açıklama |
|---|---|
| ➕ **Program Ekle** | `.exe` dosyalarını veritabanına ekler, otomatik kopyalar |
| ▶ **Tekli / Toplu Kur** | Seçili programları sırayla yönetici yetkisiyle çalıştırır |
| 🗑 **Sil** | Programı veritabanından ve diskten kaldırır |
| 🔄 **Yenile** | Disk ve XML listesini otomatik senkronize eder |
| 📂 **Klasör** | Veritabanı klasörünü Dosya Gezgini'nde açar |

### 🌐 Online İndirme & Kurulum
Kategori bazlı, **katılımsız (sessiz) kurulum** destekli hazır uygulama kataloğu:

| Kategori | Uygulamalar |
|---|---|
| 🌐 Tarayıcı | Chrome, Firefox, Brave |
| 🎬 Medya | VLC Media Player, OBS Studio |
| 🗜 Araçlar | 7-Zip, WinRAR, AnyDesk, TeamViewer |
| 🖊 Geliştirici | VS Code, Git, Notepad++, Python 3 |
| 📹 İletişim | Zoom, Discord |

### 🎨 Arayüz
- **Dark tema** — Koyu lacivert (`#0F172A`) arka plan, modern flat tasarım
- **Toplu seçim** — Checkbox ile istediğin programları seç, hepsini tek seferde kur
- **Gerçek zamanlı ilerleme** — Progress bar ve durum metni ile kurulum takibi
- **İptal desteği** — Kurulum sırasında istediğin zaman durdurabilirsin
- **Otomatik DB senkronizasyonu** — Klasöre EXE ekle, liste otomatik güncellenir

---

## 🚀 Kullanım

### Yerel Program Ekleme & Kurma
```
1. "➕ Program Ekle" butonuna tıkla
2. Eklemek istediğin .exe dosyalarını seç (çoklu seçim desteklenir)
3. Listeden kurmak istediğin programları işaretle (✓)
4. "▶ Seçilenleri Kur" butonuna tıkla → Programlar sırayla yüklenir
```

### Online Program İndirme
```
1. "🌐 İndir" butonuna tıkla
2. Sol panelden kategori seç
3. İstediğin uygulamanın yanındaki "⬇ İndir & Kur" butonuna bas
4. Program otomatik indirilir ve sessiz kurulum başlar
```

### İpuçları
- Satıra **çift tıklayarak** tek program hızlıca kurulabilir
- **Tümünü Seç** butonu tüm listeyi işaretler
- Kurulum devam ederken tikini kaldırdığın program **atlanır**

---

## 🗂️ Proje Yapısı

```
AIO-Installer/
├── AIOInstaller/
│   ├── MainForm.cs          # Ana ekran — program listesi, toplu kurulum
│   ├── OnlineAppsForm.cs    # Online indirme ekranı — kategori & kart arayüzü
│   ├── OnlineApp.cs         # Online uygulama modeli
│   ├── ProgramManager.cs    # Veritabanı yönetimi, kurulum, senkronizasyon
│   ├── ProgramInfo.cs       # Program veri modeli (XML serialize)
│   └── Properties/
│       └── AssemblyInfo.cs
├── AIOInstaller.sln
└── README.md
```

### Veritabanı
Uygulama, çalıştığı dizinde bir `Database/` klasörü oluşturur:
```
Database/
├── ProgramsList.xml    ← Program meta verisi (isim, versiyon, boyut, tarih)
├── Chrome.exe
├── VLC.exe
└── ...
```

---

## 🛠️ Gereksinimler

| Gereksinim | Detay |
|---|---|
| **İşletim Sistemi** | Windows 10/11 |
| **.NET Framework** | 4.8 veya üstü |
| **Çalışma Yetkisi** | Yönetici (Admin) — kurulum için gerekli |

---

## ⚙️ Derleme

```bash
# Visual Studio ile
Dosya → Aç → AIOInstaller.sln → Build → Release

# MSBuild ile
msbuild AIOInstaller.sln /p:Configuration=Release
```

Çıktı: `AIOInstaller/bin/Release/AIOInstaller.exe`

---

## 📝 Lisans

Bu proje kişisel kullanım amacıyla geliştirilmiştir.

---

<div align="center">
Made with ❤️ by <strong>Demircan</strong>
</div>
