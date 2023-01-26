using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Properties;
using Sandaab.WindowsApp.Forms.Dialogs;
using System.Diagnostics;

namespace Sandaab.WindowsApp.Forms
{
    public partial class MainForm : Form
    {
        private Device _currentDevice;
        private Dictionary<Device, RemoteControlForm> _remoteControlForms;

        public MainForm()
        {
            _remoteControlForms = new();

            InitializeComponent();

            Text = Config.AppName;

            Cursor = Cursors.WaitCursor;
            Enabled = false;

            lvDevices.Sorting = SortOrder.Ascending;
            LvDevice_Resize(this, EventArgs.Empty);

            btnRemoteControl.Text = Locale.RemoteControl;

            mnuDevicesUnpair.Text = Locale.UnpairDevice;

            Device.Event += Device_Event;
        }

        private void Device_Event(object sender, EventArgs args)
        {
            var device = sender as Device;

            if (args is Device.ChangeEventArgs changeEventArgs)
                switch (changeEventArgs.Change)
                {
                    case DeviceEvent.Paired:
                        if (device.State == DeviceState.Paired)
                        {
                            foreach (var item in lvDevices.Items)
                                if (((ListViewItem)item).Tag == device)
                                    return;

                            var newItem = new ListViewItem(device.Name)
                            {
                                Tag = device,
                            };
                            lvDevices.Items.Add(newItem);
                            lvDevices.Sort();

                            lvDevices.Height = Math.Min(5, lvDevices.Items.Count) * lvDevices.GetItemRect(0).Height + (lvDevices.Height - lvDevices.ClientRectangle.Height);

                            newItem.Selected = true;
                        }
                        return;
                    case DeviceEvent.Updated:
                        foreach (var item in lvDevices.Items)
                            if ((item as ListViewItem).Tag.Equals(device))
                                (item as ListViewItem).Text = device.Name;
                        break;
                    case DeviceEvent.Removed:
                        for (var i = lvDevices.Items.Count - 1; i >= 0; i--)
                            if (lvDevices.Items[i].Tag.Equals(device))
                                lvDevices.Items.RemoveAt(i);
                        return;
                    case DeviceEvent.Connected:
                        lblConnected.Text = "Connected";
                        break;
                    case DeviceEvent.Disconnected:
                        lblConnected.Text = "Unconnected";
                        break;
                }
        }

        public void Initialized(Task task)
        {
            Cursor = Cursors.Default;
            Enabled = true;

            if (task.IsFaulted)
            {
                using (new DialogCenteringService(this))
                    MessageBox.Show(
                        this,
                        task.Exception.Message,
                        Locale.MessageBoxTitleError,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1);
                System.Environment.Exit(1);
                return;
            }

            if (SandaabContext.Devices.Count == 0)
                new AddDeviceDialog(AddDeviceDialog.Action.Install).ShowDialog(this);
        }

        private void Btn1_Click(object sender, EventArgs args)
        {
            new AddDeviceDialog(AddDeviceDialog.Action.AddDevice).ShowDialog(this);
        }

        private void LvDevices_SelectedIndexChanged(object sender, EventArgs args)
        {
            if (lvDevices.SelectedItems.Count != 0)
                _currentDevice = lvDevices.SelectedItems[0].Tag as Device;
        }

        private void LvDevice_Resize(object sender, EventArgs e)
        {
            lvDevices.Columns[0].Width = lvDevices.Width - (lvDevices.Width - lvDevices.ClientRectangle.Width);
        }

        private void MnuDeviceUnpair_Click(object sender, EventArgs args)
        {
            foreach (var item in lvDevices.SelectedItems)
                SandaabContext.Devices.RemoveAsync(((ListViewItem)item).Tag as Device);
        }

        private void btnRemoteControl_Click(object sender, EventArgs args)
        {
            Debug.Assert(_currentDevice != null);

            if (_remoteControlForms.ContainsKey(_currentDevice)
                && _remoteControlForms[_currentDevice].IsDisposed)
                _remoteControlForms.Remove(_currentDevice);

            if (!_remoteControlForms.ContainsKey(_currentDevice))
            {
                var remoteControlForm = new RemoteControlForm(_currentDevice);
                remoteControlForm.Show();
                _remoteControlForms.Add(_currentDevice, remoteControlForm);
            }
            else
            {
                _remoteControlForms[_currentDevice].Show();
                _remoteControlForms[_currentDevice].BringToFront();
            }
        }
    }
}
