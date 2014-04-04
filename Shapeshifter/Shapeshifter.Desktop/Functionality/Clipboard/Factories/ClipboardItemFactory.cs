using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using System.Collections;

namespace Shapeshifter.Desktop.Functionality.Clipboard.Factories
{
    public abstract class ClipboardItemFactory
    {
        #region apis

        [DllImport("user32.dll")]
        protected static extern IntPtr GetClipboardOwner();

        #endregion

        private ClipboardSnapshot _snapshot;

        protected ClipboardItemFactory(ClipboardSnapshot snapshot)
        {
            _snapshot = snapshot;
        }
        
#if NETFX_CORE
        protected abstract Task<ClipboardItem> CreateNew(ClipboardSnapshot snapshot);
#else
        protected abstract ClipboardItem CreateNew(ClipboardSnapshot snapshot);
#endif

        /// <summary>
        /// Creates a new clipboard item from the clipboard. If no snapshot has been specified for this factory, a new clipboard snapshot will be made.
        /// </summary>
        /// <returns>The newly created clipboard item.</returns>
#if !NETFX_CORE
        public ClipboardItem CreateNewClipboardItem(IntPtr clipboardOwner, IEnumerable<ClipboardItem> possibleDuplicates)
#else
        public async Task<ClipboardItem> CreateNewClipboardItem()
#endif
        {
            if (_snapshot == null)
            {
#if !NETFX_CORE
                _snapshot = ClipboardSnapshot.CreateSnapshot(clipboardOwner);
#else
                _snapshot = ClipboardSnapshot.CreateSnapshot();
#endif
            }

#if !NETFX_CORE
            ClipboardItem result = CreateNew(_snapshot);
#else
            ClipboardItem result = await CreateNew(_snapshot);
#endif

            var comparableClipboardItem = result as IClipboardDataComparable;
            if(comparableClipboardItem != null)
            {
                foreach(var possibleDuplicate in possibleDuplicates)
                {
                    if(comparableClipboardItem.IsIdentical(possibleDuplicate))
                    {
                        result = possibleDuplicate;
                        break;
                    }
                }
            }

            result.Snapshot = _snapshot;

#if !NETFX_CORE
            result.Source = ClipboardSource.SourceFromWindowHandle(_snapshot.OutsideClipboardOwnerHandle);
#endif

            return result;
        }
    }
}