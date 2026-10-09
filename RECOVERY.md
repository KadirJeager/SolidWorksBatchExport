# Kaynak kurtarma kaydı

Proje sahibi tarafından sağlanan `SolidWorksBatchExport-hizli-sifreli.exe` dosyası statik olarak incelenmiştir. Orijinal EXE değiştirilmemiş ve kurtarma sırasında çalıştırılmamıştır.

- Orijinal boyut: 164661224 bayt.
- SHA-256: `98b2b8320c88d08469539c31c596ae9800663addbdf333dfeb6df12a26e6ba69`.
- Paket: .NET tek dosya Windows x64 uygulaması; uygulama assembly'si `SolidWorksBatchExport.dll`.
- Manifestteki çalışma zamanı: .NET / Windows Desktop 8.0.29.
- Interop başvuruları: `SolidWorks.Interop.sldworks` ve `SolidWorks.Interop.swconst` 27.3.0.

## Yapılan işlemler

1. Tek dosya paketindeki yönetilen uygulama ve WPF kaynakları çıkarıldı.
2. XOR ile gizlenen 355 metin çözüldü; 548 metin çözücü çağrısı aynı metinleri yükleyen IL talimatlarıyla değiştirildi.
3. Gizlenmiş sınıf ve üye adlarının çakışmaları giderildi; ayar ve sonuç özelliklerinin metadata'sı yeniden oluşturuldu.
4. ILSpy ile C# ve WPF XAML geri çıkarıldı. Decompiler'ın zaten lambda olarak ürettiği kodun kullanılmayan üç yardımcı sınıf kopyası kaldırıldı.
5. .NET 8 / WPF proje dosyası, özgün bağımlılık sürümleriyle yeniden oluşturuldu. `AssemblyName` içindeki `hizli`, özgün hızlı mod seçimini korur.

Çentik ve dışa aktarma algoritmalarına özellik değişikliği yapılmadı. Statik incelemede kullanıcıdan parola isteyen veya lisans anahtarı doğrulayan bir başlangıç akışı bulunmadı; saptanan koruma kod/metin gizleme katmanıdır.

## Sınırlar ve doğrulama

Bu, özgün editör dosyalarının bire bir yedeği değildir. Korumanın sildiği özgün özel üye adları ve yorumlar geri getirilemez; bazı adlar `Method_060000...` / `field_040000...` biçiminde metadata kimlikleriyle korunmuştur. XAML ve C# decompiler çıktısından yeniden derlenebilir hale getirilmiştir.

- Bağımsız IL karşılaştırmasında 195 uygulama yöntemindeki 10592 talimat kontrol edildi. İzin verilen 548 metin çağrısı değişikliği dışında talimatlar ve operandları, dallanma hedefleri, yerel değişken tipleri ve istisna bölgeleri eşleşti. Bu karşılaştırma C# decompiler çıktısının yeniden derlenmesini değil, gizleme katmanı kaldırılmış yönetilen assembly dönüşümünü doğrular. Kaldırılan metin çözücüye başvuru kalmadığı ayrıca kontrol edildi.
- Release derleme ve `win-x64`, self-contained, tek dosya publish tamamlandı.
- Kurtarma EXE'si, özgün dosyayla aynı .NET 8.0.29 çalışma zamanı kullanılarak üretildi.
- `NU1701`: .NET Framework hedefli SOLIDWORKS interop paketlerinin .NET 8 üzerinde kullanımı için NuGet uyarısı devam eder; paket sürümleri sessizce yükseltilmedi.
- Windows/SOLIDWORKS üzerinde gerçek CAD işlemi kurtarma ortamında çalıştırılmadı. Derleme başarısı tüm geometrilerde çalışma eşdeğerliği kanıtı değildir.

Depodaki ilk commit eski kaynak arşivini saklar. Bu kurtarma sürümü, proje sahibinin daha sonra sağladığı çalışan hızlı EXE'yi esas alır. Assembly sürümü `1.0.0.0` tek başına WhatsApp sürüm adlarıyla eşleştirme sağlamaz.
