using System.ComponentModel;
using System.Windows.Controls;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;

namespace Shapeshifter.Controls.Clipboard
{
    /// <summary>
    /// Interaction logic for TextClipboardItem.xaml
    /// </summary>
    public partial class FileClipboardItem : ListBoxItem, INotifyPropertyChanged
    {
        private ClipboardFile _fileData;

        public ClipboardFile FileData
        {
            get { return _fileData; }
            set
            {
                _fileData = value;
                DataContext = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("FileData"));
                }
            }
        }

        public FileClipboardItem()
        {
            InitializeComponent();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}