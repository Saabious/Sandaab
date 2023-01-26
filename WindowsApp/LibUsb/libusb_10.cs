using System.Runtime.InteropServices;

namespace Sandaab.WindowsApp.LibUsb
{
    using libusb_device = IntPtr;
    using libusb_device_handle = IntPtr;
    using libusb_device_list = IntPtr;
    using ssize_t = Int32;
    using uint16_t = UInt16;
    using uint8_t = Byte;

    internal static class libusb_10
    {
        private const int Pack = 0;
        private const string dllName = "libusb-1.0";

        public const uint8_t LIBUSB_ENDPOINT_IN = 0x80;
        public const uint8_t LIBUSB_ENDPOINT_OUT = 0x00;
        public const uint8_t LIBUSB_TRANSFER_TYPE_MASK = 0x03;
        public const uint8_t LIBUSB_TRANSFER_TYPE_BULK = 0x02;

        public enum libusb_error : int
        {
            LIBUSB_SUCCESS = 0,
            LIBUSB_ERROR_IO = -1,
            LIBUSB_ERROR_INVALID_PARAM = -2,
            LIBUSB_ERROR_ACCESS = -3,
            LIBUSB_ERROR_NO_DEVICE = -4,
            LIBUSB_ERROR_NOT_FOUND = -5,
            LIBUSB_ERROR_BUSY = -6,
            LIBUSB_ERROR_TIMEOUT = -7,
            LIBUSB_ERROR_OVERFLOW = -8,
            LIBUSB_ERROR_PIPE = -9,
            LIBUSB_ERROR_INTERRUPTED = -10,
            LIBUSB_ERROR_NO_MEM = -11,
            LIBUSB_ERROR_NOT_SUPPORTED = -12,
            LIBUSB_ERROR_OTHER = -99,
        }

        public const uint8_t LIBUSB_CLASS_PER_INTERFACE = 0;
        public const uint8_t LIBUSB_CLASS_AUDIO = 1;
        public const uint8_t LIBUSB_CLASS_COMM = 2;
        public const uint8_t LIBUSB_CLASS_HID = 3;
        public const uint8_t LIBUSB_CLASS_PHYSICAL = 5;
        public const uint8_t LIBUSB_CLASS_PTP = 6; // legacy name from libusb-0.1 usb.h
        public const uint8_t LIBUSB_CLASS_IMAGE = 6;
        public const uint8_t LIBUSB_CLASS_PRINTER = 7;
        public const uint8_t LIBUSB_CLASS_MASS_STORAGE = 8;
        public const uint8_t LIBUSB_CLASS_HUB = 9;
        public const uint8_t LIBUSB_CLASS_DATA = 10;
        public const uint8_t LIBUSB_CLASS_SMART_CARD = 0x0b;
        public const uint8_t LIBUSB_CLASS_CONTENT_SECURITY = 0x0d;
        public const uint8_t LIBUSB_CLASS_VIDEO = 0x0e;
        public const uint8_t LIBUSB_CLASS_PERSONAL_HEALTHCARE = 0x0f;
        public const uint8_t LIBUSB_CLASS_DIAGNOSTIC_DEVICE = 0xdc;
        public const uint8_t LIBUSB_CLASS_WIRELESS = 0xe0;
        public const uint8_t LIBUSB_CLASS_APPLICATION = 0xfe;
        public const uint8_t LIBUSB_CLASS_VENDOR_SPEC = 0xff;


