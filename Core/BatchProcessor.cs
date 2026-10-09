using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

namespace SolidWorksBatchExport.Core;

public class BatchProcessor
{

	private readonly ISldWorks field_04000037;

	private readonly ProcessingSettings field_04000038;

	private readonly Scanner field_04000039;

	private readonly NotchManager field_0400003A;

	private readonly ExportManager field_0400003B;

	private readonly ReportManager field_0400003C;

	private readonly HashSet<string> field_0400003D = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> field_0400003E = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> field_0400003F = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private string field_04000040;

	private readonly Dictionary<string, int> field_04000041 = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	public BatchProcessor(ISldWorks P_0, ProcessingSettings P_1)
	{
		field_04000037 = P_0 ?? throw new ArgumentNullException("swApp");
		field_04000038 = P_1 ?? throw new ArgumentNullException("settings");
		field_04000039 = new Scanner();
		field_0400003A = new NotchManager(field_04000037, P_1);
		field_0400003B = new ExportManager(field_04000037, P_1);
		field_0400003C = new ReportManager();
	}

	public void ProcessFolder(string P_0, IProgress<string> P_1)
	{
		List<string> modelFilesFromFolder;
		try
		{
			modelFilesFromFolder = field_04000039.GetModelFilesFromFolder(P_0);
		}
		catch (Exception ex)
		{
			P_1?.Report("HATA: Klasör taranamadı: " + ex.Message);
			return;
		}
		ProcessFiles(modelFilesFromFolder, P_0, P_1);
	}

