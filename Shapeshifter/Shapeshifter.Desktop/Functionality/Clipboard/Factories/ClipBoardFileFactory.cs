using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using Shapeshifter.Desktop.Functionality.Factories;
using Shapeshifter.Desktop.Functionality.Helpers;
#if !NETFX_CORE

#else

using Windows.ApplicationModel.DataTransfer;

using MetroClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;
using Windows.Storage;

#endif

namespace Shapeshifter.Desktop.Functionality.Clipboard.Factories
{
    public sealed class ClipboardFileFactory : ClipboardItemFactory
    {

#if !NETFX_CORE

        #region Desktop APIs

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int DragQueryFile(IntPtr hDrop, int iFile, StringBuilder lpszFile, int cch);

        #endregion

#endif

        public ClipboardFileFactory(ClipboardSnapshot snapshot = null)
            : base(snapshot)
        {
        }

#if NETFX_CORE
        protected async override Task<ClipboardItem> CreateNew(ClipboardSnapshot snapshot)
#else
        protected override ClipboardItem CreateNew(ClipboardSnapshot snapshot)
#endif
        {

            //does it have one or multiple files?
#if !NETFX_CORE
            if (snapshot.HasFormat(KnownClipboardFormats.CF_HDROP))
#else
            if(snapshot.HasFormat(StandardDataFormats.StorageItems))
#endif
            {
#if !NETFX_CORE

                IntPtr pointer = snapshot.FetchDataPointer(KnownClipboardFormats.CF_HDROP);
                try
                {

                    var paths = new List<string>();

                    //get amount of files. see http://msdn.microsoft.com/en-us/library/windows/desktop/bb776408(v=vs.85).aspx
                    int fileCount = DragQueryFile(pointer, -1, null, 0);

#else

                var data = (IEnumerable<IStorageItem>) snapshot.FetchData(StandardDataFormats.StorageItems);
                var paths = data.ToList();
                int fileCount = paths.Count();
 
#endif

                    if (fileCount > 0)
                    {
#if !NETFX_CORE
                        for (int i = 0; i < fileCount; i++)
                        {
                            int fileNameLength = DragQueryFile(pointer, i, null, 0) + 1;

                            var fileNameBuilder = new StringBuilder(fileNameLength);
                            DragQueryFile(pointer, i, fileNameBuilder, fileNameBuilder.Capacity);

                            string fileName = fileNameBuilder.ToString();
                            paths.Add(fileName);
                        }
#endif

                        if (fileCount > 1)
                        {
                            var result = new ClipboardFileCollection();
                            result.Paths = paths;

                            return result;
                        }
                        else
                        {
                            var result = new ClipboardFile();
                            result.Path = paths.First();

                            result.Icon = IconHelper.GetIcon(result.Path, true, 64);

                            return result;
                        }
                    }

                    return null;

#if !NETFX_CORE

                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pointer);
                    }
                }
#endif

            }
            else
            {
                throw new InvalidOperationException("Can't create file collection clipboard item when there is no recognizable file data present");
            }
        }
    }
}