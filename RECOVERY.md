# Kaynak kurtarma kaydı

Bu depodaki kaynak kod, proje sahibinin üretimde kullandığı `SolidWorksBatchExport-hizli-sifreli.exe` dosyasından geri çıkarılmıştır. Orijinal EXE değiştirilmemiştir.

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

## Doğrulama

Korumanın sildiği özgün özel üye adları ve yorumlar geri getirilemez; bazı adlar `Method_060000...` / `field_040000...` biçiminde metadata kimlikleriyle korunmuştur. Davranış bundan etkilenmez.

- Bağımsız IL karşılaştırmasında 195 uygulama yöntemindeki 10592 talimat kontrol edildi. İzin verilen 548 metin çağrısı değişikliği dışında talimatlar ve operandları, dallanma hedefleri, yerel değişken tipleri ve istisna bölgeleri eşleşti. Bu karşılaştırma C# decompiler çıktısının yeniden derlenmesini değil, gizleme katmanı kaldırılmış yönetilen assembly dönüşümünü doğrular. Kaldırılan metin çözücüye başvuru kalmadığı ayrıca kontrol edildi.
- Release derleme ve `win-x64`, self-contained, tek dosya publish tamamlandı.
- Kurtarma EXE'si, özgün dosyayla aynı .NET 8.0.29 çalışma zamanı kullanılarak üretildi.
- .NET 8 SDK ile derlenen DLL, gizlemesi kaldırılmış orijinal DLL ile ILSpy üzerinden karşılaştırıldı: 195/195 yöntem eşleşir; farklar yalnız yerel değişken adları ve derleyici yerleşimidir. WPF kaynakları ve `runtimeconfig` aynıdır.
- `NU1701`: .NET Framework hedefli SOLIDWORKS interop paketleri için NuGet uyarısıdır; paket sürümleri orijinal EXE ile aynı tutuldu.

Depodaki ilk commit eski kaynak arşivini saklar. Bu kurtarma sürümü, proje sahibinin daha sonra sağladığı çalışan hızlı EXE'yi esas alır.
