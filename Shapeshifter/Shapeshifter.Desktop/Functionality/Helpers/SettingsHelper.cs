using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace Shapeshifter.Desktop.Functionality.Helpers
{
    public static class SettingsHelper
    {

        public static void ResetDefaults()
        {
            var applicationName = Assembly.GetExecutingAssembly().GetName().Name;

            var currentUser = Registry.CurrentUser;

            var softwareKey = currentUser.OpenSubKey("Software", RegistryKeyPermissionCheck.ReadWriteSubTree);

            Debug.Assert(softwareKey != null, "softwareKey != null");

            var flamefusionKey = softwareKey.OpenSubKey("Flamefusion", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (flamefusionKey != null)
            {

                var onlyThisProgramPresent = flamefusionKey.GetSubKeyNames().Length == 1 && flamefusionKey.GetValueNames().Length == 0;

                var applicationKey = flamefusionKey.OpenSubKey(applicationName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (applicationKey != null)
                {
                    flamefusionKey.DeleteSubKeyTree(applicationName, false);

                    if (onlyThisProgramPresent)
                    {
                        softwareKey.DeleteSubKeyTree("Flamefusion", false);
                    }
                }

                flamefusionKey.Dispose();

            }

            softwareKey.Dispose();
        }

        public static void SaveSetting(string key, string value)
        {
            var applicationName = Assembly.GetExecutingAssembly().GetName().Name;

            var currentUser = Registry.CurrentUser;

            var softwareKey = currentUser.OpenSubKey("Software", RegistryKeyPermissionCheck.ReadWriteSubTree);

            Debug.Assert(softwareKey != null, "softwareKey != null");

            var flamefusionKey = softwareKey.OpenSubKey("Flamefusion", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (flamefusionKey == null)
            {
                flamefusionKey = softwareKey.CreateSubKey("Flamefusion", RegistryKeyPermissionCheck.ReadWriteSubTree);
            }

            softwareKey.Dispose();

            if (flamefusionKey != null)
            {
                var applicationKey = flamefusionKey.OpenSubKey(applicationName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (applicationKey == null)
                {
                    applicationKey = flamefusionKey.CreateSubKey(applicationName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                flamefusionKey.Dispose();

                if (applicationKey != null)
                {
                    applicationKey.SetValue(key, value, RegistryValueKind.String);

                    applicationKey.Dispose();
                }
            }
        }

        public static string GetSetting(string key, string defaultValue = "")
        {
            var applicationName = Assembly.GetExecutingAssembly().GetName().Name;

            var currentUser = Registry.CurrentUser;

            var softwareKey = currentUser.OpenSubKey("Software", RegistryKeyPermissionCheck.ReadWriteSubTree);

            Debug.Assert(softwareKey != null, "softwareKey != null");

            var flamefusionKey = softwareKey.OpenSubKey("Flamefusion", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (flamefusionKey == null)
            {
                flamefusionKey = softwareKey.CreateSubKey("Flamefusion", RegistryKeyPermissionCheck.ReadWriteSubTree);
            }

            softwareKey.Dispose();

            if (flamefusionKey != null)
            {
                var applicationKey = flamefusionKey.OpenSubKey(applicationName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (applicationKey == null)
                {
                    applicationKey = flamefusionKey.CreateSubKey(applicationName, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                string value = defaultValue;
                if (applicationKey != null)
                {
                    value = (string)applicationKey.GetValue(key, defaultValue);

                    applicationKey.Dispose();
                }

                if(value == defaultValue)
                {
                    
                    //the key is not present for the application. check for global Flamefusion settings instead.
                    value = (string) flamefusionKey.GetValue(key, defaultValue);

                }

                flamefusionKey.Dispose();

                return value;
            }

            return defaultValue;
        }
    }
}
