using Android.Graphics;
using Sandaab.AndroidApp.Activities;
using Sandaab.Core.Components;
using Sandaab.Core.Entities;

namespace Sandaab.AndroidApp.Components
{
    internal class AndroidScreen : Core.Components.Screen
    {
        private DateTimeOffset _checkImageFormatAfter;
        private float _dpiX;
        private float _dpiY;
        private RemoteControlResponseCommand.ImageFormat _imageFormat;

        public override Task InitializeAsync()
        {
            _checkImageFormatAfter = DateTimeOffset.Now;
            _imageFormat = RemoteControlResponseCommand.ImageFormat.Png;

            _dpiX = MainActivity.Instance.Resources.DisplayMetrics.Xdpi;
            _dpiY = MainActivity.Instance.Resources.DisplayMetrics.Ydpi;

            return base.InitializeAsync();
        }

        public override RemoteControlResponseCommand Capture(ScreenArea area, bool getPrevious)
        {
            Logger.Point(3);

            if (ScreenCaptureService.IsStopped)
                return new()
                {
                    Result = RemoteControlResponseCommand.ResultType.Stopped,
                };

            if ((!ScreenCaptureService.GetBitmap(out var bitmap)
                && !getPrevious)
                || bitmap == null)
                return null;

            var screenWidth = bitmap.Width;
            var screenHeight = bitmap.Height;

            // Scale down bitmap, if remote DPIs are smaller
            var scale = area.DpiX < _dpiX;
            var scaleFactorX = scale ? area.DpiX / _dpiX : 1;
            var scaleFactorY = scale ? area.DpiY / _dpiY : 1;

            if (area.X != 0 || area.Y != 0 || area.Width != bitmap.Width || area.Height != bitmap.Height)
                bitmap = Bitmap.CreateBitmap(
                    bitmap,
                    area.X,
                    area.Y,
                    (int)Math.Round(area.Width / scaleFactorX),
                    (int)Math.Round(area.Height / scaleFactorY));

            Logger.Point(6);

            if (scale)
                bitmap = Bitmap.CreateScaledBitmap(
                    bitmap,
                    (int)Math.Round(bitmap.Width * scaleFactorX),
                    (int)Math.Round(bitmap.Height * scaleFactorY),
                    false);

            Logger.Point(4);

            var quality = 100;
            byte[] attachment;

            if (_checkImageFormatAfter < DateTimeOffset.Now)
            {
                var pngStream = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Png, quality, pngStream);
                var jpegStream = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, quality, jpegStream);
                if (pngStream.Length < jpegStream.Length)
                {
                    _imageFormat = RemoteControlResponseCommand.ImageFormat.Png;
                    attachment = pngStream.ToArray();
                }
                else
                {
                    _imageFormat = RemoteControlResponseCommand.ImageFormat.Jpeg;
                    attachment = jpegStream.ToArray();
                }
                _checkImageFormatAfter = DateTimeOffset.Now + TimeSpan.FromSeconds(10);
            }
            else
            {
                var stream = new MemoryStream();
                if (_imageFormat == RemoteControlResponseCommand.ImageFormat.Png)
                    bitmap.Compress(Bitmap.CompressFormat.Png, quality, stream);
                else
                    bitmap.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                attachment = stream.ToArray();
            }

            Logger.Point(5);

            return new RemoteControlResponseCommand()
            {
                Attachments = new[]
                {
                    attachment,
                },
                DpiX = scale ? area.DpiX : _dpiX,
                DpiY = scale ? area.DpiY : _dpiY,
                Frame =
                    new RemoteControlResponseCommand.FrameRecord()
                    {
                        X = area.X,
                        Y = area.Y,
                        ImageFormat = _imageFormat,
                    },
                Result = RemoteControlResponseCommand.ResultType.Valid,
                Scaled = scale,
                ScreenHeight = screenHeight,
                ScreenWidth = screenWidth,
            };
        }

        public override void Draw(object control, RemoteControlResponseCommand command)
        {
            //// byte[] -> Bitmap
            //var byteBuffer2 = ByteBuffer.Wrap(attachment);
            //_activityBitmap.CopyPixelsFromBuffer(byteBuffer2);
        }

        public override void BeginCaptureing()
        {
            ScreenCaptureService.BeginCaptureing();
        }

        public override void EndCaptureing()
        {
            ScreenCaptureService.EndCaptureing();
        }
    }
}
