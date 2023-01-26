using System.Runtime.InteropServices;

namespace Sandaab.WindowsApp.Win32
{
    using HMONITOR = IntPtr;
    using HRESULT = IntPtr;
    using UINT = UInt32;

    internal class SHCore
    {
        private const string dllName = "SHCore.dll";

        public enum MONITOR_DPI_TYPE
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2,
            MDT_DEFAULT
        }

        [DllImport(dllName)]
        public static extern HRESULT GetDpiForMonitor(HMONITOR hmonitor,
                                  MONITOR_DPI_TYPE dpiType,
                                  out UINT dpiX,
                                  out UINT dpiY);
    }
}
