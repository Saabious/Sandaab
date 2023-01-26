using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Display;
using Android.Media;
using Android.Media.Projection;
using Android.OS;
using Android.Views;
using Java.Lang;
using Sandaab.AndroidApp.Activities;
using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Properties;

namespace Sandaab.AndroidApp.Components
{
    [Service(Name = "com.sandaab.service.ScreenCaptureService")]
    internal class ScreenCaptureService : Service, ScreenCaptureService.ImageAvailableListener.IImageAvailableListener
    {
        internal class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
        {
            public interface IImageAvailableListener
            {
                public void OnImageAvailable(ImageReader reader);
            }

            private IImageAvailableListener _listener;

            public ImageAvailableListener(IImageAvailableListener listener)
                : base()
            {
                _listener = listener;
            }

            public void OnImageAvailable(ImageReader reader)
            {
                _listener.OnImageAvailable(reader);
            }
        }

        internal class MediaProjectionCallback : MediaProjection.Callback
        {
            public override void OnStop()
            {
                _instance.StopCapturing();
            }
        }

        public class EventArgs : System.EventArgs
        {
            public ScreenCaptureServiceChange Change;
        }

        private const int THREAD_PRIORITY_BACKGROUND = 10;

        public class Extras
        {
            private const string Base = "com.sandaab.service.ScreenCaptureService.extra.";

            public const string ResultCode = Base + "RESULT_CODE";
            public const string ResultIntent = Base + "RESULT_INTENT";
        }

        private static Bitmap _bitmap;
        private static int _capturingCounter;
        public static event EventHandler Event;
        private static bool _newBitmapAvailable;
        private ImageReader _imageReader;
        private static ScreenCaptureService _instance;
        private static object _lockObj;
        private MediaProjection _mediaProjection;
        public static bool IsStopped { get { return _instance == null; } }
        private VirtualDisplay _virtualDisplay;

        public ScreenCaptureService()
        {
            _lockObj ??= new();

            MainActivity.Instance.StartScreenshotService();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            System.Diagnostics.Debug.Assert(_capturingCounter == 0);

            var resultCode = intent.GetIntExtra(Extras.ResultCode, 0);
            var resultData
                = Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu
#pragma warning disable CS0618 // Typ oder Element ist veraltet
                ? intent.Extras.GetParcelable(Extras.ResultIntent) as Intent
#pragma warning restore CS0618 // Typ oder Element ist veraltet
#pragma warning disable CA1416 // Plattformkompatibilität überprüfen
                : intent.Extras.GetParcelable(Extras.ResultIntent, Class.FromType(typeof(Intent))) as Intent;
#pragma warning restore CA1416 // Plattformkompatibilität überprüfen
            StartCapturing(resultCode, resultData);

            return StartCommandResult.NotSticky;
        }

        private void Notification_Action(Notifications.EventArgs args)
        {
            if (args is Notifications.StopEventArgs)
                StopCapturing();
        }

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        public override void OnDestroy()
        {
            StopCapturing();

            base.OnDestroy();
        }

        private void StartCapturing(int resultCode, Intent resultData)
        {
            var notification = ((AndroidNotifications)SandaabContext.Notifications).CreateScreenCaptureServiceHint(
                Locale.ScreenshotServiceNotificationTitle,
                Locale.ScreenshotServiceNotificationMessage,
                Notification_Action);

            StartForeground(notification.Id, notification.Notification, ForegroundService.TypeMediaProjection);

            var mediaProjectionManager = (MediaProjectionManager)MainActivity.Instance.GetSystemService(Context.MediaProjectionService);
            _mediaProjection = mediaProjectionManager.GetMediaProjection(resultCode, resultData);

            var windowManager = MainActivity.Instance.WindowManager;
            var width = windowManager.CurrentWindowMetrics.Bounds.Width();
            var height = windowManager.CurrentWindowMetrics.Bounds.Height();

            var backgroundThread = new HandlerThread(this.GetType().FullName, THREAD_PRIORITY_BACKGROUND);
            backgroundThread.Start();
            var backgroundHandler = new Handler(backgroundThread.Looper);

            _imageReader = ImageReader.NewInstance(width, height, (ImageFormatType)1 /* PixelFormat.RGBA_8888 */, 2);
            _imageReader.SetOnImageAvailableListener(new ImageAvailableListener(this), backgroundHandler);

            _virtualDisplay = _mediaProjection.CreateVirtualDisplay(
                Config.AppName,
                _imageReader.Width,
                _imageReader.Height,
                (int)MainActivity.Instance.Resources.DisplayMetrics.DensityDpi,
                (DisplayFlags)(VirtualDisplayFlags.OwnContentOnly | VirtualDisplayFlags.Public),
                _imageReader.Surface,
                null,
                backgroundHandler);

            _mediaProjection.RegisterCallback(new MediaProjectionCallback(), backgroundHandler);

            SandaabContext.App.InvokeAsync(Event, this, new EventArgs() { Change = ScreenCaptureServiceChange.Started });
        }

        private void StopCapturing()
        {
            StopForeground(StopForegroundFlags.Remove);

            _mediaProjection.Stop();
            _virtualDisplay.Release();

            lock (_lockObj)
                _bitmap = null;

            _instance = null;
            _capturingCounter = 0;
            _newBitmapAvailable = false;

            StopSelf();

            SandaabContext.App.InvokeAsync(Event, this, new EventArgs() { Change = ScreenCaptureServiceChange.Stopped });
        }

        public void OnImageAvailable(ImageReader reader)
        {
            if (_instance != null)
                lock (_lockObj)
                {
                    var image = _instance._imageReader.AcquireLatestImage();
                    if (image == null)
                        return;

                    if (_bitmap == null)
                        _bitmap = Bitmap.CreateBitmap(image.Width, image.Height, Bitmap.Config.Argb8888);
                    else if (_bitmap.Width != image.Width
                        || _bitmap.Height != image.Height)
                        _bitmap.Reconfigure(image.Width, image.Height, Bitmap.Config.Argb8888);

                    var planes = image.GetPlanes();
                    if (planes.Length > 0)
                    {
                        _bitmap.CopyPixelsFromBuffer(planes[0].Buffer);
                        _newBitmapAvailable = true;
                    }

                    image.Close();
                }
        }

        public static bool GetBitmap(out Bitmap bitmap)
        {
            lock (_lockObj)
            {
                bitmap = _bitmap;
                
                var newBitmapAvailable = _newBitmapAvailable;
                _newBitmapAvailable = false;

                return newBitmapAvailable;
            }
        }

        public static void BeginCaptureing()
        {
            if (_capturingCounter == 0)
                _instance = new ScreenCaptureService();

            _capturingCounter++;
        }

        public static void EndCaptureing()
        {
            System.Diagnostics.Debug.Assert(_capturingCounter > 0);

            _capturingCounter--;

            if (_capturingCounter == 0)
            {
                _instance.StopCapturing();
                _instance = null;
            }
        }
    }
}
