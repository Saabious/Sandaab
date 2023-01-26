using AndroidX.Activity;

namespace Sandaab.AndroidApp.Components
{
    internal interface IBackPressedHandler
    {
        public void OnBackPressed();
    }

    internal class BackPressedHandler : OnBackPressedCallback
    {
        private IBackPressedHandler _handler;

        public BackPressedHandler(bool enabled, IBackPressedHandler handler)
            : base(enabled)
        {
            _handler = handler;
        }

        public override void HandleOnBackPressed()
        {
            _handler.OnBackPressed();
        }
    }
}
