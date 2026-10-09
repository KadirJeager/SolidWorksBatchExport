using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidWorksBatchExport.Core
{
    public static class ComHelper
    {
        [DllImport("ole32.dll")]
        private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        public static object GetActiveObject(string progId)
        {
            CLSIDFromProgID(progId, out Guid clsid);
            GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
            return obj;
        }
    }

    public class SolidWorksEngine : IDisposable
    {
        private ISldWorks _swApp;
        public ISldWorks App => _swApp;
        public bool IsConnected { get; private set; }

        public SolidWorksEngine()
        {
            Connect();
        }

        private void Connect()
        {
            try
            {
                // Try to get an active instance of SolidWorks using P/Invoke (since Marshal.GetActiveObject is unsupported in .NET 8)
                _swApp = (ISldWorks)ComHelper.GetActiveObject("SldWorks.Application");
                IsConnected = true;
            }
            catch (COMException)
            {
                // SolidWorks is not running, so we start it in the background
                try
                {
                    Type swType = Type.GetTypeFromProgID("SldWorks.Application");
                    if (swType != null)
                    {
                        _swApp = (ISldWorks)Activator.CreateInstance(swType);
                        if (_swApp != null)
                        {
                            _swApp.Visible = true; // Set to false if you want it to run completely hidden
                            IsConnected = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("SolidWorks başlatılamadı. Bilgisayarda kurulu olduğundan emin olun.", ex);
                }
            }
            
            if (_swApp != null)
            {
                _swApp.UserControl = true;
            }
        }

        public void Dispose()
        {
            if (_swApp != null)
            {
                // Release COM object
                Marshal.ReleaseComObject(_swApp);
                _swApp = null;
                IsConnected = false;
            }
        }
    }
}
