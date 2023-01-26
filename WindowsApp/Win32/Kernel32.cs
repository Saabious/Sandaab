using System.Runtime.InteropServices;

namespace Sandaab.WindowsApp.Win32
{
    using DWORD = UInt32;
    using HANDLE = IntPtr;

    internal class Kernel32
    {
        private const string dllName = "Kernel32.dll";

        public const int BTH_MAX_NAME_SIZE = 248;

        public const DWORD DELETE = 0x10000;
        public const DWORD READ_CONTROL = 0x20000;
        public const DWORD WRITE_DAC = 0x40000;
        public const DWORD WRITE_OWNER = 0x80000;
        public const DWORD SYNCHRONIZE = 0x100000;
        public const DWORD STANDARD_RIGHTS_READ = READ_CONTROL;
        public const DWORD STANDARD_RIGHTS_WRITE = STANDARD_RIGHTS_READ;
        public const DWORD STANDARD_RIGHTS_EXECUTE = READ_CONTROL;
        public const DWORD STANDARD_RIGHTS_REQUIRED = 0xF0000;
        public const DWORD STANDARD_RIGHTS_ALL = 0x1F0000;
        public const DWORD FILE_READ_DATA = 0x0001;
        public const DWORD FILE_LIST_DIRECTORY = 0x0001;
        public const DWORD FILE_WRITE_DATA = 0x0002;
        public const DWORD FILE_ADD_FILE = 0x0002;
        public const DWORD FILE_APPEND_DATA = 0x0004;
        public const DWORD FILE_ADD_SUBDIRECTORY = 0x0004;
        public const DWORD FILE_CREATE_PIPE_INSTANCE = 0x0004;
        public const DWORD FILE_READ_EA = 0x0008;
        public const DWORD FILE_WRITE_EA = 0x0010;
        public const DWORD FILE_EXECUTE = 0x0020;
        public const DWORD FILE_TRAVERSE = 0x0020;
        public const DWORD FILE_DELETE_CHILD = 0x0040;
        public const DWORD FILE_READ_ATTRIBUTES = 0x0080;
        public const DWORD FILE_WRITE_ATTRIBUTES = 0x0100;
        public const DWORD GENERIC_READ = 0x80000000;
        public const DWORD GENERIC_WRITE = 0x40000000;
        public const DWORD GENERIC_EXECUTE = 0x20000000;
        public const DWORD GENERIC_ALL = 0x10000000;
        public const DWORD SPECIFIC_RIGHTS_ALL = 0x00FFFF;
        public const DWORD FILE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF;
        public const DWORD FILE_GENERIC_READ = STANDARD_RIGHTS_READ | FILE_READ_DATA | FILE_READ_ATTRIBUTES | FILE_READ_EA | SYNCHRONIZE;
        public const DWORD FILE_GENERIC_WRITE = STANDARD_RIGHTS_WRITE | FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | FILE_APPEND_DATA | SYNCHRONIZE;
        public const DWORD FILE_GENERIC_EXECUTE = STANDARD_RIGHTS_EXECUTE | FILE_READ_ATTRIBUTES | FILE_EXECUTE | SYNCHRONIZE;

        public const DWORD FILE_SHARE_NONE = 0x00000000;
        public const DWORD FILE_SHARE_READ = 0x00000001;
        public const DWORD FILE_SHARE_WRITE = 0x00000002;
        public const DWORD FILE_SHARE_DELETE = 0x00000004;

        public const DWORD CREATE_NEW = 1;
        public const DWORD CREATE_ALWAYS = 2;
        public const DWORD OPEN_EXISTING = 3;
        public const DWORD OPEN_ALWAYS = 4;
        public const DWORD TRUNCATE_EXISTING = 5;

        public const DWORD FILE_ATTRIBUTE_READONLY = 0x1;
        public const DWORD FILE_ATTRIBUTE_HIDDEN = 0x2;
        public const DWORD FILE_ATTRIBUTE_SYSTEM = 0x4;
        public const DWORD FILE_ATTRIBUTE_DIRECTORY = 0x10;
        public const DWORD FILE_ATTRIBUTE_ARCHIVE = 0x20;
        public const DWORD FILE_ATTRIBUTE_DEVICE = 0x40;
        public const DWORD FILE_ATTRIBUTE_NORMAL = 0x80;
        public const DWORD FILE_ATTRIBUTE_TEMPORARY = 0x100;
        public const DWORD FILE_ATTRIBUTE_SPARSE_FILE = 0x200;
        public const DWORD FILE_ATTRIBUTE_REPARSE_POINT = 0x400;
        public const DWORD FILE_ATTRIBUTE_COMPRESSED = 0x800;
        public const DWORD FILE_ATTRIBUTE_OFFLINE = 0x1000;
        public const DWORD FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x2000;
        public const DWORD FILE_ATTRIBUTE_ENCRYPTED = 0x4000;
        public const DWORD FILE_ATTRIBUTE_VIRTUAL = 0x10000;
        public const DWORD FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
        public const DWORD FILE_FLAG_DELETE_ON_CLOSE = 0x4000000;
        public const DWORD FILE_FLAG_NO_BUFFERING = 0x20000000;
        public const DWORD FILE_FLAG_OPEN_NO_RECALL = 0x100000;
        public const DWORD FILE_FLAG_OPEN_REPARSE_POINT = 0x200000;
        public const DWORD FILE_FLAG_OVERLAPPED = 0x40000000;
        public const DWORD FILE_FLAG_POSIX_SEMANTICS = 0x1000000;
        public const DWORD FILE_FLAG_RANDOM_ACCESS = 0x10000000;
        public const DWORD FILE_FLAG_SEQUENTIAL_SCAN = 0x8000000;
        public const DWORD FILE_FLAG_WRITE_THROUGH = 0x80000000;

