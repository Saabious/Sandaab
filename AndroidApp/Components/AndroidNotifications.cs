using Android.Content;
using Android.OS;
using Sandaab.AndroidApp.Activities;
using Sandaab.AndroidApp.Entities;
using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Properties;
using Notification = Sandaab.Core.Entities.Notification;

namespace Sandaab.AndroidApp.Components
{
    internal class AndroidNotifications : Notifications, IBroadcastReceiver
    {
        internal class Action
        {
            private const string Base = "com.sandaab.notification.action.";

            public const string Content = Base + "CONTENT";
            public const string Delete = Base + "DELETE";
            public const string Pair = Base + "PAIR";
            public const string Reject = Base + "REJECT";
            public const string Stop = Base + "STOP";
            // Warning: On further actions, don't forget to add an intent filter
        }

        internal class Extras
        {
            private const string Base = "com.sandaab.notification.extra.";

            public const string Id = Base + "ID";
        }

        private const string CHANNEL_ID_LOCAL = "LOCAL";
        private const string CHANNEL_ID_NETWORK = "NETWORK";
        private const string CHANNEL_ID_PAIR = "PAIR";
        private const string CHANNEL_ID_REMOTEDEVICE_PREFIX = "DEVICE.";
        private const string CHANNEL_ID_REMOTECONTROL = "REMOTECONTROL";

        private static int _id = 1;
        private NotificationChannel _networkChannel;
        public NotificationManager NotificationManager { get; private set; }
        private NotificationChannel _pairNotificationChannel;
        private NotificationChannel _remoteControlChannel;
        private Dictionary<Device, NotificationChannel> _remoteDeviceChannels;

        public AndroidNotifications()
        {
            _remoteDeviceChannels = new();
        }

        public override Task InitializeAsync()
        {
            var intentFilter = new IntentFilter();
            intentFilter.AddAction(Action.Content);
            intentFilter.AddAction(Action.Delete);
            intentFilter.AddAction(Action.Pair);
            intentFilter.AddAction(Action.Reject);
            intentFilter.AddAction(Action.Stop);
            MainActivity.Instance.RegisterReceiver(new BroadcastReceiver(this), intentFilter);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationManager = (NotificationManager)MainActivity.Instance.GetSystemService(Context.NotificationService);

                _networkChannel = NotificationManager.GetNotificationChannel(CHANNEL_ID_NETWORK);
                if (_networkChannel == null)
                {
                    _networkChannel = new NotificationChannel(
                        CHANNEL_ID_NETWORK,
                        Locale.NotificationChannelNetworkName,
                        NotificationImportance.Default)
                    {
                        Description = Locale.NotificationChannelNetworkDescription,
                    };
                    NotificationManager.CreateNotificationChannel(_networkChannel);
                }

                _pairNotificationChannel = NotificationManager.GetNotificationChannel(CHANNEL_ID_PAIR);
                if (_pairNotificationChannel == null)
                {
                    _pairNotificationChannel = new NotificationChannel(
                        CHANNEL_ID_PAIR,
                        Locale.NotificationChannelPairName,
                        NotificationImportance.Max)
                    {
                        Description = Locale.NotificationChannelPairDescription,
                    };
                    NotificationManager.CreateNotificationChannel(_pairNotificationChannel);
                }

                NotificationManager.DeleteNotificationChannel(CHANNEL_ID_REMOTECONTROL);
                _remoteControlChannel = NotificationManager.GetNotificationChannel(CHANNEL_ID_REMOTECONTROL);
                if (_remoteControlChannel == null)
                {
                    _remoteControlChannel = new NotificationChannel(
                        CHANNEL_ID_REMOTECONTROL,
                        Locale.NotificationChannelRemoteControlName,
                        NotificationImportance.Low)
                    {
                        Description = Locale.NotificationChannelRemoteControlDescription,
                    };
                    _remoteControlChannel.SetShowBadge(false);
                    NotificationManager.CreateNotificationChannel(_remoteControlChannel);
                }
            }

            Device.Event += Device_Event;

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            Device.Event -= Device_Event;

            base.Dispose();
        }

        private int BuildId()
        {
            return _id++;
        }

        private void Device_Event(object sender, System.EventArgs args)
        {
            var device = sender as Device;

            if (args is Device.ChangeEventArgs changeEventArgs)
            {
                switch (changeEventArgs.Change)
                {
                    case DeviceEvent.Paired:
                        RemoteChannelId(device);
                        break;
                    case DeviceEvent.Removed:
                        RemoveRemoteChannel(device);
                        break;
                }
            }
        }

        private string RemoteChannelId(Device device)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return null;

            var channelId = CHANNEL_ID_REMOTEDEVICE_PREFIX + device.Id;

            if (!_remoteDeviceChannels.ContainsKey(device))
            {
                var notificationChannel = NotificationManager.GetNotificationChannel(channelId);

                if (notificationChannel == null)
                {
                    notificationChannel = new NotificationChannel(
                        channelId,
                        string.Format(Locale.NotificationChannelRemoteName, device.Name),
                        NotificationImportance.High)
                    {
                        Description = Locale.NotificationChannelRemoteDescription,
                    };
                    NotificationManager.CreateNotificationChannel(notificationChannel);
                }

                _remoteDeviceChannels.Add(device, notificationChannel);
            }

            return _remoteDeviceChannels[device].Id;
        }

        private void RemoveRemoteChannel(Device device)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            NotificationManager.DeleteNotificationChannel(CHANNEL_ID_REMOTEDEVICE_PREFIX + device.Id);
        }

        public void IntentReceived(Context context, Intent intent)
        {
            var id = intent.Extras.GetInt(Extras.Id);

            foreach (var notification in this)
                if (notification.Id == id)
                {
                    ((AndroidNotification)notification).IntentReceived(intent);
                    break;
                }
        }

        public override Notification ShowNetworkMessage(string title, string message, Action<EventArgs> action = null)
        {
            return Show(AndroidNotification.CreateMessage(_networkChannel.Id, BuildId(), title, message, action));
        }

        public override Notification ShowRemoteMessage(Device device, string title, string message, Action<EventArgs> action)
        {
            return Show(AndroidNotification.CreateMessage(RemoteChannelId(device), BuildId(), title, message, action));
        }

        public override Notification ShowPairRequest(Device device, string title, string message, Action<EventArgs> action)
        {
            return Show(AndroidNotification.CreatePairRequest(_pairNotificationChannel.Id, BuildId(), title, message, device.Id, action));
        }

        public AndroidNotification CreateScreenCaptureServiceHint(string title, string message, Action<EventArgs> action)
        {
            return AndroidNotification.CreateScreenCaptureServiceHint(_remoteControlChannel.Id, BuildId(), title, message, action);
        }

        private Notification Show(AndroidNotification notification)
        {
            lock (this)
                Add(notification);

            NotificationManager.Notify(notification.Id, notification.Notification);

            return notification;
        }
    }
}
