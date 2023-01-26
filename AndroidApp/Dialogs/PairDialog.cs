using Android.Content;
using Sandaab.AndroidApp.Activities;
using Sandaab.AndroidApp.Forms.Dialogs;
using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;

namespace Sandaab.AndroidApp.Dialogs
{
    internal class PairDialog : BaseDialog
    {
        private Action _abortAction;
        private readonly Device _device;
        private bool _isInitiator;

        private PairDialog(Device device, bool isInitiator)
            : base()
        {
            _device = device;
            _isInitiator = isInitiator;

            var builder = new AlertDialog.Builder(BaseActivity.CurrentInstance)
                .SetTitle(Locale.PairDialogTitle)
                .SetMessage(_isInitiator ? string.Format(Locale.PairInitiatorDialogMessage, device.Name) : string.Format(Locale.PairAffectorDialogMessage, device.Name))
                .SetNegativeButton(Locale.CancelButtonText, NegativeButton_Event)
                .SetCancelable(false);

            if (!_isInitiator)
                builder
                    .SetPositiveButton(Locale.PairButtonText, PositiveButton_Event);

            Dialog = builder
                .Create();
        }

        private void PositiveButton_Event(object sender, DialogClickEventArgs e)
        {
            _device.SendQueued(
                new PairResponseCommand()
                {
                    Granted = true,
                });

            _device.State = Core.Constantes.DeviceState.Paired;

            Continue();
        }

        public static PairDialog Show(Device device, bool isInitiator)
        {
            var dialog = new PairDialog(device, isInitiator);
            dialog.Show();
            return dialog;
        }

        protected override void OnResume()
        {
            base.OnResume();

            Device.Event += Device_Event;
        }

        protected override void OnPause()
        {
            Device.Event -= Device_Event;

            base.OnPause();
        }

        protected override void OnCancel()
        {
            _device.SendQueued(
                new PairResponseCommand()
                {
                    Granted = false,
                });

            base.OnCancel();
        }

        private void Device_Event(object sender, EventArgs args)
        {
            if (args is Dispatcher.PairResponseEventArgs pairedInfo)
                if (pairedInfo.Granted && _isInitiator)
                    Continue();
                else if (!pairedInfo.Granted)
                    Abort();
        }

        public PairDialog AbortWith(System.Action action)
        {
            _abortAction = action;

            return this;
        }

        public void Abort()
        {
            Hide();
            _abortAction?.Invoke();
        }
    }
}
