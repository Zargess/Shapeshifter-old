using System.ComponentModel;
using System.Windows.Controls;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;

namespace Shapeshifter.Controls.Clipboard
{
    /// <summary>
    /// Interaction logic for ImageClipboardItem.xaml
    /// </summary>
    public partial class ImageClipboardItem : ListBoxItem, INotifyPropertyChanged
    {
        private ClipboardImage _imageData;

        public ImageClipboardItem()
        {
            InitializeComponent();
        }

        public ClipboardImage ImageData
        {
            get { return _imageData; }
            set
            {
                _imageData = value;
                DataContext = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ImageData"));
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}