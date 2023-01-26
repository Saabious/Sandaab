using Microsoft.Win32;
using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;
using System.Collections.ObjectModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Devices.Usb;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using static Sandaab.Core.Constantes.COMExceptionHResults;
using static Sandaab.WindowsApp.Win32.BthProps;
using static Sandaab.WindowsApp.Win32.Kernel32;

namespace Sandaab.WindowsApp.Components
{
    using DWORD = UInt32;

    internal class WindowsNetwork : Core.Components.Network, IDisposable
    {
        private readonly IntPtr NULL = IntPtr.Zero;

        const string GUID_DEVINTERFACE_ANDROID = "f72fe0d4-cbcb-407d-8814-9ed673d0dd6b";
        //const string GUID_DEVINTERFACE_USB_DEVICE = "a5dcbf10-6530-11d2-901f-00c04fb951ed";

        private record BtListener
        {
            public StreamSocketListener Listener;
            public RfcommServiceProvider Provider;
        }

        private readonly Adb _adb;
        private readonly Collection<string> _adbDevices;
        private readonly DeviceWatcher _adbDeviceWatcher;
        private readonly List<Adb.PortMapping> _adbPortMappings;
        //private readonly Dictionary<string, AndroidAccessory> _androidAccessories;
        private readonly DeviceWatcher _btDeviceWatcher;
        private BtListener _btListener;
        private readonly Collection<Radio> _btRadios;

        public WindowsNetwork()
        {
            _adb = new();
            _adbDevices = new();
            _adbPortMappings = new();
            //_androidAccessories = new();
            _btRadios = new();

            _btDeviceWatcher = DeviceInformation.CreateWatcher(BluetoothDevice.GetDeviceSelector());
            _btDeviceWatcher.Added += BtDeviceWatcher_AddedAsync;
            _btDeviceWatcher.Removed += BtDeviceWatcher_RemovedAsync;

            _adbDeviceWatcher = DeviceInformation.CreateWatcher(
                UsbDevice.GetDeviceSelector(Guid.Parse(GUID_DEVINTERFACE_ANDROID)));
            _adbDeviceWatcher.Added += AdbDeviceWatcher_Added;
            _adbDeviceWatcher.Removed += AdbDeviceWatcher_Removed;
        }

        public override void Dispose()
        {
            _btDeviceWatcher.Stop();
            _adbDeviceWatcher.Stop();

            try
            {
                _adb.Dispose();
            }
            catch
            {
            }

            base.Dispose();
        }

        public override void Initialize()
        {
            Task[] tasks = new[]
            {
                InitializeBluetoothNameAsync(),
                InitializeBluetoothAdapterAsync(),
                //InitializeUsbAsync()
            };

            Task.WaitAll(tasks);

            base.Initialize();

            _btDeviceWatcher.Start();
            _adbDeviceWatcher.Start();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs args)
        {
            foreach (var networkType in (NetworkType[])Enum.GetValues(typeof(NetworkType)))
                if (args.Mode == PowerModes.Resume)
                    Network_Changed(networkType, NetworkEvent.Connected);
                else if (args.Mode == PowerModes.Suspend)
                    Network_Changed(networkType, NetworkEvent.Sleep);
        }

        private Task InitializeBluetoothNameAsync()
        {
            var result = BluetoothAdapter.GetDefaultAsync().AsTask<BluetoothAdapter>()
                .ContinueWith
                (
                    task =>
                    {
                        var btfrp = new BLUETOOTH_FIND_RADIO_PARAMS();
                        btfrp.dwSize = Marshal.SizeOf(btfrp);
                        var hFind = BluetoothFindFirstRadio(ref btfrp, out var hRadio);
                        if (hFind != NULL)
                        {
                            var localRadioInfo = new BTH_LOCAL_RADIO_INFO();
                            do
                                if (DeviceIoControl(hRadio, GET_LOCAL_INFO, NULL, 0, ref localRadioInfo, (DWORD)Marshal.SizeOf(localRadioInfo), out var bytesReturned, NULL)
                                    && localRadioInfo.localInfo.address == task.Result.BluetoothAddress)
                                {
                                    ((WindowsLocalDevice)SandaabContext.LocalDevice).MachineName = localRadioInfo.localInfo.name;
                                    break;
                                }
                            while (BluetoothFindNextRadio(hFind, out hRadio));
                        }
                    }
                );

            return result;
        }

