using Sandaab.Core.Constantes;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;

namespace Sandaab.Core.Components
{
    public class Dispatcher
    {
        internal record QueueRecord
        {
            public Command Command;
            public string DeviceId;
            public Route Route;
        }

        public class SearchResponseEventArgs : EventArgs
        {
        }

        public class PairRequestEventArgs : EventArgs
        {
            public Device Device;
            public bool QuestionShown; // Sould be set by an event handler
        }

        public class PairResponseEventArgs : EventArgs
        {
            public bool Granted;
        }

        internal class RemoteControlSender : IDisposable
        {
            private const int TIMER_INTERVAL = 1000; // in Milliseconds

            private ScreenArea _area;
            public Device Device { get; private set; }
            private bool _inTimer;
            private System.Timers.Timer _timer;
            private bool _updated;

            public RemoteControlSender(Device device, ScreenArea area)
            {
                Device = device;

                _timer = new();
                _timer.Interval = TIMER_INTERVAL;
                _timer.Elapsed += Timer_Elapsed;


                SandaabContext.Screen.BeginCaptureing();

                Update(area);
            }

            public void Dispose()
            {
                SandaabContext.Screen.EndCaptureing();

                SandaabContext.Dispatcher._remoteControlSender = null;

                GC.SuppressFinalize(this);
            }

            public void Update(ScreenArea area)
            {
                Logger.Point(7);
                _area = area;
                _updated = true;

                _timer.Stop();
                _timer.Start();

                Logger.Point(7);
                Timer_Elapsed(this, null);
                Logger.Point(7);
            }

            private void Timer_Elapsed(object sender, ElapsedEventArgs args)
            {
                bool inTimer;
                lock (_timer)
                {
                    inTimer = _inTimer;
                    _inTimer = true;
                }

                Logger.Debug("21");

                if (!inTimer
                    && (args == null || args.SignalTime < DateTime.Now + TimeSpan.FromMilliseconds(TIMER_INTERVAL * 0.2)))
                {
                    Logger.Point(2);
                    var response = SandaabContext.Screen.Capture(_area, _updated);
                    Logger.Point(2);

                    if (response != null)
                    {
                        Device.SendQueued(response);

                        if (response.Result == RemoteControlResponseCommand.ResultType.Stopped)
                            Dispose();
                    }

                    _updated = false;
                }

                if (inTimer)
                    lock (_timer)
                        if (inTimer)
                            _inTimer = false;
            }
        }

        private Notification _pairRequestNotification;
        private readonly ConcurrentQueue<QueueRecord> _queue;
        private readonly Semaphore _queueSemaphore;
        internal RemoteControlSender _remoteControlSender;
        private Dictionary<Device, Action<RemoteControlResponseCommand>> _remoteControlDevices;

        public Dispatcher()
        {
            _queue = new();
            _queueSemaphore = new(0, 1024);
            _remoteControlDevices = new();


            new Task(new(QueueTask)).Start();
        }

        public void Add(string deviceId, Command command, Route route)
        {
            var queueRecord = new QueueRecord()
            {
                Command = command,
                DeviceId = deviceId,
                Route = route,
            };
            _queue.Enqueue(queueRecord);
            _queueSemaphore.Release();
        }

        private void QueueTask()
        {
            QueueRecord queueRecord;

            while (true)
            {
                _queueSemaphore.WaitOne();
                while (!_queue.TryDequeue(out queueRecord))
                    Thread.Sleep(50); // Sometimes the semaphore is faster than the queue

                Logger.Debug("QueueTask: " + queueRecord.Command + " start " + Thread.CurrentThread.ManagedThreadId);
                Dispatch(queueRecord.DeviceId, queueRecord.Command, queueRecord.Route);
                Logger.Debug("QueueTask: " + queueRecord.Command + " end ");
            }
        }

        private void Dispatch(string deviceId, Command command, Route route)
        {
            Device device;

            lock (SandaabContext.Devices)
            {
                device = SandaabContext.Devices.GetByDeviceId(deviceId);
                if (device == null)
                {
                    if (command is SearchRequestCommand searchRequest)
                        device = new(searchRequest.SenderDevicePlatform, deviceId, searchRequest.SenderDeviceName, route);
                    else if (command is SearchResponseCommand searchResponse)
                        device = new(searchResponse.DevicePlatform, deviceId, searchResponse.DeviceName, route);
                    else
                        return;
                    device.State = DeviceState.Found;
                    SandaabContext.Devices.Add(device);
                }
            }

            lock (device)
            {
                Logger.Debug("Dispatch " + command + " command");

                Command response = null;

                switch (command)
                {
                    case DisconnectCommand:
                        DoDisconnectInfo(device, command);
                        break;
                    case PairRequestCommand:
                        DoPairRequest(device);
                        break;
                    case PairResponseCommand:
                        DoPairResponse(device, command);
                        break;
                    case SearchRequestCommand:
                        DoSearchRequest(device, command, route);
                        break;
                    case SearchResponseCommand:
                        DoSearchResponse(device, command, route);
                        break;
                    default:
                        if (device.State != DeviceState.Paired)
                            device.SendQueued(
                                new DisconnectCommand()
                                {
                                    Unpair = true
                                });
                        else
                            switch (command)
                            {
                                case RemoteControlRequestCommand:
                                    DoRemoteControlRequestCommand(device, command);
                                    break;
                                case RemoteControlResponseCommand:
                                    DoRemoteControlResponseCommand(device, command);
                                    break;
                            }

                        break;
                }

                if (response != null)
                {
                    Logger.Debug("Dispach result: " + response + " command");
                    device.SendQueued(response);
                }
            }
        }

