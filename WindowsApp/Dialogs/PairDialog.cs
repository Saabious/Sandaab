using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;
using Sandaab.WindowsApp.Dialogs;

namespace Sandaab.WindowsApp.Forms.Dialogs
{
    public partial class PairDialog : BaseDialog
    {
        private readonly Device _device;
        private bool _isInitiator;

        public PairDialog(Device device, bool isInitiator)
        {
            InitializeComponent();

            _device = device;
            _isInitiator = isInitiator;

            VisibleChanged += Visible_Changed;

            Text = Locale.PairDialogTitle;
            lblMessage.Text = _isInitiator ? string.Format(Locale.PairInitiatorDialogMessage, device.Name) : string.Format(Locale.PairAffectorDialogMessage, device.Name);
            btnOk.Text = Locale.PairButtonText;
            btnCancel.Text = Locale.CancelButtonText;

            btnOk.Visible = !_isInitiator;

            lblMessage.MaximumSize = new Size(ClientSize.Width - 2 * lblMessage.Left, lblMessage.MaximumSize.Height);
        }

        private void Visible_Changed(object sender, EventArgs args)
        {
            if (Visible)
            {
                Device.Event += Device_Event;
            }
            else
            {
                Device.Event -= Device_Event;

                if (!_isInitiator
                    || DialogResult == DialogResult.Cancel)
                    _device.SendQueued(
                        new PairResponseCommand()
                        {
                            Granted = DialogResult == DialogResult.OK,
                        });

                if (!_isInitiator
                    && DialogResult == DialogResult.OK)
                    _device.State = DeviceState.Paired;
            }
        }

        private void Device_Event(object sender, EventArgs args)
        {
            if (args is Dispatcher.PairResponseEventArgs pairedInfo)
            {
                if (pairedInfo.Granted)
                    DialogResult = DialogResult.OK;
                else
                    DialogResult = DialogResult.Abort;

                Close();
            }
        }
    }
}
