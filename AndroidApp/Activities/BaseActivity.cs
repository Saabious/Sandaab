using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using Java.Lang;
using Sandaab.AndroidApp.Dialogs;
using Sandaab.Core;
using Sandaab.Core.Components;
using Xamarin.Essentials;

namespace Sandaab.AndroidApp.Activities
{
    internal abstract class BaseActivity : AppCompatActivity
    {
        private class UncaughtExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
        {
            public event EventHandler<Throwable> UncaughtExceptionHandled;

            public void UncaughtException(Java.Lang.Thread t, Throwable e)
            {
                UncaughtExceptionHandled?.Invoke(null, e);
            }
        }

        public static BaseActivity CurrentInstance { get; private set; }
        protected AndroidX.AppCompat.Widget.Toolbar Toolbar { get; private set; }

        protected virtual View OnCreate(Bundle savedInstanceState, int layoutContentResId, int layoutActivityLayout = Resource.Layout.activity_base)
        {
            base.OnCreate(savedInstanceState);

            Platform.Init(this, savedInstanceState);
            SetContentView(layoutActivityLayout);

            ViewGroup content = null;
            var rootView = FindViewById<ViewGroup>(Resource.Id.root);
            if (rootView != null)
                content = LayoutInflater.Inflate(layoutContentResId, rootView) as ViewGroup;

            Toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);

            SetSupportActionBar(Toolbar);

            if (layoutActivityLayout != Resource.Layout.activity_navigation)
            {
                SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                SupportActionBar.SetDefaultDisplayHomeAsUpEnabled(true);
            }

            var uncaughtHandler = new UncaughtExceptionHandler();
            uncaughtHandler.UncaughtExceptionHandled += OnUncaughtExceptionHandled;
            Java.Lang.Thread.DefaultUncaughtExceptionHandler = uncaughtHandler;

            CurrentInstance = this;

            return content;
        }

        protected override void OnResume()
        {
            base.OnResume();

            CurrentInstance = this;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    OnBackPressedDispatcher.OnBackPressed();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }

        }

        private void OnUncaughtExceptionHandled(object sender, Throwable e)
        {
            Logger.Fatal(e);

            if (new[] { AndroidX.Lifecycle.Lifecycle.State.Started, AndroidX.Lifecycle.Lifecycle.State.Resumed }.Contains(Lifecycle.CurrentState))
                MessageDialog.Show(
                    e.Message)
                    .ContinueWith(() => { throw e; });
            else
                throw e;
        }

        public void Finish(Result resultCode, Intent resultData = null)
        {
            if (resultData == null)
                SetResult(resultCode);
            else
                SetResult(resultCode, resultData);
            Finish();
        }
    }
}
