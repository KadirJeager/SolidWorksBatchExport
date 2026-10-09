using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidWorksBatchExport.Core
{
    public class ReportManager
    {
        private ISldWorks _swApp;

        public ReportManager(ISldWorks swApp)
        {
            _swApp = swApp;
        }

        /// <summary>
        /// Seçilen parça içindeki kesim listesi (Cut List) özelliklerini okur
        /// </summary>
        public Dictionary<string, string> GetCutListProperties(ModelDoc2 swModel)
        {
            var properties = new Dictionary<string, string>();

            if (swModel != null)
            {
                Feature swFeat = (Feature)swModel.FirstFeature();
                while (swFeat != null)
                {
                    // CutListFolder unsurunu bul
                    if (swFeat.GetTypeName2() == "CutListFolder")
                    {
                        CustomPropertyManager propMgr = swFeat.CustomPropertyManager;
                        if (propMgr != null)
                        {
                            object propNamesObj = null;
                            object propTypesObj = null;
                            object propValuesObj = null;
                            object resolvedValuesObj = null;
                            object propLinkObj = null;

                            propMgr.GetAll3(ref propNamesObj, ref propTypesObj, ref propValuesObj, ref resolvedValuesObj, ref propLinkObj);

                            if (propNamesObj != null && resolvedValuesObj != null)
                            {
                                Array names = propNamesObj as Array;
                                Array values = resolvedValuesObj as Array;

                                if (names != null && values != null)
                                {
                                    for (int i = 0; i < names.Length; i++)
                                    {
                                        properties[Convert.ToString(names.GetValue(i))] = Convert.ToString(values.GetValue(i));
                                    }
                                }
                            }
                        }
                    }
                    swFeat = (Feature)swFeat.GetNextFeature();
                }
            }

            return properties;
        }

        /// <summary>
        /// Kesim listesi verilerini CSV formatında Excel uyumlu şekilde dışa aktarır.
        /// </summary>
        public void ExportToCsv(List<Dictionary<string, string>> allPartsData, string outputPath)
        {
            if (allPartsData == null || allPartsData.Count == 0) return;

            // Tüm benzersiz özellikleri bul (Kolon başlıkları için)
            var allKeys = new HashSet<string>();
            foreach (var data in allPartsData)
            {
                foreach (var key in data.Keys)
                {
                    allKeys.Add(key);
                }
            }

            var csvContent = new StringBuilder();
            
            // Başlık satırı
            csvContent.AppendLine(string.Join(";", allKeys));

            // Veri satırları
            foreach (var data in allPartsData)
            {
                var row = new List<string>();
                foreach (var key in allKeys)
                {
                    if (data.TryGetValue(key, out string val))
                    {
                        row.Add($"\"{val.Replace("\"", "\"\"")}\"");
                    }
                    else
                    {
                        row.Add("");
                    }
                }
                csvContent.AppendLine(string.Join(";", row));
            }

            // UTF8 BOM ile kaydet ki Excel Türkçe karakterleri doğru okusun
            File.WriteAllText(outputPath, csvContent.ToString(), new UTF8Encoding(true));
        }
    }
}
