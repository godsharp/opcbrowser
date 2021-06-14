using System.ComponentModel;
// ReSharper disable PossibleNullReferenceException

namespace OpcDaBrowser
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            set
            {
                _itemName = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ItemName)));
            }
        }

        public int ClientHandle { get; set; }

        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        private string _quality;
        public string Quality
        {
            get => _quality;
            set
            {
                _quality = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Quality)));
            }
        }

        private string _timestamp;
        public string Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Timestamp)));
            }
        }

        private int _counter;
        public int Counter
        {
            get => _counter;
            set
            {
                _counter = value;
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Counter)));
            }
        }

        public ViewModel(string itemName, int clientHandle, string value = null, string quality = null, string timestamp = null, int counter = 0)
        {
            ClientHandle = clientHandle;
            _itemName = itemName;
            _value = value;
            _quality = quality;
            _timestamp = timestamp;
            _counter = counter;
        }
    }
}