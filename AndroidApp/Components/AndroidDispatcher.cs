using Android.Content;
using Sandaab.AndroidApp.Activities;
using Sandaab.Core;
using Sandaab.Core.Components;

namespace Sandaab.AndroidApp.Components
{
    internal class AndroidDispatcher : Dispatcher
    {
        public override void PairRequestNotification_Clicked(Device device)
        {
            var intent = new Intent(BaseActivity.CurrentInstance, typeof(AddDeviceActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            intent.SetAction(AddDeviceActivity.Action.PairRequest);
            intent.PutExtra(AddDeviceActivity.Extras.DeviceId, device.Id);
            ((AndroidApp)SandaabContext.App).StartActivity(intent);
        }
    }
}
