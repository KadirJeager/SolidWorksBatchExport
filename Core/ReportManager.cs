using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SolidWorks.Interop.sldworks;

namespace SolidWorksBatchExport.Core;

public class ReportManager
{
	private class CutListRow
	{
		public string field_04000082;

		public string field_04000083;

		public string field_04000084;

		public string field_04000085;

		public string field_04000086;

		public string field_04000087;

		public string field_04000088;

		public string field_04000089;

		public string field_0400008A;
	}

	private static readonly string[] field_04000080 = new string[9] { "DosyaAdı", "Adet", "Kalınlık", "Malzeme", "Kütle", "Sınırlandırma Kutusu Uzunluğu", "Sınırlandırma Kutusu Genişliği", "Kesim Alanı", "Çentik" };

	private readonly List<CutListRow> field_04000081 = new List<CutListRow>();

	public void Method_060000AA(ModelDoc2 P_0, string P_1, string P_2, string P_3)
	{
		if (P_0 == null)
		{
			return;
		}
		string text = Method_060000AE(P_0, new string[3] { "Kalınlık", "Thickness", "Sac Kalınlığı" });
		string text2 = Method_060000AB(P_0);
		string text3 = Method_060000AC(P_0);
		string text4 = Method_060000AE(P_0, new string[4] { "Sınırlandırma Kutusu Uzunluğu", "Bounding Box Length", "Uzunluk", "Length" });
		string text5 = Method_060000AE(P_0, new string[4] { "Sınırlandırma Kutusu Genişliği", "Bounding Box Width", "Genişlik", "Width" });
		string text6 = Method_060000AE(P_0, new string[4] { "Sınırlandırma Kutusu Alanı", "Bounding Box Area", "Alan", "Area" });
		CustomPropertyManager customPropertyManager = ((IModelDocExtension)P_0.Extension).get_CustomPropertyManager("");
		if (customPropertyManager != null)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				text = Method_060000AF(customPropertyManager, "Kalınlık") ?? Method_060000AF(customPropertyManager, "Thickness");
			}
			if (string.IsNullOrWhiteSpace(text2) || text2 == "Bilinmeyen_Malzeme")
			{
				text2 = Method_060000AF(customPropertyManager, "Malzeme") ?? Method_060000AF(customPropertyManager, "Material") ?? text2;
			}
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = Method_060000AF(customPropertyManager, "Sınırlandırma Kutusu Uzunluğu") ?? Method_060000AF(customPropertyManager, "Bounding Box Length");
			}
			if (string.IsNullOrWhiteSpace(text5))
			{
				text5 = Method_060000AF(customPropertyManager, "Sınırlandırma Kutusu Genişliği") ?? Method_060000AF(customPropertyManager, "Bounding Box Width");
			}
			if (string.IsNullOrWhiteSpace(text6))
			{
				text6 = Method_060000AF(customPropertyManager, "Sınırlandırma Kutusu Alanı") ?? Method_060000AF(customPropertyManager, "Bounding Box Area");
			}
			Marshal.ReleaseComObject(customPropertyManager);
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Method_060000AD(P_0);
		}
		field_04000081.Add(new CutListRow
		{
			field_04000082 = P_1,
			field_04000083 = (P_2 ?? P_1),
			field_04000084 = (text ?? ""),
			field_04000085 = (text2 ?? ""),
			field_04000086 = (text3 ?? ""),
			field_04000087 = (text4 ?? ""),
			field_04000088 = (text5 ?? ""),
			field_04000089 = (text6 ?? ""),
			field_0400008A = (P_3 ?? "-")
		});
	}

	private string Method_060000AB(ModelDoc2 P_0)
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

	private string Method_060000AC(ModelDoc2 P_0)
	{
		try
		{
			double[] array = (double[])P_0.Extension.GetMassProperties2(1, out var _, UseSelected: true);
			if (array != null && array.Length > 5)
			{
				double num = array[5];
				return num.ToString("F3", CultureInfo.InvariantCulture) + " kg";
			}
		}
		catch
		{
		}
		CustomPropertyManager customPropertyManager = ((IModelDocExtension)P_0.Extension).get_CustomPropertyManager("");
		if (customPropertyManager != null)
		{
			string text = Method_060000AF(customPropertyManager, "Kütle") ?? Method_060000AF(customPropertyManager, "Mass");
			Marshal.ReleaseComObject(customPropertyManager);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return "Bilinmeyen_Kutle";
	}

	private string Method_060000AD(ModelDoc2 P_0)
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
						return num.ToString("0.##", CultureInfo.InvariantCulture) + "mm";
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

	private string Method_060000AE(ModelDoc2 P_0, string[] P_1)
	{
		Feature feature = (Feature)P_0.FirstFeature();
		while (feature != null)
		{
			if (feature.GetTypeName2() == "CutListFolder")
			{
				CustomPropertyManager customPropertyManager = feature.CustomPropertyManager;
				foreach (string text in P_1)
				{
					string text2 = Method_060000AF(customPropertyManager, text);
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

	private string Method_060000AF(CustomPropertyManager P_0, string P_1)
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

	private int Method_060000B0(CutListRow P_0, Dictionary<string, int> P_1)
	{
		if (P_1 != null && P_1.TryGetValue(P_0.field_04000083, out var value) && value > 0)
		{
			return value;
		}
		return 1;
	}

	public void Method_060000B1(string P_0, Dictionary<string, int> P_1)
	{
		if (field_04000081.Count != 0)
		{
			Method_060000B2(P_0, P_1);
			Method_060000B4(P_0, P_1);
		}
	}

	private void Method_060000B2(string P_0, Dictionary<string, int> P_1)
	{
		using StreamWriter streamWriter = new StreamWriter(Path.Combine(P_0, "KesimListesiRaporu.csv"), append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		Method_060000B3(streamWriter, field_04000080);
		foreach (CutListRow item in field_04000081)
		{
			Method_060000B3(streamWriter, new string[9]
			{
				item.field_04000082,
				Method_060000B0(item, P_1).ToString(),
				item.field_04000084,
				item.field_04000085,
				item.field_04000086,
				item.field_04000087,
				item.field_04000088,
				item.field_04000089,
				item.field_0400008A
			});
		}
	}

	private static void Method_060000B3(StreamWriter P_0, string[] P_1)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < P_1.Length; i++)
		{
			string text = (P_1[i] ?? "").Replace("\"", "\"\"");
			list.Add("\"" + text + "\"");
		}
		P_0.WriteLine(string.Join(";", list));
	}

	private void Method_060000B4(string P_0, Dictionary<string, int> P_1)
	{
		string path = Path.Combine(P_0, "KesimListesiRaporu.html");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<!DOCTYPE html><html lang=\"tr\"><head><meta charset=\"utf-8\"/>");
		stringBuilder.AppendLine("<title>Kesim Listesi Raporu</title>");
		stringBuilder.AppendLine("<style>");
		stringBuilder.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}");
		stringBuilder.AppendLine("h1{font-size:20px} .meta{color:#666;font-size:12px;margin-bottom:14px}");
		stringBuilder.AppendLine("table{border-collapse:collapse;width:100%} th,td{border:1px solid #ccc;padding:6px 10px;font-size:13px;text-align:left}");
		stringBuilder.AppendLine("th{background:#f0f0f0} tr:nth-child(even){background:#fafafa}");
		stringBuilder.AppendLine("img{max-width:160px;max-height:120px;border:1px solid #ddd;background:#fff}");
		stringBuilder.AppendLine(".num{text-align:right} @media print{body{margin:8mm}}");
		stringBuilder.AppendLine("</style></head><body>");
		stringBuilder.AppendLine("<h1>Kesim Listesi Raporu</h1>");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(65, 1, stringBuilder2);
		handler.AppendLiteral("<div class=\"meta\">Oluşturulma: ");
		handler.AppendFormatted(DateTime.Now, "yyyy-MM-dd HH:mm");
		handler.AppendLiteral(" — SolidWorks Batch Exporter</div>");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder.AppendLine("<table><tr><th>Önizleme</th>");
		string[] array = field_04000080;
		foreach (string text in array)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<th>");
			handler.AppendFormatted(Method_060000B5(text));
			handler.AppendLiteral("</th>");
			stringBuilder4.AppendLine(ref handler);
		}
		stringBuilder.AppendLine("</tr>");
		foreach (CutListRow item in field_04000081)
		{
			string text2 = item.field_04000082;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			string text3 = "Thumbs/" + text2 + ".bmp";
			string value = (File.Exists(Path.Combine(P_0, "Thumbs", text2 + ".bmp")) ? ("<img src=\"" + Method_060000B5(text3) + "\" alt=\"\"/>") : "-");
			stringBuilder.AppendLine("<tr>");
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<td>");
			handler.AppendFormatted(value);
			handler.AppendLiteral("</td>");
			stringBuilder5.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<td>");
			handler.AppendFormatted(Method_060000B5(item.field_04000082));
			handler.AppendLiteral("</td>");
			stringBuilder6.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("<td class=\"num\">");
			handler.AppendFormatted(Method_060000B0(item, P_1));
			handler.AppendLiteral("</td>");
			stringBuilder7.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<td>");
			handler.AppendFormatted(Method_060000B5(item.field_04000084));
			handler.AppendLiteral("</td>");
			stringBuilder8.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder9 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<td>");
			handler.AppendFormatted(Method_060000B5(item.field_04000085));
			handler.AppendLiteral("</td>");
			stringBuilder9.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder10 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("<td class=\"num\">");
			handler.AppendFormatted(Method_060000B5(item.field_04000086));
			handler.AppendLiteral("</td>");
			stringBuilder10.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder11 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("<td class=\"num\">");
			handler.AppendFormatted(Method_060000B5(item.field_04000087));
			handler.AppendLiteral("</td>");
			stringBuilder11.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder12 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("<td class=\"num\">");
			handler.AppendFormatted(Method_060000B5(item.field_04000088));
			handler.AppendLiteral("</td>");
			stringBuilder12.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder13 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("<td class=\"num\">");
			handler.AppendFormatted(Method_060000B5(item.field_04000089));
			handler.AppendLiteral("</td>");
			stringBuilder13.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder14 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
			handler.AppendLiteral("<td>");
			handler.AppendFormatted(Method_060000B5(item.field_0400008A));
			handler.AppendLiteral("</td>");
			stringBuilder14.AppendLine(ref handler);
			stringBuilder.AppendLine("</tr>");
		}
		stringBuilder.AppendLine("</table></body></html>");
		File.WriteAllText(path, stringBuilder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
	}

	private static string Method_060000B5(string P_0)
	{
		return (P_0 ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
			.Replace("\"", "&quot;");
	}
}
