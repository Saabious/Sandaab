using Sandaab.Core.Components;
using Sandaab.WindowsApp.Forms.Dialogs;

namespace Sandaab.WindowsApp.Components
{
    internal class WindowsDispatcher : Dispatcher
    {
        public override void PairRequestNotification_Clicked(Device device)
        {
            var dialog = new PairDialog(device, false);
            dialog.ShowDialog(Application.OpenForms.Cast<Form>().Last());
        }
    }
}
