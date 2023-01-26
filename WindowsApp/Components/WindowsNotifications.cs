using Microsoft.Toolkit.Uwp.Notifications;
using Sandaab.Core.Components;
using Sandaab.Core.Properties;
using Sandaab.WindowsApp.Entities;
using System.Text.RegularExpressions;
using Notification = Sandaab.Core.Entities.Notification;

namespace Sandaab.WindowsApp.Components
{
    internal class WindowsNotifications : Notifications, IDisposable
    {
        private int _id;
        public ToastNotifierCompat ToastNotifier { get; private set; }
        private const string NETWORK_GROUP = "Network";
        private const string PAIR_GROUP = "Pair";
        private const string REMOTE_GROUP_PREFIX = "Remote.";

        public WindowsNotifications()
            : base()
        {
            ToastNotifier = ToastNotificationManagerCompat.CreateToastNotifier();

            ToastNotificationManagerCompat.OnActivated += Notification_Activated;
        }

        public override Task InitializeAsync()
        {
            return Task.Run(
                () =>
                {
                    if (ToastNotifier.Setting != Windows.UI.Notifications.NotificationSetting.Enabled)
                        Logger.Warn(string.Format(Messages.NoticationDisabled, ToastNotifier.Setting));
                });
        }

        private void Notification_Activated(ToastNotificationActivatedEventArgsCompat args)
        {
            var match = Regex.Match(args.Argument, "^(\\d+):(.*)$");
            if (match.Success)
            {
                var id = Convert.ToInt32(match.Groups[1].Value);
                lock (this)
                    for (var i = Count - 1; i >= 0; i--)
                        if (this[i].Id == id)
                            ((WindowsNotification)this[i]).Activated(match.Groups[2].Value);
            }
        }

        public static string BuildArgument(int id, Notification.Command command)
        {
            return id.ToString() + ":" + command;
        }

        private int BuildId()
        {
            return _id++;
        }

        public string RemoteGroup(Device device)
        {
            return REMOTE_GROUP_PREFIX + device.DatabaseId;
        }

        public override Notification ShowNetworkMessage(string title, string message, Action<EventArgs> action = null)
        {
            return Show(WindowsNotification.CreateMessage(NETWORK_GROUP, BuildId(), title, message, action));
        }

        public override Notification ShowRemoteMessage(Device device, string title, string message, Action<EventArgs> action)
        {
            return Show(WindowsNotification.CreateMessage(RemoteGroup(device), BuildId(), title, message, action));
        }

        public override Notification ShowPairRequest(Device device, string title, string message, Action<EventArgs> action)
        {
            return Show(WindowsNotification.CreatePairRequest(PAIR_GROUP, BuildId(), title, message, device.Id, action));
        }

        private Notification Show(WindowsNotification notification)
        {
            lock (this)
                Add(notification);

            ToastNotifier.Show(notification.ToastNotification);

            return notification;
        }
    }
}
