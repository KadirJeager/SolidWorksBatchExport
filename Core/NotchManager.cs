using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

namespace SolidWorksBatchExport.Core;

public class NotchManager
{
	private class BendEndpoint
	{
		public int field_04000075;

		public double[] field_04000076;

		public double[] field_04000077;

		public double[] field_04000078;

		public double[] field_04000079;

		public int field_0400007A = -1;

		public double field_0400007B;

		public double[] field_0400007C;

		public double[] field_0400007D;

		public double[] field_0400007E;

		public string field_0400007F = "-";
	}

	private const double field_04000064 = 0.0001;

	private const double field_04000065 = 0.0001;

	private const double field_04000066 = 0.0001;

	private const double field_04000067 = 1E-07;

	private const double field_04000068 = 0.002;

	private const double field_04000069 = 5E-05;

	private const int field_0400006A = 3002;

	private const string field_0400006B = "FlatPattern";

	private const string field_0400006C = "ProfileFeature";

	private static readonly CompareInfo field_0400006D = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

	private readonly string[] field_0400006E = new string[4] { "Büküm-Çizgileri", "Bend-Lines", "Büküm Çizgileri", "Bend Lines" };

	private readonly ISldWorks field_0400006F;

	private readonly ProcessingSettings field_04000070;

	private readonly bool field_04000071;

	private const double field_04000072 = 4.0;

	private const double field_04000073 = 4.0;

	private const double field_04000074 = 6.0;

	public NotchManager(ISldWorks P_0, ProcessingSettings P_1)
	{
		field_0400006F = P_0 ?? throw new ArgumentNullException("swApp");
		field_04000070 = P_1 ?? throw new ArgumentNullException("settings");
		field_04000071 = P_1.FastNotches || Method_0600008A();
	}

