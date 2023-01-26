using System.Runtime.InteropServices;
using System.Text;

namespace Sandaab.WindowsApp.Win32
{
    using CONFIGRET = UInt32;
    using DEVINST = UInt32;
    using ULONG = UInt32;
    using DEVPROPGUID = Guid;
    using DEVPROPID = UInt32;
    using DEVPROPTYPE = UInt32;
    using BYTE = Byte;

    internal class CfgMgr32
    {
        private const string dllName = "CfgMgr32.dll";

        public const CONFIGRET CR_SUCCESS = 0x00000000;
        public const CONFIGRET CR_DEFAULT = 0x00000001;
        public const CONFIGRET CR_OUT_OF_MEMORY = 0x00000002;
        public const CONFIGRET CR_INVALID_POINTER = 0x00000003;
        public const CONFIGRET CR_INVALID_FLAG = 0x00000004;
        public const CONFIGRET CR_INVALID_DEVNODE = 0x00000005;
        public const CONFIGRET CR_INVALID_DEVINST = CR_INVALID_DEVNODE;
        public const CONFIGRET CR_INVALID_RES_DES = 0x00000006;
        public const CONFIGRET CR_INVALID_LOG_CONF = 0x00000007;
        public const CONFIGRET CR_INVALID_ARBITRATOR = 0x00000008;
        public const CONFIGRET CR_INVALID_NODELIST = 0x00000009;
        public const CONFIGRET CR_DEVNODE_HAS_REQS = 0x0000000A;
        public const CONFIGRET CR_DEVINST_HAS_REQS = CR_DEVNODE_HAS_REQS;
        public const CONFIGRET CR_INVALID_RESOURCEID = 0x0000000B;
        public const CONFIGRET CR_DLVXD_NOT_FOUND = 0x0000000C;
        public const CONFIGRET CR_NO_SUCH_DEVNODE = 0x0000000D;
        public const CONFIGRET CR_NO_SUCH_DEVINST = CR_NO_SUCH_DEVNODE;
        public const CONFIGRET CR_NO_MORE_LOG_CONF = 0x0000000E;
        public const CONFIGRET CR_NO_MORE_RES_DES = 0x0000000F;
        public const CONFIGRET CR_ALREADY_SUCH_DEVNODE = 0x00000010;
        public const CONFIGRET CR_ALREADY_SUCH_DEVINST = CR_ALREADY_SUCH_DEVNODE;
        public const CONFIGRET CR_INVALID_RANGE_LIST = 0x00000011;
        public const CONFIGRET CR_INVALID_RANGE = 0x00000012;
        public const CONFIGRET CR_FAILURE = 0x00000013;
        public const CONFIGRET CR_NO_SUCH_LOGICAL_DEV = 0x00000014;
        public const CONFIGRET CR_CREATE_BLOCKED = 0x00000015;
        public const CONFIGRET CR_NOT_SYSTEM_VM = 0x00000016;
        public const CONFIGRET CR_REMOVE_VETOED = 0x00000017;
        public const CONFIGRET CR_APM_VETOED = 0x00000018;
        public const CONFIGRET CR_INVALID_LOAD_TYPE = 0x00000019;
        public const CONFIGRET CR_BUFFER_SMALL = 0x0000001A;
        public const CONFIGRET CR_NO_ARBITRATOR = 0x0000001B;
        public const CONFIGRET CR_NO_REGISTRY_HANDLE = 0x0000001C;
        public const CONFIGRET CR_REGISTRY_ERROR = 0x0000001D;
        public const CONFIGRET CR_INVALID_DEVICE_ID = 0x0000001E;
        public const CONFIGRET CR_INVALID_DATA = 0x0000001F;
        public const CONFIGRET CR_INVALID_API = 0x00000020;
        public const CONFIGRET CR_DEVLOADER_NOT_READY = 0x00000021;
        public const CONFIGRET CR_NEED_RESTART = 0x00000022;
        public const CONFIGRET CR_NO_MORE_HW_PROFILES = 0x00000023;
        public const CONFIGRET CR_DEVICE_NOT_THERE = 0x00000024;
        public const CONFIGRET CR_NO_SUCH_VALUE = 0x00000025;
        public const CONFIGRET CR_WRONG_TYPE = 0x00000026;
        public const CONFIGRET CR_INVALID_PRIORITY = 0x00000027;
        public const CONFIGRET CR_NOT_DISABLEABLE = 0x00000028;
        public const CONFIGRET CR_FREE_RESOURCES = 0x00000029;
        public const CONFIGRET CR_QUERY_VETOED = 0x0000002A;
        public const CONFIGRET CR_CANT_SHARE_IRQ = 0x0000002B;
        public const CONFIGRET CR_NO_DEPENDENT = 0x0000002C;
        public const CONFIGRET CR_SAME_RESOURCES = 0x0000002D;
        public const CONFIGRET CR_NO_SUCH_REGISTRY_KEY = 0x0000002E;
        public const CONFIGRET CR_INVALID_MACHINENAME = 0x0000002F;
        public const CONFIGRET CR_REMOTE_COMM_FAILURE = 0x00000030;
        public const CONFIGRET CR_MACHINE_UNAVAILABLE = 0x00000031;
        public const CONFIGRET CR_NO_CM_SERVICES = 0x00000032;
        public const CONFIGRET CR_ACCESS_DENIED = 0x00000033;
        public const CONFIGRET CR_CALL_NOT_IMPLEMENTED = 0x00000034;
        public const CONFIGRET CR_INVALID_PROPERTY = 0x00000035;
        public const CONFIGRET CR_DEVICE_INTERFACE_ACTIVE = 0x00000036;
        public const CONFIGRET CR_NO_SUCH_DEVICE_INTERFACE = 0x00000037;
        public const CONFIGRET CR_INVALID_REFERENCE_STRING = 0x00000038;
        public const CONFIGRET CR_INVALID_CONFLICT_LIST = 0x00000039;
        public const CONFIGRET CR_INVALID_INDEX = 0x0000003A;
        public const CONFIGRET CR_INVALID_STRUCTURE_SIZE = 0x0000003B;
        public const CONFIGRET NUM_CR_RESULTS = 0x0000003C;

