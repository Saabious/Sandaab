using Sandaab.Core;
using Sandaab.Core.Entities;
using System.Drawing.Imaging;
using System.Management;
using static Sandaab.WindowsApp.Win32.SHCore;
using static Sandaab.WindowsApp.Win32.User32;
using Screen = System.Windows.Forms.Screen;

namespace Sandaab.WindowsApp.Components
{
    internal class WindowsScreen : Core.Components.Screen
    {
        public override RemoteControlResponseCommand Capture(ScreenArea area, bool getPrevious)
        {
            var bitmap = new Bitmap(
                Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height,
                PixelFormat.Format32bppArgb);

            var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(
                Screen.PrimaryScreen.Bounds.X,
                Screen.PrimaryScreen.Bounds.Y,
                0,
                0,
                Screen.PrimaryScreen.Bounds.Size,
                CopyPixelOperation.SourceCopy);

            bitmap.Save("C:\\Screenshot.png", ImageFormat.Png);

            return new RemoteControlResponseCommand()
            {

            };
        }

        public override void Draw(object control, RemoteControlResponseCommand remoteControlResponse)
        {
            var graphics = control as Graphics;

            var fileStream = new FileStream("C:\\Test.png", FileMode.Create);
            fileStream.Write(remoteControlResponse.Attachments[0]);
            fileStream.Dispose();

            var stream = new MemoryStream(remoteControlResponse.Attachments[0]);
            stream.Seek(0, SeekOrigin.Begin);
            var image = Bitmap.FromStream(stream);
            SandaabContext.App.RunOnUiThread(
                () =>
                {
                    graphics.DrawImage(image, remoteControlResponse.Frame.X, remoteControlResponse.Frame.Y);
                });
        }

        public static void GetDpi(Form form, out float dpiX, out float dpiY)
        {
            var hMonitor = MonitorFromWindow(form.Handle, MONITOR_DEFAULTTOPRIMARY);
            GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_RAW_DPI, out var x, out var y);
            dpiX = x;
            dpiY = y;
        }
    }
}
