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
    public class ClipboardCustomData : ClipboardItem, INotifyPropertyChanged
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

        #region INotifyPropertyChanged Members

        public new event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public string Name { get; set; }

        public override ListBoxItem BindToListBoxItem()
        {
            var listBoxItem = new CustomDataItem();
            listBoxItem.CustomData = this;

            return listBoxItem;
        }

#if !NETFX_CORE
        protected override void DrawVisualThumbnail(System.Windows.Media.DrawingContext drawingContext, out string title, ref BitmapSource icon)
        {

            var aeroColor = (Color)Application.Current.Resources["AeroColor"];
            var aeroBrush = new SolidColorBrush(aeroColor);
            var aeroPen = new Pen(aeroBrush, 7);

            var grayBrush = new SolidColorBrush(Color.FromArgb(255, 0xaf, 0xaf, 0xaf));

            var fontFamily = (FontFamily)Application.Current.Resources["CuprumFont"];
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Light, FontStretches.Normal);

            const int sourceTextOffset = VisualThumbnailMargin;

            var sourceText = new FormattedText(Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                         typeface, 14, grayBrush);
            sourceText.MaxTextWidth = VisualThumbnailWidth - sourceTextOffset - VisualThumbnailMargin;
            sourceText.MaxTextHeight = 32;

            var imageRectangle = new Rect(VisualThumbnailWidth / 2 - 64 / 2, VisualThumbnailHeight / 2 - 64 / 2, 64, 64);

            drawingContext.DrawRectangle(Brushes.White, aeroPen, new Rect(-1, -1, VisualThumbnailWidth + 2, VisualThumbnailHeight + 2));
            drawingContext.DrawImage(Icon, imageRectangle);

            drawingContext.DrawText(sourceText, new Point(sourceTextOffset, VisualThumbnailHeight - VisualThumbnailMargin - 16));

            title = Name + " from " + Source.ApplicationName;
        }
#endif
    }
}