        private Task InitializeBluetoothAdapterAsync()
        {
            return DeviceInformation.FindAllAsync(BluetoothAdapter.GetDeviceSelector()).AsTask<DeviceInformationCollection>()
                .ContinueWith
                (
                    async (task) =>
                    {
                        if (task.Result != null)
                        {
                            foreach (var deviceInformation in task.Result)
                            {
                                var bluetoothAdapter = await BluetoothAdapter.FromIdAsync(deviceInformation.Id);

                                var radio = await bluetoothAdapter.GetRadioAsync();
                                radio.StateChanged += BtRadio_StateChanged;

                                _btRadios.Add(radio);
                            }

                            foreach (var radio in _btRadios)
                                if (radio.State == RadioState.On)
                                    BtRadio_StateChanged(radio, null);
                        }
                        BluetoothAvailable = _btRadios.Count > 0;
                    }
                );
        }

        //public Task InitializeUsbAsync()
        //{
        //    return Task.Run
        //        (
        //            () =>
        //            {
        //                //if (_adb.GetDevices(out var devices))
        //                //{

        //                //}

        //                //var aqsFilter = Windows.Devices.Usb.UsbDevice.GetDeviceSelector(Guid.Parse(GUID_DEVINTERFACE_ANDROID));
        //                //var deviceInformations = await DeviceInformation.FindAllAsync(aqsFilter);
        //                //if (deviceInformations != null)
        //                //    foreach (var deviceInformation in deviceInformations)
        //                //    {
        //                //        var match = Regex.Match(deviceInformation.Id, "^\\\\\\\\\\?\\\\USB#VID_" + AndroidAccessory.USB_ACCESSORY_VENDOR_ID.ToString("X4") + "&", RegexOptions.IgnoreCase);
        //                //        if (!match.Granted)
        //                //        {
        //                //            var accessory = new AndroidAccessory();
        //                //            await accessory.SwitchAsync(deviceInformation.Id, deviceInformation.Name);
        //                //        }
        //                //    }

        //                if (false)
        //                {
        //                    const byte ACCESSORY_SUB_CLASS_CODE = 0xFF;

        //                    const ushort USB_ACCESSORY_VENDOR_ID = 0x18D1;
        //                    const ushort USB_ACCESSORY_PRODUCT_ID = 0x2D00;
        //                    const ushort USB_ACCESSORY_ADB_PRODUCT_ID = 0x2D01;
        //                    const ushort USB_AUDIO_PRODUCT_ID = 0x2D02;
        //                    const ushort USB_AUDIO_ADB_PRODUCT_ID = 0x2D03;
        //                    const ushort USB_ACCESSORY_AUDIO_PRODUCT_ID = 0x2D03;
        //                    const ushort USB_ACCESSORY_AUDIO__ADB_PRODUCT_ID = 0x2D03;

        //                    int epInAddr = -1;
        //                    int epOutAddr = -1;

        //                    libusb_init(NULL);

        //                    var versionPtr = libusb_get_version();
        //                    var version = Marshal.PtrToStructure<libusb_version>(versionPtr);

        //                    var count = libusb_get_device_list(NULL, out var deviceList);
        //                    if (count >= 0)
        //                    {
        //                        IntPtr devicePtr = deviceList;
        //                        for (var i = 0; i < count; i++)
        //                        {
        //                            var device = Marshal.PtrToStructure<IntPtr>(devicePtr);
        //                            if (libusb_get_device_descriptor(device, out var deviceDescriptor) == (int)LIBUSB_SUCCESS
        //                                && deviceDescriptor.idVendor == USB_ACCESSORY_VENDOR_ID
        //                                && new[] {
        //                                        USB_ACCESSORY_PRODUCT_ID,
        //                                        USB_ACCESSORY_ADB_PRODUCT_ID,
        //                                        USB_AUDIO_PRODUCT_ID,
        //                                        USB_AUDIO_ADB_PRODUCT_ID,
        //                                        USB_ACCESSORY_AUDIO_PRODUCT_ID,
        //                                        USB_ACCESSORY_AUDIO__ADB_PRODUCT_ID }.Contains(deviceDescriptor.idProduct)
        //                                && libusb_open(device, out var deviceHandle) == (int)LIBUSB_SUCCESS
        //                                && libusb_get_active_config_descriptor(device, out var configDescriptorPtr) == (int)LIBUSB_SUCCESS)
        //                            {
        //                                var configDescriptor = Marshal.PtrToStructure<libusb_config_descriptor>(configDescriptorPtr);
        //                                var interfacePtr = configDescriptor.interfacePtr;
        //                                for (var j = 0; j < configDescriptor.bNumInterfaces; j++)
        //                                {
        //                                    var interface_ = Marshal.PtrToStructure<libusb_interface>(interfacePtr);
        //                                    var interfaceDescriptorPtr = interface_.altsetting;
        //                                    for (var k = 0; k < interface_.num_altsetting; k++)
        //                                    {
        //                                        var interfaceDescriptor = Marshal.PtrToStructure<libusb_interface_descriptor>(interfaceDescriptorPtr);
        //                                        if (interfaceDescriptor.bInterfaceClass == LIBUSB_CLASS_VENDOR_SPEC
        //                                            && interfaceDescriptor.bInterfaceSubClass == ACCESSORY_SUB_CLASS_CODE)
        //                                        {
        //                                            IntPtr endpointDescriptorPtr = interfaceDescriptor.endpointPtr;
        //                                            for (var l = 0; l < interfaceDescriptor.bNumEndpoints; l++)
        //                                            {
        //                                                var endpointDescriptor = Marshal.PtrToStructure<libusb_endpoint_descriptor>(endpointDescriptorPtr);
        //                                                if ((endpointDescriptor.bmAttributes & LIBUSB_TRANSFER_TYPE_MASK) == LIBUSB_TRANSFER_TYPE_BULK)
        //                                                    if ((endpointDescriptor.bEndpointAddress & LIBUSB_ENDPOINT_IN) == LIBUSB_ENDPOINT_IN)
        //                                                        epInAddr = endpointDescriptor.bEndpointAddress;
        //                                                    else
        //                                                        epOutAddr = endpointDescriptor.bEndpointAddress;

