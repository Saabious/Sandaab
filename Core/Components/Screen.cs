using Sandaab.Core.Entities;

namespace Sandaab.Core.Components
{
    public class Screen : IDisposable
    {
        public Screen()
        { 
        }

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public virtual RemoteControlResponseCommand Capture(ScreenArea area, bool getPrevious)
        {
            throw new NotImplementedException();
        }

        public virtual void Draw(object control, RemoteControlResponseCommand command)
        {
            throw new NotImplementedException();
        }

        public virtual void BeginCaptureing()
        {
        }

        public virtual void EndCaptureing()
        {
        }
    }
}
