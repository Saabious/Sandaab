using Google.Android.Material.TextView;
using Sandaab.AndroidApp.Dialogs;
using Sandaab.AndroidApp.Entities;
using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Entities;
using Sandaab.Core.Properties;

namespace Sandaab.AndroidApp.Activities
{
    [Activity]
    internal class AddDeviceActivity : BaseActivity
    {
        internal class Action
        {
            private const string Base = "com.sandaaab.activity.AddDeviceActivity.action.";

            public const string Install = Base + "INSTALL";
            public const string AddDevice = Base + "ADD_DEVICE";
            public const string PairRequest = Base + "PAIR_QUESTION";
        }

        public class Extras
        {
            private const string Base = "com.sandaaab.activity.AddDeviceActivity.extra.";

            public const string DeviceId = Base + "device.ID";
        }

        private ArrayAdapter<Device> _adapter;
        private Network.DeviceSearch _deviceSearch;
        private ListView _devicesView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState, Resource.Layout.content_add_device);

            SupportActionBar.Title = Locale.AddDeviceDialogTitle;

            var intro = FindViewById<MaterialTextView>(Resource.Id.intro);
            intro.Text = string.Format(Locale.AddDeviceDialogIntro, Config.AppName);

            _adapter = new(this, Android.Resource.Layout.SimpleListItem1);

            _devicesView = FindViewById<ListView>(Resource.Id.devices);
            _devicesView.Adapter = _adapter;
            _devicesView.ItemClick += DeviceView_ItemClicked;
        }

        protected override void OnResume()
        {
            base.OnResume();

            Device.Event += Device_Event;

            _deviceSearch = new Network.DeviceSearch(TimeSpan.FromMinutes(5));
            _deviceSearch.Event += DeviceSearch_Event;
            _deviceSearch.Start();

            if (Intent.Action == Action.PairRequest)
            {
                var deviceId = Intent.Extras.GetString(Extras.DeviceId);
                var device = SandaabContext.Devices.GetByDeviceId(deviceId);
                ShowPairDialog(device, false);
            }
        }

        private void Device_Event(object sender, EventArgs args)
        {
            var device = sender as Device;

            if (args is Device.ChangeEventArgs changeEventArgs)
            {
                if (changeEventArgs.Change == DeviceEvent.Paired
                    && Intent.Action == Action.Install)
                    Finish(Result.Ok);
                else if (changeEventArgs.Change == DeviceEvent.Updated && device.State == DeviceState.Removed
                    || changeEventArgs.Change == DeviceEvent.Disconnected)
                    for (var i = _adapter.Count - 1; i >= 0; i--)
                        if (_adapter.GetItem(i) == sender)
                            _adapter.Remove(_adapter.GetItem(i));
            }
            else if (args is Dispatcher.SearchResponseEventArgs)
            {
                for (var i = 0; i < _adapter.Count; i++)
                    if (_adapter.GetItem(i) == device)
                        return;

                _adapter.Add(device);
                _adapter.Sort(Java.Lang.String.CaseInsensitiveOrder);
                _adapter.NotifyDataSetChanged();

                _deviceSearch.Reset();
            }
            else if (args is Dispatcher.PairRequestEventArgs pairRequestEventArgs)
            {
                PairDialog.Show(pairRequestEventArgs.Device, false);

                ((Dispatcher.PairRequestEventArgs)args).QuestionShown = true;
            }
        }

        private void DeviceSearch_Event(object sender, EventArgs args)
        {
            Finish(Result.Canceled);
        }

        protected override void OnPause()
        {
            Device.Event -= Device_Event;

            _deviceSearch.Dispose();

            base.OnPause();
        }

        private void DeviceView_ItemClicked(object sender, AdapterView.ItemClickEventArgs args)
        {
            var device = _adapter.GetItem(args.Position);

            device.SendQueued(
                new PairRequestCommand())
                .ContinueWith(
                    (success) =>
                    {
                        if (!success)
                            MessageDialog.Show(
                                string.Format(Locale.CommandSendFailure, device.Name),
                                Locale.MessageBoxTitleError);
                        else
                            ShowPairDialog(device, true);
                    });
        }

        private void ShowPairDialog(Device device, bool initiator)
        {
            PairDialog.Show(device, initiator)
                .AbortWith(
                    () =>
                    {
                        MessageDialog.Show(
                            string.Format(Locale.AddDeviceDialogRejectedMessage, device.Name),
                            Locale.MessageBoxTitleInformation,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    })
                .ContinueWith(
                    () =>
                    {
                        MessageDialog.Show(
                            string.Format(Locale.AddDeviceDialogPairedMessage, device.Name),
                            Locale.MessageBoxTitleInformation,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
                            .ContinueWith(
                                () =>
                                {
                                    Finish(Result.Ok);
                                });
                    })
                .CancelWith(
                    () =>
                    {
                        _devicesView.ClearChoices();
                    });
        }
    }
}
