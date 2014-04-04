using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.Desktop.Functionality.Clipboard.DataTypes
{
    interface IClipboardDataComparable
    {

        bool IsIdentical(ClipboardItem other);

    }
}
