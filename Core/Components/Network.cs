using Newtonsoft.Json;
using Sandaab.Core.Constantes;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using static Sandaab.Core.Constantes.HResults;
using static Sandaab.Core.Constantes.IOExceptionHResults;

namespace Sandaab.Core.Components
{
    public abstract class Network : IDisposable
    {
        public record ConnectionPacket
        {
            public string DeviceId;
            public string Version;
        }

        public record CommandPacket
        {
            [JsonProperty(Required = Required.Always)]
            private string CommandType;
            [JsonProperty(Required = Required.Always)]
            private string CommandJson;
            [JsonIgnore]
            public Command Command
            {
                get { return JsonHelper.Get(CommandType, CommandJson) as Command; }
                set { JsonHelper.Set(value, out CommandType, out CommandJson); }
            }
        }

        public class Connection
        {
            private const int PING_TIMER_INTERVAL = 30000;

            public string DeviceId { get; private set; }
            private readonly bool _initiator;
            public Stream InputStream { get; private set; }
            private bool IsClosed { get { return Source.IsCancellationRequested; } }
            public Stream OutputStream { get; private set; }
            private System.Timers.Timer _pingTimer;
            private EventWaitHandle _pingTimerEvent;
            private byte[] _readBuffer = null;
            public Route Route { get; private set; }
            public CancellationTokenSource Source { get; private set; }

            public Connection(bool initiator, EndPoint localEndPoint, EndPoint remoteEndPoint, Stream inputStream, Stream outputStream)
            {
                _initiator = initiator;
                InputStream = inputStream;
                OutputStream = outputStream;
                Route = new Route(localEndPoint, remoteEndPoint);
                Source = new();
                _pingTimer = new();
                _pingTimer.Elapsed += PingTimer_Elapsed;
                _pingTimer.Interval = PING_TIMER_INTERVAL;
                _pingTimerEvent = new(false, EventResetMode.ManualReset);
            }

            private void PingTimer_Elapsed(object sender, ElapsedEventArgs args)
            {
                if (_initiator)
                {
                    var networkResult = Send(new PingCommand());
                    if (networkResult != NetworkResult.Success)
                        Close(networkResult);
                }
                else
                {
                    lock (_pingTimer)
                        _pingTimerEvent.Reset();
                    if (!_pingTimerEvent.WaitOne(TimeSpan.FromSeconds(5)))
                        Close(NetworkResult.PingTimeout);
                }
            }

            public Task<NetworkResult> OpenAsync()
            {
                return Task.Run(
                    () =>
                    {
                        Logger.Debug("Open connection " + Route.ToString());

                        var welcomePacket = new ConnectionPacket()
                        {
                            DeviceId = SandaabContext.LocalDevice.Id,
                            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                        };
                        var buffer = ConvertFrom(welcomePacket);

                        var networkResult = Write(Config.NetworkIdentifier);
                        if (networkResult == NetworkResult.Success)
                            networkResult = Write(buffer.Length);
                        if (networkResult == NetworkResult.Success)
                            networkResult = Write(buffer, 0, buffer.Length);
                        if (networkResult == NetworkResult.Success)
                            networkResult = Execute(ConnectionAction.Flush);

                        if (networkResult == NetworkResult.Success)
                        {
                            networkResult = Read(out long networkIdentifier);
                            if (networkResult == NetworkResult.Success
                                && networkIdentifier != Config.NetworkIdentifier)
                            {
                                Logger.Warn(string.Format(Messages.InvalidListener, Config.AppName));
                                networkResult = NetworkResult.InvalidRemoteListener;
                            }
                        }
                        if (networkResult == NetworkResult.Success)
                        {
                            networkResult = Read(out int length);
                            if (networkResult == NetworkResult.Success)
                            {
                                buffer = new byte[length];
                                networkResult = Read(buffer, 0, buffer.Length);
                                if (networkResult == NetworkResult.Success)
                                {
                                    welcomePacket = ConvertFrom<ConnectionPacket>(buffer);
                                    DeviceId = welcomePacket.DeviceId;
                                }
                            }
                        }

                        if (networkResult == NetworkResult.Success)
                        {
                            Logger.Debug("Connection opened " + Route.ToString());

                            lock (_connections)
                                _connections.Add(this);
                            new Task(new(ReceiverTask)).Start();

                            return networkResult;
                        }

                        Logger.Debug("Failed to open connection " + Route.ToString());

                        return networkResult;
                    });
            }