        public const DWORD LMEM_FIXED = 0x0000;
        public const DWORD LMEM_MOVEABLE = 0x0002;
        public const DWORD LMEM_NOCOMPACT = 0x0010;
        public const DWORD LMEM_NODISCARD = 0x0020;
        public const DWORD LMEM_ZEROINIT = 0x0040;
        public const DWORD LMEM_MODIFY = 0x0080;
        public const DWORD LMEM_DISCARDABLE = 0x0F00;
        public const DWORD LMEM_VALID_FLAGS = 0x0F72;
        public const DWORD LMEM_INVALID_HANDLE = 0x8000;
        public const DWORD LHND = (LMEM_MOVEABLE | LMEM_ZEROINIT);
        public const DWORD LPTR = (LMEM_FIXED | LMEM_ZEROINIT);
        public const DWORD NONZEROLHND = (LMEM_MOVEABLE);
        public const DWORD NONZEROLPTR = (LMEM_FIXED);

        public const DWORD GET_LOCAL_INFO = 0x410000;
        public const DWORD GET_RADIO_INFO = 0x410004;
        public const DWORD GET_DEVICE_INFO = 0x410008;
        public const DWORD DISCONNECT_DEVICE = 0x41000c;
        public const DWORD GET_DEVICE_RSSI = 0x410014;
        public const DWORD EIR_GET_RECORDS = 0x410040;
        public const DWORD EIR_SUBMIT_RECORD = 0x410044;
        public const DWORD EIR_UPDATE_RECORD = 0x410048;
        public const DWORD EIR_REMOVE_RECORD = 0x41004c;
        public const DWORD HCI_VENDOR_COMMAND = 0x410050;
        public const DWORD SDP_CONNECT = 0x410200;
        public const DWORD SDP_DISCONNECT = 0x410204;
        public const DWORD SDP_SERVICE_SEARCH = 0x410208;
        public const DWORD SDP_ATTRIBUTE_SEARCH = 0x41020c;
        public const DWORD SDP_SERVICE_ATTRIBUTE_SEARCH = 0x410210;
        public const DWORD SDP_SUBMIT_RECORD = 0x410214;
        public const DWORD SDP_REMOVE_RECORD = 0x410218;
        public const DWORD SDP_SUBMIT_RECORD_WITH_INFO = 0x41021c;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BTH_DEVICE_INFO
        {
            public ulong flags;
            public ulong address;
            public DWORD classOfDevice;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BTH_MAX_NAME_SIZE)]
            public string name;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BTH_RADIO_INFO
        {
            public ulong lmpSupportedFeatures;
            public ushort mfg;
            public ushort lmpSubversion;
            public byte lmpVersion;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BTH_LOCAL_RADIO_INFO
        {
            public BTH_DEVICE_INFO localInfo;
            public ulong flags;
            public ushort hciRevision;
            public byte hciVersion;
            public BTH_RADIO_INFO radioInfo;
        }

        [DllImport(dllName, SetLastError = true)]
        public static extern bool DeviceIoControl
        (
            HANDLE hDevice,
            DWORD ioControlCode,
            IntPtr inBuffer,
            DWORD inBufferSize,
            ref BTH_LOCAL_RADIO_INFO outBuffer,
            DWORD outBufferSize,
            out DWORD bytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport(dllName, SetLastError = true)]
        public static extern HANDLE CreateFile(
            string lpFileName,
            DWORD dwDesiredAccess,
            DWORD dwShareMode,
            IntPtr lpSecurityAttributes,
            DWORD dwCreationDisposition,
            DWORD dwFlagsAndAttributes,
            HANDLE hTemplateFile);

        [DllImport(dllName, SetLastError = true)]
        public static extern bool CloseHandle(HANDLE hObject);
    }
}
