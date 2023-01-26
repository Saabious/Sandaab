using System.Runtime.InteropServices;

namespace Sandaab.WindowsApp.Win32
{
    using HANDLE = IntPtr;

    internal class BthProps
    {
        private const string dllName = "BthProps.cpl";
        private const int BLUETOOTH_MAX_NAME_SIZE = 248;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct BLUETOOTH_FIND_RADIO_PARAMS
        {
            public int dwSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct BLUETOOTH_RADIO_INFO
        {
            internal int dwSize;
            internal ulong address;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BLUETOOTH_MAX_NAME_SIZE)]
            internal string szName;
            internal uint ulClassofDevice;
            internal ushort lmpSubversion;
            [MarshalAs(UnmanagedType.U2)]
            internal ushort manufacturer;
        }
        [DllImport(dllName, ExactSpelling = true, SetLastError = true)]
        internal static extern HANDLE BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAMS pbtfrp, out HANDLE phRadio);

        [DllImport(dllName, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BluetoothFindNextRadio(HANDLE hFind, out HANDLE phRadio);

        [DllImport(dllName, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BluetoothFindRadioClose(HANDLE hFind);

        [DllImport(dllName, ExactSpelling = true, SetLastError = true)]
        internal static extern int BluetoothGetRadioInfo(HANDLE hRadio, ref BLUETOOTH_RADIO_INFO pRadioInfo);

        [DllImport(dllName, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BluetoothIsVersionAvailable(byte MajorVersion, byte MinorVersion);

        [DllImport(dllName, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BluetoothIsConnectable(HANDLE hRadio);

        [DllImport(dllName, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BluetoothIsDiscoverable(HANDLE hRadio);
    }
}
