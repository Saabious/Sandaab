namespace Sandaab.WindowsApp.Android
{
    internal class UsbHost
    { 
        public const int DEV_DESCR_LEN = 18;
        public const int CONF_DESCR_LEN = 9;
        public const int INTR_DESCR_LEN = 9;
        public const int EP_DESCR_LEN = 7;

        public const int USB_REQUEST_GET_STATUS = 0;
        public const int USB_REQUEST_CLEAR_FEATURE = 1;
        public const int USB_REQUEST_SET_FEATURE = 3;
        public const int USB_REQUEST_SET_ADDRESS = 5;
        public const int USB_REQUEST_GET_DESCRIPTOR = 6;
        public const int USB_REQUEST_SET_DESCRIPTOR = 7;
        public const int USB_REQUEST_GET_CONFIGURATION = 8;
        public const int USB_REQUEST_SET_CONFIGURATION = 9;
        public const int USB_REQUEST_GET_INTERFACE = 10;
        public const int USB_REQUEST_SET_INTERFACE = 11;
        public const int USB_REQUEST_SYNCH_FRAME = 12;
        public const int USB_FEATURE_ENDPOINT_HALT = 0;
        public const int USB_FEATURE_DEVICE_REMOTE_WAKEUP = 1;
        public const int USB_FEATURE_TEST_MODE = 2;

        public const int USB_SETUP_HOST_TO_DEVICE = 0x00;
        public const int USB_SETUP_DEVICE_TO_HOST = 0x80;

        public const int USB_SETUP_TYPE_STANDARD = 0x00;
        public const int USB_SETUP_TYPE_CLASS = 0x20;
        public const int USB_SETUP_TYPE_VENDOR = 0x40;

        public const int USB_SETUP_RECIPIENT_DEVICE = 0x00;
        public const int USB_SETUP_RECIPIENT_INTERFACE = 0x01;
        public const int USB_SETUP_RECIPIENT_ENDPOINT = 0x02;
        public const int USB_SETUP_RECIPIENT_OTHER = 0x03;

        public const int USB_DESCRIPTOR_DEVICE = 0x01;
        public const int USB_DESCRIPTOR_CONFIGURATION = 0x02;
        public const int USB_DESCRIPTOR_STRING = 0x03;
        public const int USB_DESCRIPTOR_INTERFACE = 0x04;
        public const int USB_DESCRIPTOR_ENDPOINT = 0x05;
        public const int USB_DESCRIPTOR_DEVICE_QUALIFIER = 0x06;
        public const int USB_DESCRIPTOR_OTHER_SPEED = 0x07;
        public const int USB_DESCRIPTOR_INTERFACE_POWER = 0x08;
        public const int USB_DESCRIPTOR_OTG = 0x09;
        public const int HID_DESCRIPTOR_HID = 0x21;

        public const int OTG_FEATURE_B_HNP_ENABLE = 3;
        public const int OTG_FEATURE_A_HNP_SUPPORT = 4;
        public const int OTG_FEATURE_A_ALT_HNP_SUPPORT = 5;

        public const int USB_TRANSFER_TYPE_CONTROL = 0x00;
        public const int USB_TRANSFER_TYPE_ISOCHRONOUS = 0x01;
        public const int USB_TRANSFER_TYPE_BULK = 0x02;
        public const int USB_TRANSFER_TYPE_INTERRUPT = 0x03;

        public const int bmUSB_TRANSFER_TYPE = 0x03;
        public const int USB_FEATURE_ENDPOINT_STALL = 0;
        public const int bmREQ_GET_DESCR = USB_SETUP_DEVICE_TO_HOST | USB_SETUP_TYPE_STANDARD | USB_SETUP_RECIPIENT_DEVICE;
        public const int bmREQ_SET = USB_SETUP_HOST_TO_DEVICE|USB_SETUP_TYPE_STANDARD|USB_SETUP_RECIPIENT_DEVICE;
        public const int bmREQ_CL_GET_INTF = USB_SETUP_DEVICE_TO_HOST|USB_SETUP_TYPE_CLASS|USB_SETUP_RECIPIENT_INTERFACE;
        public const int USB_CLASS_USE_CLASS_INFO = 0x00;
        public const int USB_CLASS_AUDIO = 0x01;
        public const int USB_CLASS_COM_AND_CDC_CTRL = 0x02;
        public const int USB_CLASS_HID = 0x03;
        public const int USB_CLASS_PHYSICAL = 0x05;
        public const int USB_CLASS_IMAGE = 0x06;
        public const int USB_CLASS_PRINTER = 0x07;
        public const int USB_CLASS_MASS_STORAGE = 0x08;
        public const int USB_CLASS_HUB = 0x09;
        public const int USB_CLASS_CDC_DATA = 0x0a;
        public const int USB_CLASS_SMART_CARD = 0x0b;
        public const int USB_CLASS_CONTENT_SECURITY = 0x0d;
        public const int USB_CLASS_VIDEO = 0x0e;
        public const int USB_CLASS_PERSONAL_HEALTH = 0x0f;
        public const int USB_CLASS_DIAGNOSTIC_DEVICE = 0xdc;
        public const int USB_CLASS_WIRELESS_CTRL = 0xe0;
        public const int USB_CLASS_MISC = 0xef;
        public const int USB_CLASS_APP_SPECIFIC = 0xfe;
        public const int USB_CLASS_VENDOR_SPECIFIC = 0xff;
        public const int USB_DEV_CONFIG_ERROR_DEVICE_NOT_SUPPORTED = 0xD1;
        public const int USB_DEV_CONFIG_ERROR_DEVICE_INIT_INCOMPLETE = 0xD2;
        public const int USB_ERROR_UNABLE_TO_REGISTER_DEVICE_CLASS = 0xD3;
        public const int USB_ERROR_OUT_OF_ADDRESS_SPACE_IN_POOL = 0xD4;
        public const int USB_ERROR_HUB_ADDRESS_OVERFLOW = 0xD5;
        public const int USB_ERROR_ADDRESS_NOT_FOUND_IN_POOL = 0xD6;
        public const int USB_ERROR_EPINFO_IS_NULL = 0xD7;
        public const int USB_ERROR_INVALID_ARGUMENT = 0xD8;
        public const int USB_ERROR_CLASS_INSTANCE_ALREADY_IN_USE = 0xD9;
        public const int USB_ERROR_INVALID_MAX_PKT_SIZE = 0xDA;
        public const int USB_ERROR_EP_NOT_FOUND_IN_TBL = 0xDB;
        public const int USB_ERROR_CONFIG_REQUIRES_ADDITIONAL_RESET = 0xE0;
        public const int USB_ERROR_FailGetDevDescr = 0xE1;
        public const int USB_ERROR_FailSetDevTblEntry = 0xE2;
        public const int USB_ERROR_FailGetConfDescr = 0xE3;
        public const int USB_ERROR_TRANSFER_TIMEOUT = 0xFF;
        public const int USB_XFER_TIMEOUT = 5000;
        public const int USB_RETRY_LIMIT = 3;
        public const int USB_SETTLE_DELAY = 200;
        public const int USB_NUMDEVICES = 16;
        public const int HUB_PORT_RESET_DELAY = 20;
        public const int USB_STATE_MASK = 0xf0;
        public const int USB_STATE_DETACHED = 0x10;
        public const int USB_DETACHED_SUBSTATE_INITIALIZE = 0x11;
        public const int USB_DETACHED_SUBSTATE_WAIT_FOR_DEVICE = 0x12;
        public const int USB_DETACHED_SUBSTATE_ILLEGAL = 0x13;
        public const int USB_ATTACHED_SUBSTATE_SETTLE = 0x20;
        public const int USB_ATTACHED_SUBSTATE_RESET_DEVICE = 0x30;
        public const int USB_ATTACHED_SUBSTATE_WAIT_RESET_COMPLETE = 0x40;
        public const int USB_ATTACHED_SUBSTATE_WAIT_SOF = 0x50;
        public const int USB_ATTACHED_SUBSTATE_WAIT_RESET = 0x51;
        public const int USB_ATTACHED_SUBSTATE_GET_DEVICE_DESCRIPTOR_SIZE = 0x60;
        public const int USB_STATE_ADDRESSING = 0x70;
        public const int USB_STATE_CONFIGURING = 0x80;
        public const int USB_STATE_RUNNING = 0x90;
        public const int USB_STATE_ERROR = 0xa0;
    }
}