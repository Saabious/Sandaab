using Sandaab.Core.Components;

namespace Sandaab.Core.Entities
{
    public class Notification : IDisposable
    {
        public enum NotificationType
        {
            Message,
            PairQuestion,
            ScreenCaptureServiceHint,
        }

        public enum Command
        {
            ContentClick,
            Dismiss,
            Pair,
            Stop,
        }

        public Action<Notifications.EventArgs> Action { get; protected set; }
        public int Id { get; protected set; }
        public NotificationType Type { get; protected set; }
        public string DeviceId { get; protected set; }

        public Notification(NotificationType type, int id, Action<Notifications.EventArgs> action = null)
        {
            Type = type;
            Id = id;
            Action = action;
        }

        public virtual void Dispose()
        {
            lock (SandaabContext.Notifications)
                SandaabContext.Notifications.Remove(this);

            GC.SuppressFinalize(this);
        }

        private Notifications.EventArgs GetEventArgs(Command notificationCommand)
        {
            switch (notificationCommand)
            {
                case Command.ContentClick:
                    switch (Type)
                    {
                        case NotificationType.PairQuestion:
                            if (OperatingSystem.IsWindows())
                                return new Notifications.PairAnswerEventArgs() { Granted = true };
                            else
                                return null;
                        default:
                            return new Notifications.ContentClickEventArgs();
                    }
                case Command.Dismiss:
                    switch (Type)
                    {
                        case NotificationType.PairQuestion:
                            return new Notifications.PairAnswerEventArgs() { Granted = false };
                        default:
                            return new Notifications.CancelEventArgs();
                    }
                case Command.Stop:
                    return new Notifications.StopEventArgs();
                case Command.Pair:
                    return new Notifications.PairAnswerEventArgs() { Granted = true };
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual void Dispatch(Notification notificaton, Command command)
        {
            var args = GetEventArgs(command);
            if (args != null)
            {
                args.Notification = this;

                Action?.Invoke(args);
            }
        }
    }
}
