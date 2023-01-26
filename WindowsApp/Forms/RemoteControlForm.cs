using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;
using Sandaab.WindowsApp.Components;

namespace Sandaab.WindowsApp.Forms
{
    public partial class RemoteControlForm : Form
    {
        private Device _device;
        private float _dpiX;
        private float _dpiY;
        private Graphics _pnlRemoteScreenGraphics;

        public RemoteControlForm(Device device)
        {
            _device = device;

            InitializeComponent();

            Text = _device.Name;

            WindowsScreen.GetDpi(this, out _dpiX, out _dpiY);

            Shown += Form_Shown;
            FormClosed += Form_Closed;
        }

        ~RemoteControlForm()
        {
            Shown -= Form_Shown;
            FormClosed -= Form_Closed;
        }

        private void Form_Shown(object sender, EventArgs args)
        {
            var fileStream = new FileStream("C:\\Test.png", FileMode.Open);

            SandaabContext.Dispatcher.RegisterRemoteController(_device, RemoteScreen_Update);
            SendRemoteControlCommand(RemoteControlRequestCommand.EventType.Start);
        }

        private void RemoteScreen_Update(RemoteControlResponseCommand remoteControlResponse)
        {
            if (pnlRemoteScreen.ClientSize.Width > 0
                && pnlRemoteScreen.ClientSize.Height > 0)
            {
                if (pnlRemoteScreen.BackgroundImage == null)
                {
                    pnlRemoteScreen.BackgroundImage = new Bitmap(pnlRemoteScreen.ClientSize.Width, pnlRemoteScreen.ClientSize.Height);
                    _pnlRemoteScreenGraphics = Graphics.FromImage(pnlRemoteScreen.BackgroundImage);
                }

                lock (pnlRemoteScreen.BackgroundImage)
                {
                    var stream = new MemoryStream(remoteControlResponse.Attachments[0]);
                    var bitmap = Bitmap.FromStream(stream);

                    if (!remoteControlResponse.Scaled)
                        bitmap = new Bitmap(
                            bitmap,
                            (int)Math.Round(bitmap.Width * _dpiX / remoteControlResponse.DpiX),
                            (int)Math.Round(bitmap.Height * _dpiY / remoteControlResponse.DpiY));

                    _pnlRemoteScreenGraphics.DrawImage(bitmap, remoteControlResponse.Frame.X, remoteControlResponse.Frame.Y);
                    pnlRemoteScreen.Invalidate();
                }
            }
        }

        private void Form_Closed(object sender, FormClosedEventArgs e)
        {
            SendRemoteControlCommand(RemoteControlRequestCommand.EventType.Stop);
            SandaabContext.Dispatcher.UnregisterRemoteController(_device);
        }

        private void SendRemoteControlCommand(RemoteControlRequestCommand.EventType change)
        {
            ScreenArea area = null;
            if (change != RemoteControlRequestCommand.EventType.Stop)
                area = new()
                {
                    X = pnlRemoteScreen.HorizontalScroll.Value,
                    Y = pnlRemoteScreen.VerticalScroll.Value,
                    Width = pnlRemoteScreen.Width,
                    Height = pnlRemoteScreen.Height,
                    DpiX = _dpiX,
                    DpiY = _dpiY,
                };

            var command = new RemoteControlRequestCommand()
            {
                Area = area,
                Event = change,
            };

            if (change == RemoteControlRequestCommand.EventType.Start)
            {
                Cursor = Cursors.WaitCursor;
                _device.SendQueued(command)
                    .ContinueWithOnUiThread(
                        (success) =>
                        {
                            Cursor = Cursors.Default;
                            if (!success)
                            {
                                MessageBox.Show(
                                    string.Format(Locale.CommandSendFailure, _device.Name),
                                    Locale.MessageBoxTitleError);
                                Close();
                            }
                        });
            }
            else
            {
                _device.SendQueued(command);
            }
        }

        private void PnlRemoteScreen_Scroll(object sender, ScrollEventArgs args)
        {
            SendRemoteControlCommand(RemoteControlRequestCommand.EventType.Change);
        }

        private void PnlRemoteScreen_Resize(object sender, EventArgs args)
        {
            if (pnlRemoteScreen.ClientSize.Width > 0
                && pnlRemoteScreen.ClientSize.Height > 0
                && pnlRemoteScreen.BackgroundImage != null)
                lock (pnlRemoteScreen.BackgroundImage)
                {
                    var newBitmap = new Bitmap(pnlRemoteScreen.ClientSize.Width, pnlRemoteScreen.ClientSize.Height);
                    _pnlRemoteScreenGraphics = Graphics.FromImage(newBitmap);
                    _pnlRemoteScreenGraphics.DrawImage(pnlRemoteScreen.BackgroundImage, 0, 0);
                    pnlRemoteScreen.BackgroundImage = newBitmap;
                    Invalidate();
                }

            SendRemoteControlCommand(RemoteControlRequestCommand.EventType.Change);
        }
    }
}