            public Task CloseAsync(bool sendDisconnectCommand)
            {
                return Task.Run(
                    () =>
                    {
                        if (sendDisconnectCommand)
                            Send(
                                new DisconnectCommand());

                        Close(NetworkResult.Disconnect);
                    });
            }

            public void Close(NetworkResult networkResult)
            {
                Logger.Debug("Connection " + Route + " closed because of " + networkResult);

                Source.Cancel();
                InputStream.Close();
                OutputStream.Close();

                lock (_connections)
                    _connections.Remove(this);
            }

            public NetworkResult Read(out int value)
            {
                const int LENGTH = sizeof(int);

                if (_readBuffer == null || _readBuffer.Length < LENGTH)
                    _readBuffer = new byte[LENGTH];
                var result = Read(_readBuffer, 0, LENGTH);
                if (result == NetworkResult.Success)
                    value = BitConverter.ToInt32(_readBuffer);
                else
                    value = 0;
                return result;
            }

            public NetworkResult Read(out long value)
            {
                const int LENGTH = sizeof(long);

                if (_readBuffer == null || _readBuffer.Length < LENGTH)
                    _readBuffer = new byte[LENGTH];
                var result = Read(_readBuffer, 0, LENGTH);
                if (result == NetworkResult.Success)
                    value = BitConverter.ToInt64(_readBuffer);
                else
                    value = 0;
                return result;
            }

            public NetworkResult Read(byte[] buffer, int offset, int length)
            {
                return Execute(ConnectionAction.Read, buffer, offset, length);
            }

            public NetworkResult Read(out string value)
            {
                value = string.Empty;

                var result = Read(out int length);
                if (result == NetworkResult.Success
                    && length > 0xFFFF)
                    result = NetworkResult.UnexpectedDataReceived;

                if (result == NetworkResult.Success)
                {
                    if (_readBuffer == null || _readBuffer.Length < length)
                        _readBuffer = new byte[length];
                    result = Read(_readBuffer, 0, length);
                    value = Encoding.UTF8.GetString(_readBuffer);
                }

                return result;
            }

            public NetworkResult Write(int value)
            {
                byte[] buffer = BitConverter.GetBytes(value);
                return Write(buffer, 0, buffer.Length);
            }

            public NetworkResult Write(long value)
            {
                byte[] buffer = BitConverter.GetBytes(value);
                return Write(buffer, 0, buffer.Length);
            }

            public NetworkResult Write(byte[] buffer, int offset, int length)
            {
                return Execute(ConnectionAction.Write, buffer, offset, length);
            }

            public NetworkResult Write(string text)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                var result = Write(buffer.Length);
                if (result == NetworkResult.Success)
                    result = Write(buffer, 0, buffer.Length);
                return result;
            }

