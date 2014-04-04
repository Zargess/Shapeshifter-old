using System.ComponentModel;
using System.Windows;
using System.Globalization;

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
    public class ClipboardText : ClipboardItem, INotifyPropertyChanged, IClipboardDataComparable
    {
        private string _text;

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                if (!string.IsNullOrEmpty(_text))
                {
                    _text = _text.Trim();
                }
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Text"));
                }
            }
        }

        #region INotifyPropertyChanged Members

        public new event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public override ListBoxItem BindToListBoxItem()
        {
            var listBoxItem = new TextClipboardItem();
            listBoxItem.TextData = this;

            return listBoxItem;
        }

#if !NETFX_CORE

        protected override void DrawVisualThumbnail(DrawingContext drawingContext, out string title, ref BitmapSource icon)
        {

            var aeroColor = (Color) Application.Current.Resources["AeroColor"];
            var aeroBrush = new SolidColorBrush(aeroColor);
            var aeroPen = new Pen(aeroBrush, 7);

            var largeIcon = (BitmapSource) Application.Current.Resources["TextClipboardItemIcon"];

            var fontFamily = (FontFamily) Application.Current.Resources["CuprumFont"];
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Light, FontStretches.Normal);

            const int textOffset = VisualThumbnailMargin + 51 + VisualThumbnailMargin / 2;

            var text = new FormattedText(Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                         typeface, 15, aeroBrush);
            text.TextAlignment = TextAlignment.Left;
            text.MaxTextWidth = VisualThumbnailWidth - textOffset - VisualThumbnailMargin;
            text.MaxTextHeight = VisualThumbnailHeight - VisualThumbnailMargin * 2;

            drawingContext.DrawRectangle(Brushes.White, aeroPen, new Rect(-1, -1, VisualThumbnailWidth + 2, VisualThumbnailHeight + 2));
            drawingContext.DrawImage(largeIcon, new Rect(VisualThumbnailMargin, VisualThumbnailMargin, 51, 66));
            drawingContext.DrawText(text, new Point(textOffset, VisualThumbnailMargin));

            title = Text;

        }

#endif

        public bool IsIdentical(ClipboardItem other)
        {
            var clipboardText = other as ClipboardText;
            if(clipboardText == null)
            {
                return false;
            } else
            {
                return clipboardText.Text == Text;
            }
        }
    }
}