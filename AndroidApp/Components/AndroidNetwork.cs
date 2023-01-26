using Android;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Java.Lang;
using Java.Util;
using Sandaab.AndroidApp.Activities;
using Sandaab.AndroidApp.Properties;
using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;
using System.Net;
using static Sandaab.Core.Constantes.HResults;
using Exception = System.Exception;
using Intent = Android.Content.Intent;

namespace Sandaab.AndroidApp.Components
{
    internal class AndroidNetwork : Network, IBroadcastReceiver
    {
        const string GUID_RFCOMM_SERIALPORT = "00001101-0000-1000-8000-00805f9b34fb";
        const string ACTION_USB_PERMISSION = "com.sandaab.permission.network.USB";

        private record BtListener
        {
            public BluetoothServerSocket ServerSocket;
            public CancellationTokenSource Source;
            public Task Task;
        }

        private readonly EndPoint _adbEndPoint;
        private BluetoothAdapter _btAdapter;
        private BtListener _btListener;
        //private readonly UsbManager _usbManager;
        private bool AdbEnabled { get { return GetAdbEnabled(); } }

        public AndroidNetwork()
            : base()
        {
            _adbEndPoint = new AdbEndPoint(Config.AdbPort);
        }

        public override void Initialize()
        {
            var intentFilter = new IntentFilter();
            intentFilter.AddAction(Intent.ActionPowerConnected);
            intentFilter.AddAction(Intent.ActionPowerDisconnected);

            if (MainActivity.Instance.CheckSelfPermission(Manifest.Permission.Bluetooth) == Permission.Granted
                && (Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.S || MainActivity.Instance.CheckSelfPermission(Manifest.Permission.BluetoothAdvertise) == Permission.Granted)
                && (Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.S || MainActivity.Instance.CheckSelfPermission(Manifest.Permission.BluetoothConnect) == Permission.Granted))
            {
                var bluetoothManager = (BluetoothManager)MainActivity.Instance.GetSystemService(Android.Content.Context.BluetoothService);
                if (bluetoothManager != null)
                {
                    _btAdapter = bluetoothManager.Adapter; // The Android API supports one Bluetooth adapter only
                    if (_btAdapter != null)
                    {
                        intentFilter.AddAction(BluetoothAdapter.ActionStateChanged);
                        intentFilter.AddAction(BluetoothDevice.ActionAclConnected);
                        intentFilter.AddAction(BluetoothDevice.ActionAclDisconnected);

                        if (_btAdapter.Name != null)
                            ((AndroidLocalDevice)Core.SandaabContext.LocalDevice).MachineName = _btAdapter.Name;

                        BluetoothAvailable = true;
                        BluetoothEnabled = _btAdapter.IsEnabled;
                    }
                }
            }

            //_usbManager = (UsbManager)MainActivity._instance.GetSystemService(Context.UsbService);
            //if (_usbManager != null)
            //{
            //    var packageManager = MainActivity._instance.PackageManager;
            //    if (packageManager != null
            //        && packageManager.HasSystemFeature(PackageManager.FeatureUsbAccessory))
            //    {
            //        intentFilter.AddAction(UsbManager.ActionUsbAccessoryAttached);
            //        intentFilter.AddAction(UsbManager.ActionUsbAccessoryDetached);
            //        intentFilter.AddAction(UsbManager.ExtraAccessory);
            //        intentFilter.AddAction(ACTION_USB_PERMISSION);

            //        var accessoryList = _usbManager.GetAccessoryList();
            //        if (accessoryList != null)
            //            foreach (var accessory in accessoryList)
            //                OpenAccessory(accessory);
            //    }
            //}

            MainActivity.Instance.RegisterReceiver(new BroadcastReceiver(this), intentFilter);

            base.Initialize();
        }

        //private void OpenAccessory(UsbAccessory accessory)
        //{
        //    if (!_usbManager.HasPermission(accessory))
        //    {
        //        var pendingIntent = PendingIntent.GetBroadcast(MainActivity._instance, 0, new(ACTION_USB_PERMISSION), 0);
        //        _usbManager.RequestPermission(accessory, pendingIntent);
        //        return;
        //    }

        //    var fileDescriptor = _usbManager.OpenAccessory(accessory);
        //    if (fileDescriptor == null)
        //        throw new Exception("Cannot open accessory");

