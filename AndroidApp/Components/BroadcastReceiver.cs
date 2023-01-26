using Android.Content;

namespace Sandaab.AndroidApp.Components
{
    internal interface IBroadcastReceiver
    {
        public void IntentReceived(Context context, Intent intent);
    }

    [BroadcastReceiver]
    internal class BroadcastReceiver : Android.Content.BroadcastReceiver
    {
        private readonly IBroadcastReceiver _listener;

        public BroadcastReceiver()
        {
        }

        public BroadcastReceiver(IBroadcastReceiver listener)
        {
            _listener = listener;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            _listener.IntentReceived(context, intent);
        }
    }

}
