using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Shapeshifter.Desktop.Functionality.Helpers;
using System;
using System.Windows.Input;
using System.Windows;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using Localization = Shapeshifter.Desktop.Localization;

namespace Shapeshifter.Desktop.Windows
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : Window
    {
        public Setup()
        {
            InitializeComponent();

            chkExternalMode.IsEnabled = Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 1;
            chkMixedMode.IsEnabled = chkExternalMode.IsEnabled;

            chkIntegratedMode.IsChecked = !chkExternalMode.IsEnabled;

            //branding
            BrandingHelper.CreateBrandingImage(BrandLogo);
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void checkState_Checked(object sender, RoutedEventArgs e)
        {
            btnGetStarted.IsEnabled = true;
        }

        private void btnGetStarted_Click(object sender, RoutedEventArgs e)
        {
            if (chkExternalMode.IsChecked == true)
            {
                SettingsHelper.SaveSetting("ManagementMode", "External");
            }
            else if (chkIntegratedMode.IsChecked == true)
            {
                SettingsHelper.SaveSetting("ManagementMode", "Integrated");
            }
            else if (chkMixedMode.IsChecked == true)
            {
                SettingsHelper.SaveSetting("ManagementMode", "Mixed");
            } else
            {
                throw new InvalidOperationException("Can't set up Shapeshifter without selecting a recognized mode");
            }

            MessageBox.Show(Localization.Language.MessageBoxSeeSettingsContent,
                            Localization.Language.MessageBoxSeeSettingsTitle, MessageBoxButton.OK,
                            MessageBoxImage.Information);
            var settings = new Settings();
            settings.Show();

            var currentApplication = (App)Application.Current;
            currentApplication.ShowBalloonTip(60000, Localization.Language.BalloonTipSettingsTitle, Localization.Language.BalloonTipSettingsContent, ToolTipIcon.Info);
            SettingsHelper.SaveSetting("LastSetupVersion", App.MajorVersion + "");

            Close();
        }
    }
}
