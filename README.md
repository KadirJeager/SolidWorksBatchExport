# SolidWorksBatchExport

SOLIDWORKS sac levha parçaları için toplu DXF/DWG dışa aktarma, kesim listesi raporu ve isteğe bağlı büküm ucu çentikleme aracı. C# / .NET 8 / WPF ile geliştirilmiş bir Windows masaüstü uygulamasıdır.

**Durum: Mevcut kapsamıyla çalışan uygulama.** Toplu dışa aktarma, kesim listesi raporu ve büküm ucu çentikleme için geliştirilmiştir. Proje sahibi, kendi kullanımında çentiklerin kusursuz oluşturulduğunu belirtmiştir. Projenin daha geniş bir kapsama taşınması planlanmış, ancak bu kapsam genişlemesi gerçekleştirilememiştir. Bu paylaşım, çalışan mevcut sürümü açık kaynak olarak sunar.

## Ne işe yarar?

Bir klasördeki sac parçaların açınımını tek tek dışa aktarma işini toplu hale getirmeyi amaçlar. İsteğe bağlı çentikleme, büküm çizgilerinin uçlarına dairesel kesimler ekleyerek büküm uçlarını çıktı geometrisinde işaretler.

Uygulamadaki işlem akışı:

1. Çalışan SOLIDWORKS oturumuna COM üzerinden bağlanır; oturum bulunamazsa SOLIDWORKS'ü başlatmayı dener.
2. Seçilen klasörü ve alt klasörlerini `.sldprt` dosyaları için tarar. Üst düzey unsur ağacında `SheetMetal` veya `FlatPattern` bulunan parçaları seçer.
3. Her parçanın kesim listesi (`CutListFolder`) özelliklerini okur.
4. Çentik seçeneği açıksa açınımı etkinleştirir, büküm çizgisi uçlarını bulur ve kullanıcı yarıçapıyla kesimler oluşturur.
5. SOLIDWORKS `ExportToDWG2` API'siyle her parça için `.dxf` ve `.dwg` çıktıları üretir.
6. Kesim listesi özelliklerini, Excel ile açılabilen noktalı virgül ayırıcılı ve UTF-8 BOM içeren CSV dosyasına yazar.

Çıktılar seçilen klasörün içindeki `Output_DXF` klasörüne gider:

- Her parça için kendi dosya adıyla `.dxf` ve `.dwg`.
- `KesimListesiRaporu.csv`: parça başına bir satır, `DosyaAdı` ve bulunan kesim listesi özellikleri.

Büküm çizgilerini dışa aktarma seçeneği açıksa çizimler de dışa aktarma seçeneklerine dahildir. Teknik resim şablonu gerekmez; mevcut kod doğrudan parça açınımını dışa aktarır.

## Gereksinimler

- Windows ve kurulu, lisanslı SOLIDWORKS. COM otomasyonu için `SldWorks.Application` kaydının kullanılabilir olması gerekir.
- Kaynaktan derlemek için .NET 8 SDK; WPF destekli geliştirme ortamı.
- NuGet erişimi. Proje `SolidWorks.Interop.sldworks` ve `SolidWorks.Interop.swconst` paketlerinin `32.1.0` sürümüne başvurur. Paket sürümü, tüm SOLIDWORKS sürümleriyle uyumluluk garantisi değildir; doğrulanmış bir sürüm matrisi henüz yoktur.

SOLIDWORKS uygulaması, lisansı ve üçüncü taraf interop DLL'leri bu depoya dahil edilmemiştir.

## Derleme ve çalıştırma

Windows'ta deponun kökünde:

```powershell
dotnet restore SolidWorksBatchExport.csproj
dotnet build SolidWorksBatchExport.csproj -c Release --no-restore
dotnet run --project SolidWorksBatchExport.csproj -c Release
```

SOLIDWORKS ile gerçek işlem yapmak için önce uygulamayı ve gerekli lisansı hazır edin. Gözat düğmesiyle parça klasörünü seçin, çentik ve büküm çizgisi seçeneklerini ayarlayın, ardından işlemi başlatın. Yarıçap milimetre cinsindedir; arayüzde `1.5` ve `1,5` biçimleri okunur. Geçerli, pozitif ve sonlu bir yarıçap kullanın; mevcut kod bu koşulların tamamını doğrulamaz.

64 bit Windows için, .NET çalışma zamanını içeren tek dosya yayımlama komutu:

```powershell
dotnet publish SolidWorksBatchExport.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false
```

Çıktı yolu: `bin/Release/net8.0-windows/win-x64/publish/SolidWorksBatchExport.exe`. `Publish.bat` aynı yayımlama komutunu içerir. Tek dosya paketleme, SOLIDWORKS kurulum gereksinimini kaldırmaz. Bu ilk paylaşımda doğrulanmış çalıştırılabilir dosya dağıtılmamaktadır.

## Mevcut kapsam ve kullanım notları

