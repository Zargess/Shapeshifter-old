using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Shapeshifter.Desktop.Functionality.Clipboard.Data
{

    public struct ThirdPartyClipboardFormats
    {

        #region apis

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint RegisterClipboardFormat(string lpszFormat);

        #endregion

        public static readonly uint CF_PNG = RegisterClipboardFormat("PNG");
        public static readonly uint CF_SYSTEM_DRAWING_BITMAP = RegisterClipboardFormat("System.Drawing.Bitmap");

    }
}
