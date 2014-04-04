using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Shapeshifter.Desktop.Functionality.Helpers
{
    public static class BrandingHelper
    {

        private static void StartWebsite(string url)
        {
            var startInfo = new ProcessStartInfo("explorer.exe", "\"" + url + "\"");
            Process.Start(startInfo);
        }

        public static void CreateBrandingImage(Image image)
        {

            var brandLogoUrl = SettingsHelper.GetSetting("BrandLogoUrl");
            if (!string.IsNullOrEmpty(brandLogoUrl))
            {
                image.Visibility = Visibility.Visible;
                image.Source = new BitmapImage(new Uri(brandLogoUrl));
                image.Stretch = Stretch.None;

                var brandName = SettingsHelper.GetSetting("BrandName");
                if(!string.IsNullOrEmpty(brandName))
                {
                    image.ToolTip = brandName;
                }

                var brandClickUrl = SettingsHelper.GetSetting("BrandUrl");
                if (!string.IsNullOrEmpty(brandClickUrl))
                {

                    image.Cursor = Cursors.Hand;
                    image.PreviewMouseDown += (sender, args) => StartWebsite(brandClickUrl);

                }
            }
            else
            {
                image.Visibility = Visibility.Collapsed;
            }

        }

    }
}
