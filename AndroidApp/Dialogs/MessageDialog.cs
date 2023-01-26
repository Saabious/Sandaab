using Sandaab.AndroidApp.Forms.Dialogs;
using Sandaab.Core.Properties;
using Sandaab.AndroidApp.Entities;
using Sandaab.AndroidApp.Activities;

namespace Sandaab.AndroidApp.Dialogs
{
    internal class MessageDialog : BaseDialog
    {
        private MessageDialog(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
            : base()
        {
            var builder = new AlertDialog.Builder(BaseActivity.CurrentInstance)
                .SetTitle(title)
                .SetMessage(message);

            if ((buttons & MessageBoxButtons.Cancel) != 0)
                builder.SetNegativeButton(Locale.CancelButtonText, NegativeButton_Event);
            else
                builder.SetCancelable(false);

            if ((buttons & MessageBoxButtons.OK) != 0)
                builder.SetPositiveButton(Locale.OkButtonText, Ok_Event);

            Dialog = builder
                .Create();
        }

        public static MessageDialog Show(string message, string title = null, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            var dialog = new MessageDialog(message, title ?? Config.AppName, buttons, icon);
            dialog.Show();
            return dialog;
        }

        private void Ok_Event(object sender, EventArgs args)
        {
            Continue();
        }
    }
}
