using Android.OS;
using Android.Provider;
using Sandaab.AndroidApp.Activities;
using Sandaab.AndroidApp.Properties;
using Sandaab.Core;
using Sandaab.Core.Components;
using Xamarin.Essentials;
using DevicePlatform = Sandaab.Core.Constantes.DevicePlatform;

namespace Sandaab.AndroidApp.Components
{
    internal class AndroidLocalDevice : Core.Components.LocalDevice
    {
        public string MachineName;

        public AndroidLocalDevice()
            : base()
        {
            MachineName = GetMachineName();
        }

        protected override string GetId()
        {
            string id;

            id = Settings.Secure.GetString(MainActivity.Instance.Application.ContentResolver, Settings.Secure.AndroidId);
            if (!string.IsNullOrEmpty(id))
                return id;

            try
            {
                id = Preferences.Get(AndroidConfig.PreferencesLocalDeviceId, string.Empty);
                if (string.IsNullOrEmpty(id))
                {
                    Preferences.Set(AndroidConfig.PreferencesLocalDeviceId, SandaabContext.GenerateToken());
                    id = Preferences.Get(AndroidConfig.PreferencesLocalDeviceId, string.Empty);
                }
                return id;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }
        }

        private String Capitalize(String word) {
            if (string.IsNullOrEmpty(word))
                return String.Empty;

            char firstChar = word[0];
            if (Char.IsUpper(firstChar))
                return word;
            else
                return Char.ToUpper(firstChar) + word[1..word.Length];
        } 

        private string GetMachineName()
        {
            string manufacturer =
                Build.Manufacturer switch
                {
                    "lg" => "LG",
                    "oneplus" => "OnePlus",
                    "zte" => "ZTE",
                    _ => Capitalize(Build.Manufacturer),
                };
            string model = Build.Model;
            if (model.ToLower().StartsWith(manufacturer.ToLower()))
                return Capitalize(model);
            else
                return manufacturer + " " + model;
        }

        protected override string GetName()
        {
            return MachineName;
        }

        protected override DevicePlatform GetPlatform()
        {
            return DevicePlatform.Android;
        }
    }
}