        //    var fd = fileDescriptor.FileDescriptor;

        //    var accessoryReceiver = new AccessoryReceiver()
        //    {
        //        FileInputStream = new FileInputStream(fileDescriptor.FileDescriptor)
        //    };
        //    var task = new Task(new(AccessoryReceiverTask), accessoryReceiver);
        //    task.Start();

        //    var accessorySender = new AccessorySender()
        //    {
        //        FileOutputStream = new FileOutputStream(fileDescriptor.FileDescriptor)
        //    };
        //    task = new(new(AccessorySenderTask), accessorySender);
        //    task.Start();
        //}

        //private record AccessoryReceiver
        //{
        //    public Connection connection;
        //    public FileInputStream FileInputStream;
        //}

        //private record AccessorySender
        //{
        //    public Connection connection;
        //    public FileOutputStream FileOutputStream;
        //}

        //private void AccessoryReceiverTask(object state)
        //{
        //    const int BUFFER_SIZE = 32768;

        //    var accessoryReceiver = state as AccessoryReceiver;

        //    byte[] buffer = new byte[BUFFER_SIZE];

        //    while (true)
        //    {
        //        var length = accessoryReceiver.FileInputStream.Read(buffer);
        //        MessageDialog.Show(MainActivity._instance, length.ToString() + " bytes received!");
        //        accessoryReceiver.connection.InputStream.Write(buffer, 0, length);
        //    }
        //}

        //private void AccessorySenderTask(object state)
        //{
        //    //const int BUFFER_SIZE = 32768;

        //    //var accessorySender = state as AccessorySender;

        //    //byte[] buffer = new byte[BUFFER_SIZE];

        //    //buffer = Encoding.UTF8.GetBytes("Hallo");
        //    //accessorySender.FileOutputStream.Write(buffer, 0, buffer.Length);

        //    //return;

        //    //while (true)
        //    //{
        //    //    var length = accessorySender.connection.OutputStream.Read(buffer, 0, buffer.Length);
        //    //    accessorySender.FileOutputStream.Write(buffer, 0, length);
        //    //}
        //}

        public void IntentReceived(Android.Content.Context context, Intent intent)
        {
            switch (intent.Action)
            {
                case BluetoothAdapter.ActionStateChanged:
                    OnBluetoothChanged(intent);
                    break;
                case BluetoothDevice.ActionAclConnected:
                    OnBluetoothAclConnected(intent);
                    break;
                case BluetoothDevice.ActionAclDisconnected:
                    OnBluetoothAclDisconnected(intent);
                    break;
                //case UsbManager.ActionUsbAccessoryAttached:
                //    MessageDialog.Show(MainActivity._instance, "ActionUsbAccessoryAttached");
                //    break;
                //case UsbManager.ActionUsbAccessoryDetached:
                //    MessageDialog.Show(MainActivity._instance, "ActionUsbAccessoryDetached");
                //    break;
                //case UsbManager.ExtraAccessory:
                //    MessageDialog.Show(MainActivity._instance, "ExtraAccessory");
                //    break;
                //case ACTION_USB_PERMISSION:
                //    break;
                case Intent.ActionPowerConnected:
                    OnPowerConnected();
                    break;
                case Intent.ActionPowerDisconnected:
                    OnPowerDisconnected();
                    break;
            }
        }

        private void OnBluetoothChanged(Intent intent)
        {
            const int STATE_OFF = 10;
            const int STATE_ON = 12;

            if (intent.Extras.GetInt(BluetoothAdapter.ExtraState) == STATE_ON)
                Network_Changed(NetworkType.Bluetooth, NetworkEvent.Connected);
            else if (intent.Extras.GetInt(BluetoothAdapter.ExtraState) == STATE_OFF)
                Network_Changed(NetworkType.Bluetooth, NetworkEvent.Disconnected);
        }

        private void OnBluetoothAclConnected(Intent intent)
        {
            OnBluetoothAclChanged(intent, NetworkEvent.Connected);
        }

        private void OnBluetoothAclDisconnected(Intent intent)
        {
            OnBluetoothAclChanged(intent, NetworkEvent.Disconnected);
        }