            private NetworkResult Execute(ConnectionAction action, byte[] buffer = null, int offset = 0, int length = 0)
            {
                Debug.Assert(length > 0 || action == ConnectionAction.Flush);

                if (IsClosed)
                    return NetworkResult.ConnectionClosed;

                lock (_pingTimer)
                    _pingTimer.Stop();

                NetworkResult result;

                try
                {
                    switch (action)
                    {
                        case ConnectionAction.Read:
                            int transferred = 0;

                            do
                            {
                                int singleTransferred = InputStream.Read(buffer, offset + transferred, length);
                                transferred += singleTransferred;
                                if (singleTransferred == 0)
                                {
                                    Logger.Info(string.Format(Messages.ConnectionLost, Route, "No data"));
                                    result = NetworkResult.ConnectionLost;
                                }
                                else if (transferred < length)
                                {
                                    Logger.Warn(string.Format(Messages.IncompleteReceive, transferred, length));
                                    result = NetworkResult.ReceivedIncomplete;
                                }
                                else
                                    result = NetworkResult.Success;
                            }
                            while (result == NetworkResult.ReceivedIncomplete);
                            break;
                        case ConnectionAction.Write:
                            lock (OutputStream)
                                OutputStream.Write(buffer, offset, length);
                            result = NetworkResult.Success;
                            break;
                        case ConnectionAction.Flush:
                            OutputStream.Flush();
                            result = NetworkResult.Success;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                catch (COMException e) when (OperatingSystem.IsWindows() && e.HResult == (int)COMExceptionHResults.WSAECONNABORTED) // Bluetooth
                {
                    Logger.Info(string.Format(Messages.ConnectionLost, Route, (COMExceptionHResults)e.HResult));
                    result = NetworkResult.ConnectionLost;
                }
                catch (ObjectDisposedException e) when (OperatingSystem.IsWindows() && e.HResult == (int)RO_E_CLOSED)
                {
                    Logger.Info(string.Format(Messages.ConnectionLost, Route, (HResults)e.HResult));
                    result = NetworkResult.ConnectionLost;
                }
                catch (IOException e) when (e.HResult == (int)COR_E_IO)
                {
                    // Verbindung wurde vom Remote Host geschlossen
                    Logger.Info(string.Format(Messages.ConnectionLost, Route, (HResults)e.HResult));
                    result = NetworkResult.ConnectionLost;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    throw;
                }

                if (result != NetworkResult.Success)
                    Close(result);
                else
                {
                    lock (_pingTimer)
                    {
#if DEBUG
#else
                        _pingTimer.Start();
#endif
                        _pingTimerEvent.Set();
                    }
                }

                return result;
            }

            private void ReceiverTask()
            {
                while (!IsClosed)
                {
                    var networkResult = Read(out int length);
                    if (networkResult == NetworkResult.Success)
                    {
                        if (_readBuffer == null || _readBuffer.Length < length)
                            _readBuffer = new byte[length];

                        networkResult = Read(_readBuffer, 0, length);
                        if (networkResult == NetworkResult.Success)
                        {
                            var command = ConvertFrom<CommandPacket>(_readBuffer).Command;

                            if (command is AttachmentsCommand)
                            {
                                networkResult = Read(out int attachmentLength);
                                if (networkResult == NetworkResult.Success)
                                {
                                    ((AttachmentsCommand)command).Attachments = new byte[attachmentLength][];
                                    for (int i = 0; i < attachmentLength; i++)
                                    {
                                        if (networkResult == NetworkResult.Success)
                                            networkResult = Read(out length);
                                        if (networkResult == NetworkResult.Success)
                                        {
                                            ((AttachmentsCommand)command).Attachments[i] = new byte[length];
                                            networkResult = Read(((AttachmentsCommand)command).Attachments[i], 0, length);
                                        }
                                    }
                                }
                            }

                            Logger.Debug(command.ToString() + " command received from " + Route.RemoteEndPoint.ToString());

                            SandaabContext.Dispatcher.Add(DeviceId, command, Route);
                        }
                    }
                }
            }

            public NetworkResult Send(Command command)
            {
                Logger.Debug("Send " + command.ToString() + " command to " + Route.RemoteEndPoint.ToString());

                lock (OutputStream)
                {
                    var commandPacket = new CommandPacket()
                    {
                        Command = command,
                    };

                    byte[] buffer = ConvertFrom(commandPacket);

                    Debug.Assert(buffer.Length > 0);

                    var result = Write(buffer.Length);
                    if (result == NetworkResult.Success)
                        result = Write(buffer, 0, buffer.Length);
                    if (command is AttachmentsCommand attachmentCommand
                        && attachmentCommand.Attachments != null)
                    {
                        if (result == NetworkResult.Success)
                            result = Write(attachmentCommand.Attachments.Length);
                        foreach (var attachment in attachmentCommand.Attachments)
                        {
                            if (result == NetworkResult.Success)
                                result = Write(attachment.Length);
                            if (result == NetworkResult.Success)
                                result = Write(attachment, 0, attachment.Length);
                        }
                    }
                    if (result == NetworkResult.Success)
                        result = Execute(ConnectionAction.Flush);

                    if (result == NetworkResult.Success)
                        Logger.Debug(command.ToString() + " command sent to " + Route.RemoteEndPoint.ToString());

                    return result;
                }
            }
        }

        public class DeviceSearch : IDisposable
        {
            const int SEARCH_INTERVAL = 1000; // in milliseconds
            const int SEARCH_TIMES = 5; // number of searches, should be multiple, since UDP multicast can fail

            private Device Device;
            private TimeSpan _duration;
            public DateTimeOffset End;
            public event EventHandler Event;
            private System.Timers.Timer Timer;

            public DeviceSearch(Device device = null, TimeSpan duration = default)
            {
                if (duration == default)
                    duration = TimeSpan.FromMilliseconds(SEARCH_TIMES * SEARCH_INTERVAL);

                Device = device;
                _duration = duration;

                End = DateTimeOffset.Now + _duration;

                Timer = new(SEARCH_INTERVAL);
                Timer.Elapsed += SearchTimer_Elapsed;

                lock (_deviceSearches)
                    _deviceSearches.Add(this);
            }

            public DeviceSearch(TimeSpan duration)
                : this(null, duration)
            {
            }

            public void Start()
            {
                Reset();
            }

            public void Dispose()
            {
                if (Device == null)
                    SandaabContext.App.InvokeAsync(Event, this, EventArgs.Empty);

                lock (_deviceSearches)
                    _deviceSearches.Remove(this);

                GC.SuppressFinalize(this);
            }

            private void SearchTimer_Elapsed(object timer, ElapsedEventArgs args)
            {
                if (DateTimeOffset.Now < End + TimeSpan.FromMilliseconds(Timer.Interval))
                    SandaabContext.Network.SendSearchRequestAsync(Device);
                else
                    Dispose();
            }

            public void Reset()
            {
                Timer.Stop();
                End = DateTimeOffset.Now + _duration;
                Timer.Start();
                SearchTimer_Elapsed(Timer, null);
            }
        }

        [JsonIgnore]
        public bool BluetoothAvailable { get; protected set; }
        [JsonIgnore]
        public bool BluetoothEnabled { get; protected set; }
        private readonly Collection<EndPoint> _connectings;
        [JsonIgnore]
        private static Collection<Connection> _connections;
        private static Collection<DeviceSearch> _deviceSearches;
        private int _previousNetworkInterfacesUp;
        private TcpListener _tcpListener;
        [JsonIgnore]
        public int TcpListenerPort { get; private set; }
        private UdpClient _udpListenerClientIp4;
        private UdpClient _udpListenerClientIp6;

        public Network()
        {
            _connectings = new();
            _connections = new();
            _deviceSearches = new();
        }

        public virtual void Initialize()
        {
            int port = Config.UdpPort;
            foreach (AddressFamily addressFamily in new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 })
                try
                {
                    switch (addressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            _udpListenerClientIp4 = new UdpClient(port, addressFamily);
                            new Task(new(UdpReceiverTask), _udpListenerClientIp4).Start();
                            break;
                        case AddressFamily.InterNetworkV6:
                            _udpListenerClientIp6 = new UdpClient(port, addressFamily);
                            new Task(new(UdpReceiverTask), _udpListenerClientIp6).Start();
                            break;
                    }
                }
                //catch (SocketException e) when (OperatingSystem.IsWindows() && e.HResult == (int)E_FAIL)
                //{
                //    Logger.Warn(string.Format(Messages.ListenError, "UDP", port, (Win32Errors)e.NativeErrorCode));
                //}
                catch (Exception e)
                {
                    Logger.Error(e);
                }

            if (BluetoothAvailable)
                StartBtListenerAsync();

            port = OperatingSystem.IsAndroid() ? Config.TcpPort : 0;
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();
            new Task(new(TcpListenerTask)).Start();

            TcpListenerPort = ((IPEndPoint)_tcpListener.LocalEndpoint).Port;

            try
            {
                NetworkChange.NetworkAddressChanged += NetworkAddress_Changed;
            }
            catch (NetworkInformationException e) when (OperatingSystem.IsAndroid() && e.ErrorCode == 0)
            {
                // Bug auf Android
            }

            NetworkAddress_Changed(null, EventArgs.Empty);
        }

