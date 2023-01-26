using Android.Content;
using Android.Views;
using AndroidX.Activity;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.View.Menu;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.Navigation;
using Google.Android.Material.TextView;
using Sandaab.AndroidApp.Components;
using Sandaab.Core.Properties;

namespace Sandaab.AndroidApp.Activities
{
    internal class NavigationActivity : BaseActivity, NavigationView.IOnNavigationItemSelectedListener, IBackPressedHandler
    {
        private DrawerLayout _drawer;

        protected override View OnCreate(Bundle savedInstanceState, int layoutContentResId, int layoutActivityLayout = Resource.Layout.activity_navigation)
        {
            var content = base.OnCreate(savedInstanceState, layoutContentResId, layoutActivityLayout);

            _drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            var toggle = new ActionBarDrawerToggle(this, _drawer, Toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            _drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            var navigation = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigation.SetNavigationItemSelectedListener(this);

            var navigationHeader = navigation.GetHeaderView(0);
            var navigationHeaderText = navigationHeader.FindViewById<MaterialTextView>(Resource.Id.nav_view_header_text);
            navigationHeaderText.SetText(Locale.MenuViewText, MaterialTextView.BufferType.Normal);

            navigation.Menu.FindItem(Resource.Id.nav_devices).SetTitle(Locale.MenuDevices);

            OnBackPressedDispatcher.AddCallback(this, new BackPressedHandler(true, this));

            return content;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.options, menu);

            if (menu is MenuBuilder builder)
                foreach (var item in builder.VisibleItems)
                    item.SetTitle(
                        item.ItemId switch
                        {
                            Resource.Id.action_devices => Locale.MenuDevices,
                            Resource.Id.action_settings => Locale.MenuSettings,
                            _ => item.Title
                        });

            return true;
        }

        public new void OnBackPressed()
        {
            if (_drawer.IsDrawerOpen(GravityCompat.Start))
                _drawer.CloseDrawer(GravityCompat.Start);
            else
                Finish();
        }

        public bool OnNavigationItemSelected(IMenuItem menuItem)
        {
            _drawer.CloseDrawer(GravityCompat.Start);

            //switch (menuItem.ItemId)
            //{
            //}

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem menuItem)
        {
            switch (menuItem.ItemId)
            {
                case Resource.Id.action_devices:
                    var intent = new Intent(this, typeof(DevicesActivity));
                    StartActivity(intent);
                    return true;
                default:
                   return base.OnOptionsItemSelected(menuItem);
            }
        }
    }
}
