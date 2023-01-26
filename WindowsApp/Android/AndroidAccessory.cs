using Microsoft.Win32.SafeHandles;
using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Properties;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using static Sandaab.WindowsApp.Android.UsbHost;
using static Sandaab.Core.Constantes.Win32Errors;
using static Sandaab.WindowsApp.Win32.Kernel32;
using static Sandaab.WindowsApp.Win32.WinUsb;

namespace Sandaab.Android
{
    using HANDLE = IntPtr;
    using UCHAR = Byte;
    using ULONG = UInt32;
    using USHORT = UInt16;
    using WINUSB_INTERFACE_HANDLE = SafeFileHandle;

    public record UCHARRecord
    {
        public UCHAR UCHAR;
    }

    internal class AndroidAccessory : IDisposable
    {
        private readonly IntPtr NULL = IntPtr.Zero;
        private readonly HANDLE INVALID_HANDLE_VALUE = (HANDLE)0xFFFFFFFF;

        public const USHORT USB_ACCESSORY_VENDOR_ID = 0x18D1;
        public const USHORT USB_ACCESSORY_PRODUCT_ID = 0x2D00;
        public const USHORT USB_ACCESSORY_ADB_PRODUCT_ID = 0x2D01;
        private const USHORT USB_AUDIO_PRODUCT_ID = 0x2D02;
        private const USHORT USB_AUDIO_ADB_PRODUCT_ID = 0x2D03;
        private const USHORT USB_ACCESSORY_AUDIO_PRODUCT_ID = 0x2D03;
        private const USHORT USB_ACCESSORY_AUDIO__ADB_PRODUCT_ID = 0x2D03;
        private const UCHAR ACCESSORY_STRING_MANUFACTURER = 0;
        private const UCHAR ACCESSORY_STRING_MODEL = 1;
        private const UCHAR ACCESSORY_STRING_DESCRIPTION = 2;
        private const UCHAR ACCESSORY_STRING_VERSION = 3;
        private const UCHAR ACCESSORY_STRING_URI = 4;
        private const UCHAR ACCESSORY_STRING_SERIAL = 5;
        private const UCHAR ACCESSORY_GET_PROTOCOL = 51;
        private const UCHAR ACCESSORY_SEND_STRING = 52;
        private const UCHAR ACCESSORY_START = 53;
        private const UCHAR ACCESSORY_REGISTER_HID = 54;
        private const UCHAR ACCESSORY_UNREGISTER_HID = 55;
        private const UCHAR ACCESSORY_SET_HID_REPORT_DESC = 56;
        private const UCHAR ACCESSORY_SEND_HID_EVENT = 57;

        private const UCHAR SET_CONFIGURATION = 0x09;

        private const int BUFFER_SIZE = 4096;

        private HANDLE _deviceHandle;
        public Stream InputStream { get; private set; }
        public Stream OutputStream { get; private set; }
        private WINUSB_INTERFACE_HANDLE _winUsbHandle;
        private readonly HANDLE _controlTransferBuffer;

        public AndroidAccessory()
        {
            _controlTransferBuffer = Marshal.AllocHGlobal(BUFFER_SIZE);
            InputStream = new MemoryStream();
            OutputStream = new MemoryStream();
            _winUsbHandle = null;
        }

        public void Dispose()
        {
            Close();
            InputStream.Dispose();
            OutputStream.Dispose();
            Marshal.FreeHGlobal(_controlTransferBuffer);

            GC.SuppressFinalize(this);
        }

        public Task<bool> SwitchAsync(string deviceId, string deviceName)
        {
            return Task.Run(
                () =>
                {
                    if (!Open(deviceId, deviceName))
                        return false;

                    SwitchDevice();
                    Close();
                    return true;
                });
        }

        public Task<bool> ConnectAsync(string deviceId, string deviceName)
        {
            return Task.Run(
                () =>
                {
                    if (!Open(deviceId, deviceName))
                        return false;

                    Connect();
                    Close();
                    return true;
                });
        }

        private bool Open(string deviceId, string deviceName)
        {
            _deviceHandle = CreateFile(
                deviceId, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, // Todo: RemoveAsync Share?
                OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);

            var error = Marshal.GetLastWin32Error();
            if (error == (int)ERROR_ACCESS_DENIED)
            {
                Logger.Warn(string.Format(Messages.UsbDeviceAccessDenied, deviceName));
                return false;
            }
            else if (error != (int)ERROR_SUCCESS)
                throw new Win32Exception(error);
            else if (_deviceHandle == INVALID_HANDLE_VALUE)
                throw new Exception("Should never happens.");
            else if (!WinUsb_Initialize(_deviceHandle, out _winUsbHandle))
            {
                Close();
                throw new Win32Exception(error);
            }
            else
                return true;
        }

        private void Close()
        {
            if (_winUsbHandle != null)
            {
                WinUsb_Free(_winUsbHandle);
                _winUsbHandle = null;
            }
            if (_deviceHandle != NULL)
            {
                if (_deviceHandle != INVALID_HANDLE_VALUE)
                    CloseHandle(_deviceHandle);
                _deviceHandle = NULL;
            }
        }