        //                                                endpointDescriptorPtr = IntPtr.Add(endpointDescriptorPtr, Marshal.SizeOf(endpointDescriptor));
        //                                            }
        //                                        }

        //                                        interfaceDescriptorPtr = IntPtr.Add(interface_.altsetting, Marshal.SizeOf(interfaceDescriptorPtr));
        //                                    }

        //                                    interfacePtr = IntPtr.Add(configDescriptor.interfacePtr, Marshal.SizeOf(interface_));
        //                                }
        //                            }

        //                            devicePtr = IntPtr.Add(devicePtr, Marshal.SizeOf(devicePtr));
        //                        }

        //                        libusb_free_device_list(deviceList, 0);
        //                    }
        //                }
        //            }
        //        );
        //}

        private async void BtDeviceWatcher_AddedAsync(DeviceWatcher sender, DeviceInformation args)
        {
            if (args.IsEnabled)
                try
                {
                    var device = await BluetoothDevice.FromIdAsync(args.Id);
                    var rfcommServices = await device.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort, BluetoothCacheMode.Uncached);
                    if (rfcommServices.Error != BluetoothError.Success)
                        Logger.Warn(string.Format(Locale.BluetoothRfcommServicesNotReceived, device.Name, rfcommServices.Error.ToString()));
                    else
                        Network_Changed(NetworkType.Bluetooth, NetworkEvent.Connected, new BtEndPoint(device.BluetoothAddress));
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
        }

        private async void BtDeviceWatcher_RemovedAsync(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            try
            {
                var device = await BluetoothDevice.FromIdAsync(args.Id);
                Network_Changed(NetworkType.Bluetooth, NetworkEvent.Disconnected, new BtEndPoint(device.BluetoothAddress));
            }
            catch
            {
            }
        }

        private void BtRadio_StateChanged(Radio radio, object args)
        {
            if (radio.State == RadioState.On)
            {
                StopBtListener();
                _ = StartBtListenerAsync();
            }
        }

        protected override NetworkResult BtConnect(BtEndPoint remoteEndPoint, out Connection connection)
        {
            NetworkResult networkResult = NetworkResult.UnknownRemoteEndPoint;
            connection = null;

            BluetoothDevice device = null;

            var deviceInformations = DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelector()).AsTask().Result;
            if (deviceInformations != null)
                foreach (var deviceInformation in deviceInformations)
                    try
                    {
                        device = BluetoothDevice.FromIdAsync(deviceInformation.Id).AsTask().Result;
                        if (device.BluetoothAddress.Equals(remoteEndPoint.BluetoothAddress))
                        {
                            var rfcommServices =  device.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort, BluetoothCacheMode.Uncached).AsTask().Result;
                            if (rfcommServices.Error != BluetoothError.Success)
                                Logger.Warn(string.Format(Locale.BluetoothRfcommServicesNotReceived, device.Name, rfcommServices.Error.ToString()));
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

            if (device != null)
            {
                deviceInformations = DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort)).AsTask().Result;
                if (deviceInformations != null)
                    foreach (var rfcommServiceDeviceInformation in deviceInformations)
                        if (rfcommServiceDeviceInformation.Name == Config.AppName)
                        {
                            var socket = new StreamSocket();

                            var rfcommDeviceService = RfcommDeviceService.FromIdAsync(rfcommServiceDeviceInformation.Id).AsTask().Result;

                            try
                            {
                                socket.ConnectAsync(rfcommDeviceService.ConnectionHostName, rfcommDeviceService.ConnectionServiceName, SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication).AsTask().Wait();
                                networkResult = NetworkResult.Success;
                            }
                            catch (COMException e) when (new COMExceptionHResults[] { WSASERVICE_NOT_FOUND, WSAEADDRINUSE, WSAEHOSTDOWN }.Contains((COMExceptionHResults)e.HResult))
                            {
                                Logger.Warn(string.Format(Messages.ConnectFailure, new BtEndPoint(rfcommDeviceService.Device.BluetoothAddress).ToString(), (COMExceptionHResults)e.HResult));
                                networkResult = NetworkResult.RemoteEndPointInUse;
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                                networkResult = NetworkResult.ConnectFailure;
                            }

                            if (networkResult == NetworkResult.Success)
                            {
                                var localEndPoint = new BtEndPoint(BluetoothAddress(socket.Information.LocalAddress));
                                connection = new Connection(true, localEndPoint, remoteEndPoint, socket.InputStream.AsStreamForRead(), socket.OutputStream.AsStreamForWrite());

                                return connection.OpenAsync().Result;
                            }
                        }
            }