        public virtual void Dispose()
        {
            lock (_deviceSearches)
                for (var i = _deviceSearches.Count - 1; i >= 0; i--)
                    _deviceSearches[i].Dispose();

            _udpListenerClientIp4?.Dispose();
            _udpListenerClientIp6?.Dispose();
            _tcpListener.Stop();

            Task.WaitAll(CloseConnectionsAsync(null, true), 500);

            GC.SuppressFinalize(this);
        }

        private static bool IsLANAddress(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
                return address.IsIPv6SiteLocal;

            if (address.AddressFamily != AddressFamily.InterNetwork)
                return false;

            long ipStart = BitConverter.ToInt32(IPAddress.Parse("10.0.0.0").GetAddressBytes().Reverse().ToArray(), 0);
            long ipEnd = BitConverter.ToInt32(IPAddress.Parse("10.255.255.255").GetAddressBytes().Reverse().ToArray(), 0);
            long ip = BitConverter.ToInt32(address.GetAddressBytes().Reverse().ToArray(), 0);
            if (ipStart <= ip && ip <= ipEnd)
                return true;

            ipStart = BitConverter.ToInt32(IPAddress.Parse("172.16.0.0").GetAddressBytes().Reverse().ToArray(), 0);
            ipEnd = BitConverter.ToInt32(IPAddress.Parse("172.31.255.255").GetAddressBytes().Reverse().ToArray(), 0);
            ip = BitConverter.ToInt32(address.GetAddressBytes().Reverse().ToArray(), 0);
            if (ipStart <= ip && ip <= ipEnd)
                return true;

            ipStart = BitConverter.ToInt32(IPAddress.Parse("192.168.0.0").GetAddressBytes().Reverse().ToArray(), 0);
            ipEnd = BitConverter.ToInt32(IPAddress.Parse("192.168.255.255").GetAddressBytes().Reverse().ToArray(), 0);
            ip = BitConverter.ToInt32(address.GetAddressBytes().Reverse().ToArray(), 0);
            if (ipStart <= ip && ip <= ipEnd)
                return true;

            return false;
        }

