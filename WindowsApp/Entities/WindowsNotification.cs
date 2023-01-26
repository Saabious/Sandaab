using Microsoft.Toolkit.Uwp.Notifications;
using Sandaab.Core.Components;
using Sandaab.Core.Properties;
using Sandaab.WindowsApp.Components;
using Windows.UI.Notifications;
using Notification = Sandaab.Core.Entities.Notification;

namespace Sandaab.WindowsApp.Entities
{
    internal class WindowsNotification : Notification, IDisposable
    {
        public ToastNotification ToastNotification { get; private set; }

        private WindowsNotification(NotificationType type, string group, int id, ToastContentBuilder builder, Action<Notifications.EventArgs> action = null)
            : base(type, id, action)
        {
            builder
                .AddToastActivationInfo(WindowsNotifications.BuildArgument(id, Command.ContentClick), ToastActivationType.Foreground);

            ToastNotification = new ToastNotification(builder.GetXml())
            {
                Tag = id.ToString(),
            };
            ToastNotification.Dismissed += ToastNotification_Dismissed;
            ToastNotification.Failed += ToastNotification_Failed;
            ToastNotification.Group = group;
        }

        public override void Dispose()
        {
            ToastNotificationManagerCompat.History.Remove(Id.ToString());

            base.Dispose();
        }

        public static WindowsNotification CreateMessage(string group, int id, string title, string message, Action<Notifications.EventArgs> action = null)
        {
            return new WindowsNotification(
                NotificationType.Message,
                group,
                id,
                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message),
                action);
        }

        public static WindowsNotification CreatePairRequest(string group, int id, string title, string message, string deviceId, Action<Notifications.EventArgs> action = null)
        {
            return new WindowsNotification(
                NotificationType.PairQuestion,
                group,
                id,
                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .AddButton(new ToastButton(
                        Locale.PairButtonText,
                        WindowsNotifications.BuildArgument(id, Command.Pair)))
                    .AddButton(new ToastButton(
                        Locale.RejectButtonText,
                        WindowsNotifications.BuildArgument(id, Command.Dismiss)))
                    .SetToastDuration(ToastDuration.Long),
                action)
            {
                DeviceId = deviceId,
            };
        }

        private void ToastNotification_Failed(ToastNotification toastNotification, ToastFailedEventArgs args)
        {
            Logger.Error(args.ErrorCode);

            Action?.Invoke(
                new Notifications.FailedEventArgs()
                {
                    Notification = this,
                    Exception = args.ErrorCode,
                });
        }

        private void ToastNotification_Dismissed(ToastNotification toastNotification, ToastDismissedEventArgs args)
        {
            if (args.Reason == ToastDismissalReason.UserCanceled)
                Activated(Command.Dismiss.ToString());
        }

        public void Activated(string argument)
        {
            Dispatch(this, Enum.Parse<Command>(argument));
        }
    }
}