            return networkResult;
        }

        private ulong BluetoothAddress(Windows.Networking.HostName hostName)
        {
            return ulong.Parse(hostName.ToString().Replace(":", "").Replace("(", "").Replace(")", ""), System.Globalization.NumberStyles.HexNumber);
        }

        protected async override Task StartBtListenerAsync()
        {
            if (!BluetoothEnabled || _btListener != null)
                return;

            _btListener = new();

            try
            { 
                _btListener.Provider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.SerialPort);

                _btListener.Listener = new StreamSocketListener();
                _btListener.Listener.ConnectionReceived += BtConnection_ReceivedEvent;

                await _btListener.Listener.BindServiceNameAsync(_btListener.Provider.ServiceId.AsString(),
                    SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

                InitializeServiceSdpAttributes(_btListener.Provider);

                _btListener.Provider.StartAdvertising(_btListener.Listener);
            }
            catch (COMException e) when (e.HResult == (int)ERROR_DEVICE_NOT_AVAILABLE)
            {
                Logger.Warn(Locale.BluetoothAdapterNotEnabled);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void StopBtListener()
        {
            _btListener = null;
        }

        private void InitializeServiceSdpAttributes(RfcommServiceProvider rfcommProvider)
        {
            // Set the Service Discovery Protocol attributes and start Bluetooth advertising

            const byte SERVICE_VERSION_ATTRIBUTE_TYPE = (4 << 3) | 5;
            const uint SERVICE_VERSION_ATTRIBUTE_ID = 0x100;
            string SdpServiceName = Config.AppName;

            var sdpWriter = new DataWriter();

            // Write the Service Name Attribute.
            sdpWriter.WriteByte(SERVICE_VERSION_ATTRIBUTE_TYPE);

            // The length of the UTF-8 encoded Service Name SDP Attribute.
            sdpWriter.WriteByte((byte)SdpServiceName.Length);

            // The UTF-8 encoded Service Name value.
            sdpWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            sdpWriter.WriteString(SdpServiceName);

            // Set the SDP Attribute on the RFCOMM Service Provider.
            rfcommProvider.SdpRawAttributes.Add(SERVICE_VERSION_ATTRIBUTE_ID, sdpWriter.DetachBuffer());
        }

        private void BtConnection_ReceivedEvent(StreamSocketListener socketListener, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            Logger.Debug("Incoming Bluetooth connection");

            try
            {
                var localEndPoint = new BtEndPoint(BluetoothAddress(args.Socket.Information.LocalAddress));
                var remoteEndPoint = new BtEndPoint(BluetoothAddress(args.Socket.Information.RemoteAddress));
                var connection = new Connection(false, localEndPoint, remoteEndPoint, args.Socket.InputStream.AsStreamForRead(), args.Socket.OutputStream.AsStreamForWrite());

                _ = connection.OpenAsync();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected override void SendBtMulticastAsync(Command command)
        {
            Task.Run(
                async () =>
                {
                    var deviceInformations = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelector());
                    if (deviceInformations != null)
                        foreach (var deviceInformation in deviceInformations)
                            try
                            {
                                var btDevice = await BluetoothDevice.FromIdAsync(deviceInformation.Id);
                                if (btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                                    _ = SendAsync(command, new BtEndPoint(btDevice.BluetoothAddress));
                            }
                            catch (COMException e) when (e.HResult == (int)ERROR_DEVICE_NOT_AVAILABLE)
                            {
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }
                });
        }

        private void AdbDeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            lock (_adbDevices)
                _adbDevices.Add(args.Id);

            Network_Changed(NetworkType.Adb, NetworkEvent.Connected);
        }

        private void AdbDeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            lock (_adbDevices)
                _adbDevices.Remove(args.Id);

            Network_Changed(NetworkType.Adb, NetworkEvent.Disconnected);
        }

        public override void ChangeLocalDnsEntry(string hostname, Collection<IPAddress> addresses = null)
        {
            try
            {
                FileStream fileStream = File.Open("%windir%\\system32\\drivers\\etc\\hosts", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                var streamReader = new StreamReader(fileStream);
                Collection<string> allLines = new();
                string streamLine;
                while ((streamLine = streamReader.ReadLine()) != null)
                    allLines.Add(streamLine);

                for (int i = allLines.Count - 1; i >= 0; i--)
                    if (Regex.Match(allLines[i], "\\b" + hostname + "\\b(#.*)?$").Success)
                        allLines.Remove(allLines[i]);

                if (addresses != null)
                    foreach (IPAddress address in addresses)
                        allLines.Add(address.ToString() + " " + hostname + " # added temporarily by " + Config.AppName);

                fileStream.SetLength(0);
                var streamWriter = new StreamWriter(fileStream);
                foreach (string line in allLines)
                    streamWriter.Write(line);
                fileStream.Close();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected override void SendAdbMulticastAsync(Command command)
        {
            lock (_adbDevices)
                if (_adbDevices.Count > 0)
                    lock (_adbPortMappings)
                        foreach (var adpPortMapping in _adbPortMappings)
                            _ = SendAsync(command, new AdbEndPoint(adpPortMapping.LocalPort));
        }

        public override void Network_Changed(NetworkType network, NetworkEvent change, EndPoint remoteEndPoint = null, string deviceId = null)
        {
            Network_Changed(network, change, remoteEndPoint, deviceId, 1);
        }

        private void Network_Changed(NetworkType network, NetworkEvent change, EndPoint remoteEndPoint, string deviceId, int tries)
        {
            if (network == NetworkType.Adb)
            {
                _adb.UpdatePortMappingsAsync()
                    .ContinueWith(
                        (task) =>
                        {
                            if (task.IsFaulted)
                                switch (_adb.LastErrorCode)
                                {
                                    case Adb.ErrorCode.Unauthorized:
                                        SandaabContext.Notifications.ShowNetworkMessage(Locale.MessageBoxTitleError, Locale.AdbAuthorization);
                                        break;
                                    case Adb.ErrorCode.Offline:
                                        SandaabContext.Notifications.ShowNetworkMessage(Locale.MessageBoxTitleError, Locale.AdbOffline);
                                        break;
                                    case Adb.ErrorCode.NoDevices:
                                        lock (_adbDevices)
                                            lock (_adbPortMappings)
                                            {
                                                _adbDevices.Clear();
                                                _adbPortMappings.Clear();
                                            }
                                        SandaabContext.Notifications.ShowNetworkMessage(Locale.MessageBoxTitleError, Locale.AdbOffline);
                                        break;
                                    default:
                                        throw task.Exception;
                                }
                            else
                            {
                                lock (_adbPortMappings)
                                {
                                    foreach (var oldAdbPortMapping in _adbPortMappings)
                                    {
                                        var found = false;
                                        foreach (var newAdbPortMapping in task.Result)
                                            found |= oldAdbPortMapping == newAdbPortMapping;
                                        if (!found)
                                            base.Network_Changed(NetworkType.Adb, NetworkEvent.Disconnected, new AdbEndPoint(oldAdbPortMapping.LocalPort));
                                    }

                                    Collection<Adb.PortMapping> newAdbPortMappings = new();
                                    foreach (var newAdbPortMapping in task.Result)
                                    {
                                        var found = false;
                                        foreach (var oldAdbPortMapping in _adbPortMappings)
                                            found |= oldAdbPortMapping == newAdbPortMapping;
                                        if (!found)
                                            newAdbPortMappings.Add(newAdbPortMapping);
                                    }

                                    _adbPortMappings.Clear();
                                    _adbPortMappings.AddRange(task.Result);

                                    foreach (var newAdbPortMapping in newAdbPortMappings)
                                        base.Network_Changed(NetworkType.Adb, NetworkEvent.Connected, new AdbEndPoint(newAdbPortMapping.LocalPort));
                                }
                            }
                        });
            }
            else
                base.Network_Changed(network, change, remoteEndPoint);
        }
    }
}