        private static Collection<NetworkInterface> GetLANNetworkInterfaces()
        {
            Collection<NetworkInterface> result = new();

            try
            {
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                    if (!networkInterface.IsReceiveOnly
                        && networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        IPInterfaceProperties properties = networkInterface.GetIPProperties();
                        foreach (var unicastAddress in properties.UnicastAddresses)
                            if (IsLANAddress(unicastAddress.Address))
                                if (!result.Contains(networkInterface))
                                    result.Add(networkInterface);
                    }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return result;
        }

        public virtual void ChangeLocalDnsEntry(string hostname, Collection<IPAddress> addresses = null)
        {
            throw new NotImplementedException();
        }

        private void UdpReceiverTask(object state)
        {
            Thread.CurrentThread.Name = "UdpReceiverTask";

            var udpClient = state as UdpClient;

            var localEndPoint = new UdpEndPoint(((IPEndPoint)udpClient.Client.LocalEndPoint).Address, ((IPEndPoint)udpClient.Client.LocalEndPoint).Port);

            while (true)
                try
                {
                    IPEndPoint remoteEndPoint = new UdpEndPoint(IPAddress.None, 0);
                    var datagram = udpClient.Receive(ref remoteEndPoint);

                    var commandPacket = ConvertFrom<CommandPacket>(datagram);

                    if (commandPacket.Command is SearchRequestCommand searchRequest)
                    {
                        var route = new Route(localEndPoint,
                            new TcpEndPoint(remoteEndPoint.Address, searchRequest.TcpListenerPort));

                        SandaabContext.Dispatcher.Add(searchRequest.SenderDeviceId, commandPacket.Command, route);
                    }
                }
                catch (SocketException e) when (OperatingSystem.IsAndroid() && e.HResult == (int)E_FAIL)
                {
                    break;
                }
                catch (SocketException e) when (OperatingSystem.IsWindows() && e.NativeErrorCode == (int)Win32Errors.WSAEINTR)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
        }

        private void NetworkAddress_Changed(object sender, EventArgs args)
        {
            int networkInterfacesUp = 0;


            foreach (var networkInterface in GetLANNetworkInterfaces())
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var properties = networkInterface.GetIPProperties();
                    foreach (var multicastAddress in properties.MulticastAddresses)
                        if (multicastAddress.IsDnsEligible)
                            foreach (var unicastAddress in properties.UnicastAddresses)
                                if (unicastAddress.Address.AddressFamily == multicastAddress.Address.AddressFamily)
                                    try
                                    {
                                        switch (multicastAddress.Address.AddressFamily)
                                        {
                                            case AddressFamily.InterNetwork:
                                                _udpListenerClientIp4.JoinMulticastGroup(multicastAddress.Address);
                                                break;
                                            case AddressFamily.InterNetworkV6:
                                                _udpListenerClientIp6.JoinMulticastGroup(multicastAddress.Address);
                                                break;
                                        }
                                        Logger.Debug(multicastAddress.Address.ToString());
                                    }
                                    catch (SocketException e) when (OperatingSystem.IsWindows() && e.SocketErrorCode == SocketError.HostUnreachable)
                                    {
                                        Logger.Warn(string.Format(Messages.MulticastAddressUnreachable, multicastAddress.Address, (Win32Errors)e.NativeErrorCode));
                                    }
                                    catch (SocketException e) when (OperatingSystem.IsWindows())
                                    {
                                        Logger.Error(string.Format(Messages.SocketError, e.Message, (Win32Errors)e.NativeErrorCode));
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Error(e);
                                    }

                    networkInterfacesUp++;
                }

            NetworkEvent change;
            if (sender == null
                || _previousNetworkInterfacesUp > networkInterfacesUp)
                change = NetworkEvent.Connected;
            else if (_previousNetworkInterfacesUp < networkInterfacesUp)
                change = NetworkEvent.Disconnected;
            else
                change = NetworkEvent.Unknown;
            Network_Changed(NetworkType.Ip, change);

            _previousNetworkInterfacesUp = networkInterfacesUp;
        }

