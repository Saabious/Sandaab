using Sandaab.Core.Constantes;
using System.Management;
using System.Net;
using System.Security.Principal;

namespace Sandaab.WindowsApp.Components
{
    internal class WindowsLocalDevice : Core.Components.LocalDevice
    {
        private string _machineName;
        public string MachineName { get { return _machineName; } set { SetMachineName(value); } }

        public WindowsLocalDevice()
        {
            _machineName = GetMachineName();
        }

        private string GetManagementProperty(string className, string propertyName)
        {
            try
            {
                foreach (var managementObject in new ManagementClass(className).GetInstances())
                {
                    var property = managementObject.Properties[propertyName].Value.ToString();
                    if (!string.IsNullOrEmpty(property))
                        return property;
                }
            }
            catch
            {
            }

            return null;
        }

        protected override string GetId()
        {
            string property;
            string userId = WindowsIdentity.GetCurrent().User.Value;

            property = GetManagementProperty("Win32_Processor", "ProcessorID");
            if (!string.IsNullOrEmpty(property))
                return property + userId;

            property = GetManagementProperty("Win32_BaseBoard", "SerialNumber");
            if (!string.IsNullOrEmpty(property))
                return property + userId;

            property = GetManagementProperty("Win32_BIOS", "SerialNumber");
            if (!string.IsNullOrEmpty(property))
                return property + userId;

            return userId;
        }

        private string GetMachineName()
        {
            string machineName = Dns.GetHostName();
            if (string.IsNullOrEmpty(machineName))
                machineName = Environment.MachineName;
            return machineName;
        }

        protected override string GetName()
        {
            return MachineName + "\\" + Environment.UserName;
        }

        protected override DevicePlatform GetPlatform()
        {
            return DevicePlatform.Windows;
        }

        public string GetManufacturer()
        {
            string manufacturer = GetManagementProperty("Win32_ComputerSystem", "Manufacturer");

            if (string.IsNullOrEmpty(manufacturer))
                manufacturer = "Unknown";

            return manufacturer;
        }

        public string GetModel()
        {
            string model = GetManagementProperty("Win32_ComputerSystem", "Model");

            if (string.IsNullOrEmpty(model))
                model = "Unknown";

            return model;
        }

        public string GetDescription()
        {
            string description = GetManagementProperty("Win32_ComputerSystem", "Systemtype");

            if (string.IsNullOrEmpty(description))
                description = "Unknown";

            return description;
        }

        private void SetMachineName(string machineName)
        {
            if (machineName.ToUpper() != _machineName.ToUpper())
                _machineName = machineName;
        }
    }
}
