using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Sandaab.WindowsApp.Win32
{
    using BOOL = Boolean;
    using HANDLE = IntPtr;
    using UCHAR = Byte;
    using ULONG = UInt32;
    using USHORT = UInt16;
    using WINUSB_INTERFACE_HANDLE = SafeFileHandle;

    internal static partial class WinUsb
    {
        private const string dllName = "WinUsb.dll";

        public const int EnglishLanguageID = 1033;
        public const ULONG DEVICE_SPEED = 1;
        public const UCHAR USB_ENDPOINT_DIRECTION_MASK = 0x80;
        public const UCHAR WritePipeId = 0x80;

        public const UCHAR USB_DEVICE_DESCRIPTOR_TYPE = 0x01;
        public const UCHAR USB_ENDPOINT_DESCRIPTOR_TYPE = 0x05;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WINUSB_SETUP_PACKET
        {
            public UCHAR RequestType;
            public UCHAR Request;
            public USHORT Value;
            public USHORT Index;
            public USHORT Length;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USB_INTERFACE_DESCRIPTOR
        {
            public UCHAR bLength;
            public UCHAR bDescriptorType;
            public UCHAR bInterfaceNumber;
            public UCHAR bAlternateSetting;
            public UCHAR bNumEndpoints;
            public UCHAR bInterfaceClass;
            public UCHAR bInterfaceSubClass;
            public UCHAR bInterfaceProtocol;
            public UCHAR iInterface;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USB_DEVICE_DESCRIPTOR
        {
            public UCHAR bLength;
            public UCHAR bDescriptorType;
            public USHORT bcdUSB;
            public UCHAR bDeviceClass;
            public UCHAR bDeviceSubClass;
            public UCHAR bDeviceProtocol;
            public UCHAR bMaxPacketSize0;
            public USHORT idVendor;
            public USHORT idProduct;
            public USHORT bcdDevice;
            public UCHAR iManufacturer;
            public UCHAR iProduct;
            public UCHAR iSerialNumber;
            public UCHAR bNumConfigurations;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USB_ENDPOINT_DESCRIPTOR
        {
            public UCHAR bLength;
            public UCHAR bDescriptorType;
            public UCHAR bEndpointAddress;
            public UCHAR bmAttributes;
            public USHORT wMaxPacketSize;
            public UCHAR bInterval;
        }

        public enum USBD_PIPE_TYPE : ULONG
        {
            UsbdPipeTypeControl = 0,
            UsbdPipeTypeIsochronous = 1,
            UsbdPipeTypeBulk = 2,
            UsbdPipeTypeInterrupt = 3
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WINUSB_PIPE_INFORMATION
        {
            public USBD_PIPE_TYPE PipeType;
            public UCHAR PipeId;
            public USHORT MaximumPacketSize;
            public UCHAR Interval;
        }

        public static bool USB_ENDPOINT_DIRECTION_IN(UCHAR addr)
        {
            return (addr & USB_ENDPOINT_DIRECTION_MASK) == USB_ENDPOINT_DIRECTION_MASK;
        }
        public static bool USB_ENDPOINT_DIRECTION_OUT(UCHAR addr)
        {
            return !USB_ENDPOINT_DIRECTION_IN(addr);
        }

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_ControlTransfer(
            WINUSB_INTERFACE_HANDLE InterfaceHandle,
            WINUSB_SETUP_PACKET SetupPacket,
            UCHAR[] Buffer,
            ULONG BufferLength,
            out ULONG LengthTransferred,
            IntPtr Overlapped);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_GetAssociatedInterface(
            WINUSB_INTERFACE_HANDLE InterfaceHandle,
            UCHAR AssociatedInterfaceIndex,
            out WINUSB_INTERFACE_HANDLE AssociatedInterfaceHandle);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_GetDescriptor(
            WINUSB_INTERFACE_HANDLE InterfaceHandle,
            UCHAR DescriptorType,
            UCHAR Index,
            USHORT LanguageID,
            IntPtr Buffer,
            ULONG BufferLength,
            out ULONG LengthTransfered);

        [DllImport(dllName)]
        public static extern BOOL WinUsb_Free(
            WINUSB_INTERFACE_HANDLE InterfaceHandle);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_Initialize(
            HANDLE DeviceHandle,
            out WINUSB_INTERFACE_HANDLE InterfaceHandle);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_QueryDeviceInformation(
            WINUSB_INTERFACE_HANDLE InterfaceHandle,
            ULONG InformationType,
            ref ULONG BufferLength,
            byte[] Buffer);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_QueryInterfaceSettings(
            WINUSB_INTERFACE_HANDLE InterfaceHandle, 
            UCHAR AlternateInterfaceNumber, 
            out USB_INTERFACE_DESCRIPTOR UsbAltInterfaceDescriptor);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_QueryPipe(
            WINUSB_INTERFACE_HANDLE InterfaceHandle, 
            UCHAR AlternateInterfaceNumber, 
            UCHAR PipeIndex, 
            out WINUSB_PIPE_INFORMATION PipeInformation);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_ReadPipe(
            WINUSB_INTERFACE_HANDLE InterfaceHandle,
            UCHAR PipeID,
            UCHAR[] Buffer,
            ULONG BufferLength,
            out ULONG LengthTransferred,
            IntPtr Overlapped);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_SetPipePolicy(
            WINUSB_INTERFACE_HANDLE InterfaceHandle, 
            UCHAR PipeID, 
            ULONG PolicyType, 
            ULONG ValueLength, 
            ref ULONG Value);

        [DllImport(dllName, SetLastError = true)]
        public static extern BOOL WinUsb_WritePipe(
            WINUSB_INTERFACE_HANDLE InterfaceHandle, 
            UCHAR PipeID, 
            UCHAR[] Buffer, 
            ULONG BufferLength, 
            out ULONG LengthTransferred, 
            IntPtr Overlapped);
    }
}