        private void DoSearchRequest(Device device, Command command, Route route)
        {
            var searchRequest = command as SearchRequestCommand;

            if (!string.IsNullOrEmpty(searchRequest.ReceiverDeviceId)
                && searchRequest.ReceiverDeviceId != SandaabContext.LocalDevice.Id)
                return;

            if (Network.NetworkTypeOf(route) == NetworkType.Ip)
                SandaabContext.Network.Network_Changed(NetworkType.Ip, NetworkEvent.Connected, route.RemoteEndPoint, device.Id);

            device.AddRoute(route);

            _ = SandaabContext.Network.SendAsync(
                new SearchResponseCommand()
                {
                    DeviceName = SandaabContext.LocalDevice.Name,
                    DevicePlatform = SandaabContext.LocalDevice.Platform,
                },
                route.RemoteEndPoint);
        }

        private void DoSearchResponse(Device device, Command command, Route route)
        {
            var searchResponse = command as SearchResponseCommand;

            device.AddRoute(route);
            device.Name = searchResponse.DeviceName;

            Device.InvokeAsync(
                device,
                new SearchResponseEventArgs());
        }

        private void DoPairRequest(Device device)
        {
            var args = new PairRequestEventArgs()
            {
                Device = device,
            };

            Device.Invoke(
                device,
                args);

            if (!args.QuestionShown)
            {
                if (_pairRequestNotification != null)
                    _pairRequestNotification.Dispose();

                _pairRequestNotification = SandaabContext.Notifications.ShowPairRequest(
                    device,
                    string.Format(Locale.PairNotificationTitle, device.Name),
                    Locale.PairNotificationMessage,
                    (args) =>
                    {
                        if (args is Notifications.ContentClickEventArgs)
                            SandaabContext.App.RunOnUiThreadAsync(
                                () =>
                                {
                                    PairRequestNotification_Clicked(device);
                                });
                        else if (args is Notifications.PairAnswerEventArgs pairAnswer)
                        {
                            device.SendQueued(
                                new PairResponseCommand()
                                {
                                    Granted = pairAnswer.Granted,
                                });
                        }

                        _pairRequestNotification.Dispose();
                        _pairRequestNotification = null;
                    });
            }
        }

        public virtual void PairRequestNotification_Clicked(Device device)
        {
            throw new NotImplementedException();
        }

        private void DoPairResponse(Device device, Command command)
        {
            if (_pairRequestNotification != null)
            {
                _pairRequestNotification.Dispose();
                _pairRequestNotification = null;
            }

            var pairedInfo = command as PairResponseCommand;

            if (pairedInfo.Granted)
                device.State = DeviceState.Paired;

            Device.InvokeAsync(
                device,
                new PairResponseEventArgs()
                {
                    Granted = pairedInfo.Granted,
                });
        }

        private void DoDisconnectInfo(Device device, Command command)
        {
            var disconnectCommand = command as DisconnectCommand;

            if (disconnectCommand.Unpair)
                SandaabContext.Devices.RemoveAsync(device);
            else
                device.ClearRoutes(false);
        }

        private Command DoRemoteControlRequestCommand(Device device, Command command)
        {
            var remoteControlRequest = command as RemoteControlRequestCommand;

            if (_remoteControlSender != null
                && _remoteControlSender.Device != device)
                return new RemoteControlResponseCommand()
                {
                    Result = RemoteControlResponseCommand.ResultType.AccessDenied,
                };

            if (remoteControlRequest.Event == RemoteControlRequestCommand.EventType.Stop)
            {
                if (_remoteControlSender != null)
                {
                    _remoteControlSender.Dispose();
                    _remoteControlSender = null;
                }
            }
            else
            {
                Logger.Point(1);
                if (_remoteControlSender == null)
                    _remoteControlSender = new RemoteControlSender(device, remoteControlRequest.Area);
                else
                    _remoteControlSender.Update(remoteControlRequest.Area);
                Logger.Point(1);
            }

            return null;
        }

        private void DoRemoteControlResponseCommand(Device device, Command command)
        {
            var remoteControlResponseCommand = (RemoteControlResponseCommand)command;

            if (_remoteControlDevices.ContainsKey(device))
                _remoteControlDevices[device].Invoke(remoteControlResponseCommand);
        }

        public void RegisterRemoteController(Device device, Action<RemoteControlResponseCommand> action)
        {
            Debug.Assert(!_remoteControlDevices.ContainsKey(device));

            _remoteControlDevices.Add(device, action);
        }

        public void UnregisterRemoteController(Device device)
        {
            Debug.Assert(_remoteControlDevices.ContainsKey(device));

            _remoteControlDevices.Remove(device);
        }
    }
}
