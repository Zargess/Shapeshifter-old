using System.ComponentModel;
using System.Globalization;
using System.Windows;
using Shapeshifter.Controls.Clipboard;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;

#if !NETFX_CORE

using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Shapeshifter.Desktop.Functionality.Proxy;

#else

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

#endif

namespace Shapeshifter.Desktop.Functionality.Clipboard.DataTypes
{
    public class ClipboardImage : ClipboardItem, INotifyPropertyChanged
    {
        private BitmapSource _image;

        public BitmapSource Image
        {
            get { return _image; }
            set
            {
                _image = value;

#if !NETFX_CORE
                if (_image != null && _image.CanFreeze && !_image.IsFrozen)
                {
                    _image.Freeze();
                }
#endif

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Image"));
                }
            }
        }

        #region INotifyPropertyChanged Members

        public new event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public override ListBoxItem BindToListBoxItem()
        {
            var listBoxItem = new ImageClipboardItem();
            listBoxItem.ImageData = this;

            return listBoxItem;
        }

#if !NETFX_CORE

        protected override void DrawVisualThumbnail(System.Windows.Media.DrawingContext drawingContext, out string title, ref BitmapSource icon)
        {

            var aeroColor = (Color)Application.Current.Resources["AeroColor"];
            var aeroBrush = new SolidColorBrush(aeroColor);
            var aeroPen = new Pen(aeroBrush, 7);

            var grayBrush = new SolidColorBrush(Color.FromArgb(0x70, 255, 255, 255));
            var transparencyOverlayBrush = new SolidColorBrush(Color.FromArgb(0x22, 0, 0, 0));

            var fontFamily = (FontFamily)Application.Current.Resources["CuprumFont"];
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Light, FontStretches.Normal);

            const int sourceTextOffset = VisualThumbnailMargin + 16 + VisualThumbnailMargin / 2;

            var sourceText = new FormattedText(Source.ApplicationName ?? "Unknown application", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                         typeface, 14, grayBrush);
            sourceText.MaxTextWidth = VisualThumbnailWidth - sourceTextOffset - VisualThumbnailMargin;
            sourceText.MaxTextHeight = 32;

            var imageRectangle = new Rect(-1, -1, VisualThumbnailWidth + 1 * 2, VisualThumbnailHeight + 1 * 2);

            drawingContext.DrawImage(Image, imageRectangle);
            drawingContext.DrawRectangle(transparencyOverlayBrush, aeroPen, imageRectangle);

            drawingContext.DrawImage(Source.Icon, new Rect(VisualThumbnailMargin, VisualThumbnailHeight - VisualThumbnailMargin - 16, 16, 16));
            drawingContext.DrawText(sourceText, new Point(sourceTextOffset, VisualThumbnailHeight - VisualThumbnailMargin - 16));

            title = "Image from " + Source.ApplicationName;
        }

#endif

    }
}