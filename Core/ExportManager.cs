using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

namespace SolidWorksBatchExport.Core;

public class ExportManager
{
	private readonly ISldWorks field_04000047;

	private readonly ProcessingSettings field_04000048;

	public ExportManager(ISldWorks P_0, ProcessingSettings P_1)
	{
		field_04000047 = P_0 ?? throw new ArgumentNullException("swApp");
		field_04000048 = P_1 ?? throw new ArgumentNullException("settings");
	}

	public void Method_06000045(ModelDoc2 P_0, string P_1, string P_2, int P_3, IProgress<string> P_4, string P_5 = null)
	{
		if (P_0 == null)
		{
			return;
		}
		string text = P_1;
		if (field_04000048.OutputDirectoryMode == "SameAsPart")
		{
			string pathName = P_0.GetPathName();
			if (!string.IsNullOrEmpty(pathName))
			{
				text = Path.GetDirectoryName(pathName);
			}
		}
		else if (field_04000048.OutputDirectoryMode == "Custom" && !string.IsNullOrEmpty(field_04000048.CustomOutputDirectory))
		{
			text = field_04000048.CustomOutputDirectory;
		}
		string text2 = Method_06000049(P_0);
		string text3 = Method_0600004A(P_0);
		string text4 = Method_06000046(text, text2, text3);
		try
		{
			if (!Directory.Exists(text4))
			{
				Directory.CreateDirectory(text4);
			}
		}
		catch (Exception ex)
		{
			P_4?.Report("    -> HATA (Klasör Oluşturma): " + ex.Message + ". Varsayılan klasör kullanılıyor.");
			text4 = P_1;
		}
		string text5 = Method_0600004B(P_0, P_2, P_3);
		List<string> list = new List<string>();
		if (field_04000048.ExportDXF)
		{
			string text6 = Path.Combine(text4, text5 + ".dxf");
			Method_06000047(P_0, text6, P_4);
			list.Add(text6);
		}
		if (field_04000048.ExportDWG)
		{
			string text7 = Path.Combine(text4, text5 + ".dwg");
			Method_06000047(P_0, text7, P_4);
			list.Add(text7);
		}
		if (string.IsNullOrEmpty(P_5))
		{
			return;
		}
		string text8 = Method_06000046(P_5, text2, text3);
		try
		{
			if (!Directory.Exists(text8))
			{
				Directory.CreateDirectory(text8);
			}
			foreach (string item in list)
			{
				if (File.Exists(item))
				{
					string text9 = Path.Combine(text8, Path.GetFileName(item));
					File.Copy(item, text9, overwrite: true);
					P_4?.Report("    -> Merkez kopya: " + text9);
				}
			}
		}
		catch (Exception ex2)
		{
			P_4?.Report("    -> HATA (merkez kopya): " + ex2.Message);
		}
	}

	private string Method_06000046(string P_0, string P_1, string P_2)
	{
		return field_04000048.FolderGrouping switch
		{
			"Material" => Path.Combine(P_0, P_1),
			"Thickness" => Path.Combine(P_0, P_2),
			"Material_Thickness" => Path.Combine(P_0, P_1, P_2),
			"Thickness_Material" => Path.Combine(P_0, P_2, P_1),
			_ => P_0,
		};
	}

	private void Method_06000047(ModelDoc2 P_0, string P_1, IProgress<string> P_2)
	{
		string text = Path.GetExtension(P_1).ToLower().TrimStart('.');
		try
		{
			field_04000047.SetUserPreferenceToggle(21, OnFlag: true);
		}
		catch
		{
		}
		if (field_04000048.UseDrawingTemplate && Method_06000048(P_0, P_1, text, P_2))
		{
			P_2?.Report("    -> Şablon ile başarıyla oluşturuldu: " + P_1);
			return;
		}
		int Errors = 0;
		int Warnings = 0;
		bool flag = false;
		if (P_0.GetType() == 1)
		{
			PartDoc partDoc = (PartDoc)P_0;
			int num = 1;
			if (!field_04000048.HideBendLines)
			{
				num |= 4;
			}
			flag = partDoc.ExportToDWG2(P_1, P_0.GetPathName(), 1, ExportToSingleFile: true, null, IsXDirFlipped: false, IsYDirFlipped: false, num, null);
		}
		else
		{
			flag = P_0.Extension.SaveAs(P_1, 0, 1, null, ref Errors, ref Warnings);
		}
		if (flag)
		{
			P_2?.Report("    -> Başarıyla oluşturuldu: " + P_1);
			return;
		}
		string text2 = ((P_0.GetType() == 1) ? "(ExportToDWG2 false döndü — API ayrıntılı hata kodu vermez)" : $"Kod: {Errors}");
		P_2?.Report("    -> HATA: " + P_1 + " oluşturulamadı. " + text2);
	}