        protected abstract void SendBtMulticastAsync(Command command);

        protected abstract void SendAdbMulticastAsync(Command command);

        private void SendUdpMulticastAsync(Command command)
        {
            Task.Run(
                async () =>
                {
                    var commandPacket = new CommandPacket()
                    {
                        Command = command,
                    };

                    byte[] datagram = ConvertFrom(commandPacket);
                    Debug.Assert(datagram.Length <= Convert.ToInt64(Config.MaxUdpSize));

                    var udpClientV4 = new UdpClient(AddressFamily.InterNetwork);
                    var udpClientV6 = new UdpClient(AddressFamily.InterNetworkV6);

                    foreach (var networkInterface in GetLANNetworkInterfaces())
                        if (networkInterface.OperationalStatus == OperationalStatus.Up
                            && networkInterface.SupportsMulticast)
                        {
                            Collection<IPAddress> sentMulticastAddresses = new();
                            var properties = networkInterface.GetIPProperties();
                            foreach (AddressFamily addressFamily in new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 })
                                foreach (var multicastAddress in properties.MulticastAddresses)
                                    if (multicastAddress.Address.AddressFamily == addressFamily)
                                        if (!sentMulticastAddresses.Contains(multicastAddress.Address))
                                        {
                                            try
                                            {
                                                var remoteEndPoint = new UdpEndPoint(multicastAddress.Address, Config.UdpPort);
                                                int sentBytes;
                                                if (multicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                                                    sentBytes = await udpClientV4.SendAsync(datagram, remoteEndPoint);
                                                else
                                                    sentBytes = await udpClientV6.SendAsync(datagram, remoteEndPoint);
                                                if (sentBytes != datagram.Length)
                                                    Logger.Warn(string.Format(Messages.IncompleteSent, sentBytes, datagram.Length));
                                            }
                                            catch (Exception e)
                                            {
                                                Logger.Error(e);
                                                throw;
                                            }

                                            sentMulticastAddresses.Add(multicastAddress.Address);

                                            break;
                                        }
                        }
                });
        }

        public static byte[] ConvertFrom<T>(T packet)
        {
            byte[] datagram;

            string jsonString = JsonConvert.SerializeObject(packet, Formatting.None);

            using (var packetStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            using (var datagramStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(datagramStream, CompressionLevel.SmallestSize))
                    packetStream.CopyTo(gzipStream);
                datagram = datagramStream.ToArray();
            }

            return datagram;
        }

        public static T ConvertFrom<T>(byte[] datagram)
        {
            string json;
            using (var datagramStream = new MemoryStream(datagram))
            using (var gzipStream = new GZipStream(datagramStream, CompressionMode.Decompress))
            using (var jsonStream = new MemoryStream())
            {
                gzipStream.CopyTo(jsonStream);
                json = Encoding.UTF8.GetString(jsonStream.ToArray());
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public NetworkInterfaceType GetNetworkInterfaceType(IPAddress address)
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var properties = networkInterface.GetIPProperties();
                foreach (var unicastAddress in properties.UnicastAddresses)
                    if (unicastAddress.Address.Equals(address))
                        return networkInterface.NetworkInterfaceType;
            }

            return NetworkInterfaceType.Unknown;
        }

