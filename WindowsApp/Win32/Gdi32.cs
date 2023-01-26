using System.Runtime.InteropServices;

namespace Sandaab.WindowsApp.Win32
{
    using HDC = IntPtr;

    internal class Gdi32
    {
        private const string dllName = "gdi32.dll";

        public const int HORZRES = 8;
        public const int VERTRES = 10;
        public const int ASPECTX = 40;
        public const int ASPECTY = 42;
        public const int LOGPIXELSX = 88;
        public const int LOGPIXELSY = 90;
        public const int DESKTOPVERTRES = 117;
        public const int DESKTOPHORZRES = 118;

        [DllImport(dllName, CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int GetDeviceCaps(HDC hDC, int nIndex);
    }
}