	public void ProcessFiles(List<string> P_0, string P_1, IProgress<string> P_2)
	{
		string text = null;
		field_0400003D.Clear();
		field_0400003E.Clear();
		field_0400003F.Clear();
		field_04000041.Clear();
		try
		{
			if (P_0 == null || P_0.Count == 0)
			{
				P_2?.Report("HATA: İşlenecek .SLDPRT veya .SLDASM dosyası yok.");
				return;
			}
			string path = Method_06000037(P_0);
			text = Path.Combine(P_1, path);
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			field_04000040 = text;
			P_2?.Report($"Toplam {P_0.Count} dosya işlenecek. Merkez çıktı klasörü: {text}");
			P_2?.Report($"Ayarlar: çentik={(field_04000038.CreateNotch ? "açık" : "kapalı")}, çap={field_04000038.NotchDiameterMm.ToString("0.###", CultureInfo.InvariantCulture)}mm, DXF={(field_04000038.ExportDXF ? "✓" : "✗")}, DWG={(field_04000038.ExportDWG ? "✓" : "✗")}, bükümÇizgileriniGizle={(field_04000038.HideBendLines ? "AÇIK" : "kapalı")}, şablon={((!field_04000038.UseDrawingTemplate) ? "kapalı" : (string.IsNullOrEmpty(field_04000038.DrawingTemplatePath) ? "varsayılan" : field_04000038.DrawingTemplatePath))}, tümKonfigürasyonlar={(field_04000038.ProcessAllConfigurations ? "açık" : "kapalı")}, klasörleme={field_04000038.FolderGrouping}, çıktıKonumu={field_04000038.OutputPlacement}, dosyaDeseni={field_04000038.FileNamePattern}, adaKalınlıkAdet={(field_04000038.AppendThicknessQtyToName ? "açık" : "kapalı")}");
			Method_06000031("koşu başlangıcı", P_2);
			Method_0600003C(P_0, P_2);
			P_0 = P_0.OrderByDescending((string text2) => text2.ToLower().EndsWith(".sldasm")).ToList();
			int num = 1;
			foreach (string item in P_0)
			{
				P_2?.Report($"[{num}/{P_0.Count}] İşleniyor: {Path.GetFileNameWithoutExtension(item)}");
				try
				{
					Method_06000033(item, text, P_2);
				}
				catch (Exception ex)
				{
					P_2?.Report($"    -> HATA: {Path.GetFileName(item)} işlenirken beklenmeyen hata: {ex.Message}. Sonraki dosyaya geçiliyor.");
				}
				num++;
			}
			Method_06000031("koşu sonu", P_2);
			P_2?.Report("Tüm dosyalar işlendi.");
		}
		catch (Exception ex2)
		{
			P_2?.Report("KRİTİK HATA: " + ex2.Message);
		}
		finally
		{
			if (text != null)
			{
				try
				{
					field_0400003C.Method_060000B1(text, field_04000041);
					P_2?.Report("Kesim listesi raporu (CSV): " + Path.Combine(text, "KesimListesiRaporu.csv"));
					P_2?.Report("Kesim listesi raporu (HTML, önizlemeli): " + Path.Combine(text, "KesimListesiRaporu.html"));
				}
				catch (Exception ex3)
				{
					P_2?.Report("HATA: Rapor yazılamadı: " + ex3.Message);
				}
			}
		}
	}

	private void Method_06000031(string P_0, IProgress<string> P_1)
	{
		try
		{
			P_1?.Report($"SW oturumu ({P_0}): açık belge sayısı = {field_04000037.GetDocumentCount()}");
		}
		catch (Exception ex)
		{
			P_1?.Report($"SW oturumu ({P_0}): belge sayısı okunamadı ({ex.Message})");
		}
	}

	private void Method_06000032(ModelDoc2 P_0, string P_1, IProgress<string> P_2)
	{
		string title;
		try
		{
			title = P_0.GetTitle();
		}
		catch (Exception ex)
		{
			P_2?.Report(P_1 + "KAPAT: başlık okunamadı (" + ex.Message + ") — kapatma atlandı.");
			return;
		}
		string value;
		try
		{
			value = (P_0.GetSaveFlag() ? "KİRLİ(kaydedilmemiş değişiklik var)" : "temiz");
		}
		catch (Exception ex2)
		{
			value = "okunamadı(" + ex2.Message + ")";
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			field_04000037.QuitDoc(title);
		}
		finally
		{
			stopwatch.Stop();
			string value2 = ((stopwatch.ElapsedMilliseconds > 2000) ? "  <== YAVAŞ (beklenmedik diyalog şüphesi)" : "");
			P_2?.Report($"{P_1}KAPAT(QuitDoc): durum={value}, süre={stopwatch.ElapsedMilliseconds}ms{value2}");
		}
	}

	private void Method_06000033(string P_0, string P_1, IProgress<string> P_2)
	{
		int Errors = 0;
		int Warnings = 0;
		ModelDoc2 modelDoc = null;
		try
		{
			int num = ((!P_0.ToLower().EndsWith(".sldasm")) ? 1 : 2);
			if (num == 1)
			{
				if (field_0400003F.Contains(Path.GetFullPath(P_0)))
				{
					P_2?.Report("    -> Atlandı: bu parça seçili bir montajın bileşeni olarak zaten KENDİ klasörüne alındı (Output'a mükerrer basılmadı).");
					return;
				}
				if (!field_0400003D.Add(Method_06000036("", P_0)))
				{
					P_2?.Report("    -> Atlandı: bu parça bu koşuda zaten işlendi (tekil mükerrerliği).");
					return;
				}
			}
			modelDoc = field_04000037.OpenDoc6(P_0, num, 1, "", ref Errors, ref Warnings);
			if (modelDoc == null)
			{
				P_2?.Report($"    -> HATA: Dosya açılamadı. Hata Kodu: {Errors}");
			}
			else if (num == 2)
			{
				Method_06000034(modelDoc, P_1, P_2);
			}
			else
			{
				field_04000037.ActivateDoc3(modelDoc.GetTitle(), UseUserPreferences: true, 2, ref Errors);
				Method_06000038(modelDoc, P_1, P_2);
			}
		}
		finally
		{
			if (modelDoc != null)
			{
				Method_06000032(modelDoc, "    -> ", P_2);
				Marshal.ReleaseComObject(modelDoc);
			}
		}
	}

	private void Method_06000034(ModelDoc2 P_0, string P_1, IProgress<string> P_2)
	{
		P_2?.Report("    -> Montaj dosyası algılandı. Alt-montaj hiyerarşisi taranıyor...");
		AssemblyDoc assemblyDoc = (AssemblyDoc)P_0;
		assemblyDoc.ResolveAllLightWeightComponents(WarnUser: true);
		object[] array = (object[])assemblyDoc.GetComponents(ToplevelOnly: true);
		if (array == null || array.Length == 0)
		{
			P_2?.Report("    -> Montajda üst-seviye bileşen bulunamadı (boş/çözülemeyen montaj).");
			return;
		}
		string text = Path.GetDirectoryName(P_0.GetPathName());
		if (string.IsNullOrEmpty(text))
		{
			text = P_1;
		}
		List<string[]> list = new List<string[]>();
		object[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Component2 component = (Component2)array2[i];
			try
			{
				if (component.GetSuppression() == 0)
				{
					continue;
				}
				string pathName = component.GetPathName();
				if (string.IsNullOrEmpty(pathName))
				{
					continue;
				}
				if (pathName.ToLower().EndsWith(".sldprt"))
				{
					list.Add(new string[3] { pathName, text, "(ana montaj klasörü)" });
				}
				else if (pathName.ToLower().EndsWith(".sldasm"))
				{
					string text2 = Path.GetDirectoryName(pathName);
					if (string.IsNullOrEmpty(text2))
					{
						text2 = text;
					}
					Method_06000035(component, text2, Path.GetFileNameWithoutExtension(pathName), list, P_2);
				}
			}
			catch (Exception ex)
			{
				P_2?.Report("    -> HATA: üst-seviye bileşen taranırken çökme: " + ex.Message);
			}
			finally
			{
				Marshal.ReleaseComObject(component);
			}
		}
		P_2?.Report($"    -> {list.Count} parça-örneği işlenecek (çıktılar kendi klasörlerine).");
		foreach (string[] item in list)
		{
			string text3 = item[0];
			string text4 = item[1];
			string text5 = item[2];
			field_0400003F.Add(Path.GetFullPath(text3));
			string text6 = (string.Equals(field_04000038.OutputPlacement, "Central", StringComparison.OrdinalIgnoreCase) ? field_04000040 : text4);
			if (!field_0400003D.Add(Method_06000036(text6, text3)))
			{
				P_2?.Report($"      => '{Path.GetFileNameWithoutExtension(text3)}' [{text5}] klasöründe zaten işlendi — atlandı (mükerrer önleme).");
				continue;
			}
			int Errors = 0;
			int Warnings = 0;
			ModelDoc2 modelDoc = field_04000037.OpenDoc6(text3, 1, 1, "", ref Errors, ref Warnings);
			if (modelDoc == null)
			{
				P_2?.Report($"      => HATA: parça açılamadı: {Path.GetFileName(text3)} (kod {Errors}).");
				continue;
			}
			try
			{
				P_2?.Report("      => İşleniyor: " + Path.GetFileNameWithoutExtension(text3) + "  →  klasör: " + text5);
				field_04000037.ActivateDoc3(modelDoc.GetTitle(), UseUserPreferences: true, 2, ref Errors);
				Method_06000038(modelDoc, text4, P_2);
			}
			catch (Exception ex2)
			{
				P_2?.Report("      => Montaj bileşeninde beklenmeyen hata: " + ex2.Message);
			}
			finally
			{
				Method_06000032(modelDoc, "      => ", P_2);
				Marshal.ReleaseComObject(modelDoc);
			}
		}
	}

	private void Method_06000035(Component2 P_0, string P_1, string P_2, List<string[]> P_3, IProgress<string> P_4)
	{
		object[] array = (object[])P_0.GetChildren();
		if (array == null)
		{
			P_4?.Report("      => UYARI: '" + P_2 + "' alt-montajının altı taranamadı (GetChildren boş/çözülememiş) — bu daldan parça eklenmedi.");
			return;
		}
		object[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Component2 component = (Component2)array2[i];
			try
			{
				if (component.GetSuppression() == 0)
				{
					continue;
				}
				string pathName = component.GetPathName();
				if (!string.IsNullOrEmpty(pathName))
				{
					if (pathName.ToLower().EndsWith(".sldprt"))
					{
						P_3.Add(new string[3] { pathName, P_1, P_2 });
					}
					else if (pathName.ToLower().EndsWith(".sldasm"))
					{
						Method_06000035(component, P_1, P_2, P_3, P_4);
					}
				}
			}
			finally
			{
				Marshal.ReleaseComObject(component);
			}
		}
	}

	private static string Method_06000036(string P_0, string P_1)
	{
		return P_0 + "\n" + Path.GetFullPath(P_1);
	}

	private static string Method_06000037(List<string> P_0)
	{
		string text = P_0?.FirstOrDefault((string text4) => text4.ToLower().EndsWith(".sldasm"));
		if (text == null)
		{
			return "Output";
		}
		string text2 = Path.GetFileNameWithoutExtension(text).Trim();
		if (text2.Length == 0)
		{
			return "Output";
		}
		int num = text2.LastIndexOf('-');
		string text3 = ((num > 0) ? (text2.Substring(0, num + 1) + "KESİM") : (text2 + "-KESİM"));
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text3 = text3.Replace(oldChar, '_');
		}
		return text3;
	}

