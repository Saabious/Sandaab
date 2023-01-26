using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Sandaab.Core.Constantes;
using Sandaab.Core.Entities;
using System.Diagnostics;

namespace Sandaab.Core.Components
{
    public class Device
    {
        public class ChangeEventArgs : EventArgs
        {
            public DeviceEvent Change;
        }

        public class SendHandler
        {
            internal Action<bool> Action;
            internal Command Command;
            internal bool OnUiThread;

            public SendHandler(Command command)
            {
                Command = command;
            }

            public void ContinueWith(Action<bool> action)
            {
                Action = action;
            }

            public void ContinueWithOnUiThread(Action<bool> action)
            {
                Action = action;
                OnUiThread = true;
            }
        }

        private record RouteRecord
        {
            public Route Route;
            public bool Sending;
        }

        private record CommandRecord
        {
            public Command Command;
            public SendHandler CommandSender;
        }

        private readonly List<CommandRecord> _commandRecords;
        [JsonIgnore]
        public long DatabaseId;
        private Network.DeviceSearch _deviceSearch;
        public static event EventHandler Event;
        [JsonProperty(Required = Required.Always)]
        public string Id { get; private set; }
        private string _name;
        [JsonProperty(Required = Required.Always)]
        public string Name { get { return _name; } set { SetName(value); } }
        [JsonProperty(Required = Required.Always)]
        public DevicePlatform Platform { get; private set; }
        private readonly List<RouteRecord> _routeRecords;
        private DeviceState _state;
        private bool _stateChanged;
        public DeviceState State { get { return _state; } set { SetDeviceState(value); } }
        private int _updateCounter;

        public Device(DevicePlatform platform, string id, string name, Route route)
        {
            Id = id;
            Name = name;
            Platform = platform;
            _commandRecords = new();
            _routeRecords = new();

            if (route != null)
                AddRoute(route);
        }

        public override string ToString()
        {
            return Name;
        }

        public SendHandler SendQueued(Command command)
        {
            lock (_commandRecords)
            {
                var commandSender = new SendHandler(command);

                _commandRecords.Add(
                    new CommandRecord()
                    {
                        Command = command,
                        CommandSender = commandSender,
                    });

                SendNextCommand();

                return commandSender;
            }
        }

        private int _commandIndex = 0;

        private void SendNextCommand()
        {
            lock (_commandRecords)
                if (_commandRecords.Count > 0)
                {
                    RouteRecord routeRecord = null;
                    lock (_routeRecords)
                        foreach (var r in _routeRecords)
                            if (!r.Sending)
                            {
                                routeRecord = r;
                                routeRecord.Sending = true;
                                break;
                            }

                    if (routeRecord != null)
                    {
                        var commandRecord = _commandRecords.First();
                        SandaabContext.Network.SendAsync(commandRecord.Command, routeRecord.Route.RemoteEndPoint)
                            .ContinueWith(
                                (task) =>
                                {
                                    lock (_routeRecords)
                                        lock (_commandRecords)
                                        {
                                            routeRecord.Sending = false;

                                            if (task.IsFaulted)
                                                Logger.Error(task.Exception);
                                            else
                                            {
                                                if (task.Result == NetworkResult.WaitingForConnection)
                                                    Thread.Sleep(200);
                                                else if (task.Result == NetworkResult.Success)
                                                    _commandRecords.Remove(commandRecord);
                                                else
                                                    RemoveRoute(routeRecord.Route, false);

                                                if (task.Result != NetworkResult.WaitingForConnection
                                                    && commandRecord.CommandSender.Action != null)
                                                {
                                                    var success = task.Result == NetworkResult.Success;
                                                    if (!commandRecord.CommandSender.OnUiThread)
                                                        commandRecord.CommandSender.Action(success);
                                                    else
                                                        SandaabContext.App.RunOnUiThreadAsync(
                                                            () =>
                                                            {
                                                                commandRecord.CommandSender.Action(success);
                                                            });
                                                    if (task.Result != NetworkResult.Success)
                                                        _commandRecords.Remove(commandRecord);
                                                }
                                            }

                                            SendNextCommand();
                                        }
                                });
                    }
                    else if (_deviceSearch == null)
                    {
                        _deviceSearch = new Network.DeviceSearch(this);
                        _deviceSearch.Event += DeviceSearch_Event;
                        _deviceSearch.Start();
                    }
                }
        }