        private void OnBluetoothAclChanged(Intent intent, NetworkEvent change)
        {
            BluetoothDevice btDevice
                = Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Tiramisu
#pragma warning disable CS0618 // Typ oder Element ist veraltet
                ? intent.Extras.GetParcelable(BluetoothDevice.ExtraDevice) as BluetoothDevice
#pragma warning restore CS0618 // Typ oder Element ist veraltet
#pragma warning disable CA1416 // Plattformkompatibilität überprüfen
                : intent.Extras.GetParcelable(BluetoothDevice.ExtraDevice, Class.FromType(typeof(BluetoothDevice))) as BluetoothDevice;
#pragma warning restore CA1416 // Plattformkompatibilität überprüfen
            Network_Changed(NetworkType.Bluetooth, change, new BtEndPoint(btDevice.Address));
        }

        protected override Task StartBtListenerAsync()
        {
            if (_btListener == null)
            {
                BluetoothServerSocket serverSocket;
                try
                {
                    serverSocket = _btAdapter.ListenUsingRfcommWithServiceRecord(Config.AppName, UUID.FromString(GUID_RFCOMM_SERIALPORT));
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    return Task.CompletedTask;
                }

                var source = new CancellationTokenSource();
                _btListener = new()
                {
                    ServerSocket = serverSocket,
                    Source = source,
                    Task = new(new(BtListenerTask), serverSocket, source.Token)
                };
                _btListener.Task.Start();
            }

            return Task.CompletedTask;
        }

        protected override NetworkResult BtConnect(BtEndPoint remoteEndPoint, out Connection connection)
        {
            NetworkResult networkResult = NetworkResult.UnknownRemoteEndPoint;
            connection = null;

            foreach (var remoteDevice in _btAdapter.BondedDevices)
                if (remoteDevice.Address.Equals(remoteEndPoint.Address))
                {
                    BluetoothSocket socket = remoteDevice.CreateRfcommSocketToServiceRecord(UUID.FromString(GUID_RFCOMM_SERIALPORT));

                    try
                    {
                        socket.Connect();
                        networkResult = NetworkResult.Success;
                    }
                    catch (Java.IO.IOException e) when (e.HResult == (int)COR_E_EXCEPTION) // Happened if Bluetooth-Adapter is not enabled or if there is no listener
                    {
                        Logger.Warn(string.Format(Messages.ConnectFailure, remoteEndPoint, (HResults)e.HResult));
                        networkResult = NetworkResult.ConnectFailure;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        networkResult = NetworkResult.ConnectFailure;
                    }

                    if (networkResult == NetworkResult.Success)
                    {
                        connection = new Connection(true, null, remoteEndPoint, socket.InputStream, socket.OutputStream);

                        return connection.OpenAsync().Result;
                    }
                }

            return networkResult;
        }

        private void BtListenerTask(object state)
        {
            var serverSocket = state as BluetoothServerSocket;

            while (!_btListener.Task.IsCanceled)
                try
                {
                    var socket = serverSocket.Accept();

                    var connection = new Connection(false, null, new BtEndPoint(socket.RemoteDevice.Address), socket.InputStream, socket.OutputStream);

                    _ = connection.OpenAsync();
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
        }

        protected override void SendBtMulticastAsync(Command command)
        {
            if (_btAdapter != null)
                foreach (var boundedDevice in _btAdapter.BondedDevices)
                    _ = SendAsync(command, new BtEndPoint(boundedDevice.Address));
        }

        public bool GetAdbEnabled()
        {
            return Settings.Secure.GetInt(MainActivity.Instance.ContentResolver, Settings.Global.AdbEnabled, 0) == 1;
        }

        private void OnPowerConnected()
        {
            Network_Changed(NetworkType.Adb, NetworkEvent.Connected);
        }

        private void OnPowerDisconnected()
        {
            Network_Changed(NetworkType.Adb, NetworkEvent.Disconnected);
        }

        protected override void SendAdbMulticastAsync(Command command)
        {
            if (AdbEnabled)
                _ = SendAsync(command, _adbEndPoint);
        }

        public override void Network_Changed(NetworkType network, NetworkEvent change, EndPoint remoteEndPoint = null, string deviceId = null)
        {
            if (network == NetworkType.Adb)
                remoteEndPoint = _adbEndPoint;

            base.Network_Changed(network, change, remoteEndPoint);
        }
    }
}
