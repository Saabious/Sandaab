using Android.Content;
using Android.Runtime;
using Android.Views;

namespace Sandaab.AndroidApp.Forms.Dialogs
{
    internal abstract class BaseDialog : IDisposable
    {
        private sealed class KeyListener : Java.Lang.Object, IDialogInterfaceOnKeyListener
        {
            private readonly BaseDialog _baseDialog;

            public KeyListener(BaseDialog baseDialog)
            {
                _baseDialog = baseDialog;
            }

            public bool OnKey(IDialogInterface dialog, [GeneratedEnum] Android.Views.Keycode keyCode, KeyEvent e)
            {
                var isBack = keyCode == Keycode.Back
                    && e.Action == KeyEventActions.Up
                    && !e.IsCanceled;

                if (isBack)
                    _baseDialog.BackAction?.Invoke();

                return isBack;
            }
        }

        protected Action BackAction;
        private Action CancelAction;
        private Action _continueAction;
        protected AlertDialog Dialog;
        private static List<BaseDialog> _dialogs;
        public bool Visible { get; private set; }

        protected BaseDialog()
        {
            _dialogs ??= new();
        }

        public virtual void Dispose()
        {
            Dialog.CancelEvent -= NegativeButton_Event;
            Dialog.Dispose();

            _dialogs.Remove(this);

            GC.SuppressFinalize(this);
        }

        public void Show()
        {
            if (!Visible)
            {
                if (!_dialogs.Contains(this))
                {
                    Dialog.SetOnKeyListener(new KeyListener(this));
                    Dialog.CancelEvent += NegativeButton_Event;
                    _dialogs.Add(this);
                }
                Dialog.Show();
                Visible = true;
            }

            OnResume();
        }

        protected virtual void OnResume()
        {
        }

        protected virtual void OnBack()
        {
            BackAction?.Invoke();
            Hide();
            Dispose();
        }

        protected virtual void NegativeButton_Event(object sender, EventArgs args)
        {
            Cancel();
        }

        protected virtual void OnCancel()
        {
        }

        public BaseDialog BackWith(Action backAction)
        {
            BackAction = backAction;
            return this;
        }

        public void Cancel()
        {
            Hide();
            OnCancel();
            CancelAction?.Invoke();
        }

        public BaseDialog CancelWith(Action cancelAction)
        {
            CancelAction = cancelAction;
            return this;
        }

        public void Hide()
        {
            if (Visible)
            {
                OnPause();
                Dialog.Hide();
                Visible = false;
            }
        }

        protected virtual void OnPause()
        {
        }

        public BaseDialog ContinueWith(Action action)
        {
            _continueAction = action;

            return this;
        }

        public void Continue()
        {
            Hide();
            OnContinue();
            _continueAction?.Invoke();
        }

        protected virtual void OnContinue()
        {
        }
    }
}
