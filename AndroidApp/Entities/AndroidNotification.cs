using Android.Views;
using Sandaab.AndroidApp.Activities;
using Sandaab.AndroidApp.Components;
using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Properties;
using Intent = Android.Content.Intent;
using Notification = Sandaab.Core.Entities.Notification;

namespace Sandaab.AndroidApp.Entities
{
    internal class AndroidNotification : Notification
    {
        public Android.App.Notification Notification { get; private set; }

        private AndroidNotification(NotificationType type, int id, Android.App.Notification.Builder builder, Action<Notifications.EventArgs> action = null)
            : base(type, id, action)
        {
            Notification = builder
                .SetSmallIcon(Resource.Mipmap.ic_notification)
                .SetContentIntent(BuildPendingEvent(id, AndroidNotifications.Action.Content))
                .SetDeleteIntent(BuildPendingEvent(id, AndroidNotifications.Action.Delete))
                .SetAutoCancel(true)
                .Build();

            SandaabContext.Notifications.Add(this);
        }

        public override void Dispose()
        {
            ((AndroidNotifications)SandaabContext.Notifications).NotificationManager.Cancel(Id);

            GC.SuppressFinalize(this);
        }

        public static AndroidNotification CreateMessage(string channelId, int id, string title, string message, Action<Notifications.EventArgs> action)
        {
            return new AndroidNotification(
                NotificationType.Message,
                id,
                new Android.App.Notification.Builder(
                    MainActivity.Instance,
                    channelId)
                    .SetContentTitle(title)
                    .SetContentText(message),
                action);
        }

        public static AndroidNotification CreatePairRequest(string channelId, int id, string title, string message, string deviceId, Action<Notifications.EventArgs> action)
        {
            return new AndroidNotification(
                NotificationType.PairQuestion,
                id,
                new Android.App.Notification.Builder(
                    MainActivity.Instance,
                    channelId)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .AddAction(BuildAction(id, Resource.Drawable.ic_keyboard_black_24dp, Locale.RejectButtonText, AndroidNotifications.Action.Reject))
                    .AddAction(BuildAction(id, Resource.Drawable.ic_keyboard_black_24dp, Locale.PairButtonText, AndroidNotifications.Action.Pair)),
                action)
            {
                DeviceId = deviceId,
            };
        }

        public static AndroidNotification CreateScreenCaptureServiceHint(string channelId, int id, string title, string message, Action<Notifications.EventArgs> action)
        {
            return new AndroidNotification(
                NotificationType.ScreenCaptureServiceHint,
                id,
                new Android.App.Notification.Builder(
                    MainActivity.Instance,
                    channelId)
                    .SetContentTitle(title)
                    .AddAction(BuildAction(id, Resource.Drawable.ic_keyboard_black_24dp, Locale.StopButtonText, AndroidNotifications.Action.Stop)),
                action);
        }

        private static Android.App.Notification.Action BuildAction(int id, int icon, string title, string action)
        {
            return new Android.App.Notification.Action.Builder(
                icon,
                title,
                BuildPendingEvent(id, action))
                .Build();
        }

        private static PendingIntent BuildPendingEvent(int id, string action)
        {
            var intent = new Intent();
            intent.PutExtra(AndroidNotifications.Extras.Id, id);
            intent.SetAction(action);
            return PendingIntent.GetBroadcast(MainActivity.Instance, 0, intent, 0);
        }

        public void IntentReceived(Intent intent)
        {
            Command notifcationCommand;
            if (intent.Action == AndroidNotifications.Action.Content)
                notifcationCommand = Command.ContentClick;
            else if (intent.Action == AndroidNotifications.Action.Delete)
                notifcationCommand = Command.Dismiss;
            else if (intent.Action == AndroidNotifications.Action.Pair)
                notifcationCommand = Command.Pair;
            else if (intent.Action == AndroidNotifications.Action.Stop)
                notifcationCommand = Command.Stop;
            else
                throw new NotImplementedException();

            Dispatch(this, notifcationCommand);
        }
    }
}