- **Çıktı üzerine yazma:** Alt klasörlerde aynı adı taşıyan parçalar aynı çıktı yolunu kullanır. Kod, mevcut DXF/DWG dosyalarını yeni dışa aktarma denemesinden önce siler. İlk denemeyi parça kopyalarının bulunduğu ayrı bir klasörde yapın ve eski çıktıları koruyun.
- **Açık belgeler:** Uygulama mevcut SOLIDWORKS oturumunu kullanabilir ve işlediği belgeleri kapatır. Başlamadan önce açık çalışmalarınızı kaydedin. Kod işlenen parçayı kaydetmeden kapatmayı amaçlar; hata yollarında belge kapatma ve COM nesnesi temizliği her durumda güvence altında değildir.
- **Çentikleme yöntemi:** Büküm çizimi adlarında Türkçe/İngilizce metin arar ve en büyük düz yüzey ile en yakın kenarı kullanır. İşlem kayıtlarında oluşturulan ve atlanan kesim sayıları gösterilir. Proje sahibinin kullandığı parçalarda çentikleme kusursuz çalışmaktadır; farklı CAD dosyalarındaki sonuçlar kendi geometrileriyle değerlendirilmelidir.
- **Başarı sayacının anlamı:** DXF veya DWG çıktılarından en az biri için API başarılı dönerse parça başarılı sayılır. Bu sayaç iki dosyanın da oluştuğunu veya çentiklerin doğru işlendiğini göstermez.
- **Rapor kapsamı:** Kesim listesi özellikleri parça başına tek sözlükte birleştirilir; farklı kesim listelerinde aynı adlı özellikler birbirinin üzerine yazılabilir. Alt unsurlardaki listeler ve tüm konfigürasyonlar ayrı ayrı dolaşılmaz. Veriler dışa aktarma öncesinde toplandığından raporda çıktısı başarısız parçalar da bulunabilir.
- **İşlem kapsamı:** Girdi olarak klasör içindeki parça dosyaları işlenir. Montajdan parça çıkarma, tüm konfigürasyonları ayrı ayrı dışa aktarma ve işlem iptal/duraklatma seçenekleri mevcut arayüzde bulunmaz.

Planlanan kapsam genişlemesinin ayrıntıları bu kaynak arşivinde belgelenmemiştir. Mevcut özellikler bu nedenle başarısız veya yarım kalmış özellikler olarak nitelendirilmez.

## Doğrulama durumu

- Kaynak arşivi incelenmiş, uygulama kodu ilk paylaşımda değiştirilmemiştir.
- GitHub Actions Windows üzerinde NuGet geri yükleme ve Release derlemesini kontrol eder. Bu bir SOLIDWORKS işlev testi değildir.
- Yayıma hazırlığın yapıldığı Linux ortamında Windows/WPF uygulaması çalıştırılmamış ve SOLIDWORKS üzerinden gerçek CAD çıktıları doğrulanmamıştır.
- Arşivde otomatik test veya doğrulanmış örnek CAD dosyası bulunmamaktadır.

## Kaynak düzeni

| Dosya | Görev |
| --- | --- |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | Klasör, seçenekler ve işlem kayıtları için WPF arayüzü |
| `Core/SolidWorksEngine.cs` | SOLIDWORKS COM bağlantısı |
| `Core/Scanner.cs` | Sac parça taraması |
| `Core/NotchManager.cs` | Büküm ucu çentikleme |
| `Core/ExportManager.cs` | DXF/DWG dışa aktarma |
| `Core/ReportManager.cs` | Kesim listesi özellikleri ve CSV |
| `Core/BatchProcessor.cs` | Toplu işlem akışı |

## Katkı

Hata bildirirken SOLIDWORKS sürümünü, Windows sürümünü, seçili seçenekleri, beklenen sonucu ve gerçekleşen sonucu belirtin. İşlem kayıtlarını ve CAD dosyalarını paylaşmadan önce kişisel, müşteri veya kurumsal bilgileri kaldırın. Gerçek parça üzerinde doğruladığınız değişiklikler için kullanılan konfigürasyonu ve DXF/DWG/çentik/CSV sonuçlarını yazın.

## Lisans

Bu projenin kaynak kodu **GNU General Public License v3.0 only (`GPL-3.0-only`)** altında yayımlanmıştır. Tam metin [LICENSE](LICENSE) dosyasındadır.

SOLIDWORKS ve üçüncü taraf bağımlılıklarının hakları kendi sahiplerine aittir; bu lisans onları yeniden lisanslamaz. Ayrıntılar [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) dosyasındadır. Bu proje Dassault Systèmes veya SOLIDWORKS tarafından yayımlanmış ya da desteklenmiş resmî bir ürün değildir.

## English overview

A working Windows desktop tool written in C# / .NET 8 / WPF that automates SOLIDWORKS to scan sheet-metal parts, batch-export DXF/DWG flat patterns, collect cut-list properties in CSV, and optionally create circular notches at bend-line endpoints.

**Status: working within its current scope.** The project owner reports flawless notch creation in their use. A broader project scope was planned but never implemented; this does not mean the existing features failed. The initial publication preserves the supplied application source. Windows and a licensed SOLIDWORKS installation are required to use the application; .NET 8 SDK is required to build it. CI checks compilation, while CAD runtime behavior was not independently tested in the Linux publication environment. Licensed under GPL-3.0-only; proprietary SOLIDWORKS components are not included.
