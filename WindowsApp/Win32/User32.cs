using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Sandaab.WindowsApp.Win32
{
    using BOOL = Boolean;
    using HWND = IntPtr;
    using LRESULT = UInt32;
    using UINT = UInt32;
    using WPARAM = UInt32;
    using LPARAM = UInt32;
    using HMONITOR = IntPtr;
    using DWORD = UInt32;

    internal class User32
    {
        private const string dllName = "User32.dll";

        public const UINT GW_HWNDFIRST = 0;
        public const UINT GW_HWNDLAST = 1;
        public const UINT GW_HWNDNEXT = 2;
        public const UINT GW_HWNDPREV = 3;
        public const UINT GW_OWNER = 4;
        public const UINT GW_CHILD = 5;
        public const UINT GW_ENABLEDPOPUP = 6;

        public const int MONITOR_DEFAULTTONULL = 0x00000000;
        public const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;
        public const int MONITOR_DEFAULTTONEAREST = 0x00000002;
 
        [DllImport(dllName)]
        public static extern LRESULT SendMessage(HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam);

        [DllImport(dllName, SetLastError = true)]
        public static extern bool PostMessage(HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam);

        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern HWND FindWindow(string lpClassName, string lpWindowName);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern HWND FindWindow(IntPtr lpClassName, string lpWindowName);

        [DllImport(dllName)]
        public static extern HWND GetWindow(HWND hWnd, UINT uCmd);

        [DllImport(dllName)]
        public static extern HWND GetTopWindow(HWND hWnd);

        [DllImport(dllName)]
        public static extern BOOL CloseWindow(HWND hWnd);

        [DllImport(dllName)]
        public static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport(dllName)]
        public static extern BOOL EndDialog(HWND hDlg, IntPtr nResult);

        [DllImport(dllName, SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        [DllImport(dllName)]
        public static extern HMONITOR MonitorFromWindow(HWND hwnd, DWORD dwFlags);
    }
}
