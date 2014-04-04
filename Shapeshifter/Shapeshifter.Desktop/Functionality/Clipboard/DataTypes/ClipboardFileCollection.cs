using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

#if !NETFX_CORE

using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Shapeshifter.Controls.Clipboard;
using Shapeshifter.Desktop.Controls.Clipboard;
using Shapeshifter.Desktop.Functionality.Helpers;
using Shapeshifter.Desktop.Functionality.Proxy;

#else

using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

#endif

namespace Shapeshifter.Desktop.Functionality.Clipboard.DataTypes
{
    public class ClipboardFileCollection : ClipboardItem, INotifyPropertyChanged
    {

        public class IconCollection : INotifyPropertyChanged
        {
            private BitmapSource _icon;
            public BitmapSource Icon
            {
                get { return _icon; }
                set
                {
                    _icon = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Icon"));
                    }
                }
            }

            private int _count;
            public int Count
            {
                get { return _count; }
                set
                {
                    _count = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Count"));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private IEnumerable<IconCollection> _iconCollections;

        private int _pathCount;

#if !NETFX_CORE
        private IEnumerable<string> _paths;
        public IEnumerable<string> Paths
#else
        private IEnumerable<IStorageItem> _paths;
        public IEnumerable<IStorageItem> Paths
#endif
        {
            get { return _paths; }
            set
            {
                if (value == null)
                {
                    _paths = null;
                    _iconCollections = null;
                    _pathCount = 0;
                }
                else
                {
                    _pathCount = value.Count();

                    var collections = value.Take(12).GroupBy(p =>
                        {

#if !NETFX_CORE
                            var isFile = File.Exists(p);

                            string extension;
                            if(isFile)
                            {
                                extension = Path.GetExtension(p);
                            } else
                            {
                                extension = "DIRECTORY";
                            }
#else
                            var isFile = p.IsOfType(StorageItemTypes.File);

                            string extension;
                            if (isFile)
                            {
                                var file = (StorageFile)p;
                                extension = file.FileType;
                            }
                            else
                            {
                                extension = "DIRECTORY";
                            }
#endif

                            return extension != null ? (extension.ToLower()) : "";
                        }, p => p, (a, b) => b).ToList();

                    _paths = collections.First();
                    FetchIconCollections(collections);

                }

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Paths"));
                    PropertyChanged(this, new PropertyChangedEventArgs("PathCount"));
                }
            }
        }

        public IEnumerable<IconCollection> IconCollections
        {
            get
            {
                return _iconCollections;
            }
            private set
            {
                _iconCollections = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IconCollection"));
                }
            }
        }

#if NETFX_CORE
        private async void FetchIconCollections(IEnumerable<IEnumerable<IStorageItem>> itemCollections)
#else
        private void FetchIconCollections(IEnumerable<IEnumerable<string>> itemCollections)
#endif
        {

            var iconCollections = new List<IconCollection>();
            foreach (var internalCollection in itemCollections.OrderByDescending(p => p.Count()))
            {
                var storageItems = internalCollection.ToArray();
                var firstElement = storageItems.First();

                BitmapSource icon;
#if !NETFX_CORE
                icon = IconHelper.GetIcon(firstElement, false, 32);
#else
                StorageItemThumbnail thumbnail;

                var isFile = firstElement.IsOfType(StorageItemTypes.File);
                if (isFile)
                {
                    thumbnail = await ((StorageFile) firstElement).GetThumbnailAsync(ThumbnailMode.ListView);
                }
                else
                {
                    thumbnail = await ((StorageFolder) firstElement).GetThumbnailAsync(ThumbnailMode.ListView);
                }

                var stream = thumbnail.CloneStream();
                icon = new BitmapImage();
                await icon.SetSourceAsync(stream);
#endif

                var collection = new IconCollection();
                collection.Count = storageItems.Count();
                collection.Icon = icon;

                iconCollections.Add(collection);
            }

            IconCollections = iconCollections;
        }

        public int PathCount
        {
            get { return _pathCount; }
        }

        public string ParentLocation
        {
            get { return Path.GetDirectoryName(_paths.First()); }
        }

        #region INotifyPropertyChanged Members

        public new event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public override ListBoxItem BindToListBoxItem()
        {
            var listBoxItem = new FileCollectionClipboardItem();
            listBoxItem.FileCollectionData = this;

            return listBoxItem;
        }

#if !NETFX_CORE

        protected override void DrawVisualThumbnail(DrawingContext drawingContext, out string title, ref BitmapSource icon)
        {

            var aeroColor = (Color)Application.Current.Resources["AeroColor"];
            var aeroBrush = new SolidColorBrush(aeroColor);
            var aeroPen = new Pen(aeroBrush, 7);

            var grayBrush = new SolidColorBrush(Color.FromArgb(255, 0xaf, 0xaf, 0xaf));

            var parentLocationIcon = (BitmapSource)Application.Current.Resources["FileClipboardItemParentLocationIcon"];

            var fontFamily = (FontFamily)Application.Current.Resources["CuprumFont"];
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Light, FontStretches.Normal);

            int fileIconOffset = 0;

            const int parentLocationTextOffset = VisualThumbnailMargin + 16 + VisualThumbnailMargin / 2;

            var parentLocationText = new FormattedText(ParentLocation, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                         typeface, 14, grayBrush);
            parentLocationText.MaxTextWidth = VisualThumbnailWidth - parentLocationTextOffset - VisualThumbnailMargin;
            parentLocationText.MaxTextHeight = 32;

            var countText = new FormattedText(PathCount + "", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                         typeface, 25, aeroBrush);
            countText.MaxTextWidth = VisualThumbnailWidth - VisualThumbnailMargin * 2;
            countText.MaxTextHeight = 32;

            drawingContext.DrawRectangle(Brushes.White, aeroPen, new Rect(-1, -1, VisualThumbnailWidth + 2, VisualThumbnailHeight + 2));
            drawingContext.DrawText(countText, new Point(VisualThumbnailMargin + 30 / 2 - countText.Width, VisualThumbnailMargin));

            var firstItem = IconCollections.First();

            foreach (var iconCollection in IconCollections.Take(4))
            {

                const int size = 32;

                var fileIcon = iconCollection.Icon;

                drawingContext.DrawImage(fileIcon, new Rect(VisualThumbnailMargin + 30 + fileIconOffset, VisualThumbnailMargin + countText.Height / 2 - size / 2, size, size));

                fileIconOffset += size + 3;

            }

            drawingContext.DrawImage(parentLocationIcon, new Rect(VisualThumbnailMargin, VisualThumbnailMargin + 32 + VisualThumbnailMargin, 16, 16));
            drawingContext.DrawText(parentLocationText, new Point(parentLocationTextOffset, VisualThumbnailMargin + 32 + VisualThumbnailMargin));

            title = PathCount + " entries from " + Source.ApplicationName;

            icon = firstItem.Icon;

        }

#endif

    }
}