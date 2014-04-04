using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shapeshifter.Desktop.Functionality.Clipboard.Data
{
    /// <summary>
    /// List of known clipboard formats which are the base types (that other types can be converted to).
    /// For more details, see http://msdn.microsoft.com/en-us/library/windows/desktop/ff729168(v=vs.85).aspx
    /// </summary>
    public struct AvoidedClipboardFormats
    {
        public static readonly uint CF_SYSTEM_DRAWING_BITMAP = ThirdPartyClipboardFormats.CF_SYSTEM_DRAWING_BITMAP;
    }
}