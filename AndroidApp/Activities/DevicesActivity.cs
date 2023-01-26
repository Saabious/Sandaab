using Android.Content;
using Android.Views;
using AndroidX.AppCompat.View.Menu;
using Google.Android.Material.FloatingActionButton;
using Sandaab.Core;
using Sandaab.Core.Components;
using Sandaab.Core.Constantes;
using Sandaab.Core.Properties;
using static Android.Widget.AdapterView;

namespace Sandaab.AndroidApp.Activities
{
    [Activity]
    internal class DevicesActivity : BaseActivity
    {
        internal class Action
        {
            private const string Base = "com.sandaaab.activity.AddDeviceActivity.action.";
        }

        private ArrayAdapter<Device> _adapter;
        private ListView _devicesView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState, Resource.Layout.content_devices);

            SupportActionBar.Title = Locale.MenuDevices;

            _adapter = new(this, Android.Resource.Layout.SimpleListItem1);

            _devicesView = FindViewById<ListView>(Resource.Id.devices);
            _devicesView.SetMinimumHeight(5 * _devicesView.Height);
            _devicesView.Adapter = _adapter;
            RegisterForContextMenu(_devicesView);

            var fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += OnFabClick;

            Device.Event += Device_Event;

            foreach (var device in SandaabContext.Devices)
            {
                var args = new Device.ChangeEventArgs()
                {
                    Change = Device.DeviceEventFrom(device.State),
                };
                Device_Event(device, args);
            }
        }

        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu, v, menuInfo);

            MenuInflater.Inflate(Resource.Menu.devices, menu);

            menu.FindItem(Resource.Id.device_unpair)
                .SetTitle(Locale.UnpairDevice);
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            var info = item.MenuInfo as AdapterContextMenuInfo;

            switch (item.ItemId)
            {
                case Resource.Id.device_unpair:
                    SandaabContext.Devices.RemoveAsync(_adapter.GetItem(info.Position));
                    return true;
            }

            return base.OnContextItemSelected(item);
        }

        private void Device_Event(object sender, EventArgs args)
        {
            var device = sender as Device;

            if (args is Device.ChangeEventArgs changeEventArgs)
            {
                switch (changeEventArgs.Change)
                {
                    case DeviceEvent.Paired:
                        _adapter.Add(device);
                        _adapter.Sort(Java.Lang.String.CaseInsensitiveOrder);
                        _adapter.NotifyDataSetChanged();
                        break;
                    case DeviceEvent.Removed:
                        _adapter.Remove(device);
                        break;
                }
                _adapter.NotifyDataSetChanged();
            }
        }

        private void OnFabClick(object sender, EventArgs eventArgs)
        {
            var intent = new Intent(this, typeof(AddDeviceActivity));
            intent.SetAction(AddDeviceActivity.Action.AddDevice);
            StartActivity(intent);
        }

        protected override void OnDestroy()
        {
            Device.Event -= Device_Event;

            base.OnDestroy();
        }
    }
}
