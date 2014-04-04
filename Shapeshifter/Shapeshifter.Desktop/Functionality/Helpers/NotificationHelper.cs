using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Shapeshifter.Desktop.Functionality.Helpers
{
    public static class NotificationHelper
    {

        private static string StringToHexadecimal(string input)
        {
            var engine = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hash = engine.ComputeHash(inputBytes);

            var stringBuilder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                stringBuilder.Append(hash[i].ToString("X2"));
            }
            return stringBuilder.ToString();
        }

        public static bool ShowInstructions(string message)
        {
            var key = "NotificationCount" + StringToHexadecimal(message);
            var count = int.Parse(SettingsHelper.GetSetting(key, "0"));

            const int maximum = 2;

            if (count < maximum)
            {

                var application = (App)Application.Current;
                application.ShowBalloonTip(60000, "First-time use instructions", message + "\n\nNote: This message will only appear " + maximum + " times.", ToolTipIcon.Info);

                SettingsHelper.SaveSetting(key, (count + 1) + "");

                return true;

            } else
            {

                return false;

            }
        }

    }
}
