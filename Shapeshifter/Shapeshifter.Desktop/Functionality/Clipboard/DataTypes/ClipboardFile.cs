using System.ComponentModel;
using System.Globalization;
using System.Windows;

#if !NETFX_CORE

using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Shapeshifter.Controls.Clipboard;
using Shapeshifter.Desktop.Functionality.Proxy;

#else

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

#endif

namespace Shapeshifter.Desktop.Functionality.Clipboard.DataTypes
{
    public class ClipboardFile : ClipboardItem, INotifyPropertyChanged, IClipboardDataComparable
    {
        private BitmapSource _icon;
        private string _path;

        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Path"));
                }
            }
        }

        public string Name
        {
            get { return System.IO.Path.GetFileName(_path); }
        }

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

        public string ParentLocation
        {
            get { return System.IO.Path.GetDirectoryName(_path); }
        }

        #region INotifyPropertyChanged Members

        public new event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public override ListBoxItem BindToListBoxItem()
        {
            var listBoxItem = new FileClipboardItem();
            listBoxItem.FileData = this;

            return listBoxItem;
        }

#if !NETFX_CORE
        protected override void DrawVisualThumbnail(DrawingContext drawingContext, out string title, ref BitmapSource icon)
        {

            var aeroColor = (Color)Application.Current.Resources["AeroColor"];
            var aeroBrush = new SolidColorBrush(aeroColor);
            var aeroPen = new Pen(aeroBrush, 7);

            var grayBrush = new SolidColorBrush(Color.FromArgb(255, 0xaf, 0xaf, 0xaf));

            var fileIcon = Icon;
            var parentLocationIcon = (BitmapSource) Application.Current.Resources["FileClipboardItemParentLocationIcon"];

            var fontFamily = (FontFamily)Application.Current.Resources["CuprumFont"];
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Light, FontStretches.Normal);

            const int fileNameTextOffset = VisualThumbnailMargin + 32 + VisualThumbnailMargin / 2;
            const int parentLocationTextOffset = VisualThumbnailMargin + 16 + VisualThumbnailMargin/2;

            var fileNameText = new FormattedText(Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                         typeface, 18, aeroBrush);
            fileNameText.MaxTextWidth = VisualThumbnailWidth - fileNameTextOffset - VisualThumbnailMargin;
            fileNameText.MaxTextHeight = 32;

            if (!string.IsNullOrEmpty(ParentLocation))
            {
                var parentLocationText = new FormattedText(ParentLocation, CultureInfo.CurrentCulture,
                                                           FlowDirection.LeftToRight,
                                                           typeface, 14, grayBrush);
                parentLocationText.MaxTextWidth = VisualThumbnailWidth - parentLocationTextOffset -
                                                  VisualThumbnailMargin;
                parentLocationText.MaxTextHeight = 32;

                drawingContext.DrawImage(parentLocationIcon, new Rect(VisualThumbnailMargin, VisualThumbnailMargin + 32 + VisualThumbnailMargin, 16, 16));
                drawingContext.DrawText(parentLocationText, new Point(parentLocationTextOffset, VisualThumbnailMargin + 32 + VisualThumbnailMargin));
            }

            drawingContext.DrawRectangle(Brushes.White, aeroPen, new Rect(-1, -1, VisualThumbnailWidth + 2, VisualThumbnailHeight + 2));
            drawingContext.DrawImage(fileIcon, new Rect(VisualThumbnailMargin, VisualThumbnailMargin, 32, 32));
            drawingContext.DrawText(fileNameText, new Point(fileNameTextOffset, VisualThumbnailMargin + fileNameText.MaxTextHeight / 2 - fileNameText.Height / 2));

            title = Name;

        }
#endif

        public bool IsIdentical(ClipboardItem other)
        {
            var clipboardFile = other as ClipboardFile;
            if (clipboardFile == null)
            {
                return false;
            }
            else
            {
                return clipboardFile.Path == Path;
            }
        }
    }
}