        private void SwitchDevice()
        {
            var version = GetProtocol();
            if (version < 2)
                throw new Win32Exception(string.Format("Unsupported AOA protocol ({0}).", version));

            SendString(ACCESSORY_STRING_MANUFACTURER, ((WindowsApp.Components.WindowsLocalDevice)SandaabContext.LocalDevice).GetManufacturer());
            SendString(ACCESSORY_STRING_MODEL, ((WindowsApp.Components.WindowsLocalDevice)SandaabContext.LocalDevice).GetModel());
            SendString(ACCESSORY_STRING_DESCRIPTION, SandaabContext.LocalDevice.Name);
            SendString(ACCESSORY_STRING_VERSION, Environment.OSVersion.VersionString);
            SendString(ACCESSORY_STRING_URI, "");
            SendString(ACCESSORY_STRING_SERIAL, SandaabContext.LocalDevice.Id);

            SendControlRequest(
                USB_SETUP_HOST_TO_DEVICE | USB_SETUP_TYPE_VENDOR | USB_SETUP_RECIPIENT_DEVICE,
                ACCESSORY_START);
        }

        private void Connect()
        {
            SendControlRequest(
                USB_SETUP_HOST_TO_DEVICE | USB_SETUP_TYPE_STANDARD | USB_SETUP_RECIPIENT_DEVICE,
                SET_CONFIGURATION,
                1);

            int readPipeId = -1;
            int writePipeId = -1;
            int error;

            byte interfaceIndex = 0;
            while (true)
            {
                WinUsb_QueryInterfaceSettings(_winUsbHandle, interfaceIndex, out var interfaceDescriptor);
                error = Marshal.GetLastWin32Error();
                if (error == (int)ERROR_NO_MORE_ITEMS)
                    break;
                else if (error != (int)ERROR_SUCCESS)
                    throw new Win32Exception(error);

                for (byte pipeIndex = 0; pipeIndex < interfaceDescriptor.bNumEndpoints; pipeIndex++)
                {
                    if (!WinUsb_QueryPipe(_winUsbHandle, interfaceIndex, pipeIndex, out var pipeInformation))
                        throw new Win32Exception();

                    if (pipeInformation.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeBulk)
                        if (USB_ENDPOINT_DIRECTION_IN(pipeInformation.PipeId))
                            readPipeId = pipeInformation.PipeId;
                        else
                            writePipeId = pipeInformation.PipeId;
                }

                interfaceIndex++;
            }

            if (readPipeId < 0)
                throw new Exception("Unknown PipeId for reading");
            if (writePipeId < 0)
                throw new Exception("Unknown PipeId for writing");

            //var bufferSize = Marshal.SizeOf(new USB_DEVICE_DESCRIPTOR());
            //var ptr = Marshal.AllocHGlobal(bufferSize);
            //WinUsb_GetDescriptor(
            //    _winUsbHandle,
            //    USB_DEVICE_DESCRIPTOR_TYPE,
            //    0,
            //    0,
            //    ptr,
            //    (ULONG)bufferSize,
            //    out var lengthTransferred);
            //error = Marshal.GetLastWin32Error();
            //if (error != ERROR_SUCCESS)
            //    throw new Win32Exception(error);
            //var deviceDescriptor = Marshal.PtrToStructure<USB_DEVICE_DESCRIPTOR>(ptr);


            byte[] buffer = Encoding.UTF8.GetBytes("Hallo");
            var success = WinUsb_WritePipe(_winUsbHandle, (UCHAR)writePipeId, buffer, (ULONG)buffer.Length, out var lengthTransferred, NULL);
            error = Marshal.GetLastWin32Error();
            if (error != (int)ERROR_SUCCESS)
                throw new Win32Exception(error);
            if (lengthTransferred < buffer.Length)
                throw new Exception(string.Format(Messages.IncompleteSent, lengthTransferred, buffer.Length));

            //const int BUFFER_SIZE = 32768;
            //var buffer = Marshal.AllocHGlobal(BUFFER_SIZE);
            //WinUsb_ReadPipe(_winUsbHandle, pipeId, buffer, (ULONG)readPipeId, out var lengthTransferred, NULL);
            //error = Marshal.GetLastWin32Error();
            //if (error != ERROR_SUCCESS)
            //    throw new Win32Exception(error);
        }

        private int GetProtocol()
        {
            byte[] buffer = new byte[2];
            SendControlRequest(
                USB_SETUP_DEVICE_TO_HOST | USB_SETUP_TYPE_VENDOR | USB_SETUP_RECIPIENT_DEVICE,
                ACCESSORY_GET_PROTOCOL,
                0,
                0,
                buffer);

            return BitConverter.ToInt16(buffer, 0);
        }

        private void SendControlRequest(UCHAR requestType, UCHAR request, USHORT value = 0, USHORT index = 0, UCHAR[] bytes = null)
        {
            var bufferLength = bytes == null ? 0 : bytes.Length;
            if (!WinUsb_ControlTransfer(
                _winUsbHandle,
                new WINUSB_SETUP_PACKET()
                {
                    RequestType = requestType,
                    Request = request,
                    Value = value,
                    Index = index,
                    Length = (USHORT)bufferLength,
                },
                bytes,
                (uint)bufferLength,
                out var transferredLength,
                NULL))
                throw new Win32Exception();
            else if (transferredLength < bufferLength)
                throw new Exception(string.Format(Messages.IncompleteSent, transferredLength, bufferLength));
        }

        private void SendString(USHORT index, string value)
        {
            SendControlRequest(
                USB_SETUP_HOST_TO_DEVICE | USB_SETUP_TYPE_VENDOR | USB_SETUP_RECIPIENT_DEVICE,
                ACCESSORY_SEND_STRING,
                0,
                index,
                Encoding.UTF8.GetBytes(value + "\0"));
        }
    }
}
