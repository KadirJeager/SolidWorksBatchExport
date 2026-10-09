using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidWorksBatchExport.Core
{
    public class NotchPoint
    {
        public double[] Point;
        public double[] OutwardDir;
    }

    public class NotchManager
    {
        private ISldWorks _swApp;

        public NotchManager(ISldWorks swApp)
        {
            _swApp = swApp;
        }

        private Face2 GetLargestFlatFace(PartDoc swPart)
        {
            object[] bodies = (object[])swPart.GetBodies2((int)swBodyType_e.swSolidBody, false);
            if (bodies == null || bodies.Length == 0) return null;

            Face2 bestFace = null;
            double maxArea = 0;

            foreach (object bodyObj in bodies)
            {
                Body2 body = (Body2)bodyObj;
                object[] faces = (object[])body.GetFaces();
                if (faces == null) continue;

                foreach (object faceObj in faces)
                {
                    Face2 face = (Face2)faceObj;
                    Surface surf = (Surface)face.GetSurface();
                    if (surf.IsPlane())
                    {
                        double area = face.GetArea();
                        if (area > maxArea)
                        {
                            maxArea = area;
                            bestFace = face;
                        }
                    }
                }
            }
            return bestFace;
        }

        public bool CreateNotches(ModelDoc2 swModel, double radius, IProgress<string> progress)
        {
            try
            {
                progress.Report("    -> Çentik işlemi başlatılıyor...");
                double radiusInMeters = radius / 1000.0;
                PartDoc swPart = (PartDoc)swModel;
                FeatureManager swFeatMgr = swModel.FeatureManager;

                Feature swFeat = (Feature)swModel.FirstFeature();
                while (swFeat != null)
                {
                    if (swFeat.GetTypeName2() == "FlatPattern")
                    {
                        progress.Report("    -> FlatPattern (Açınım) unsuru bulundu. Unsuppress ediliyor...");
                        swFeat.SetSuppression2(
                            (int)swFeatureSuppressionAction_e.swUnSuppressFeature, 
                            (int)swInConfigurationOpts_e.swThisConfiguration, 
                            null);

                        swModel.EditRebuild3();

                        IMathUtility mathUtil = (IMathUtility)_swApp.GetMathUtility();
                        List<NotchPoint> rawModelPoints = new List<NotchPoint>();

                        Feature subFeat = (Feature)swFeat.GetFirstSubFeature();
                        while (subFeat != null)
                        {
                            if (subFeat.GetTypeName2() == "ProfileFeature") // Büküm Çizgileri
                            {
                                Sketch swSketch = (Sketch)subFeat.GetSpecificFeature2();
                                string sketchName = subFeat.Name;
                                if (sketchName.Contains("Büküm-Çizgileri") || sketchName.Contains("Bend-Lines") || sketchName.Contains("Büküm Çizgileri") || sketchName.Contains("Bend Lines") || sketchName.Contains("Çizgiler"))
                                {
                                    if (progress != null) progress.Report($"    -> Büküm çizimi bulundu: {sketchName}. Noktalar hesaplanıyor...");

                                    MathTransform bendToModel = swSketch.ModelToSketchTransform.IInverse();
                                    object[] sketchSegments = (object[])swSketch.GetSketchSegments();

                                    if (sketchSegments != null)
                                    {
                                        foreach (object skSegObj in sketchSegments)
                                        {
                                            SketchSegment skSeg = (SketchSegment)skSegObj;
                                            if (skSeg.GetType() == (int)swSketchSegments_e.swSketchLINE)
                                            {
                                                SketchLine swLine = (SketchLine)skSeg;
                                                SketchPoint ptStart = (SketchPoint)swLine.GetStartPoint2();
                                                SketchPoint ptEnd = (SketchPoint)swLine.GetEndPoint2();

                                                IMathPoint mPtStart = (IMathPoint)mathUtil.CreatePoint(new double[] { ptStart.X, ptStart.Y, ptStart.Z });
                                                mPtStart = (IMathPoint)mPtStart.MultiplyTransform(bendToModel);
                                                double[] ptStartData = (double[])mPtStart.ArrayData;

                                                IMathPoint mPtEnd = (IMathPoint)mathUtil.CreatePoint(new double[] { ptEnd.X, ptEnd.Y, ptEnd.Z });
                                                mPtEnd = (IMathPoint)mPtEnd.MultiplyTransform(bendToModel);
                                                double[] ptEndData = (double[])mPtEnd.ArrayData;

                                                double dx = ptEndData[0] - ptStartData[0];
                                                double dy = ptEndData[1] - ptStartData[1];
                                                double dz = ptEndData[2] - ptStartData[2];
                                                double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                                                if (len > 0.0001)
                                                {
                                                    if (progress != null) 
                                                    {
                                                        progress.Report($"      => Çizgi İşleniyor (mm): Start({ptStartData[0]*1000:F2}, {ptStartData[1]*1000:F2}, {ptStartData[2]*1000:F2}) -> End({ptEndData[0]*1000:F2}, {ptEndData[1]*1000:F2}, {ptEndData[2]*1000:F2})");
                                                    }
                                                    double[] dirToEnd = new double[] { dx / len, dy / len, dz / len };
                                                    double[] dirToStart = new double[] { -dx / len, -dy / len, -dz / len };

                                                    rawModelPoints.Add(new NotchPoint { Point = ptStartData, OutwardDir = dirToStart });
                                                    rawModelPoints.Add(new NotchPoint { Point = ptEndData, OutwardDir = dirToEnd });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            subFeat = (Feature)subFeat.GetNextSubFeature();
                        }

                        if (rawModelPoints.Count > 0)
                        {
                            List<NotchPoint> uniqueModelPoints = new List<NotchPoint>();
                            foreach (NotchPoint np in rawModelPoints)
                            {
                                bool isDuplicate = false;
                                foreach (NotchPoint uPt in uniqueModelPoints)
                                {
                                    double dist = Math.Sqrt(Math.Pow(np.Point[0] - uPt.Point[0], 2) + Math.Pow(np.Point[1] - uPt.Point[1], 2) + Math.Pow(np.Point[2] - uPt.Point[2], 2));
                                    if (dist < 0.0001)
                                    {
                                        isDuplicate = true;
                                        break;
                                    }
                                }
                                if (!isDuplicate)
                                {
                                    uniqueModelPoints.Add(np);
                                }
                            }

                            if (progress != null) progress.Report($"    -> {rawModelPoints.Count} noktadan {uniqueModelPoints.Count} adet benzersiz çentik noktası filtrelendi. Çentikler büküm uçlarına ortalı (merkezli) olarak açılıyor...");

                            int successCuts = 0;
                            int failCuts = 0;

                            Face2 bestFace = GetLargestFlatFace(swPart);
                            if (bestFace == null)
                            {
                                progress.Report("    -> HATA: Düz yüzey bulunamadı.");
                                return false;
                            }
                            
                            Body2 swBody = (Body2)bestFace.GetBody();
                            Surface surf = (Surface)bestFace.GetSurface();
                            object[] edges = (object[])bestFace.GetEdges();

                            foreach (NotchPoint np in uniqueModelPoints)
                            {
                                double minHitDist = double.MaxValue;
                                double[] bestHitPt = null;

                                if (edges != null)
                                {
                                    foreach (object edgeObj in edges)
                                    {
                                        Edge swEdge = (Edge)edgeObj;
                                        object result = swEdge.GetClosestPointOn(np.Point[0], np.Point[1], np.Point[2]);
                                        if (result != null)
                                        {
                                            double[] pointData = (double[])result;
                                            double hx = pointData[0];
                                            double hy = pointData[1];
                                            double hz = pointData[2];
                                            
                                            double d = Math.Sqrt(Math.Pow(hx - np.Point[0], 2) + Math.Pow(hy - np.Point[1], 2) + Math.Pow(hz - np.Point[2], 2));
                                            if (d < minHitDist)
                                            {
                                                minHitDist = d;
                                                bestHitPt = new double[] { hx, hy, hz };
                                            }
                                        }
                                    }
                                }

                                if (bestHitPt != null)
                                {
                                    // Micro-Offset: ZTG (Sıfır Kalınlık) hatasını önlemek için 0.01mm içeri çek (OutwardDir'in tersi)
                                    double microOffset = 0.00001;
                                    np.Point[0] = bestHitPt[0] - np.OutwardDir[0] * microOffset;
                                    np.Point[1] = bestHitPt[1] - np.OutwardDir[1] * microOffset;
                                    np.Point[2] = bestHitPt[2] - np.OutwardDir[2] * microOffset;
                                    
                                    if (progress != null) progress.Report($"      => Geometrik Kenar Bulundu (Mesafe: {minHitDist*1000:F2}mm): Yeni merkez: X={np.Point[0]*1000:F2}, Y={np.Point[1]*1000:F2}, Z={np.Point[2]*1000:F2}");
                                }
                                else
                                {
                                    if (progress != null) progress.Report($"      => UYARI: Kenar bulunamadı, orijinal nokta kullanılıyor.");
                                }

                                Entity ent = (Entity)bestFace;
                                ent.Select4(false, null);
                                swModel.SketchManager.InsertSketch(true);

                                Sketch newSketch = swModel.SketchManager.ActiveSketch;
                                MathTransform modelToNewSketch = newSketch.ModelToSketchTransform;
                                swModel.ClearSelection2(true);

                                IMathPoint mPt = (IMathPoint)mathUtil.CreatePoint(np.Point);
                                mPt = (IMathPoint)mPt.MultiplyTransform(modelToNewSketch);
                                double[] localPt = (double[])mPt.ArrayData;
                                
                                swModel.SketchManager.CreateCircleByRadius(localPt[0], localPt[1], 0, radiusInMeters);
                                if (progress != null)
                                {
                                    progress.Report($"      => Çentik açılıyor (mm): Merkez X={np.Point[0]*1000:F2}, Y={np.Point[1]*1000:F2}, Z={np.Point[2]*1000:F2} | Yarıçap={radiusInMeters*1000:F2}");
                                }
                                swModel.SketchManager.InsertSketch(true);

                                Feature sketchFeat = (Feature)swModel.FeatureByPositionReverse(0);
                                string sketchFeatName = sketchFeat.Name;

                                Feature cutFeat = swFeatMgr.FeatureCut3(
                                    false, false, true, // Dir = true (BothDirections)
                                    (int)swEndConditions_e.swEndCondThroughAll, 
                                    (int)swEndConditions_e.swEndCondThroughAll, 
                                    0.01, 0.01, false, false, false, false, 0, 0, false, false, false, false, false, true, true, true, true, false, 0, 0, false);

                                if (cutFeat != null)
                                {
                                    successCuts++;
                                }
                                else
                                {
                                    swModel.Extension.SelectByID2(sketchFeatName, "SKETCH", 0, 0, 0, false, 0, null, 0);
                                    swModel.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
                                    failCuts++;
                                }
                            }

                            progress.Report($"    -> SONUÇ: {uniqueModelPoints.Count} noktadan {successCuts} adedine kesim yapıldı, {failCuts} adedi atlandı.");
                            return true;
                        }
                    }
                    swFeat = (Feature)swFeat.GetNextFeature();
                }

                progress.Report("    -> HATA: Modelde FlatPattern veya Büküm Çizgileri bulunamadı.");
                return false;
            }
            catch (Exception ex)
            {
                if (progress != null) progress.Report("    -> Çentik açılırken KRİTİK HATA oluştu: " + ex.Message);
                return false;
            }
        }
    }
}
