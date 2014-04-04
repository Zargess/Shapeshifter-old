using System.ComponentModel;
using System.Windows.Controls;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;

namespace Shapeshifter.Controls.Clipboard
{
    /// <summary>
    /// Interaction logic for TextClipboardItem.xaml
    /// </summary>
    public partial class TextClipboardItem : ListBoxItem, INotifyPropertyChanged
    {
        private ClipboardText _textData;

        public TextClipboardItem()
        {
            InitializeComponent();
        }

        public ClipboardText TextData
        {
            get { return _textData; }
            set
            {
                _textData = value;
                DataContext = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("TextData"));
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}