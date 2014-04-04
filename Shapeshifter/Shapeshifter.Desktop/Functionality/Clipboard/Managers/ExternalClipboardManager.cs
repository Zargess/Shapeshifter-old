using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;
using Shapeshifter.Desktop.Functionality.Proxy;
using Shapeshifter.Desktop.Functionality.Clipboard.Session;
using Shapeshifter.Desktop.Functionality.Helpers;
using System.Collections;
using System.Threading;
using System.Windows;

namespace Shapeshifter.Desktop.Functionality.Clipboard.Managers
{
    class ExternalClipboardManager : IDisposable
    {

        private readonly List<ClipboardThumbnailItem> _thumbnails;

        private readonly GlobalMessageListener _messageHook;

        private ClipboardThumbnailItem _selectedThumbnail;
        private ClipboardThumbnailItem SelectedThumbnail
        {
            get { return _selectedThumbnail; }
            set
            {
                _selectedThumbnail = value;
            }
        }

        public ExternalClipboardManager()
        {
            _thumbnails = new List<ClipboardThumbnailItem>();

            _messageHook = GlobalMessageListener.Instance;
            _messageHook.ClipboardDataAdded += MessageHookClipboardDataAdded;
            _messageHook.ClipboardDataRemoved += MessageHookClipboardDataRemoved;
            _messageHook.ClipboardDataSelected += MessageHookClipboardDataSelected;
            _messageHook.ClipboardDataSwapped += MessageHookClipboardDataSwapped;
        }

        void MessageHookClipboardDataSwapped(int oldPosition, int newPosition)
        {

            var oldItem = _thumbnails[oldPosition];
            _thumbnails.Remove(oldItem);

            var targetPosition = newPosition;
            if (targetPosition > oldPosition)
            {
                targetPosition--;
            }

            _thumbnails.Insert(targetPosition, oldItem);

        }

        private void MessageHookClipboardDataSelected(int index)
        {
            if (index > -1 && index < _thumbnails.Count)
            {
                var thumbnail = _thumbnails[index];
                SelectedThumbnail = thumbnail;

                if (index > 0)
                {
                    var firstThumbnail = _thumbnails[0];
                    thumbnail.MoveThumbnail(firstThumbnail.ThumbnailHandle);
                }
            }
        }

        private void MessageHookClipboardDataRemoved(int index)
        {
            var thumbnail = _thumbnails[index];
            _thumbnails.Remove(thumbnail);

            thumbnail.Close();
        }

        private void MessageHookClipboardDataAdded(ClipboardItem clipboardData)
        {
            var thumbnail = clipboardData.CreateNewVisualThumbnail();
            _selectedThumbnail = thumbnail;

            _thumbnails.Insert(0, thumbnail);

            thumbnail.Show();
            thumbnail.ThumbnailActivated += thumbnail_ThumbnailActivated;
            thumbnail.ThumbnailClosed += thumbnail_ThumbnailClosed;
        }

        void thumbnail_ThumbnailClosed(ClipboardThumbnailItem sender)
        {
            if (_thumbnails.Contains(sender))
            {
                var index = _thumbnails.IndexOf(sender);
                _messageHook.RemoveClipboardData(index);
            }
        }

        void thumbnail_ThumbnailActivated(ClipboardThumbnailItem sender)
        {
            var index = _thumbnails.IndexOf(sender);
            if (index > -1)
            {
                _messageHook.SelectedClipboardItemIndex = index;
                _messageHook.SwapClipboardItemPositions(index, 0);
            }
        }

        public void Dispose()
        {
            if (_messageHook != null)
            {
                _messageHook.ClipboardDataAdded -= MessageHookClipboardDataAdded;
                _messageHook.ClipboardDataRemoved -= MessageHookClipboardDataRemoved;
                _messageHook.ClipboardDataSelected -= MessageHookClipboardDataSelected;
            }

            if (_thumbnails != null)
            {
                foreach (var thumbnail in _thumbnails)
                {
                    thumbnail.ThumbnailClosed -= thumbnail_ThumbnailClosed;
                    thumbnail.ThumbnailActivated -= thumbnail_ThumbnailActivated;
                }
                _thumbnails.Clear();
            }
        }
    }
}