        private void DeviceSearch_Event(object sender, System.EventArgs args)
        {
            lock (_commandRecords)
                _deviceSearch = null;
        }

        public void AddRoute(Route route)
        {
            lock (_routeRecords)
            {
                foreach (var routeRecord in _routeRecords)
                    if (routeRecord.Route.Equals(route))
                        return;

                _routeRecords.Add(new RouteRecord() { Route = route });
                _routeRecords.Sort(CompareRouteRecords);

                if (_routeRecords.Count == 1)
                {
                    InvokeAsync(
                        this,
                        new ChangeEventArgs()
                        {
                            Change = DeviceEvent.Connected,
                        });
                    SendNextCommand();
                }
            }
        }

        private void RemoveRoute(Route route, bool sendDisconnectCommand)
        {
            lock (_routeRecords)
            {
                foreach (var routeRecord in _routeRecords)
                    if (routeRecord.Route.Equals(route))
                    {
                        SandaabContext.Network.CloseConnectionsAsync(route.RemoteEndPoint, sendDisconnectCommand);
                        _routeRecords.Remove(routeRecord);

                        if (_routeRecords.Count == 0)
                            InvokeAsync(
                                this, 
                                new ChangeEventArgs()
                                { 
                                    Change = DeviceEvent.Disconnected,
                                });

                        return;
                    }
            }
        }

        private int CompareRouteRecords(RouteRecord a, RouteRecord b)
        {
            var aNetworkType = Network.NetworkTypeOf(a.Route);
            var bNetworkType = Network.NetworkTypeOf(b.Route);

            if (aNetworkType > bNetworkType)
                return -1;
            if (aNetworkType < bNetworkType)
                return 1;
            if (aNetworkType == NetworkType.Ip)
            {
                var aSpeed = Network.SpeedOf(a.Route);
                var bSpeed = Network.SpeedOf(b.Route);
                if (aSpeed > bSpeed)
                    return -1;
                if (aSpeed < bSpeed)
                    return 1;
            }
            return 0;
        }

        private void SetDeviceState(DeviceState state)
        {
            if (state != _state)
            {
                if (_state != DeviceState.Initialized)
                    switch (state)
                    {
                        case DeviceState.Removed:
                            ClearRoutes(true);
                            break;
                    }

                _state = state;
                _stateChanged = true;
                Update();
            }
        }

        private void SetName(string name)
        {
            if (name != _name)
            {
                _name = name;
                Update();
            }
        }

        public void Update()
        {
            BeginUpdate();
            EndUpdate();
        }

        public void BeginUpdate()
        {
            lock (this)
                _updateCounter++;
        }

        public void EndUpdate()
        {
            Debug.Assert(_updateCounter > 0);

            lock (this)
            {
                _updateCounter--;
                if (_updateCounter > 0
                    || State == DeviceState.Initialized)
                    return;

                InvokeAsync(
                    this,
                    new ChangeEventArgs()
                    {
                        Change = !_stateChanged ? DeviceEvent.Updated : DeviceEventFrom(State)
                    });

                _stateChanged = false;

                var sql = "UPDATE Devices SET Json=@Json WHERE Id=@Id;";
                SqliteParameter[] parameters =
                {
                    new("Json", JsonConvert.SerializeObject(this)),
                    new("Id", DatabaseId),
                };

                SandaabContext.Database.ExecuteNoQueryAsync(sql, parameters);
            }
        }

        public static DeviceEvent DeviceEventFrom(DeviceState state)
        {
            switch (state)
            {
                case DeviceState.Found:
                    return DeviceEvent.Found;
                case DeviceState.Paired:
                    return DeviceEvent.Paired;
                case DeviceState.Removed:
                    return DeviceEvent.Removed;
                default:
                    throw new NotImplementedException();
            }
        }

        public void ClearRoutes(bool sendDisconnectCommand)
        {
            lock (_routeRecords)
                for (var i = _routeRecords.Count - 1; i >= 0; i--)
                    RemoveRoute(_routeRecords[i].Route, sendDisconnectCommand);
        }

        public static void Invoke(Device device, EventArgs args)
        {
            SandaabContext.App.Invoke(Event, device, args);
        }

        public static void InvokeAsync(Device device, EventArgs args)
        {
            SandaabContext.App.InvokeAsync(Event, device, args);
        }
    }
}
