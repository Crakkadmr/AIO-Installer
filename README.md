# AIO Program Kurucu

C# tabanlı, .NET Framework 4.8 altyapılı All-in-One program kurucu uygulaması.

## Özellikler

- ✅ Program ekleme: Kendi .exe dosyalarınızı Database klasörüne ekleyebilirsiniz
- ✅ Program listeleme: Eklenen tüm programları görüntüleyebilirsiniz
- ✅ Program yükleme: Seçilen programları çalıştırabilir/yükleyebilirsiniz
- ✅ Program silme: Eklenen programları silebilirsiniz
- ✅ Database klasörü yönetimi: Database klasörünü doğrudan açabilirsiniz

## Kurulum

1. Visual Studio 2019 veya üzeri bir sürüm ile projeyi açın
2. .NET Framework 4.8'in yüklü olduğundan emin olun
3. Projeyi derleyin (Build > Build Solution)

## Kullanım

1. Uygulamayı çalıştırın
2. "Program Ekle" butonuna tıklayarak .exe dosyalarınızı ekleyin
3. Program bilgilerini girin (Ad, Açıklama, Versiyon)
4. Eklenen programlar otomatik olarak Database klasörüne kopyalanır
5. "Yükle/Kur" butonu ile programları çalıştırabilirsiniz
6. "Database Klasörünü Aç" butonu ile Database klasörüne doğrudan erişebilirsiniz

## Klasör Yapısı

```
AIOInstaller/
├── Database/              # Exe dosyalarının saklandığı klasör
│   ├── ProgramsList.xml  # Program bilgilerinin XML dosyası
│   └── *.exe             # Eklenen program dosyaları
├── AIOInstaller.csproj
├── Program.cs
├── MainForm.cs
├── ProgramInfo.cs
├── ProgramManager.cs
└── AddProgramDialog.cs
```

## Notlar

- Program bilgileri XML formatında `Database/ProgramsList.xml` dosyasında saklanır
- Tüm .exe dosyaları `Database` klasörüne kopyalanır
- Program yükleme işlemi yönetici yetkisi ile çalıştırılır
- Aynı isimde dosya varsa otomatik olarak yeni isim verilir (örn: program_1.exe)

## Gereksinimler

- Windows İşletim Sistemi
- .NET Framework 4.8 veya üzeri
- Visual Studio 2019+ (geliştirme için)
