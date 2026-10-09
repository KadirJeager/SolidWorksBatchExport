# Üçüncü taraf bileşenleri

GPL-3.0-only lisansı bu depodaki proje kaynak koduna uygulanır. Üçüncü taraf bileşenlerin hakları kendi sahiplerine aittir.

SOLIDWORKS kurulu ve lisanslı olmalıdır. Depo SOLIDWORKS uygulaması, CAD dosyaları, lisans anahtarları veya üçüncü taraf DLL'leri içermez.

## NuGet başvuruları

| Paket | Sürüm | Kaynak |
| --- | --- | --- |
| SolidWorks.Interop.sldworks | 27.3.0 | https://www.nuget.org/packages/SolidWorks.Interop.sldworks/27.3.0 |
| SolidWorks.Interop.swconst | 27.3.0 | https://www.nuget.org/packages/SolidWorks.Interop.swconst/27.3.0 |

Sürümler kurtarılan EXE'nin bağımlılık manifestinden alınmıştır. Paket metadata'sı lisans URL'si olarak SOLIDWORKS API yardım sayfasına işaret eder: http://help.solidworks.com/2019/english/api/sldworksapiprogguide/welcome.htm . Bu bir GPL lisans beyanı değildir. Depo yalnız paket başvurularını dağıtır; üçüncü taraf bileşenleri içeren derlenmiş paketlerin herkese açık dağıtımı için ilgili yeniden dağıtım koşulları ayrıca doğrulanmalıdır.

## Kurtarma araçları

Kaynak kurtarmada ILSpy ve Mono.Cecil kullanılmıştır. Bu araçlar uygulamanın çalışma zamanı bağımlılığı değildir ve depoya dahil edilmemiştir.

- ILSpy: https://github.com/icsharpcode/ILSpy
- Mono.Cecil: https://github.com/jbevain/cecil
