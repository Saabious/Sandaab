using System.Runtime.InteropServices;
using System.Text;
using static Sandaab.WindowsApp.Win32.CfgMgr32;
using static Sandaab.WindowsApp.Win32.SetupApi;

namespace Sandaab.WindowsApp.Win32
{
    using BOOL = Boolean;
    using GUID = Guid;
    using HWND = IntPtr;
    using DWORD = UInt32;
    using HDEVINFO = IntPtr;
    using ULONG_PTR = IntPtr;
    using PCTSTR = String;
    using ULONG = UInt32;
    using PBYTE = IntPtr;
    using BYTE = Byte;

    internal class SetupApi
    {
        private const string dllName = "SetupApi.dll";

        public const DWORD DIGCF_DEFAULT = 0x00000001;
        public const DWORD DIGCF_PRESENT = 0x00000002;
        public const DWORD DIGCF_ALLCLASSES = 0x00000004;
        public const DWORD DIGCF_PROFILE = 0x00000008;
        public const DWORD DIGCF_DEVICEINTERFACE = 0x00000010;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SP_DEVINFO_DATA
        {
            public DWORD cbSize;
            public GUID ClassGuid;
            public DWORD DevInst;
            public ULONG_PTR Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public DWORD cbSize;
            public GUID InterfaceClassGuid;
            public DWORD Flags;
            public ULONG_PTR Reserved;
        };

        [DllImport(dllName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern HDEVINFO SetupDiCreateDeviceInfoList(
            ref GUID ClassGuid,
            HWND hwndParent);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL SetupDiDestroyDeviceInfoList(HDEVINFO DeviceInfoSet);

        [DllImport(dllName, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern HDEVINFO SetupDiGetClassDevs(
            IntPtr ClassGuid,
            PCTSTR Enumerator,
            HWND hwndParent,
            DWORD Flags);
        [DllImport(dllName, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern HDEVINFO SetupDiGetClassDevs(
            GUID ClassGuid,
            PCTSTR Enumerator,
            HWND hwndParent,
            DWORD Flags);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL SetupDiEnumDeviceInterfaces(
            HDEVINFO DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref GUID InterfaceClassGuid,
            DWORD MemberIndex,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL SetupDiEnumDeviceInfo(
            HDEVINFO DeviceInfoSet,
            DWORD MemberIndex,
            out SP_DEVINFO_DATA DeviceInfoData);

        [DllImport(dllName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern BOOL SetupDiGetDeviceInstanceId(
            HDEVINFO DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            IntPtr DeviceInstanceId,
            DWORD DeviceInstanceIdSize,
            out DWORD RequiredSize);

        [DllImport(dllName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern BOOL SetupDiGetDeviceInstanceId(
            HDEVINFO DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            StringBuilder DeviceInstanceId,
            DWORD DeviceInstanceIdSize,
            out DWORD RequiredSize);

        [DllImport(dllName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern BOOL SetupDiGetDeviceProperty(
            HDEVINFO DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref DEVPROPKEY PropertyKey,
            out ULONG PropertyType,
            PBYTE PropertyBuffer,
            DWORD PropertyBufferSize,
            out DWORD RequiredSize,
            DWORD Flags);
        [DllImport(dllName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern BOOL SetupDiGetDeviceProperty(
            HDEVINFO DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref DEVPROPKEY PropertyKey,
            out ULONG PropertyType,
            StringBuilder PropertyBuffer,
            DWORD PropertyBufferSize,
            out DWORD RequiredSize,
            DWORD Flags);
    }
}
