using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using Shapeshifter.Desktop.Functionality.Clipboard.Factories;

#if NETFX_CORE

using Windows.ApplicationModel.DataTransfer;

#endif

namespace Shapeshifter.Desktop.Functionality.Factories
{
    public sealed class ClipboardTextFactory : ClipboardItemFactory
    {

        public ClipboardTextFactory(ClipboardSnapshot snapshot = null)
            : base(snapshot)
        {
        }

#if NETFX_CORE
        protected async override Task<ClipboardItem> CreateNew(ClipboardSnapshot snapshot)
#else
        protected override ClipboardItem CreateNew(ClipboardSnapshot snapshot)
#endif
        {

#if !NETFX_CORE
            if (snapshot.HasFormat(KnownClipboardFormats.CF_UNICODETEXT)) //general text data
#else
            if(snapshot.HasFormat(StandardDataFormats.Text))
#endif
            {

#if !NETFX_CORE
                IntPtr textPointer = snapshot.FetchDataPointer(KnownClipboardFormats.CF_UNICODETEXT);

                try
                {

                    string value = Marshal.PtrToStringUni(textPointer);
#else
                var value = (string)snapshot.FetchData(StandardDataFormats.Text);
#endif

                    var result = new ClipboardText();
                    result.Text = value;

                    return result;

#if !NETFX_CORE

                }
                finally
                {

                    if (textPointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(textPointer);
                    }

                }

#endif

            }
            else
            {
                throw new InvalidOperationException("The text format was not known");
            }
        }
    }
}