	private void Method_06000038(ModelDoc2 P_0, string P_1, IProgress<string> P_2)
	{
		try
		{
			if (!Method_0600003D(P_0))
			{
				P_2?.Report("    -> Parça sac levha (Sheet Metal) değil. Atlanıyor.");
				return;
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(P_0.GetPathName());
			int num = ((!field_04000041.TryGetValue(fileNameWithoutExtension, out var value) || value <= 0) ? 1 : value);
			Method_06000039(P_0, P_2);
			List<string> list = new List<string> { null };
			if (field_04000038.ProcessAllConfigurations)
			{
				List<string> list2 = Method_0600003A(P_0);
				if (list2.Count > 1)
				{
					P_2?.Report($"    -> {list2.Count} konfigürasyon bulundu, hepsi işlenecek: {string.Join(", ", list2)}");
					list = list2;
				}
			}
			bool flag = list.Count > 1;
			foreach (string item in list)
			{
				string text = fileNameWithoutExtension;
				if (item != null)
				{
					if (flag)
					{
						P_2?.Report("    -> Konfigürasyon işleniyor: " + item);
					}
					if (!P_0.ShowConfiguration2(item))
					{
						P_2?.Report("    -> HATA: '" + item + "' konfigürasyonuna geçilemedi — atlandı.");
						continue;
					}
					P_0.EditRebuild3();
					if (flag)
					{
						text = fileNameWithoutExtension + "_" + item;
					}
				}
				NotchResult notchResult = null;
				Stopwatch stopwatch = Stopwatch.StartNew();
				if (field_04000038.CreateNotch)
				{
					notchResult = field_0400003A.CreateNotches(P_0, P_2);
					if (!notchResult.FlatPatternFound || !notchResult.BendLinesFound || notchResult.FailedCuts > 0)
					{
						P_2?.Report("    -> UYARI: Çentik sonucu '" + notchResult.GetSummary() + "' — DXF/DWG bu eksikle üretilecek.");
					}
				}
				stopwatch.Stop();
				Stopwatch stopwatch2 = Stopwatch.StartNew();
				P_0.EditRebuild3();
				stopwatch2.Stop();
				bool flag2 = string.Equals(field_04000038.OutputPlacement, "Central", StringComparison.OrdinalIgnoreCase);
				bool flag3 = string.Equals(field_04000038.OutputPlacement, "Both", StringComparison.OrdinalIgnoreCase);
				string text2 = (flag2 ? field_04000040 : P_1);
				string text3 = ((flag3 && !string.Equals(Path.GetFullPath(P_1), Path.GetFullPath(field_04000040), StringComparison.OrdinalIgnoreCase)) ? field_04000040 : null);
				Stopwatch stopwatch3 = Stopwatch.StartNew();
				field_0400003B.Method_06000045(P_0, text2, text, num, P_2, text3);
				stopwatch3.Stop();
				int num2 = notchResult?.SuccessfulCuts ?? 0;
				string value2 = ((num2 > 0) ? $" ({stopwatch.ElapsedMilliseconds / num2}ms/kesim × {num2})" : "");
				P_2?.Report($"    -> SÜRE [{text}]: çentik={stopwatch.ElapsedMilliseconds}ms{value2}, rebuild={stopwatch2.ElapsedMilliseconds}ms, export={stopwatch3.ElapsedMilliseconds}ms");
				if (field_0400003E.Add(text))
				{
					Method_0600003B(P_0, field_04000040, text, P_2);
					field_0400003C.Method_060000AA(P_0, text, fileNameWithoutExtension, (!field_04000038.CreateNotch) ? "Kapalı" : (notchResult?.GetSummary() ?? "-"));
				}
			}
		}
		catch (Exception ex)
		{
			P_2?.Report("    -> HATA (ProcessPart): " + ex.Message);
		}
	}

	private void Method_06000039(ModelDoc2 P_0, IProgress<string> P_1)
	{
		try
		{
			if (!(P_0 is PartDoc partDoc))
			{
				return;
			}
			object[] array = (object[])partDoc.GetBodies2(0, BVisibleOnly: true);
			if (array == null)
			{
				return;
			}
			if (array.Length > 1)
			{
				P_1?.Report($"    -> UYARI: Parçada {array.Length} katı gövde var — en büyük gövde işlenir (tam çok-gövde desteği yol haritasında).");
			}
			object[] array2 = array;
			foreach (object obj in array2)
			{
				if (obj != null)
				{
					Marshal.ReleaseComObject(obj);
				}
			}
		}
		catch
		{
		}
	}

	private List<string> Method_0600003A(ModelDoc2 P_0)
	{
		List<string> list = new List<string>();
		try
		{
			object configurationNames = P_0.GetConfigurationNames();
			IEnumerable<string> enumerable;
			if (configurationNames is string[] array)
			{
				enumerable = array;
			}
			else
			{
				enumerable = ((!(configurationNames is object[] source)) ? Enumerable.Empty<string>() : source.Select((object obj3) => Convert.ToString(obj3)));
			}
			foreach (string item in enumerable)
			{
				if (string.IsNullOrEmpty(item))
				{
					continue;
				}
				Configuration configuration = (Configuration)P_0.GetConfigurationByName(item);
				if (configuration != null)
				{
					bool flag = false;
					try
					{
						flag = configuration.IsDerived();
					}
					catch
					{
					}
					if (!flag)
					{
						list.Add(item);
					}
					Marshal.ReleaseComObject(configuration);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private void Method_0600003B(ModelDoc2 P_0, string P_1, string P_2, IProgress<string> P_3)
	{
		try
		{
			string text = Path.Combine(P_1, "Thumbs");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string text2 = P_2;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			string fileNameIn = Path.Combine(text, text2 + ".bmp");
			P_0.ViewZoomtofit2();
			if (!P_0.SaveBMP(fileNameIn, 320, 240))
			{
				P_3?.Report("    -> UYARI: Küçük resim kaydedilemedi (SaveBMP false döndü).");
			}
		}
		catch (Exception ex)
		{
			P_3?.Report("    -> UYARI: Küçük resim alınamadı: " + ex.Message);
		}
	}

	private void Method_0600003C(List<string> P_0, IProgress<string> P_1)
	{
		List<string> list = P_0.Where((string text) => text.ToLower().EndsWith(".sldasm")).ToList();
		if (list.Count == 0)
		{
			return;
		}
		P_1?.Report($"Adet ön sayımı: {list.Count} montaj taranıyor...");
		foreach (string item in list)
		{
			int Errors = 0;
			int Warnings = 0;
			ModelDoc2 modelDoc = null;
			try
			{
				modelDoc = field_04000037.OpenDoc6(item, 2, 1, "", ref Errors, ref Warnings);
				if (modelDoc == null)
				{
					P_1?.Report($"    -> UYARI: adet sayımı için montaj açılamadı ({Path.GetFileName(item)}, kod {Errors}).");
					continue;
				}
				object[] array = (object[])((AssemblyDoc)modelDoc).GetComponents(ToplevelOnly: false);
				if (array == null)
				{
					continue;
				}
				object[] array2 = array;
				for (int num = 0; num < array2.Length; num++)
				{
					Component2 component = (Component2)array2[num];
					try
					{
						if (component.GetSuppression() != 0)
						{
							string pathName = component.GetPathName();
							if (!string.IsNullOrEmpty(pathName) && pathName.ToLower().EndsWith(".sldprt"))
							{
								string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pathName);
								field_04000041[fileNameWithoutExtension] = ((!field_04000041.TryGetValue(fileNameWithoutExtension, out var value)) ? 1 : (value + 1));
							}
						}
					}
					finally
					{
						Marshal.ReleaseComObject(component);
					}
				}
			}
			catch (Exception ex)
			{
				P_1?.Report("    -> UYARI: adet sayımında hata (" + Path.GetFileName(item) + "): " + ex.Message);
			}
			finally
			{
				if (modelDoc != null)
				{
					Method_06000032(modelDoc, "    -> ", P_1);
					Marshal.ReleaseComObject(modelDoc);
				}
			}
		}
		foreach (KeyValuePair<string, int> item2 in field_04000041)
		{
			P_1?.Report($"    -> Adet: {item2.Key} = {item2.Value}");
		}
	}

	private bool Method_0600003D(ModelDoc2 P_0)
	{
		Feature feature = (Feature)P_0.FirstFeature();
		while (feature != null)
		{
			if (feature.GetTypeName2() == "SheetMetal")
			{
				Marshal.ReleaseComObject(feature);
				return true;
			}
			Feature feature2 = (Feature)feature.GetNextFeature();
			Marshal.ReleaseComObject(feature);
			feature = feature2;
		}
		return false;
	}
}
