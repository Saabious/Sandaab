using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;
using Sandaab.WindowsApp.Dialogs;

namespace Sandaab.WindowsApp.Forms.Dialogs
{
    public partial class AddDeviceDialog : BaseDialog
    {
        public class LbDeviceItem
        {
            public string Name { get; set; }
            public Device Device { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public enum Action
        {
            Install,
            AddDevice,
        }

        private Action _action;
        private Network.DeviceSearch _deviceSearch;

        public AddDeviceDialog(Action action)
        {
            _action = action;

            InitializeComponent();

            VisibleChanged += Visible_Changed;

            Text = Locale.AddDeviceDialogTitle;
            lblIntro.Text = string.Format(Locale.AddDeviceDialogIntro, Core.Properties.Config.AppName);
            btnAdd.Text = Locale.PairButtonText;
            btnCancel.Text = Locale.CancelButtonText;
        }

        private void Visible_Changed(object sender, EventArgs args)
        {
            if (Visible)
            {
                Device.Event += Device_Event;

                _deviceSearch = new Network.DeviceSearch(TimeSpan.FromMinutes(5));
                _deviceSearch.Event += DeviceSearch_Event;
                _deviceSearch.Start();
            }
            else
            {
                Device.Event -= Device_Event;

                _deviceSearch.Dispose();
            }
        }

        private void Device_Event(object sender, EventArgs args)
        {
            var device = sender as Device;

            if (args is Device.ChangeEventArgs changeEventArgs)
            {
                if (changeEventArgs.Change == DeviceEvent.Paired
                    && _action == Action.Install)
                    Close();
                else if (changeEventArgs.Change == DeviceEvent.Updated && device.State == DeviceState.Removed
                    || changeEventArgs.Change == DeviceEvent.Disconnected)
                    for (var i = lbDevices.Items.Count - 1; i >= 0; i--)
                        if ((lbDevices.Items[i] as LbDeviceItem).Device == device)
                            lbDevices.Items.RemoveAt(i);
            }
            else if (args is Dispatcher.SearchResponseEventArgs)
            {
                foreach (var item in lbDevices.Items)
                    if (((LbDeviceItem)item).Device == device)
                        return;

                lbDevices.Items.Add(
                    new LbDeviceItem()
                    {
                        Device = device,
                        Name = device.Name
                    });
                lbDevices.Sorted = true;

                _deviceSearch.Reset();
            }
            else if (args is Dispatcher.PairRequestEventArgs pairOfferInfoEventArgs)
            {
                new PairDialog(pairOfferInfoEventArgs.Device, false).ShowDialog(this);

                ((Dispatcher.PairRequestEventArgs)args).QuestionShown = true;
            }
        }

        private void DeviceSearch_Event(object sender, EventArgs args)
        {
            Close();
        }

        private void LbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAdd.Enabled = lbDevices.SelectedIndex != -1;
        }

        private void BtnAdd_Click(object sender, EventArgs args)
        {
            var device = (lbDevices.SelectedItem as LbDeviceItem).Device;
            PairDevice(device);
        }

        private void LbDevices_DoubleClick(object sender, EventArgs args)
        {
            if (lbDevices.SelectedItem != null)
            {
                var device = (lbDevices.SelectedItem as LbDeviceItem).Device;
                PairDevice(device);
            }
        }

        private void PairDevice(Device device)
        {
            Cursor = Cursors.WaitCursor;
            device.SendQueued(
                new PairRequestCommand())
                .ContinueWith(
                (success) =>
                {
                    Cursor = Cursors.Default;
                    if (!success)
                        MessageBox.Show(
                            string.Format(Locale.CommandSendFailure, device.Name),
                            Locale.MessageBoxTitleError);
                    else
                        ShowPairDialog(device);
                });
        }

        private void ShowPairDialog(Device device)
        {
            var dialog = new PairDialog(device, true);
            switch (dialog.ShowDialog(this))
            {
                case DialogResult.OK:
                    using (new DialogCenteringService(this))
                        MessageBox.Show(
                            this,
                            string.Format(Locale.AddDeviceDialogPairedMessage, device.Name),
                            Locale.MessageBoxTitleInformation,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    Close();
                    break;
                case DialogResult.Abort:
                    using (new DialogCenteringService(this))
                        MessageBox.Show(
                            this,
                            string.Format(Locale.AddDeviceDialogRejectedMessage, device.Name),
                            Locale.MessageBoxTitleError,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    break;
            }
        }
    }
}
