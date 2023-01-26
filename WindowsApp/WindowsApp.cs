using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualBasic.ApplicationServices;
using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Properties;
using Sandaab.WindowsApp.Components;
using Sandaab.WindowsApp.Forms;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Sandaab.WindowsApp
{
    internal class WindowsApp : WindowsFormsApplicationBase, IApp
    {
        private Semaphore _mainFormCreated;
        private SynchronizationContext _synchronizationContext;
        public static SandaabContext SandaabContext { get; private set; }
        private Task _sandaabContextInitializationTask;
        
        public WindowsApp()
        {
            IsSingleInstance = true;
        }

        protected override bool OnInitialize(ReadOnlyCollection<string> commandLineArgs)
        {
            Application.SetCompatibleTextRenderingDefault(false);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            _synchronizationContext = new WindowsFormsSynchronizationContext();
            _mainFormCreated = new(0, 1);

            SandaabContext = new(
                this,
                new WindowsNetwork(),
                new WindowsLocalDevice(),
                new WindowsNotifications(),
                new WindowsScreen());
            _sandaabContextInitializationTask = SandaabContext.InitializeAsync
            (
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + Core.Properties.Config.AppName + Path.DirectorySeparatorChar + string.Format(Config.LogFilename, Config.AppName),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + Core.Properties.Config.AppName + Path.DirectorySeparatorChar + string.Format(Config.DatabaseFilename, Config.AppName),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + Core.Properties.Config.AppName + Path.DirectorySeparatorChar + string.Format(Config.SettingsFilename, Config.AppName)
            );
            _sandaabContextInitializationTask.ContinueWith(OnInitialized, TaskScheduler.FromCurrentSynchronizationContext());

            return base.OnInitialize(commandLineArgs);
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs args)
        {
            RunOnUiThreadAsync(
                () => {
                    base.OnStartupNextInstance(args);
                });
        }

        protected override void OnCreateMainForm()
        {
            Application.EnableVisualStyles();
            MainForm = new MainForm();

            _mainFormCreated.Release();
        }

        private void OnInitialized(Task task, object arg)
        {
            _mainFormCreated.WaitOne();
            RunOnUiThreadAsync(() => { ((MainForm)MainForm).Initialized(task); });
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();

            _sandaabContextInitializationTask.Wait();
            SandaabContext.Dispose();
            SandaabContext = null;
        }

        protected override bool OnUnhandledException(Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs args)
        {
            Logger.Fatal(args.Exception);
            return base.OnUnhandledException(args);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs args)
        {
            Logger.Fatal(args.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs args)
        {
            Logger.Fatal((Exception)args.ExceptionObject);
        }

        public void InvokeAsync(EventHandler eventHandler, object sender, EventArgs args)
        {
            Debug.Assert(args != null);

            if (eventHandler != null)
                RunOnUiThreadAsync(
                    () =>
                    {
                        Debug.Assert(args != null);

                        eventHandler.Invoke(sender, args);
                    });
        }

        public void Invoke(EventHandler eventHandler, object sender, EventArgs args)
        {
            if (eventHandler != null)
                RunOnUiThread(
                    () => {
                        eventHandler.Invoke(sender, args);
                    });
        }

        public void RunOnUiThreadAsync(Action action)
        {
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

            _synchronizationContext.Post(
                (state) => {
                    action.Invoke();
                },
                null);
        }

        public void RunOnUiThread(Action action)
        {
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

            _synchronizationContext.Send(
                (state) => {
                    action.Invoke();
                },
                null);
        }
    }
}