        public long GetNetworkSpeed(IPAddress address)
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var properties = networkInterface.GetIPProperties();
                foreach (var unicastAddress in properties.UnicastAddresses)
                    if (unicastAddress.Address.Equals(address))
                        return networkInterface.Speed;
            }

            return 0;
        }

        protected abstract NetworkResult BtConnect(BtEndPoint endPoint, out Connection connection);

        protected abstract Task StartBtListenerAsync();

        public Task SendSearchRequestAsync(Device device)
        {
            var command = new SearchRequestCommand()
            {
                ReceiverDeviceId = device?.Id,
                SenderDeviceId = SandaabContext.LocalDevice.Id,
                SenderDeviceName = SandaabContext.LocalDevice.Name,
                SenderDevicePlatform = SandaabContext.LocalDevice.Platform,
                TcpListenerPort = SandaabContext.Network.TcpListenerPort,
            };


            return Task.Run(
                () =>
                {
                    SendBtMulticastAsync(command);
                    SendUdpMulticastAsync(command);
                    SendAdbMulticastAsync(command);
                });
        }

        public Task<NetworkResult> SendAsync(Command command, EndPoint remoteEndPoint)
        {
            return Task.Run(
                () =>
                {
                    var connection = GetConnectionByRemoteEndPoint(remoteEndPoint);
                    if (connection == null)
                    {
                        var networkResult = Connect(remoteEndPoint, out connection);
                        if (networkResult != NetworkResult.Success)
                            return networkResult;
                    }

                    return connection.Send(command);
                });
        }

        protected NetworkResult Connect(EndPoint remoteEndPoint, out Connection connection)
        {
            lock (_connectings)
            {
                if (_connectings.Contains(remoteEndPoint))
                {
                    connection = null;
                    return NetworkResult.WaitingForConnection;
                }
                else
                    _connectings.Add(remoteEndPoint);
            }

            try
            {
                if (remoteEndPoint is BtEndPoint btRemoteEndPoint)
                    return BtConnect(btRemoteEndPoint, out connection);
                else if (remoteEndPoint is TcpEndPoint tcpRemoteEndPoint)
                    return TcpConnect(tcpRemoteEndPoint, out connection);
                else
                    throw new NotImplementedException();
            }
            finally
            {
                lock (_connectings)
                    _connectings.Remove(remoteEndPoint);
            }
        }

        public Connection GetConnectionByRemoteEndPoint(EndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
                return null;

            lock (_connections)
            {
                foreach (var connection in _connections)
                    if (connection.Route.RemoteEndPoint.Equals(remoteEndPoint))
                        return connection;

                return null;
            }
        }

        protected void TcpListenerTask()
        {
            Thread.CurrentThread.Name = "TcpListenerTask";

            while (true)
            {
                try
                {
                    var client = _tcpListener.AcceptTcpClient();

                    IPEndPoint remoteEndPoint;
                    if (client.Client.RemoteEndPoint is not IPEndPoint ipEndPoint)
                        throw new NotImplementedException();
                    else if (ipEndPoint.Address.Equals(IPAddress.Loopback))
                        remoteEndPoint = new AdbEndPoint(ipEndPoint.Port);
                    else
                        remoteEndPoint = new TcpEndPoint(ipEndPoint.Address, ipEndPoint.Port);

                    var connection = new Connection(false, client.Client.LocalEndPoint, remoteEndPoint, client.GetStream(), client.GetStream());

                    _ = connection.OpenAsync();
                }
                catch (SocketException e) when (OperatingSystem.IsWindows() && e.NativeErrorCode == (int)Win32Errors.WSAEINTR) // listener was disposed
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private NetworkResult TcpConnect(TcpEndPoint remoteEndPoint, out Connection connection)
        {
            NetworkResult networkResult;
            connection = null;

            try
            {
                var client = new TcpClient(remoteEndPoint.AddressFamily);
                client.Connect(remoteEndPoint);

                connection = new Connection(true, client.Client.LocalEndPoint, remoteEndPoint, client.GetStream(), client.GetStream());

                return connection.OpenAsync().Result;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionRefused)
            {
                Logger.Info(string.Format(Messages.ConnectFailure, remoteEndPoint, string.Format("Socket error {0} - {1}", (int)e.SocketErrorCode, e.SocketErrorCode)));
                networkResult = NetworkResult.ConnectFailure;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                networkResult = NetworkResult.ConnectFailure;
            }

            return networkResult;
        }

        public virtual void Network_Changed(NetworkType network, NetworkEvent change, EndPoint remoteEndPoint = null, string deviceId = null)
        {
            Logger.Debug(network.ToString() + " " + change.ToString());

            if (change == NetworkEvent.Disconnected)
                GetConnectionByRemoteEndPoint(remoteEndPoint)?.Close(NetworkResult.Disconnect);

            if (network == NetworkType.Ip
                && remoteEndPoint == null)
                if (new[] { NetworkEvent.Connected, NetworkEvent.Unknown }.Contains(change))
                {
                    lock (SandaabContext.Devices)
                        foreach (var device in SandaabContext.Devices)
                            if (device.State == DeviceState.Paired)
                                new DeviceSearch(device).Start();
                }
                else if (change == NetworkEvent.Disconnected)
                {
                    Collection<IPAddress> unicastAddresses = new();
                    foreach (var networkInterface in GetLANNetworkInterfaces())
                        if (networkInterface.OperationalStatus == OperationalStatus.Up)
                        {
                            var properties = networkInterface.GetIPProperties();
                            foreach (var unicastAddress in properties.UnicastAddresses)
                                unicastAddresses.Add(unicastAddress.Address);
                        }

                    lock (_connections)
                        for (var i = _connections.Count; i >= 0; i--)
                            if (_connections[i].Route.LocalEndPoint is IPEndPoint ipEndPoint)
                            {
                                bool found = false;
                                foreach (var unicastAddress in unicastAddresses)
                                    found |= ipEndPoint.Address.Equals(unicastAddress);

                                if (!found)
                                    _connections[i].Close(NetworkResult.Disconnect);
                            }
                }

            if (change == NetworkEvent.Sleep)
            {
                CloseConnectionsAsync(null, true);
            }
            else if (change != NetworkEvent.Disconnected)
            {
                if (network == NetworkType.Adb)
                    SendSearchRequestAsync(null);
                else
                    lock (_deviceSearches)
                        foreach (var deviceSearch in _deviceSearches)
                            deviceSearch.Reset();
            }
        }

        public static NetworkType NetworkTypeOf(Route route)
        {
            if (route.RemoteEndPoint is AdbEndPoint)
                return NetworkType.Adb;
            else if (route.RemoteEndPoint is BtEndPoint)
                return NetworkType.Bluetooth;
            else
                return NetworkType.Ip;
        }

        public static long SpeedOf(Route route)
        {
            if (route.LocalEndPoint is IPEndPoint localIpEndPoint)
                try
                {
                    foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                        if (!networkInterface.IsReceiveOnly)
                            if (networkInterface.Speed >= 0)
                            {
                                IPInterfaceProperties properties = networkInterface.GetIPProperties();
                                foreach (var unicastAddress in properties.UnicastAddresses)
                                    if (unicastAddress.Equals(localIpEndPoint.Address))
                                        return networkInterface.Speed;
                            }
                            else if (OperatingSystem.IsAndroid())
                                switch (networkInterface.Name)
                                {
                                    case "wlan0":
                                    case "swlan0":
                                        return 3;
                                    case "rndis0":
                                        return 2;
                                    case "bt-pan":
                                        return 1;
                                    default:
                                        Logger.Error(string.Format("Unknown network interface \"{0}\"", networkInterface.Name));
                                        break;
                                }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

            return -1;
        }

        public Task[] CloseConnectionsAsync(EndPoint remoteEndPoint, bool sendDisconnectCommand)
        {
            lock (_connections)
            {
                Collection<Task> tasks = new();
                for (var i = _connections.Count - 1; i >= 0; i--)
                {
                    var connection = _connections[i];
                    if (remoteEndPoint == null
                        || connection.Route.RemoteEndPoint.Equals(remoteEndPoint))
                        tasks.Add(connection.CloseAsync(sendDisconnectCommand));
                }
                return tasks.ToArray();
            }
        }
    }
}