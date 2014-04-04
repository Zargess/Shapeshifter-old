using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Shapeshifter.Desktop.Functionality.Helpers;
using System.Diagnostics;
using System.Threading;

namespace Shapeshifter.Desktop.Windows
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        // determine what assemblies are available
        public static IEnumerable<CultureInfo> GetTranslatedLanguages()
        {
            yield return new CultureInfo("en-US");

            foreach (string directory in Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory))
            {
                string name = Path.GetFileNameWithoutExtension(directory);

                if (name == null || name.Length > 5)
                    continue;

                CultureInfo culture = null;
                try
                {
                    culture = CultureInfo.GetCultureInfo(name);
                }
                catch
                {
                    continue;
                }

                string resourceName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".resources.dll";
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name, resourceName)))
                    yield return culture;
            }
        }

        public Settings()
        {
            InitializeComponent();

            Title = Localization.Language.SettingsTitle;

            //color initialization
            chkAeroColor.IsEnabled = Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 0;
            chkAeroColor.IsChecked = chkAeroColor.IsEnabled && string.IsNullOrEmpty(SettingsHelper.GetSetting("AeroColor"));
            grdCustomColor.IsEnabled = !(chkAeroColor.IsChecked ?? false);
            AeroColor = AeroColor;

            //windows startup initialization
            chkStartWithWindows.IsChecked = (SettingsHelper.GetSetting("StartWithWindows", "1") == "1");

            //management mode
            var managementMode = SettingsHelper.GetSetting("ManagementMode", "Mixed");
            var selectedManagementModeItem =
                cmbManagementMode.Items.OfType<ComboBoxItem>().First(o => (string)o.Tag == managementMode);
            cmbManagementMode.SelectedItem = selectedManagementModeItem;
            selectedManagementModeItem.IsSelected = true;
            cmbManagementMode.IsEnabled = Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 1;

            //performance
            chkUseIncineration.IsChecked = (SettingsHelper.GetSetting("UseIncineration", "1") == "1");

            //language
            var chosenLanguage = SettingsHelper.GetSetting("ChosenLanguage", null);
            foreach(var language in GetTranslatedLanguages())
            {
                var item = new ComboBoxItem();
                item.Content = language.EnglishName + " (" + language.NativeName + ")";
                item.Tag = language;

                cmbLanguages.Items.Add(item);

                if ((Thread.CurrentThread.CurrentCulture.EnglishName.Contains(language.EnglishName) && cmbLanguages.SelectedItem == null) || ((chosenLanguage == null && language.EnglishName == Thread.CurrentThread.CurrentCulture.EnglishName) || (chosenLanguage != null && Thread.CurrentThread.CurrentCulture.LCID + "" == chosenLanguage)))
                {
                    item.IsSelected = true;
                    cmbLanguages.SelectedItem = item;
                    cmbLanguages.Text = (string)item.Content;
                }
            }

            //branding
            BrandingHelper.CreateBrandingImage(BrandLogo);

        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed && !tabSettings.IsMouseOver)
            {
                DragMove();
            }
        }

        private void BtnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private Color AeroColor
        {
            get { return (Color)Application.Current.Resources["AeroColor"]; }
            set
            {

                lblColorBlue.Content = value.B + "";
                lblColorGreen.Content = value.G + "";
                lblColorRed.Content = value.R + "";
                slrColorBlue.Value = value.B;
                slrColorGreen.Value = value.G;
                slrColorRed.Value = value.R;

                if (IsLoaded)
                {
                    Application.Current.Resources["AeroColor"] = value;

                    SettingsHelper.SaveSetting("AeroColor", value.R + "," + value.G + "," + value.B);
                }

            }
        }

        private void slrColorRed_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                var color = new Color();
                color.A = 255;
                color.R = (byte)e.NewValue;
                color.G = AeroColor.G;
                color.B = AeroColor.B;
                AeroColor = color;
            }
        }

        private void slrColorGreen_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                var color = new Color();
                color.A = 255;
                color.G = (byte)e.NewValue;
                color.R = AeroColor.R;
                color.B = AeroColor.B;
                AeroColor = color;
            }
        }

        private void slrColorBlue_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                var color = new Color();
                color.A = 255;
                color.B = (byte)e.NewValue;
                color.G = AeroColor.G;
                color.R = AeroColor.R;
                AeroColor = color;
            }
        }

        private void chkAeroColor_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                AeroColor = App.GetAeroColor();
                SettingsHelper.SaveSetting("AeroColor", string.Empty);
                grdCustomColor.IsEnabled = !(chkAeroColor.IsChecked ?? false);
            }
        }

        private void chkAeroColor_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                AeroColor = AeroColor;
                grdCustomColor.IsEnabled = !(chkAeroColor.IsChecked ?? false);
            }
        }

        private void chkStartWithWindows_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                SettingsHelper.SaveSetting("StartWithWindows", "1");
                App.SetStartupPath(true);
            }
        }

        private void chkStartWithWindows_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                SettingsHelper.SaveSetting("StartWithWindows", "0");
                App.SetStartupPath(false);
            }
        }

        private void cmbManagementMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {

                var comboBoxItem = (ComboBoxItem)cmbManagementMode.SelectedItem;
                var mode = (string)comboBoxItem.Tag;

                SettingsHelper.SaveSetting("ManagementMode", mode);

                MessageBox.Show(Localization.Language.MessageBoxRestartShapeshifterContent, Localization.Language.MessageBoxRestartShapeshifterTitle,
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void chkUseIncineration_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                using (var process = Process.GetCurrentProcess())
                {
                    var memoryBefore = process.WorkingSet64;

                    SettingsHelper.SaveSetting("UseIncineration", "1");
                    App.Incinerate();

                    process.Refresh();

                    var memoryNow = process.WorkingSet64;

                    MessageBox.Show(
                        string.Format(Localization.Language.MessageBoxIncinerationInitiatedContent,
                                      Math.Max((memoryNow - memoryBefore) / 1024 / 1024, 0), (memoryNow / 1024 / 1024)),
                        Localization.Language.MessageBoxIncinerationInitiatedTitle,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void chkUseIncineration_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                SettingsHelper.SaveSetting("UseIncineration", "0");
            }
        }

        private void cmbLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && cmbLanguages.SelectedItem != null)
            {
                var selectedItem = (ListBoxItem)cmbLanguages.SelectedItem;
                var culture = (CultureInfo)selectedItem.Tag;

                Thread.CurrentThread.CurrentCulture = culture;
                Localization.Language.Culture = culture;

                SettingsHelper.SaveSetting("ChosenLanguage", culture.LCID + "");

                MessageBox.Show(Localization.Language.MessageBoxRestartShapeshifterContent, Localization.Language.MessageBoxRestartShapeshifterTitle,
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}