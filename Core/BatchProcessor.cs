using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidWorksBatchExport.Core
{
    public class BatchProcessor
    {
        /// <summary>
        /// Toplu işleme sürecini yöneten ana orkestratör metot.
        /// SOLIDWORKS API COM objelerinin bellekte şişmesini önlemek için dikkatli bellek yönetimi (garbage collection) içerir.
        /// </summary>
        public async Task ProcessFolderAsync(string folderPath, bool enableNotch, double notchRadius, bool exportBendLines, IProgress<string> progress)
        {
            await Task.Delay(10);
            progress.Report("İşlem başlatılıyor... SolidWorks'e bağlanılıyor.");

            // SolidWorksEngine, IDisposable implemente ettiği için using bloğu içinde kullanıyoruz.
            // Bu sayede işlem bitince veya hata alınca SolidWorks bağlantısı güvenli bir şekilde kapatılır (Teknik borç bırakılmaz).
            using (var swEngine = new SolidWorksEngine())
            {
                if (!swEngine.IsConnected || swEngine.App == null)
                {
                    progress.Report("HATA: SolidWorks'e bağlanılamadı. Lütfen SolidWorks'ün yüklü ve lisanslı olduğundan emin olun.");
                    return;
                }

                progress.Report("SolidWorks bağlantısı başarılı. Modüller yükleniyor...");
                
                var scanner = new Scanner(swEngine.App);
                var notchManager = new NotchManager(swEngine.App);
                
                // Şablon yolu şimdilik boş bırakıldı, kendi drwdot şablonunuzu buraya girebilirsiniz.
                // Örn: @"C:\ProgramData\SolidWorks\templates\sablon.drwdot"
                var exportManager = new ExportManager(swEngine.App, ""); 
                var reportManager = new ReportManager(swEngine.App);

                progress.Report($"'{folderPath}' dizinindeki sac parçalar taranıyor...");
                List<string> sheetMetalParts = scanner.GetSheetMetalPartsFromFolder(folderPath);

                if (sheetMetalParts.Count == 0)
                {
                    progress.Report("UYARI: Seçilen klasörde hiçbir sac levha (Sheet Metal) parçası bulunamadı.");
                    return;
                }

                progress.Report($"Toplam {sheetMetalParts.Count} adet sac parça bulundu. İşlem başlıyor...");

                // Çıktılar için hedef klasör oluştur (Orijinal klasörün içine Output adında bir klasör açılır)
                string outputDir = Path.Combine(folderPath, "Output_DXF");
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var allCutListData = new List<Dictionary<string, string>>();
                int successCount = 0;

                for (int i = 0; i < sheetMetalParts.Count; i++)
                {
                    string partPath = sheetMetalParts[i];
                    string fileName = Path.GetFileNameWithoutExtension(partPath);
                    progress.Report($"[{i + 1}/{sheetMetalParts.Count}] İşleniyor: {fileName}");

                    try
                    {
                        // Modeli sessizce aç
                        int errors = 0, warnings = 0;
                        var swModel = (ModelDoc2)swEngine.App.OpenDoc6(partPath, (int)swDocumentTypes_e.swDocPART,
                            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);

                        if (swModel == null)
                        {
                            progress.Report($"  -> HATA: {fileName} SolidWorks'te açılamadı.");
                            continue;
                        }

                        // 1. Kesim Listesi özelliklerini oku
                        var cutListData = reportManager.GetCutListProperties(swModel);
                        cutListData["DosyaAdı"] = fileName;
                        allCutListData.Add(cutListData);

                        // 2. Çentik atma (İsteniyorsa)
                        if (enableNotch)
                        {
                            bool notchSuccess = notchManager.CreateNotches(swModel, notchRadius, progress);
                            if (!notchSuccess)
                                progress.Report($"  -> DİKKAT: {fileName} için çentik işlemi başarısız oldu (Hata detayları yukarıda).");
                        }

                        // 3. DXF/DWG Dışa Aktar (Model henüz açık ve çentikli iken)
                        bool exportSuccess = exportManager.ExportFlatPatternToDXF(swModel, partPath, outputDir, fileName, exportBendLines);
                        
                        // İşlem bittikten sonra parçayı KAYDETMEDEN kapatıyoruz
                        // Böylece atılan fiziksel çentikler orijinal dosyada kalıcı olmuyor.
                        swEngine.App.CloseDoc(swModel.GetTitle());
                        Marshal.ReleaseComObject(swModel);

                        if (exportSuccess)
                        {
                            successCount++;
                            progress.Report($"  -> DXF/DWG başarıyla oluşturuldu.");
                        }
                        else
                        {
                            progress.Report($"  -> HATA: DXF/DWG oluşturulamadı.");
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.Report($"  -> KRİTİK HATA ({fileName}): {ex.Message}\nStack Trace:\n{ex.StackTrace}");
                    }
                }

                // 4. Tüm verileri CSV (Excel) olarak raporla
                progress.Report("Kesim listesi CSV olarak dışa aktarılıyor...");
                string reportPath = Path.Combine(outputDir, "KesimListesiRaporu.csv");
                reportManager.ExportToCsv(allCutListData, reportPath);

                progress.Report("-------------------------------------------------");
                progress.Report($"İŞLEM TAMAMLANDI. Başarılı: {successCount} / Toplam: {sheetMetalParts.Count}");
                progress.Report($"Çıktı Klasörü: {outputDir}");
            }
        }
    }
}
