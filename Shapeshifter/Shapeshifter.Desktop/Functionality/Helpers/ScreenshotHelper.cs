using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Size = System.Drawing.Size;

namespace Shapeshifter.Desktop.Functionality.Helpers
{
    public static class ScreenshotHelper
    {

        /// <summary>
        /// Takes a screenshot and puts it into the clipboard.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static string SaveScreenshot(int x, int y, int width, int height)
        {
            var maxHeight = SystemParameters.PrimaryScreenWidth - y - 1;
            var maxWidth = SystemParameters.PrimaryScreenWidth - y - 1;

            var screen = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(screen))
            {
                graphics.CopyFromScreen(x, y, 0, 0, new Size((int)Math.Min(width, maxWidth), (int)Math.Min(height, maxHeight)));
            }

            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flamefusion", "Shapeshifter", "Screenshots");
            if(!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var now = DateTime.Now;

            //omg, it's a PNG
            var filePath = Path.Combine(directory, now.Year + "-" + (now.Month + "").PadLeft(2, '0') + "-" + (now.Day + "").PadLeft(2, '0') + "-" + (now.Hour + "").PadLeft(2, '0') + "-" + (now.Minute + "").PadLeft(2, '0') + "-" + (now.Second + "").PadLeft(2, '0') + "-" + now.Millisecond + ".png");
            screen.Save(filePath);

            return filePath;

        }

    }
}