	private bool Method_06000048(ModelDoc2 P_0, string P_1, string P_2, IProgress<string> P_3)
	{
		DrawingDoc drawingDoc = null;
		ModelDoc2 modelDoc = null;
		try
		{
			string text = field_04000048.DrawingTemplatePath;
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				text = field_04000047.GetUserPreferenceStringValue(10);
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				P_3?.Report("    -> UYARI: Teknik resim şablonu bulunamadı. Doğrudan export yöntemine geçiliyor...");
				return false;
			}
			modelDoc = (ModelDoc2)field_04000047.NewDocument(text, 11, 0.0, 0.0);
			if (modelDoc == null)
			{
				P_3?.Report("    -> UYARI: Teknik resim belgesi şablondan oluşturulamadı. Doğrudan export yöntemine geçiliyor...");
				return false;
			}
			drawingDoc = (DrawingDoc)modelDoc;
			string pathName = P_0.GetPathName();
			string name = P_0.ConfigurationManager.ActiveConfiguration.Name;
			View view = drawingDoc.CreateFlatPatternViewFromModelView3(pathName, name, 0.5, 0.5, 0.0, field_04000048.HideBendLines, FlipView: false);
			if (view == null)
			{
				P_3?.Report("    -> UYARI: Teknik resimde FlatPattern görünümü oluşturulamadı. Doğrudan export yöntemine geçiliyor...");
				field_04000047.CloseDoc(modelDoc.GetTitle());
				Marshal.ReleaseComObject(modelDoc);
				return false;
			}
			try
			{
				Sheet sheet = (Sheet)drawingDoc.GetCurrentSheet();
				if (sheet != null)
				{
					sheet.SetScale(1.0, 1.0, ScaleAnnoPosition: true, ScaleAnnoTextHeight: false);
					Marshal.ReleaseComObject(sheet);
				}
			}
			catch
			{
			}
			view.ScaleDecimal = 1.0;
			modelDoc.EditRebuild3();
			P_3?.Report("    -> Teknik resim ölçeği 1:1 sabitlendi (sayfa + görünüm).");
			modelDoc.ViewZoomtofit2();
			int Errors = 0;
			int Warnings = 0;
			bool result = modelDoc.Extension.SaveAs(P_1, 0, 1, null, ref Errors, ref Warnings);
			field_04000047.CloseDoc(modelDoc.GetTitle());
			Marshal.ReleaseComObject(view);
			Marshal.ReleaseComObject(modelDoc);
			return result;
		}
		catch (Exception ex)
		{
			P_3?.Report("    -> HATA (Drawing Export): " + ex.Message);
			if (modelDoc != null)
			{
				field_04000047.CloseDoc(modelDoc.GetTitle());
				Marshal.ReleaseComObject(modelDoc);
			}
			return false;
		}
	}

	private string Method_06000049(ModelDoc2 P_0)
	{
		if (P_0 is PartDoc partDoc)
		{
			string name = P_0.ConfigurationManager.ActiveConfiguration.Name;
			string Database = "";
			string materialPropertyName = partDoc.GetMaterialPropertyName2(name, out Database);
			if (!string.IsNullOrWhiteSpace(materialPropertyName))
			{
				return materialPropertyName.Trim();
			}
		}
		return "Bilinmeyen_Malzeme";
	}

	private string Method_0600004A(ModelDoc2 P_0)
	{
		string text = Method_0600004D(P_0, new string[3] { "Kalınlık", "Thickness", "Sac Kalınlığı" });
		if (string.IsNullOrWhiteSpace(text))
		{
			CustomPropertyManager customPropertyManager = ((IModelDocExtension)P_0.Extension).get_CustomPropertyManager("");
			if (customPropertyManager != null)
			{
				text = Method_0600004E(customPropertyManager, "Kalınlık") ?? Method_0600004E(customPropertyManager, "Thickness");
				Marshal.ReleaseComObject(customPropertyManager);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Method_0600004C(P_0);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "Bilinmeyen_Kalinlik";
		}
		text = text.Replace(" ", "").Replace("mm", "").Replace(",", ".")
			.Trim();
		return text + "mm";
	}

	private string Method_0600004B(ModelDoc2 P_0, string P_1, int P_2)
	{
		string text = field_04000048.FileNamePattern;
		if (string.IsNullOrEmpty(text))
		{
			text = "%DA%";
		}
		if (field_04000048.AppendThicknessQtyToName)
		{
			if (!text.Contains("%KA%"))
			{
				text += "_%KA%";
			}
			if (!text.Contains("%AD%"))
			{
				text += "_%AD%adet";
			}
		}
		string text2 = text.Replace("%DA%", P_1);
		if (text.Contains("%AD%"))
		{
			text2 = text2.Replace("%AD%", P_2.ToString());
		}
		if (text.Contains("%MA%"))
		{
			text2 = text2.Replace("%MA%", Method_06000049(P_0));
		}
		if (text.Contains("%KA%"))
		{
			text2 = text2.Replace("%KA%", Method_0600004A(P_0));
		}
		if (text.Contains("%KO%"))
		{
			string newValue = "";
			try
			{
				newValue = P_0.ConfigurationManager.ActiveConfiguration.Name;
			}
			catch
			{
			}
			text2 = text2.Replace("%KO%", newValue);
		}
		char[] invalidFileNameChars;
		if (text.Contains("%O1%") && !string.IsNullOrEmpty(field_04000048.CustomPropertyKey))
		{
			string text3 = Method_0600004D(P_0, new string[1] { field_04000048.CustomPropertyKey });
			if (string.IsNullOrEmpty(text3))
			{
				CustomPropertyManager customPropertyManager = ((IModelDocExtension)P_0.Extension).get_CustomPropertyManager("");
				if (customPropertyManager != null)
				{
					text3 = Method_0600004E(customPropertyManager, field_04000048.CustomPropertyKey);
					Marshal.ReleaseComObject(customPropertyManager);
				}
			}
			if (!string.IsNullOrEmpty(text3))
			{
				invalidFileNameChars = Path.GetInvalidFileNameChars();
				foreach (char oldChar in invalidFileNameChars)
				{
					text3 = text3.Replace(oldChar, '_');
				}
				text2 = text2.Replace("%O1%", text3.Trim());
			}
			else
			{
				text2 = text2.Replace("%O1%", "Bilinmeyen_Ozellik");
			}
		}
		invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar2 in invalidFileNameChars)
		{
			text2 = text2.Replace(oldChar2, '_');
		}
		return text2;
	}

	private string Method_0600004C(ModelDoc2 P_0)
	{
		try
		{
			Feature feature = (Feature)P_0.FirstFeature();
			while (feature != null)
			{
				if (feature.GetTypeName2() == "SheetMetal")
				{
					SheetMetalFeatureData sheetMetalFeatureData = (SheetMetalFeatureData)feature.GetDefinition();
					if (sheetMetalFeatureData != null)
					{
						double num = sheetMetalFeatureData.Thickness * 1000.0;
						Marshal.ReleaseComObject(sheetMetalFeatureData);
						Marshal.ReleaseComObject(feature);
						return num.ToString("0.##", CultureInfo.InvariantCulture);
					}
				}
				Feature feature2 = (Feature)feature.GetNextFeature();
				Marshal.ReleaseComObject(feature);
				feature = feature2;
			}
		}
		catch
		{
		}
		return null;
	}

	private string Method_0600004D(ModelDoc2 P_0, string[] P_1)
	{
		Feature feature = (Feature)P_0.FirstFeature();
		while (feature != null)
		{
			if (feature.GetTypeName2() == "CutListFolder")
			{
				CustomPropertyManager customPropertyManager = feature.CustomPropertyManager;
				foreach (string text in P_1)
				{
					string text2 = Method_0600004E(customPropertyManager, text);
					if (!string.IsNullOrEmpty(text2))
					{
						Marshal.ReleaseComObject(customPropertyManager);
						Marshal.ReleaseComObject(feature);
						return text2;
					}
				}
				Marshal.ReleaseComObject(customPropertyManager);
			}
			Feature feature2 = (Feature)feature.GetNextFeature();
			Marshal.ReleaseComObject(feature);
			feature = feature2;
		}
		return null;
	}

	private string Method_0600004E(CustomPropertyManager P_0, string P_1)
	{
		if (P_0 == null)
		{
			return null;
		}
		string ValOut = "";
		string ResolvedValOut = "";
		bool WasResolved = false;
		P_0.Get5(P_1, UseCached: false, out ValOut, out ResolvedValOut, out WasResolved);
		string text = ((!string.IsNullOrWhiteSpace(ResolvedValOut)) ? ResolvedValOut : ValOut);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return null;
	}
}
