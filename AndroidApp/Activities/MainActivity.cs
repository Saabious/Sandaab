using Android.Content;
using Android.Media.Projection;
using Android.Runtime;
using Android.Views;
using Sandaab.AndroidApp.Components;
using Sandaab.AndroidApp.Dialogs;
using Xamarin.Essentials;
using Config = Sandaab.Core.Properties.Config;

namespace Sandaab.AndroidApp.Activities
{
    [Activity(MainLauncher = true)]
    internal class MainActivity : NavigationActivity, IBroadcastReceiver
    {
        private enum RequestCode
        {
            ScreenCapture,
        }

        public static MainActivity Instance { get; private set; }
        private bool _waitingForScreenCaptureIntent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState, Resource.Layout.content_main, Resource.Layout.activity_navigation);

            Instance = this;

#if DEBUG
            Window.AddFlags(WindowManagerFlags.TurnScreenOn | WindowManagerFlags.DismissKeyguard);
#endif

            SupportActionBar.Title = Config.AppName;

            AndroidApp.SandaabContext.InitializeAsync(
                GetExternalFilesDir(null).ToString() + Path.DirectorySeparatorChar + string.Format(Config.LogFilename, Config.AppName),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + string.Format(Config.DatabaseFilename, Config.AppName),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + string.Format(Config.SettingsFilename, Config.AppName))
                .ContinueWith
                (
                    task =>
                    {
                        if (task.IsFaulted)
                            MessageDialog.Show(task.Exception.Message, Core.Properties.Locale.MessageBoxTitleError) // Todo: Bessere Fehlermeldung
                                .BackWith(() => { FinishAndRemoveTask(); })
                                .CancelWith(() => { FinishAndRemoveTask(); });
                        else if (Core.SandaabContext.Devices.Count == 0
                            && !_waitingForScreenCaptureIntent)
                        {
                            var intent = new Intent(this, typeof(AddDeviceActivity));
                            intent.SetAction(AddDeviceActivity.Action.Install);
                            StartActivity(intent);
                        }
                    },
                    TaskScheduler.FromCurrentSynchronizationContext()
                );

            var intentFilter = new IntentFilter();
            intentFilter.AddAction(Intent.ActionShutdown);
            RegisterReceiver(new Components.BroadcastReceiver(this), intentFilter);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == (int)RequestCode.ScreenCapture)
            {
                _waitingForScreenCaptureIntent = false;
                if (resultCode == Result.Ok)
                {
                    var intent = new Intent(this, Java.Lang.Class.FromType(typeof(ScreenCaptureService)));
                    intent.PutExtra(ScreenCaptureService.Extras.ResultCode, (int)resultCode);
                    intent.PutExtra(ScreenCaptureService.Extras.ResultIntent, data);
                    StartService(intent);
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public void IntentReceived(Context context, Intent intent)
        {
            switch (intent.Action)
            {
                case Intent.ActionShutdown:
                    AndroidApp.SandaabContext.Dispose();
                    break;
            }
        }

        public void StartScreenshotService()
        {
            _waitingForScreenCaptureIntent = true;

            var mediaProjectionManager = (MediaProjectionManager)GetSystemService(MediaProjectionService);
            var intent = mediaProjectionManager.CreateScreenCaptureIntent();
            StartActivityForResult(intent, (int)RequestCode.ScreenCapture);
        }
    }
}
