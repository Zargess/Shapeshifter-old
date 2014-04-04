using System.ComponentModel;
using Shapeshifter.Desktop.Functionality.Clipboard.Data;

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
    public abstract class ClipboardItem : INotifyPropertyChanged
    {

        protected const int VisualThumbnailWidth = 200;
        protected const int VisualThumbnailHeight = 100;
        protected const int VisualThumbnailMargin = 15;

#if !NETFX_CORE 

        private ClipboardSource _source;
        public ClipboardSource Source
        {
            get { return _source; }
            set
            {
                _source = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Source"));
                }
            }
        }

        //TODO: factory?
        protected abstract void DrawVisualThumbnail(DrawingContext drawingContext, out string title, ref BitmapSource icon);

        public ClipboardThumbnailItem CreateNewVisualThumbnail()
        {

            var bitmap = new RenderTargetBitmap(VisualThumbnailWidth, VisualThumbnailHeight, 96, 96, PixelFormats.Default);
            var visual = new DrawingVisual();
            var drawingContext = visual.RenderOpen();

            var icon = ClipboardSource.IconFromWindowHandle(Snapshot.OutsideClipboardOwnerHandle, 48);
            string text;
            DrawVisualThumbnail(drawingContext, out text, ref icon);

            drawingContext.Close();

            bitmap.Render(visual);

            return new ClipboardThumbnailItem(icon, bitmap, text, Snapshot);

        }

#endif

        public ClipboardSnapshot Snapshot { get; set; }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        //TODO: factory?
        public abstract ListBoxItem BindToListBoxItem();
    }
}