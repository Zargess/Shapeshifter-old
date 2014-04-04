using System.ComponentModel;
using System.Windows.Controls;
using Shapeshifter.Desktop.Functionality.Clipboard.DataTypes;

namespace Shapeshifter.Controls.Clipboard
{
    /// <summary>
    /// Interaction logic for CustomDataItem.xaml
    /// </summary>
    public partial class CustomDataItem : ListBoxItem, INotifyPropertyChanged
    {
        private ClipboardCustomData _customData;

        public CustomDataItem()
        {
            InitializeComponent();
        }

        public ClipboardCustomData CustomData
        {
            get { return _customData; }
            set
            {
                _customData = value;
                DataContext = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("CustomData"));
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}