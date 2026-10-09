# Üçüncü taraf bileşenleri

Projenin GPL-3.0-only lisansı bu depodaki proje kaynak koduna uygulanır. Üçüncü taraf yazılımların ve ticari ürünlerin hakları kendi sahiplerine aittir.

## SOLIDWORKS

Uygulama `SldWorks.Application` COM kaydı üzerinden yerel SOLIDWORKS kurulumuyla iletişim kurar. SOLIDWORKS kurulu ve lisanslı olmalıdır. SOLIDWORKS uygulaması, lisans anahtarı, CAD dosyaları veya üretici DLL'leri bu depoya eklenmemiştir.

## NuGet bağımlılıkları

Proje dosyasındaki mevcut bağımlılıklar:

| Paket | Sürüm | Kaynak |
| --- | --- | --- |
| SolidWorks.Interop.sldworks | 32.1.0 | https://www.nuget.org/packages/SolidWorks.Interop.sldworks/32.1.0 |
| SolidWorks.Interop.swconst | 32.1.0 | https://www.nuget.org/packages/SolidWorks.Interop.swconst/32.1.0 |

Bu sürümlerin NuGet paket metadata'sında açık bir lisans ifadesi veya lisans URL'si bulunmamaktadır (9 Ekim 2026 kontrolü). Bu durum bağımlılıkları GPL yapmaz. Depo yalnız paket başvurularını içerir; üçüncü taraf paket dosyaları veya DLL'leri dağıtılmaz. Derlenmiş paketlerin dağıtımı için ilgili bileşenlerin lisans ve yeniden dağıtım koşulları ayrıca doğrulanmalıdır.