        public const int CM_LOCATE_DEVNODE_NORMAL = 0x00000000;
        public const int CM_LOCATE_DEVNODE_PHANTOM = 0x00000001;
        public const int CM_LOCATE_DEVNODE_CANCELREMOVE = 0x00000002;
        public const int CM_LOCATE_DEVNODE_NOVALIDATION = 0x00000004;
        public const int CM_LOCATE_DEVNODE_BITS = 0x00000007;


        public class CfgMgr32Exception : Exception
        {
            public CfgMgr32Exception(CONFIGRET configRet)
                : base("Configuration Manager (CfgMgr32) error 0x" + configRet.ToString("X8"))
            {
            }
        }

        public static DEVPROPKEY DEVPKEY_Device_PDOName;
        public static DEVPROPKEY DEVPKEY_Device_InstanceId;
        public static DEVPROPKEY DEVPKEY_Device_ClassGuid;
        public static DEVPROPKEY DEVPKEY_Device_Manufacturer;
        public static DEVPROPKEY DEVPKEY_Device_FriendlyName;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DEVPROPKEY
        {
            public DEVPROPGUID fmtid;
            public DEVPROPID pid;
        }

        public static void DEFINE_DEVPROPKEY(out DEVPROPKEY key, UInt32 l, UInt16 w1, UInt16 w2, Byte b1, Byte b2, Byte b3, Byte b4, Byte b5, Byte b6, Byte b7, Byte b8, DEVPROPID pid)
        {
            key.fmtid = new Guid(l, w1, w2, b1, b2, b3, b4, b5, b6, b7, b8);
            key.pid = pid;
        }

        public static void InitializeDevPropKeys()
        {
            // https://github.com/tpn/winsdk-10/blob/master/Include/10.0.16299.0/shared/devpkey.h
            DEFINE_DEVPROPKEY(out DEVPKEY_Device_PDOName, 0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 16);    // DEVPROP_TYPE_STRING
            DEFINE_DEVPROPKEY(out DEVPKEY_Device_InstanceId, 0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57, 256);   // DEVPROP_TYPE_STRING
            DEFINE_DEVPROPKEY(out DEVPKEY_Device_ClassGuid, 0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 10);    // DEVPROP_TYPE_GUID
            DEFINE_DEVPROPKEY(out DEVPKEY_Device_Manufacturer, 0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 13);    // DEVPROP_TYPE_STRING
            DEFINE_DEVPROPKEY(out DEVPKEY_Device_FriendlyName, 0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0, 14);    // DEVPROP_TYPE_STRING
        }


        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern CONFIGRET CM_Get_DevNode_Property(
            DEVINST dnDevInst,
            ref DEVPROPKEY PropertyKey,
            out DEVPROPTYPE PropertyType,
            IntPtr PropertyBuffer,
            ref ULONG PropertyBufferSize,
            ULONG ulFlags);
        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern CONFIGRET CM_Get_DevNode_Property(
            DEVINST dnDevInst,
            ref DEVPROPKEY PropertyKey,
            out DEVPROPTYPE PropertyType,
            StringBuilder PropertyBuffer,
            ref ULONG PropertyBufferSize,
            ULONG ulFlags);

        [DllImport(dllName, CharSet = CharSet.Unicode)]
        public static extern CONFIGRET CM_Locate_DevNode(
            out DEVINST pdnDevInst,
            string pDeviceID,
            ULONG ulFlags);

        [DllImport(dllName)]
        public static extern CONFIGRET CM_Get_Child(
            out DEVINST pdnDevInst,
            DEVINST dnDevInst,
            ULONG ulFlags);

        [DllImport(dllName)]
        public static extern CONFIGRET CM_Get_Sibling(
            out DEVINST pdnDevInst,
            DEVINST dnDevInst,
            ULONG ulFlags);
    }
}