        [StructLayoutAttribute(LayoutKind.Sequential, Pack = Pack, CharSet = CharSet.Ansi)]
        public struct libusb_version
        {
            public uint16_t major;
            public uint16_t minor;
            public uint16_t micro;
            public uint16_t nano;
            public string rc;
            public string describe;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = Pack)]
        public struct libusb_config_descriptor
        {
            public uint8_t bLength;
            public uint8_t bDescriptorType;
            public uint16_t wTotalLength;
            public uint8_t bNumInterfaces;
            public uint8_t bConfigurationValue;
            public uint8_t iConfiguration;
            public uint8_t bmAttributes;
            public uint8_t bMaxPower;
            public IntPtr interfacePtr; // array of libusb_interface_descriptor
            public IntPtr extraPtr;
            public int extra_length;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = Pack)]
        public struct libusb_interface
        {
            public IntPtr altsetting; // array of libusb_interface_descriptor
            public int num_altsetting;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = Pack)]
        public struct libusb_interface_descriptor
        {
            public uint8_t bLength;
            public uint8_t bDescriptorType;
            public uint8_t bInterfaceNumber;
            public uint8_t bAlternateSetting;
            public uint8_t bNumEndpoints;
            public uint8_t bInterfaceClass;
            public uint8_t bInterfaceSubClass;
            public uint8_t bInterfaceProtocol;
            public uint8_t iInterface;
            public IntPtr endpointPtr; // array of libusb_endpoint_descriptor
            public IntPtr extra;
            public int extra_Length;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = Pack)]
        public unsafe struct libusb_endpoint_descriptor
        {
            public uint8_t bLength;
            public uint8_t bDescriptorType;
            public uint8_t bEndpointAddress;
            public uint8_t bmAttributes;
            public uint16_t wMaxPacketSize;
            public uint8_t bInterval;
            public uint8_t bRefresh;
            public uint8_t bSynchAddress;
            public IntPtr extra;
            public int extra_length;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = Pack)]
        public struct libusb_device_descriptor
        {
            public uint8_t bLength;
            public uint8_t bDescriptorType;
            public uint16_t bcdUSB;
            public uint8_t bDeviceClass;
            public uint8_t bDeviceSubClass;
            public uint8_t bDeviceProtocol;
            public uint8_t bMaxPacketSize0;
            public uint16_t idVendor;
            public uint16_t idProduct;
            public uint16_t bcdDevice;
            public uint8_t iManufacturer;
            public uint8_t iProduct;
            public uint8_t iSerialNumber;
            public uint8_t bNumConfigurations;
        }

        public struct libusb_context
        {
        }

        [DllImport(dllName)]
        public static extern int libusb_init(ref IntPtr ctx);
        [DllImport(dllName)]
        public static extern int libusb_init(IntPtr ctx);

        [DllImport(dllName)]
        public static extern void libusb_exit(IntPtr ctx);

        [DllImport(dllName)]
        public static extern void libusb_set_debug(libusb_context ctx, int level);

        [DllImport(dllName)]
        public static extern IntPtr libusb_get_version();

        [DllImport(dllName)]
        public static extern int libusb_has_capability(uint capability);

        [DllImport(dllName)]
        public static extern IntPtr libusb_error_name(int errcode);

        [DllImport(dllName)]
        public static extern int libusb_setlocale(IntPtr locale);

        [DllImport(dllName)]
        public static extern IntPtr libusb_strerror(int errcode);

        [DllImport(dllName)]
        public static extern ssize_t libusb_get_device_list(libusb_context ctx, out libusb_device_list list);
        [DllImport(dllName)]
        public static extern ssize_t libusb_get_device_list(IntPtr ctx, out libusb_device_list list);

        [DllImport(dllName)]
        public static extern void libusb_free_device_list(libusb_device_list list, int unrefDevices);

        [DllImport(dllName)]
        public static extern libusb_device libusb_ref_device(libusb_device dev);

        [DllImport(dllName)]
        public static extern void libusb_unref_device(IntPtr dev);

        [DllImport(dllName)]
        public static extern int libusb_get_configuration(libusb_device_handle dev, ref int config);

        [DllImport(dllName)]
        public static extern int libusb_get_device_descriptor(libusb_device dev, out libusb_device_descriptor desc);

        [DllImport(dllName)]
        public static extern int libusb_get_active_config_descriptor(libusb_device dev, out IntPtr config);

        //[DllImport(dllName)]
        //public static extern int libusb_get_config_descriptor(libusb_device dev, byte configIndex, libusb_config_descriptor** config);

        //[DllImport(dllName)]
        //public static extern int libusb_get_config_descriptor_by_value(libusb_device dev, byte bconfigurationvalue, libusb_config_descriptor** config);

        //[DllImport(dllName)]
        //public static extern void libusb_free_config_descriptor(libusb_config_descriptor* config);

        //[DllImport(dllName)]
        //public static extern int libusb_get_ss_endpoint_companion_descriptor(ref libusb_context ctx, EndpointDescriptor* endpoint, SsEndpointCompanionDescriptor** epComp);

        //[DllImport(dllName)]
        //public static extern void libusb_free_ss_endpoint_companion_descriptor(SsEndpointCompanionDescriptor* epComp);

        //[DllImport(dllName)]
        //public static extern int libusb_get_bos_descriptor(libusb_device_handle devHandle, BosDescriptor** bos);

        //[DllImport(dllName)]
        //public static extern void libusb_free_bos_descriptor(BosDescriptor* bos);

        //[DllImport(dllName)]
        //public static extern int libusb_get_usb_2_0_extension_descriptor(ref libusb_context ctx, BosDevCapabilityDescriptor* devCap, Usb20ExtensionDescriptor** usb20Extension);

        //[DllImport(dllName)]
        //public static extern void libusb_free_usb_2_0_extension_descriptor(Usb20ExtensionDescriptor* usb20Extension);

        //[DllImport(dllName)]
        //public static extern int libusb_get_ss_usb_device_capability_descriptor(ref libusb_context ctx, BosDevCapabilityDescriptor* devCap, SsUsbDeviceCapabilityDescriptor** ssUsbDeviceCap);

        //[DllImport(dllName)]
        //public static extern void libusb_free_ss_usb_device_capability_descriptor(SsUsbDeviceCapabilityDescriptor* ssUsbDeviceCap);

        //[DllImport(dllName)]
        //public static extern int libusb_get_container_id_descriptor(ref libusb_context ctx, BosDevCapabilityDescriptor* devCap, ContainerIdDescriptor** containerId);

        //[DllImport(dllName)]
        //public static extern void libusb_free_container_id_descriptor(ContainerIdDescriptor* containerId);

        [DllImport(dllName)]
        public static extern byte libusb_get_bus_number(libusb_device dev);

        [DllImport(dllName)]
        public static extern byte libusb_get_port_number(libusb_device dev);

        //[DllImport(dllName)]
        //public static extern int libusb_get_port_numbers(libusb_device dev, byte* portNumbers, int portNumbersLen);

        //[DllImport(dllName)]
        //public static extern int libusb_get_port_path(libusb_context ctx, libusb_device dev, byte* path, byte pathLength);

        [DllImport(dllName)]
        public static extern libusb_device libusb_get_parent(libusb_device dev);

        [DllImport(dllName)]
        public static extern byte libusb_get_device_address(libusb_device dev);

        [DllImport(dllName)]
        public static extern int libusb_get_device_speed(libusb_device dev);

        [DllImport(dllName)]
        public static extern int libusb_get_max_packet_size(libusb_device dev, byte endpoint);

        [DllImport(dllName)]
        public static extern int libusb_get_max_iso_packet_size(libusb_device dev, byte endpoint);

        [DllImport(dllName)]
        public static extern int libusb_open(libusb_device dev, out libusb_device_handle devHandle);

        [DllImport(dllName)]
        public static extern void libusb_close(IntPtr devHandle);

        [DllImport(dllName)]
        public static extern libusb_device libusb_get_device(libusb_device_handle devHandle);

        [DllImport(dllName)]
        public static extern int libusb_set_configuration(libusb_device_handle devHandle, int configuration);

        [DllImport(dllName)]
        public static extern int libusb_claim_interface(libusb_device_handle devHandle, int interfaceNumber);

        [DllImport(dllName)]
        public static extern int libusb_release_interface(libusb_device_handle devHandle, int interfaceNumber);

        [DllImport(dllName)]
        public static extern libusb_device_handle libusb_open_device_with_vid_pid(libusb_context ctx, ushort vendorId, ushort productId);
        [DllImport(dllName)]
        public static extern IntPtr libusb_open_device_with_vid_pid(IntPtr ctx, ushort vendorId, ushort productId);

        [DllImport(dllName)]
        public static extern int libusb_set_interface_alt_setting(libusb_device_handle devHandle, int interfaceNumber, int alternateSetting);

        [DllImport(dllName)]
        public static extern int libusb_clear_halt(libusb_device_handle devHandle, byte endpoint);

        [DllImport(dllName)]
        public static extern int libusb_reset_device(libusb_device_handle devHandle);

        //[DllImport(dllName)]
        //public static extern int libusb_alloc_streams(libusb_device_handle devHandle, uint numStreams, byte* endpoints, int numEndpoints);

        //[DllImport(dllName)]
        //public static extern int libusb_free_streams(libusb_device_handle devHandle, byte* endpoints, int numEndpoints);

        //[DllImport(dllName)]
        //public static extern bytelibusb_dev_mem_allocDevMemAlloc(libusb_device_handle devHandle, UIntPtr length);

        //[DllImport(dllName)]
        //public static extern int libusb_dev_mem_free(libusb_device_handle devHandle, byte* buffer, UIntPtr length);

        [DllImport(dllName)]
        public static extern int libusb_kernel_driver_active(libusb_device_handle devHandle, int interfaceNumber);

        [DllImport(dllName)]
        public static extern int libusb_detach_kernel_driver(libusb_device_handle devHandle, int interfaceNumber);

        [DllImport(dllName)]
        public static extern int libusb_attach_kernel_driver(libusb_device_handle devHandle, int interfaceNumber);

        [DllImport(dllName)]
        public static extern int libusb_set_auto_detach_kernel_driver(libusb_device_handle devHandle, int enable);

        //[DllImport(dllName)]
        //public static extern Transferlibusb_alloc_transferAllocTransfer(int isoPackets);

        //[DllImport(dllName)]
        //public static extern int libusb_submit_transfer(Transfer* transfer);

        //[DllImport(dllName)]
        //public static extern int libusb_cancel_transfer(Transfer* transfer);

        //[DllImport(dllName)]
        //public static extern void libusb_free_transfer(Transfer* transfer);

        //[DllImport(dllName)]
        //public static extern void libusb_transfer_set_stream_id(Transfer* transfer, uint streamId);

        //[DllImport(dllName)]
        //public static extern uint libusb_transfer_get_stream_id(Transfer* transfer);

        [DllImport(dllName)]
        public static extern int libusb_control_transfer(libusb_device_handle devHandle, byte requestType, byte brequest, ushort wvalue, ushort windex, IntPtr data, ushort wlength, uint timeout);

        //[DllImport(dllName)]
        //public static extern int libusb_bulk_transfer(libusb_device_handle devHandle, byte endpoint, byte* data, int length, ref int actualLength, uint timeout);

        //[DllImport(dllName)]
        //public static extern int libusb_interrupt_transfer(libusb_device_handle devHandle, byte endpoint, byte* data, int length, ref int actualLength, uint timeout);

        //[DllImport(dllName)]
        //public static extern int libusb_get_string_descriptor_ascii(libusb_device_handle devHandle, byte descIndex, byte* data, int length);

        [DllImport(dllName)]
        public static extern int libusb_try_lock_events(libusb_context ctx);

        [DllImport(dllName)]
        public static extern void libusb_lock_events(libusb_context ctx);

        [DllImport(dllName)]
        public static extern void libusb_unlock_events(libusb_context ctx);

        [DllImport(dllName)]
        public static extern int libusb_event_handling_ok(libusb_context ctx);

        [DllImport(dllName)]
        public static extern int libusb_event_handler_active(libusb_context ctx);

        [DllImport(dllName)]
        public static extern void libusb_interrupt_event_handler(libusb_context ctx);

        [DllImport(dllName)]
        public static extern void libusb_lock_event_waiters(libusb_context ctx);

        [DllImport(dllName)]
        public static extern void libusb_unlock_event_waiters(libusb_context ctx);

        //[DllImport(dllName)]
        //public static extern int libusb_wait_for_event(libusb_context ctx, ref UnixNativeTimeval tv);

        //[DllImport(dllName)]
        //public static extern int libusb_handle_events_timeout(libusb_context ctx, ref UnixNativeTimeval tv);

        //[DllImport(dllName)]
        //public static extern int libusb_handle_events_timeout_completed(libusb_context ctx, ref UnixNativeTimeval tv, ref int completed);

        [DllImport(dllName)]
        public static extern int libusb_handle_events(libusb_context ctx);

        [DllImport(dllName)]
        public static extern int libusb_handle_events_completed(libusb_context ctx, ref int completed);

        //[DllImport(dllName)]
        //public static extern int libusb_handle_events_locked(libusb_context ctx, ref UnixNativeTimeval tv);

        [DllImport(dllName)]
        public static extern int libusb_pollfds_handle_timeouts(libusb_context ctx);

        //[DllImport(dllName)]
        //public static extern int libusb_get_next_timeout(libusb_context ctx, ref UnixNativeTimeval tv);

        //[DllImport(dllName)]
        //public static extern Pollfdlibusb_get_pollfds* GetPollfds(libusb_context ctx);

        //[DllImport(dllName)]
        //public static extern void libusb_free_pollfds(Pollfd** pollfds);

        [DllImport(dllName)]
        public static extern void libusb_set_pollfd_notifiers(libusb_context ctx, IntPtr addedDelegate, IntPtr removedDelegate, IntPtr userData);

        //[DllImport(dllName)]
        //public static extern int libusb_hotplug_register_callback(libusb_context ctx, HotplugEvent events, HotplugFlag flags, int vendorId, int productId, int devClass, IntPtr Delegate, IntPtr userData, ref int callbackHandle);

        [DllImport(dllName)]
        public static extern void libusb_hotplug_deregister_callback(libusb_context ctx, int callbackHandle);
    }
}
