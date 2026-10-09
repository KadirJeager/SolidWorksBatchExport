using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidWorksBatchExport.Core
{
    public class ExportManager
    {
        private ISldWorks _swApp;
        private string _templatePath;

        public ExportManager(ISldWorks swApp, string templatePath)
        {
            _swApp = swApp;
            _templatePath = templatePath;
        }

        /// <summary>
        /// Sac parçayı teknik resim üzerinden DXF/DWG formatında dışa aktarır.
        /// </summary>
        /// <param name="partPath">Parçanın tam dosya yolu</param>
        /// <param name="exportPath">Çıktının kaydedileceği klasör yolu</param>
        /// <param name="fileName">Çıktı dosyasının adı</param>
        public bool ExportFlatPatternToDXF(ModelDoc2 swModel, string partPath, string exportPath, string fileName, bool exportBendLines)
        {
            try
            {
                if (swModel == null) throw new Exception("Parça açık değil.");

                PartDoc swPart = (PartDoc)swModel;

                // 1. DXF olarak kaydet
                string dxfExportPath = Path.Combine(exportPath, fileName + ".dxf");
                if (File.Exists(dxfExportPath))
                    File.Delete(dxfExportPath);

                // Action: 1 = swExportToDWG_ExportSheetMetal
                // SheetMetalOptions: 1 (Geometry) + 4 (Bend Lines) + 8 (Sketches)
                int options = exportBendLines ? 13 : 1; 

                bool dxfSuccess = swPart.ExportToDWG2(
                    dxfExportPath,
                    partPath,
                    1, // swExportToDWG_ExportSheetMetal
                    true,
                    null,
                    false,
                    false,
                    options, 
                    null
                );

                // 2. DWG olarak kaydet (Kullanıcı talebi)
                string dwgExportPath = Path.Combine(exportPath, fileName + ".dwg");
                if (File.Exists(dwgExportPath))
                    File.Delete(dwgExportPath);

                bool dwgSuccess = swPart.ExportToDWG2(
                    dwgExportPath,
                    partPath,
                    1,
                    true,
                    null,
                    false,
                    false,
                    options,
                    null
                );
                
                if (!dxfSuccess && !dwgSuccess) 
                    throw new Exception("SolidWorks ExportToDWG2 API'si DXF/DWG çıktılarını oluşturamadı. Parçanın sac (sheet metal) özelliği olduğundan emin olun.");
                
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Dışa aktarım hatası: " + ex.Message, ex);
            }
        }
    }
}
