using System;
using System.Collections.Generic;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidWorksBatchExport.Core
{
    public class Scanner
    {
        private ISldWorks _swApp;

        public Scanner(ISldWorks swApp)
        {
            _swApp = swApp;
        }

        /// <summary>
        /// Belirtilen klasördeki (ve alt klasörlerindeki) tüm sac levha parçalarını bulur.
        /// </summary>
        public List<string> GetSheetMetalPartsFromFolder(string folderPath)
        {
            var sheetMetalParts = new List<string>();
            var allParts = Directory.GetFiles(folderPath, "*.sldprt", SearchOption.AllDirectories);

            foreach (var partPath in allParts)
            {
                if (IsSheetMetalPart(partPath))
                {
                    sheetMetalParts.Add(partPath);
                }
            }

            return sheetMetalParts;
        }

        /// <summary>
        /// Bir parçanın sac levha (Sheet Metal) unsuru içerip içermediğini kontrol eder.
        /// Dosyayı arka planda gizli açar, kontrol eder ve kapatır.
        /// </summary>
        private bool IsSheetMetalPart(string filePath)
        {
            bool isSheetMetal = false;
            int errors = 0;
            int warnings = 0;

            // Dosyayı görünmez (hidden) olarak açıyoruz
            ModelDoc2 swModel = (ModelDoc2)_swApp.OpenDoc6(filePath, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent | (int)swOpenDocOptions_e.swOpenDocOptions_ReadOnly, "", ref errors, ref warnings);
            
            if (swModel != null)
            {
                Feature swFeat = (Feature)swModel.FirstFeature();
                while (swFeat != null)
                {
                    // "SheetMetal" veya "FlatPattern" unsuru varsa bu bir sac parçadır
                    if (swFeat.GetTypeName2() == "SheetMetal" || swFeat.GetTypeName2() == "FlatPattern")
                    {
                        isSheetMetal = true;
                        break;
                    }
                    swFeat = (Feature)swFeat.GetNextFeature();
                }

                // Dosyayı kapatıyoruz (kaydetmeden)
                _swApp.CloseDoc(filePath);
            }

            return isSheetMetal;
        }
    }
}
