using Android.Runtime;
using Sandaab.AndroidApp.Activities;
using Sandaab.AndroidApp.Components;
using Sandaab.Core;

namespace Sandaab.AndroidApp
{
    [Application]
    public class AndroidApp : Application, IApp
    {
        public static SandaabContext SandaabContext { get; private set; }

        protected AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            SandaabContext = new(
                this,
                new AndroidNetwork(),
                new AndroidLocalDevice(),
                new AndroidNotifications(),
                new AndroidScreen());
        }

        protected override void Dispose(bool disposing)
        {
            SandaabContext.Dispose();

            base.Dispose(disposing);
        }

        public void InvokeAsync(EventHandler eventHandler, object sender, EventArgs args)
        {
            if (eventHandler != null)
                RunOnUiThreadAsync(
                    new Action(
                        () =>
                        {
                            eventHandler.Invoke(sender, args);
                        }));
        }

        public void Invoke(EventHandler eventHandler, object sender, EventArgs args)
        {
            if (eventHandler != null)
                RunOnUiThread(
                    new Action(
                        () =>
                        {
                            eventHandler.Invoke(sender, args);
                        }));
        }

        public void RunOnUiThreadAsync(Action action)
        {
            BaseActivity.CurrentInstance.RunOnUiThread(
                () =>
                {
                    action.Invoke();
                });
        }

        public void RunOnUiThread(Action action)
        {
            var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            BaseActivity.CurrentInstance.RunOnUiThread(
                () =>
                {
                    action?.Invoke();

                    eventWaitHandle.Set();
                });

            eventWaitHandle.WaitOne();
        }

        internal void RegisterComponentCallbacks(object nils)
        {
            throw new NotImplementedException();
        }
    }
}
