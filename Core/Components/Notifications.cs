using Sandaab.Core.Entities;
using System.Collections.ObjectModel;

namespace Sandaab.Core.Components
{
    public class Notifications : Collection<Notification>, IDisposable
    {
        public class EventArgs : System.EventArgs
        {
            public Notification Notification;
        }

        public class FailedEventArgs : EventArgs
        {
            public Exception Exception;
        }

        public class CancelEventArgs : EventArgs
        {
        }

        public class StopEventArgs : EventArgs
        {
        }

        public class ContentClickEventArgs : EventArgs
        {
        }

        public class PairAnswerEventArgs : EventArgs
        {
            public bool Granted;
        }

        public virtual void Dispose()
        {
            for (var i = Count - 1; i >= 0; i--)
                this[i].Dispose();

            GC.SuppressFinalize(this);
        }

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Notification ShowNetworkMessage(string title, string message, Action<EventArgs> action = null)
        {
            throw new NotImplementedException();
        }

        public virtual Notification ShowRemoteMessage(Device device, string title, string message, Action<EventArgs> action)
        {
            throw new NotImplementedException();
        }

        public virtual Notification ShowPairRequest(Device device, string title, string message, Action<EventArgs> action)
        {
            throw new NotImplementedException();
        }
    }
}
