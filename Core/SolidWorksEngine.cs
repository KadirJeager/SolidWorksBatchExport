using System;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

namespace SolidWorksBatchExport.Core;

public class SolidWorksEngine : IDisposable
{
	private ISldWorks _app;

	private bool field_0400008F;

	public ISldWorks App => _app;

	public SolidWorksEngine()
	{
		Method_060000C2();
	}

	private void Method_060000C2()
	{
		try
		{
			Type typeFromProgID = Type.GetTypeFromProgID("SldWorks.Application");
			if (typeFromProgID == null)
			{
				throw new Exception("SolidWorks yüklü değil veya COM nesnesi bulunamadı.");
			}
			_app = (ISldWorks)Activator.CreateInstance(typeFromProgID);
			if (_app == null)
			{
				throw new Exception("SolidWorks başlatılamadı.");
			}
			_app.Visible = true;
			_app.UserControl = true;
		}
		catch (Exception ex)
		{
			throw new Exception("SolidWorks bağlantı hatası: " + ex.Message);
		}
	}

	public void Dispose()
	{
		if (!field_0400008F)
		{
			if (_app != null)
			{
				Marshal.ReleaseComObject(_app);
				_app = null;
			}
			field_0400008F = true;
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}
}
