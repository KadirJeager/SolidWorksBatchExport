# SolidWorksBatchExport

SOLIDWORKS sac levha parçaları için toplu DXF/DWG açınım dışa aktarma, büküm ucu çentikleme ve kesim listesi raporlama uygulaması. Windows üzerinde C# / .NET 8 / WPF ile çalışır.

Uygulama SOLIDWORKS ile gerçek üretim işlerinde kullanılmaktadır; açınım dışa aktarma, çentikleme ve raporlama özellikleri çalışır durumdadır.

## Özellikler

- Klasör ve alt klasörlerde `.sldprt` ve `.sldasm` tarama, dosya seçimi ve montaj bileşenlerini işleme.
- Sac açınımlarını DXF ve/veya DWG olarak toplu dışa aktarma.
- Büküm uçlarına çentik oluşturma; otomatik veya kullanıcı tarafından belirlenen çentik çapı. Otomatik seçim, 4 mm altındaki saclarda 4 mm, diğerlerinde 6 mm çap kullanır.
- Hızlı sürümde çentiklerin toplu kesimle oluşturulması.
- Tüm konfigürasyonları işleme ve dışa aktarılan büküm çizgilerini gizleme seçenekleri.
- İsteğe bağlı SOLIDWORKS teknik resim şablonu (`.drwdot`).
- Çıktıları parçanın klasörüne, merkezi klasöre veya her ikisine yerleştirme; malzeme ve kalınlığa göre gruplama.
- Dosya adı şablonu: `%DA%` dosya adı, `%MA%` malzeme, `%KA%` kalınlık, `%AD%` adet, `%KO%` konfigürasyon, `%O1%` seçilen özel özellik.
- Adet, kalınlık, malzeme, kütle, ölçüler, kesim alanı ve çentik bilgilerini içeren CSV ve önizlemeli HTML kesim listesi raporları.
- Ayarların `settings.json` dosyasına kaydedilmesi ve işlem günlükleri.

## Gereksinimler

- 64 bit Windows ve kurulu, lisanslı SOLIDWORKS; `SldWorks.Application` COM otomasyonu kullanılabilir olmalıdır.
- Kaynaktan derlemek için .NET 8 SDK ve NuGet erişimi.
- `SolidWorks.Interop.sldworks` ve `SolidWorks.Interop.swconst` paketleri, çalışan EXE'de kullanılan **27.3.0** sürümündedir.

## Derleme ve çalıştırma

Windows'ta depo kökünde:

```powershell
dotnet restore SolidWorksBatchExport.csproj
dotnet build SolidWorksBatchExport.csproj -c Release --no-restore
dotnet run --project SolidWorksBatchExport.csproj -c Release
```

Uygulamada klasörü seçin, dosya listesini tarayın, işlenecek dosyaları ve çıktı/çentik seçeneklerini belirleyin, ardından işlemi başlatın. Çentik ölçüsü **çap** olarak milimetre cinsindedir.

.NET çalışma zamanını içeren tek dosya Windows EXE'si üretmek için `Publish.bat` çalıştırılabilir. Aynı komut:

```powershell
dotnet publish SolidWorksBatchExport.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false
```

Çıktı: `bin/Release/net8.0-windows/win-x64/publish/SolidWorksBatchExport-hizli.exe`.

**Dosya adını koruyun:** Hızlı çentikleme modu, EXE adında `hizli` geçtiğinde etkinleşir. Projedeki `AssemblyName` (`SolidWorksBatchExport-hizli`) bunu sağlar; EXE yeniden adlandırılırsa uygulama standart çentikleme moduyla çalışır.

## Kullanım

Uygulama SOLIDWORKS belgelerini açar, işler ve kapatır; başlatmadan önce SOLIDWORKS'te açık çalışmalarınızı kaydedin.

GitHub Actions her değişiklikte Windows üzerinde Release derlemesini kontrol eder. NuGet, .NET Framework hedefli SOLIDWORKS interop paketleri için `NU1701` uyumluluk uyarısı verir; bu uyarı uygulamanın çalışmasını etkilemez.

## Kaynak düzeni

| Dosya | Görev |
| --- | --- |
| `App.cs` | WPF başlangıcı |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | Arayüz, dosya seçimi ve ayarlar |
| `FileListItem.cs` | Dosya listesi modeli |
| `Core/BatchProcessor.cs` | Montaj/parça ve konfigürasyon işlem akışı |
| `Core/SolidWorksEngine.cs` | SOLIDWORKS COM bağlantısı |
| `Core/Scanner.cs` | Parça ve montaj taraması |
| `Core/ProcessingSettings.cs` | İşlem seçenekleri |
| `Core/NotchManager.cs` | Çentik geometrisi ve kesim |
| `Core/NotchPoint.cs` / `Core/NotchResult.cs` | Çentik verileri ve sonuçları |
| `Core/ExportManager.cs` | DXF/DWG ve şablon üzerinden dışa aktarma |
| `Core/ReportManager.cs` | CSV ve HTML kesim listesi |
| `Core/FileLogger.cs` | İşlem günlüğü |

Kaynak kodun kökeni [RECOVERY.md](RECOVERY.md) dosyasında açıklanmıştır.

## Katkı ve lisans

Hata bildirirken SOLIDWORKS/Windows sürümünü, seçili seçenekleri, beklenen ve gerçekleşen sonucu belirtin. CAD dosyaları ve günlükleri paylaşırken kişisel veya müşteri bilgilerini çıkarın.

Proje kaynak kodu **GNU General Public License v3.0 only (`GPL-3.0-only`)** altında yayımlanır; tam metin [LICENSE](LICENSE) dosyasındadır. SOLIDWORKS ve üçüncü taraf bağımlılıklarının hakları kendi sahiplerine aittir; ayrıntılar [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) dosyasındadır. Depo üçüncü taraf DLL'leri veya SOLIDWORKS lisansı içermez. Bu proje resmî bir SOLIDWORKS ürünü değildir.

## English overview

A C# / .NET 8 / WPF application for Windows that automates SOLIDWORKS sheet-metal flat-pattern DXF/DWG export, bend-end notches, assembly/configuration processing and CSV/HTML cut-list reports. The application is in production use with SOLIDWORKS. Keep `hizli` in the executable name to use the fast notching mode. See [RECOVERY.md](RECOVERY.md) for source provenance. Licensed under GPL-3.0-only.
