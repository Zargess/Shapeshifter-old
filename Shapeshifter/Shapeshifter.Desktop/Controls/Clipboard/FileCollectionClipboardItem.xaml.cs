using System.ComponentModel;
using System.Windows.Controls;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;

namespace Shapeshifter.Desktop.Controls.Clipboard
{
    /// <summary>
    /// Interaction logic for TextClipboardItem.xaml
    /// </summary>
    public partial class FileCollectionClipboardItem : ListBoxItem, INotifyPropertyChanged
    {
        private ClipboardFileCollection _fileCollectionData;

        public FileCollectionClipboardItem()
        {
            InitializeComponent();

            //throw new NotImplementedException("We have still not implemented images and preview of the file collection control");
        }

        public ClipboardFileCollection FileCollectionData
        {
            get { return _fileCollectionData; }
            set
            {
                _fileCollectionData = value;
                DataContext = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("FileCollectionData"));
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}