	private static bool Method_0600008A()
	{
		try
		{
			string processPath = System.Environment.ProcessPath;
			if (string.IsNullOrEmpty(processPath))
			{
				return false;
			}
			return Path.GetFileName(processPath).IndexOf("hizli", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static string Method_0600008B(double P_0)
	{
		return (P_0 * 1000.0).ToString("0.###", CultureInfo.InvariantCulture);
	}

	private static string Method_0600008C(double[] P_0)
	{
		return $"({Method_0600008B(P_0[0])}; {Method_0600008B(P_0[1])}; {Method_0600008B(P_0[2])})mm";
	}

	public NotchResult CreateNotches(ModelDoc2 P_0, IProgress<string> P_1)
	{
		NotchResult notchResult = new NotchResult();
		if (P_0 == null)
		{
			return notchResult;
		}
		try
		{
			double num = Method_0600008E(P_0, P_1);
			double num2 = num / 2.0 / 1000.0;
			P_1?.Report($"    -> Çentik işlemi başlıyor. Kullanılan çap={num.ToString("0.###", CultureInfo.InvariantCulture)}mm → kesilecek yarıçap={Method_0600008B(num2)}mm");
			if (num2 <= 0.0)
			{
				P_1?.Report("    -> HATA: Çentik çapı 0 veya negatif — çentik adımı atlandı.");
				return notchResult;
			}
			Feature feature = Method_06000090(P_0, P_1);
			if (feature == null)
			{
				P_1?.Report("    -> HATA: FlatPattern unsuru bulunamadı veya açılamadı.");
				return notchResult;
			}
			notchResult.FlatPatternFound = true;
			P_0.EditRebuild3();
			List<BendEndpoint> list = Method_06000091(P_0, feature, P_1);
			Marshal.ReleaseComObject(feature);
			if (list.Count == 0)
			{
				P_1?.Report("    -> HATA: Büküm çizgisi ucu bulunamadı.");
				return notchResult;
			}
			notchResult.BendLinesFound = true;
			list = Method_06000093(P_0, list, notchResult, P_1);
			if (list.Count == 0)
			{
				P_1?.Report((notchResult.SkippedInternalBends > 0) ? $"    -> Kesilecek nokta kalmadı: {notchResult.SkippedInternalBends} uç bilinçli atlandı (mevcut çentik/kısmi büküm)." : "    -> HATA: Snap/ofset aşamasından geçen çentik noktası kalmadı.");
				return notchResult;
			}
			notchResult.TotalNotches = list.Count;
			P_1?.Report("    -> Çentik kesim modu: " + (field_04000071 ? "TOPLU (hızlı — tek eskiz + tek FeatureCut)" : "nokta-başına (kanıtlanmış)"));
			if (field_04000071)
			{
				Method_0600009A(P_0, list, num2, notchResult, P_1);
			}
			else
			{
				foreach (BendEndpoint item in list)
				{
					if (Method_0600009B(P_0, item, num2, P_1))
					{
						notchResult.SuccessfulCuts++;
					}
					else
					{
						notchResult.FailedCuts++;
					}
				}
			}
			P_0.EditRebuild3();
			P_1?.Report($"    -> ÇENTİK ÖZETİ: {notchResult.SuccessfulCuts}/{notchResult.TotalNotches} kesim başarılı" + ((notchResult.FailedCuts > 0) ? $", {notchResult.FailedCuts} BAŞARISIZ (ayrıntılar yukarıdaki [N#] satırlarında)" : "") + ((notchResult.SkippedInternalBends > 0) ? $" | {notchResult.SkippedInternalBends} uç bilinçli atlandı." : "."));
			return notchResult;
		}
		catch (Exception ex)
		{
			P_1?.Report("    -> KRİTİK HATA (NotchService): " + ex.Message);
			return notchResult;
		}
	}

	private double Method_0600008E(ModelDoc2 P_0, IProgress<string> P_1)
	{
		double notchDiameterMm = field_04000070.NotchDiameterMm;
		if (string.Equals(field_04000070.NotchDiameterMode, "Fixed", StringComparison.OrdinalIgnoreCase))
		{
			P_1?.Report("    -> Çentik çapı: ÖZEL (sabit) çap " + notchDiameterMm.ToString("0.###", CultureInfo.InvariantCulture) + "mm.");
			return notchDiameterMm;
		}
		double num = Method_0600008F(P_0);
		if (num <= 0.0)
		{
			P_1?.Report("    -> Çentik çapı (otomatik): sac kalınlığı okunamadı → özel çap kutusundaki " + notchDiameterMm.ToString("0.###", CultureInfo.InvariantCulture) + "mm kullanılıyor.");
			return notchDiameterMm;
		}
		bool flag = num < 4.0;
		double result = (flag ? 4.0 : 6.0);
		P_1?.Report($"    -> Çentik çapı (otomatik): kalınlık {num.ToString("0.##", CultureInfo.InvariantCulture)}mm {(flag ? "<" : "≥")} {4.0.ToString("0.#", CultureInfo.InvariantCulture)}mm → çap {result.ToString("0.#", CultureInfo.InvariantCulture)}mm.");
		return result;
	}

	private static double Method_0600008F(ModelDoc2 P_0)
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
						double result = sheetMetalFeatureData.Thickness * 1000.0;
						Marshal.ReleaseComObject(sheetMetalFeatureData);
						Marshal.ReleaseComObject(feature);
						return result;
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
		return 0.0;
	}

	private Feature Method_06000090(ModelDoc2 P_0, IProgress<string> P_1)
	{
		Feature feature = (Feature)P_0.FirstFeature();
		while (feature != null)
		{
			if (feature.GetTypeName2() == "FlatPattern")
			{
				string name = feature.Name;
				P_1?.Report("    -> FlatPattern bulundu: " + name + ". Unsuppress ediliyor...");
				P_0.ClearSelection2(All: true);
				feature.Select2(Append: false, 0);
				bool value = P_0.EditUnsuppress2();
				P_0.ClearSelection2(All: true);
				P_1?.Report($"    -> EditUnsuppress2 dönüşü: {value}");
				Marshal.ReleaseComObject(feature);
				return (Feature)((PartDoc)P_0).FeatureByName(name);
			}
			Feature feature2 = (Feature)feature.GetNextFeature();
			Marshal.ReleaseComObject(feature);
			feature = feature2;
		}
		return null;
	}

	private List<BendEndpoint> Method_06000091(ModelDoc2 P_0, Feature P_1, IProgress<string> P_2)
	{
		List<BendEndpoint> list = new List<BendEndpoint>();
		Feature feature = Method_06000092(P_1, P_2);
		if (feature == null)
		{
			return list;
		}
		Sketch sketch = (Sketch)feature.GetSpecificFeature2();
		if (sketch == null)
		{
			Marshal.ReleaseComObject(feature);
			return list;
		}
		IMathUtility mathUtility = (IMathUtility)field_0400006F.GetMathUtility();
		object[] array = (object[])sketch.GetSketchSegments();
		if (array != null)
		{
			MathTransform modelToSketchTransform = sketch.ModelToSketchTransform;
			MathTransform mathTransform = (MathTransform)modelToSketchTransform.Inverse();
			int num = 0;
			object[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				SketchSegment sketchSegment = (SketchSegment)array2[i];
				if (sketchSegment.GetType() == 0)
				{
					num++;
					SketchLine sketchLine = (SketchLine)sketchSegment;
					SketchPoint sketchPoint = (SketchPoint)sketchLine.GetStartPoint2();
					SketchPoint sketchPoint2 = (SketchPoint)sketchLine.GetEndPoint2();
					double[] array3 = new double[3] { sketchPoint.X, sketchPoint.Y, sketchPoint.Z };
					double[] array4 = new double[3] { sketchPoint2.X, sketchPoint2.Y, sketchPoint2.Z };
					MathPoint mathPoint = (MathPoint)mathUtility.CreatePoint(array3);
					MathPoint mathPoint2 = (MathPoint)mathUtility.CreatePoint(array4);
					MathPoint mathPoint3 = (MathPoint)mathPoint.MultiplyTransform(mathTransform);
					MathPoint mathPoint4 = (MathPoint)mathPoint2.MultiplyTransform(mathTransform);
					double[] array5 = (double[])mathPoint3.ArrayData;
					double[] array6 = (double[])mathPoint4.ArrayData;
					double[] array7 = Method_060000A1(array6, array5);
					double num2 = Method_060000A4(array7);
					P_2?.Report($"    -> Büküm çizgisi #{num}: ham({Method_0600008B(array3[0])}; {Method_0600008B(array3[1])})→({Method_0600008B(array4[0])}; {Method_0600008B(array4[1])})mm | model {Method_0600008C(array5)} → {Method_0600008C(array6)} | boy={Method_0600008B(num2)}mm");
					if (num2 < 1E-06)
					{
						P_2?.Report($"    -> Büküm çizgisi #{num} sıfır boylu — atlandı.");
					}
					else
					{
						double[] array8 = Method_060000A3(array7, 1.0 / num2);
						list.Add(new BendEndpoint
						{
							field_04000076 = array3,
							field_04000077 = array5,
							field_0400007D = array8
						});
						list.Add(new BendEndpoint
						{
							field_04000076 = array4,
							field_04000077 = array6,
							field_0400007D = Method_060000A3(array8, -1.0)
						});
					}
					Marshal.ReleaseComObject(sketchPoint);
					Marshal.ReleaseComObject(sketchPoint2);
					Marshal.ReleaseComObject(mathPoint);
					Marshal.ReleaseComObject(mathPoint2);
					Marshal.ReleaseComObject(mathPoint3);
					Marshal.ReleaseComObject(mathPoint4);
				}
				Marshal.ReleaseComObject(sketchSegment);
			}
			Marshal.ReleaseComObject(modelToSketchTransform);
			Marshal.ReleaseComObject(mathTransform);
		}
		Marshal.ReleaseComObject(mathUtility);
		Marshal.ReleaseComObject(sketch);
		Marshal.ReleaseComObject(feature);
		for (int j = 0; j < list.Count; j++)
		{
			list[j].field_04000075 = j + 1;
		}
		P_2?.Report($"    -> Toplam {list.Count} büküm ucu adayı çıkarıldı.");
		return list;
	}

	private Feature Method_06000092(Feature P_0, IProgress<string> P_1)
	{
		Feature feature = null;
		Feature feature2 = (Feature)P_0.GetFirstSubFeature();
		while (feature2 != null)
		{
			if (feature2.GetTypeName2() == "ProfileFeature")
			{
				string name = feature2.Name;
				if (feature == null)
				{
					feature = feature2;
				}
				string[] array = field_0400006E;
				foreach (string value in array)
				{
					if (field_0400006D.IndexOf(name, value, CompareOptions.IgnoreCase) >= 0)
					{
						P_1?.Report("    -> Büküm çizimi bulundu (anahtar kelime): " + name);
						if (feature != feature2)
						{
							Marshal.ReleaseComObject(feature);
						}
						return feature2;
					}
				}
			}
			Feature feature3 = (Feature)feature2.GetNextSubFeature();
			if (feature != feature2)
			{
				Marshal.ReleaseComObject(feature2);
			}
			feature2 = feature3;
		}
		if (feature != null)
		{
			P_1?.Report("    -> UYARI: Anahtar kelime eşleşmedi; FlatPattern altındaki ilk eskiz kullanılıyor: " + feature.Name);
		}
		return feature;
	}

	private List<BendEndpoint> Method_06000093(ModelDoc2 P_0, List<BendEndpoint> P_1, NotchResult P_2, IProgress<string> P_3)
	{
		Face2 face = Method_060000A0(P_0);
		if (face == null)
		{
			P_3?.Report("    -> HATA: Düz sac yüzeyi bulunamadı.");
			return new List<BendEndpoint>();
		}
		List<IEdge> list = new List<IEdge>();
		try
		{
			object[] array = (object[])face.GetLoops();
			if (array != null)
			{
				object[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					Loop2 loop = (Loop2)array2[i];
					if (loop.IsOuter())
					{
						object[] array3 = (object[])loop.GetEdges();
						if (array3 != null)
						{
							object[] array4 = array3;
							foreach (object obj in array4)
							{
								if (obj != null)
								{
									list.Add((IEdge)obj);
								}
							}
						}
					}
					Marshal.ReleaseComObject(loop);
				}
			}
			P_3?.Report($"    -> Snap hedefi: seçilen düz yüzeyin DIŞ konturu ({list.Count} kenar).");
			if (list.Count == 0)
			{
				P_3?.Report("    -> HATA: Dış kontur kenarı bulunamadı.");
				return new List<BendEndpoint>();
			}
			double[] array5 = (double[])face.Normal;
			double num = Method_060000A4(array5);
			double[] array6 = ((num > 1E-06) ? Method_060000A3(array5, 1.0 / num) : new double[3] { 0.0, 0.0, 1.0 });
			double[] array7 = Method_06000097(face, P_1[0].field_04000077);
			List<BendEndpoint> list2 = new List<BendEndpoint>();
			foreach (BendEndpoint item in P_1)
			{
				double num2 = Method_060000A6(array6, Method_060000A1(item.field_04000077, array7));
				item.field_04000078 = Method_060000A1(item.field_04000077, Method_060000A3(array6, num2));
				double num3 = Method_06000096(face, item.field_04000078);
				if (num3 > 5E-05)
				{
					P_2.SkippedInternalBends++;
					P_3?.Report($"    -> [N{item.field_04000075}] ATLANDI: büküm ucu malzeme dışında (boşluğa {Method_0600008B(num3)}mm) — mevcut çentik/rölyef olasılığı. {Method_0600008C(item.field_04000078)}");
					continue;
				}
				double num4 = double.MaxValue;
				double[] array8 = null;
				int num5 = -1;
				for (int k = 0; k < list.Count; k++)
				{
					try
					{
						double[] array9 = (double[])list[k].GetClosestPointOn(item.field_04000078[0], item.field_04000078[1], item.field_04000078[2]);
						if (array9 != null)
						{
							double num6 = Method_060000A5(item.field_04000078, new double[3]
							{
								array9[0],
								array9[1],
								array9[2]
							});
							if (num6 < num4)
							{
								num4 = num6;
								array8 = new double[3]
								{
									array9[0],
									array9[1],
									array9[2]
								};
								num5 = k;
							}
						}
					}
					catch
					{
					}
				}
				if (array8 == null)
				{
					P_2.SkippedInternalBends++;
					P_3?.Report($"    -> [N{item.field_04000075}] ATLANDI: hiçbir dış kenara snap edilemedi. {Method_0600008C(item.field_04000078)}");
				}
				else if (num4 > 0.002)
				{
					P_2.SkippedInternalBends++;
					P_3?.Report($"    -> [N{item.field_04000075}] ATLANDI: büküm ucu dış kontura uzak (d={Method_0600008B(num4)}mm > {Method_0600008B(0.002)}mm) — kısmi büküm/tırnak ucu.");
				}
				else if (Method_06000098(list[num5]))
				{
					P_2.SkippedInternalBends++;
					P_3?.Report($"    -> [N{item.field_04000075}] ATLANDI: büküm ucu dairesel kenara (kenar#{num5}) oturuyor — mevcut çentik/rölyef algılandı.");
				}
				else
				{
					item.field_04000079 = array8;
					item.field_0400007A = num5;
					item.field_0400007B = num4;
					item.field_0400007C = Method_06000099(list[num5]);
					list2.Add(item);
				}
			}
			List<BendEndpoint> list3 = new List<BendEndpoint>();
			foreach (BendEndpoint item2 in list2)
			{
				BendEndpoint bendEndpoint = null;
				foreach (BendEndpoint item3 in list3)
				{
					if (Method_060000A5(item2.field_04000079, item3.field_04000079) < 0.0001)
					{
						bendEndpoint = item3;
						break;
					}
				}
				if (bendEndpoint == null)
				{
					list3.Add(item2);
					continue;
				}
				if (bendEndpoint.field_0400007C == null && item2.field_0400007C != null)
				{
					bendEndpoint.field_0400007C = item2.field_0400007C;
				}
				P_3?.Report($"    -> [N{item2.field_04000075}] {Method_0600008C(item2.field_04000079)} noktası [N{bendEndpoint.field_04000075}] ile çakışık — birleştirildi.");
			}
			foreach (BendEndpoint item4 in list3)
			{
				Method_06000094(face, array5, item4);
				P_3?.Report($"    -> [N{item4.field_04000075}] model {Method_0600008C(item4.field_04000077)} → snap kenar#{item4.field_0400007A} d={Method_0600008B(item4.field_0400007B)}mm {Method_0600008C(item4.field_04000079)} → ofset[{item4.field_0400007F}; {Method_0600008B(0.0001)}mm] → merkez {Method_0600008C(item4.field_0400007E)}");
			}
			return list3;
		}
		finally
		{
			foreach (IEdge item5 in list)
			{
				try
				{
					Marshal.ReleaseComObject(item5);
				}
				catch
				{
				}
			}
			Marshal.ReleaseComObject(face);
		}
	}

	private void Method_06000094(Face2 P_0, double[] P_1, BendEndpoint P_2)
	{
		if (P_2.field_0400007C != null && P_1 != null && P_1.Length >= 3)
		{
			double[] array = Method_060000A7(P_1, P_2.field_0400007C);
			double num = Method_060000A4(array);
			if (num > 1E-06)
			{
				array = Method_060000A3(array, 1.0 / num);
				double[] array2 = Method_060000A2(P_2.field_04000079, Method_060000A3(array, 0.0001));
				double[] array3 = Method_060000A2(P_2.field_04000079, Method_060000A3(array, -0.0001));
				bool flag = Method_06000095(P_0, array2);
				bool flag2 = Method_06000095(P_0, array3);
				if (flag && !flag2)
				{
					P_2.field_0400007E = array2;
					P_2.field_0400007F = "kenar-normali(+)";
					return;
				}
				if (flag2 && !flag)
				{
					P_2.field_0400007E = array3;
					P_2.field_0400007F = "kenar-normali(-)";
					return;
				}
				if (flag & flag2)
				{
					double num2 = ((P_2.field_0400007D != null) ? Method_060000A6(array, P_2.field_0400007D) : 1.0);
					P_2.field_0400007E = ((num2 >= 0.0) ? array2 : array3);
					P_2.field_0400007F = "kenar-normali(belirsiz→çizgi yönü)";
					return;
				}
			}
		}
		if (P_2.field_0400007D != null)
		{
			double[] array4 = Method_060000A2(P_2.field_04000079, Method_060000A3(P_2.field_0400007D, 0.0001));
			if (Method_06000095(P_0, array4))
			{
				P_2.field_0400007E = array4;
				P_2.field_0400007F = "çizgi-yönü(fallback)";
				return;
			}
		}
		P_2.field_0400007E = P_2.field_04000079;
		P_2.field_0400007F = "OFSETSİZ(uyarı)";
	}

	private static bool Method_06000095(Face2 P_0, double[] P_1)
	{
		return Method_06000096(P_0, P_1) < 1E-07;
	}

	private static double Method_06000096(Face2 P_0, double[] P_1)
	{
		try
		{
			double[] array = (double[])P_0.GetClosestPointOn(P_1[0], P_1[1], P_1[2]);
			if (array == null || array.Length < 3)
			{
				return double.MaxValue;
			}
			double num = array[0] - P_1[0];
			double num2 = array[1] - P_1[1];
			double num3 = array[2] - P_1[2];
			return Math.Sqrt(num * num + num2 * num2 + num3 * num3);
		}
		catch
		{
			return double.MaxValue;
		}
	}

	private static double[] Method_06000097(Face2 P_0, double[] P_1)
	{
		try
		{
			double[] array = (double[])P_0.GetClosestPointOn(P_1[0], P_1[1], P_1[2]);
			if (array != null && array.Length >= 3)
			{
				return new double[3]
				{
					array[0],
					array[1],
					array[2]
				};
			}
		}
		catch
		{
		}
		return new double[3]
		{
			P_1[0],
			P_1[1],
			P_1[2]
		};
	}

	private static bool Method_06000098(IEdge P_0)
	{
		Curve curve = null;
		try
		{
			curve = (Curve)P_0.GetCurve();
			return curve != null && curve.Identity() == 3002;
		}
		catch
		{
			return false;
		}
		finally
		{
			if (curve != null)
			{
				try
				{
					Marshal.ReleaseComObject(curve);
				}
				catch
				{
				}
			}
		}
	}

	private static double[] Method_06000099(IEdge P_0)
	{
		Vertex vertex = null;
		Vertex vertex2 = null;
		try
		{
			vertex = (Vertex)P_0.GetStartVertex();
			vertex2 = (Vertex)P_0.GetEndVertex();
			if (vertex == null || vertex2 == null)
			{
				return null;
			}
			double[] array = (double[])vertex.GetPoint();
			double[] array2 = Method_060000A1((double[])vertex2.GetPoint(), array);
			double num = Method_060000A4(array2);
			if (num < 1E-06)
			{
				return null;
			}
			return Method_060000A3(array2, 1.0 / num);
		}
		catch
		{
			return null;
		}
		finally
		{
			if (vertex != null)
			{
				try
				{
					Marshal.ReleaseComObject(vertex);
				}
				catch
				{
				}
			}
			if (vertex2 != null)
			{
				try
				{
					Marshal.ReleaseComObject(vertex2);
				}
				catch
				{
				}
			}
		}
	}

	private void Method_0600009A(ModelDoc2 P_0, List<BendEndpoint> P_1, double P_2, NotchResult P_3, IProgress<string> P_4)
	{
		Face2 face = Method_0600009F(P_0, P_1[0].field_0400007E) ?? Method_060000A0(P_0);
		if (face == null)
		{
			P_4?.Report("    -> TOPLU: düz yüzey bulunamadı → tümü nokta-başına.");
			{
				foreach (BendEndpoint item in P_1)
				{
					if (Method_0600009B(P_0, item, P_2, P_4))
					{
						P_3.SuccessfulCuts++;
					}
					else
					{
						P_3.FailedCuts++;
					}
				}
				return;
			}
		}
		Entity entity = (Entity)face;
		P_0.ClearSelection2(All: true);
		entity.Select4(Append: false, null);
		SketchManager sketchManager = P_0.SketchManager;
		sketchManager.InsertSketch(UpdateEditRebuild: true);
		P_0.ClearSelection2(All: true);
		Sketch activeSketch = sketchManager.ActiveSketch;
		if (activeSketch == null)
		{
			P_4?.Report("    -> TOPLU: eskiz açılamadı → tümü nokta-başına.");
			Marshal.ReleaseComObject(entity);
			Marshal.ReleaseComObject(face);
			{
				foreach (BendEndpoint item2 in P_1)
				{
					if (Method_0600009B(P_0, item2, P_2, P_4))
					{
						P_3.SuccessfulCuts++;
					}
					else
					{
						P_3.FailedCuts++;
					}
				}
				return;
			}
		}
		IMathUtility mathUtility = (IMathUtility)field_0400006F.GetMathUtility();
		MathTransform modelToSketchTransform = activeSketch.ModelToSketchTransform;
		List<BendEndpoint> list = new List<BendEndpoint>();
		List<BendEndpoint> list2 = new List<BendEndpoint>();
		bool addToDB = sketchManager.AddToDB;
		sketchManager.AddToDB = true;
		try
		{
			foreach (BendEndpoint item3 in P_1)
			{
				MathPoint mathPoint = (MathPoint)mathUtility.CreatePoint(item3.field_0400007E);
				MathPoint mathPoint2 = (MathPoint)mathPoint.MultiplyTransform(modelToSketchTransform);
				double[] array = (double[])mathPoint2.ArrayData;
				Marshal.ReleaseComObject(mathPoint);
				Marshal.ReleaseComObject(mathPoint2);
				if (Math.Abs(array[2]) > 0.0001)
				{
					list2.Add(item3);
					continue;
				}
				SketchSegment sketchSegment = sketchManager.CreateCircleByRadius(array[0], array[1], 0.0, P_2);
				if (sketchSegment == null)
				{
					list2.Add(item3);
					continue;
				}
				Marshal.ReleaseComObject(sketchSegment);
				list.Add(item3);
				P_4?.Report($"    -> [N{item3.field_04000075}] TOPLU eskize eklendi: ({Method_0600008B(array[0])}; {Method_0600008B(array[1])}; z={Method_0600008B(array[2])})mm r={Method_0600008B(P_2)}mm");
			}
		}
		finally
		{
			sketchManager.AddToDB = addToDB;
		}
		Marshal.ReleaseComObject(modelToSketchTransform);
		Marshal.ReleaseComObject(mathUtility);
		sketchManager.InsertSketch(UpdateEditRebuild: true);
		Feature feature = Method_0600009D(P_0, activeSketch);
		Marshal.ReleaseComObject(entity);
		Marshal.ReleaseComObject(face);
		if (list.Count == 0 || feature == null)
		{
			if (feature != null)
			{
				P_0.ClearSelection2(All: true);
				feature.Select2(Append: false, 0);
				P_0.EditDelete();
				Marshal.ReleaseComObject(feature);
			}
			P_4?.Report($"    -> TOPLU: eskize alınacak çentik yok ({P_1.Count} nokta farklı yüzeyde/başarısız) → nokta-başına.");
			{
				foreach (BendEndpoint item4 in P_1)
				{
					if (Method_0600009B(P_0, item4, P_2, P_4))
					{
						P_3.SuccessfulCuts++;
					}
					else
					{
						P_3.FailedCuts++;
					}
				}
				return;
			}
		}
		int num = 1;
		FeatureManager featureManager = P_0.FeatureManager;
		Feature feature2 = null;
		string value = null;
		(string, bool, bool, bool)[] array2 = new (string, bool, bool, bool)[4]
		{
			("tek-yön", true, false, false),
			("ters-yön", true, true, false),
			("tek-yön/NormalCut=ON", true, false, true),
			("çift-yön", false, false, false)
		};
		for (int i = 0; i < array2.Length; i++)
		{
			(string, bool, bool, bool) tuple = array2[i];
			P_0.ClearSelection2(All: true);
			feature.Select2(Append: false, 0);
			feature2 = Method_0600009C(featureManager, tuple.Item2, tuple.Item3, num, tuple.Item4);
			if (feature2 != null)
			{
				(value, _, _, _) = tuple;
				break;
			}
			P_4?.Report($"    -> TOPLU deneme '{tuple.Item1}' başarısız ({list.Count} daire tek eskizde).");
		}
		Marshal.ReleaseComObject(featureManager);
		if (feature2 != null)
		{
			P_4?.Report($"    -> TOPLU KESİM BAŞARILI ({value}): {list.Count} çentik tek kesimde.");
			Marshal.ReleaseComObject(feature2);
			Marshal.ReleaseComObject(feature);
			P_3.SuccessfulCuts += list.Count;
		}
		else
		{
			P_4?.Report($"    -> TOPLU KESİM BAŞARISIZ ({list.Count} daire) → nokta-başına yola düşülüyor (hiçbir çentik kaybolmaz).");
			P_0.ClearSelection2(All: true);
			feature.Select2(Append: false, 0);
			P_0.EditDelete();
			Marshal.ReleaseComObject(feature);
			foreach (BendEndpoint item5 in list)
			{
				if (Method_0600009B(P_0, item5, P_2, P_4))
				{
					P_3.SuccessfulCuts++;
				}
				else
				{
					P_3.FailedCuts++;
				}
			}
		}
		foreach (BendEndpoint item6 in list2)
		{
			P_4?.Report($"    -> [N{item6.field_04000075}] farklı yüzeyde → nokta-başına.");
			if (Method_0600009B(P_0, item6, P_2, P_4))
			{
				P_3.SuccessfulCuts++;
			}
			else
			{
				P_3.FailedCuts++;
			}
		}
	}

	private bool Method_0600009B(ModelDoc2 P_0, BendEndpoint P_1, double P_2, IProgress<string> P_3)
	{
		Face2 face = Method_0600009F(P_0, P_1.field_0400007E);
		if (face == null)
		{
			P_3?.Report($"    -> [N{P_1.field_04000075}] UYARI: noktayı içeren düz yüzey bulunamadı; en büyük düz yüzeye dönülüyor.");
			face = Method_060000A0(P_0);
		}
		if (face == null)
		{
			P_3?.Report($"    -> [N{P_1.field_04000075}] HATA: eskiz için düz yüzey bulunamadı.");
			return false;
		}
		Entity entity = (Entity)face;
		P_0.ClearSelection2(All: true);
		entity.Select4(Append: false, null);
		SketchManager sketchManager = P_0.SketchManager;
		sketchManager.InsertSketch(UpdateEditRebuild: true);
		P_0.ClearSelection2(All: true);
		Sketch activeSketch = sketchManager.ActiveSketch;
		if (activeSketch == null)
		{
			P_3?.Report($"    -> [N{P_1.field_04000075}] HATA: eskiz açılamadı (ActiveSketch null).");
			Marshal.ReleaseComObject(entity);
			Marshal.ReleaseComObject(face);
			return false;
		}
		IMathUtility mathUtility = (IMathUtility)field_0400006F.GetMathUtility();
		MathTransform modelToSketchTransform = activeSketch.ModelToSketchTransform;
		MathPoint mathPoint = (MathPoint)mathUtility.CreatePoint(P_1.field_0400007E);
		MathPoint mathPoint2 = (MathPoint)mathPoint.MultiplyTransform(modelToSketchTransform);
		double[] array = (double[])mathPoint2.ArrayData;
		Marshal.ReleaseComObject(mathPoint);
		Marshal.ReleaseComObject(mathPoint2);
		Marshal.ReleaseComObject(modelToSketchTransform);
		Marshal.ReleaseComObject(mathUtility);
		P_3?.Report($"    -> [N{P_1.field_04000075}] eskiz uzayı: ({Method_0600008B(array[0])}; {Method_0600008B(array[1])}; z={Method_0600008B(array[2])})mm | r={Method_0600008B(P_2)}mm");
		if (Math.Abs(array[2]) > 0.0001)
		{
			P_3?.Report($"    -> [N{P_1.field_04000075}] HATA: nokta eskiz düzleminde değil (z={Method_0600008B(array[2])}mm) — yüzey/düzlem uyuşmazlığı, kesim iptal.");
			sketchManager.InsertSketch(UpdateEditRebuild: true);
			Method_0600009E(P_0, activeSketch, P_3, P_1.field_04000075);
			Marshal.ReleaseComObject(entity);
			Marshal.ReleaseComObject(face);
			return false;
		}
		bool addToDB = sketchManager.AddToDB;
		sketchManager.AddToDB = true;
		SketchSegment sketchSegment = null;
		try
		{
			sketchSegment = sketchManager.CreateCircleByRadius(array[0], array[1], 0.0, P_2);
		}
		finally
		{
			sketchManager.AddToDB = addToDB;
		}
		if (sketchSegment == null)
		{
			P_3?.Report($"    -> [N{P_1.field_04000075}] HATA: CreateCircleByRadius null döndü (merkez {Method_0600008B(array[0])};{Method_0600008B(array[1])} r={Method_0600008B(P_2)}mm).");
			sketchManager.InsertSketch(UpdateEditRebuild: true);
			Method_0600009E(P_0, activeSketch, P_3, P_1.field_04000075);
			Marshal.ReleaseComObject(entity);
			Marshal.ReleaseComObject(face);
			return false;
		}
		Marshal.ReleaseComObject(sketchSegment);
		sketchManager.InsertSketch(UpdateEditRebuild: true);
		Feature feature = Method_0600009D(P_0, activeSketch);
		if (feature == null)
		{
			P_3?.Report($"    -> [N{P_1.field_04000075}] HATA: eskiz unsuru ağaçta bulunamadı.");
			Marshal.ReleaseComObject(entity);
			Marshal.ReleaseComObject(face);
			return false;
		}
		int num = 1;
		FeatureManager featureManager = P_0.FeatureManager;
		Feature feature2 = null;
		string value = null;
		(string, bool, bool, bool)[] array2 = new (string, bool, bool, bool)[4]
		{
			("tek-yön", true, false, false),
			("ters-yön", true, true, false),
			("tek-yön/NormalCut=ON", true, false, true),
			("çift-yön", false, false, false)
		};
		for (int i = 0; i < array2.Length; i++)
		{
			(string, bool, bool, bool) tuple = array2[i];
			P_0.ClearSelection2(All: true);
			feature.Select2(Append: false, 0);
			feature2 = Method_0600009C(featureManager, tuple.Item2, tuple.Item3, num, tuple.Item4);
			if (feature2 != null)
			{
				(value, _, _, _) = tuple;
				break;
			}
			P_3?.Report($"    -> [N{P_1.field_04000075}] deneme '{tuple.Item1}' başarısız (Sd={tuple.Item2}, Dir={tuple.Item3}, T1=T2={num}(ThroughAll), NormalCut={tuple.Item4}).");
		}
		Marshal.ReleaseComObject(featureManager);
		Marshal.ReleaseComObject(entity);
		Marshal.ReleaseComObject(face);
		if (feature2 != null)
		{
			P_3?.Report($"    -> [N{P_1.field_04000075}] KESİM BAŞARILI ({value}). Merkez {Method_0600008C(P_1.field_0400007E)}, r={Method_0600008B(P_2)}mm.");
			Marshal.ReleaseComObject(feature2);
			Marshal.ReleaseComObject(feature);
			return true;
		}
		P_3?.Report($"    -> [N{P_1.field_04000075}] KESİM BAŞARISIZ (3 deneme tükendi). Eskiz siliniyor. Merkez {Method_0600008C(P_1.field_0400007E)}, snap d={Method_0600008B(P_1.field_0400007B)}mm, ofset={P_1.field_0400007F}.");
		P_0.ClearSelection2(All: true);
		feature.Select2(Append: false, 0);
		P_0.EditDelete();
		Marshal.ReleaseComObject(feature);
		return false;
	}

	private Feature Method_0600009C(FeatureManager P_0, bool P_1, bool P_2, int P_3, bool P_4)
	{
		return P_0.FeatureCut4(P_1, Flip: false, P_2, P_3, P_3, 0.01, 0.01, Dchk1: false, Dchk2: false, Ddir1: false, Ddir2: false, 0.0, 0.0, OffsetReverse1: false, OffsetReverse2: false, TranslateSurface1: false, TranslateSurface2: false, P_4, UseFeatScope: true, UseAutoSelect: true, AssemblyFeatureScope: true, AutoSelectComponents: true, PropagateFeatureToParts: false, 0, 0.0, FlipStartOffset: false, OptimizeGeometry: false);
	}

	private Feature Method_0600009D(ModelDoc2 P_0, Sketch P_1)
	{
		try
		{
			Feature feature = (Feature)P_1;
			if (feature != null)
			{
				return feature;
			}
		}
		catch
		{
		}
		try
		{
			Feature feature2 = (Feature)P_0.FeatureByPositionReverse(0);
			if (feature2 != null && feature2.GetTypeName2() == "ProfileFeature")
			{
				return feature2;
			}
			if (feature2 != null)
			{
				Marshal.ReleaseComObject(feature2);
			}
		}
		catch
		{
		}
		return null;
	}

	private void Method_0600009E(ModelDoc2 P_0, Sketch P_1, IProgress<string> P_2, int P_3)
	{
		Feature feature = Method_0600009D(P_0, P_1);
		if (feature != null)
		{
			P_0.ClearSelection2(All: true);
			feature.Select2(Append: false, 0);
			P_0.EditDelete();
			Marshal.ReleaseComObject(feature);
			P_2?.Report($"    -> [N{P_3}] Yarım kalan eskiz temizlendi.");
		}
	}

	private Face2 Method_0600009F(ModelDoc2 P_0, double[] P_1)
	{
		if (!(P_0 is PartDoc partDoc))
		{
			return null;
		}
		object[] array = (object[])partDoc.GetBodies2(0, BVisibleOnly: true);
		if (array == null || array.Length == 0)
		{
			return null;
		}
		Face2 face = null;
		double num = -1.0;
		object[] array2 = array;
		foreach (object obj in array2)
		{
			if (obj == null)
			{
				continue;
			}
			Body2 body = (Body2)obj;
			object[] array3 = (object[])body.GetFaces();
			if (array3 != null)
			{
				object[] array4 = array3;
				for (int j = 0; j < array4.Length; j++)
				{
					Face2 face2 = (Face2)array4[j];
					Surface surface = (Surface)face2.GetSurface();
					bool flag = false;
					try
					{
						if (!surface.IsPlane() || !Method_06000095(face2, P_1))
						{
							continue;
						}
						double area = face2.GetArea();
						if (area > num)
						{
							num = area;
							if (face != null)
							{
								Marshal.ReleaseComObject(face);
							}
							face = face2;
							flag = true;
						}
					}
					finally
					{
						Marshal.ReleaseComObject(surface);
						if (!flag)
						{
							Marshal.ReleaseComObject(face2);
						}
					}
				}
			}
			Marshal.ReleaseComObject(body);
		}
		return face;
	}

	private Face2 Method_060000A0(ModelDoc2 P_0)
	{
		if (!(P_0 is PartDoc partDoc))
		{
			return null;
		}
		object[] array = (object[])partDoc.GetBodies2(0, BVisibleOnly: true);
		if (array == null || array.Length == 0)
		{
			return null;
		}
		Face2 face = null;
		double num = -1.0;
		object[] array2 = array;
		foreach (object obj in array2)
		{
			if (obj == null)
			{
				continue;
			}
			Body2 body = (Body2)obj;
			object[] array3 = (object[])body.GetFaces();
			if (array3 != null)
			{
				object[] array4 = array3;
				for (int j = 0; j < array4.Length; j++)
				{
					Face2 face2 = (Face2)array4[j];
					Surface surface = (Surface)face2.GetSurface();
					try
					{
						if (surface.IsPlane())
						{
							double area = face2.GetArea();
							if (area > num)
							{
								num = area;
								if (face != null)
								{
									Marshal.ReleaseComObject(face);
								}
								face = face2;
							}
							else
							{
								Marshal.ReleaseComObject(face2);
							}
						}
						else
						{
							Marshal.ReleaseComObject(face2);
						}
					}
					finally
					{
						Marshal.ReleaseComObject(surface);
					}
				}
			}
			Marshal.ReleaseComObject(body);
		}
		return face;
	}

	private static double[] Method_060000A1(double[] P_0, double[] P_1)
	{
		return new double[3]
		{
			P_0[0] - P_1[0],
			P_0[1] - P_1[1],
			P_0[2] - P_1[2]
		};
	}

	private static double[] Method_060000A2(double[] P_0, double[] P_1)
	{
		return new double[3]
		{
			P_0[0] + P_1[0],
			P_0[1] + P_1[1],
			P_0[2] + P_1[2]
		};
	}

	private static double[] Method_060000A3(double[] P_0, double P_1)
	{
		return new double[3]
		{
			P_0[0] * P_1,
			P_0[1] * P_1,
			P_0[2] * P_1
		};
	}

	private static double Method_060000A4(double[] P_0)
	{
		return Math.Sqrt(P_0[0] * P_0[0] + P_0[1] * P_0[1] + P_0[2] * P_0[2]);
	}

	private static double Method_060000A5(double[] P_0, double[] P_1)
	{
		return Method_060000A4(Method_060000A1(P_0, P_1));
	}

	private static double Method_060000A6(double[] P_0, double[] P_1)
	{
		return P_0[0] * P_1[0] + P_0[1] * P_1[1] + P_0[2] * P_1[2];
	}

	private static double[] Method_060000A7(double[] P_0, double[] P_1)
	{
		return new double[3]
		{
			P_0[1] * P_1[2] - P_0[2] * P_1[1],
			P_0[2] * P_1[0] - P_0[0] * P_1[2],
			P_0[0] * P_1[1] - P_0[1] * P_1[0]
		};
	}
}
