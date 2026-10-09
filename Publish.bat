@echo off
:: Çalışma dizinini bat dosyasının bulunduğu klasöre zorla (Yönetici olarak çalıştırıldığında System32'ye gitmesini engeller)
cd /d "%~dp0"

echo ========================================================
echo SolidWorks Batch Export - Tek Dosya (Portable) Derleyici
echo ========================================================
echo.
echo Bu islem projenizi tasiyabileceginiz tek bir .exe haline getirecektir.
echo Gerekli ortam hazirlaniyor...
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false

echo.
echo ========================================================
echo Islem tamamlandi! Eger hata almadiginizi goruyorsaniz:
echo Olusturulan program su klasordedir:
echo bin\Release\net8.0-windows\win-x64\publish\SolidWorksBatchExport.exe
echo ========================================================
pause
