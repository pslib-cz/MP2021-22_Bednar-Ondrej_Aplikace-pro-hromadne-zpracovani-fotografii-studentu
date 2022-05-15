using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace BulkPhotoProcessing.NewFolder
{
    public class ImageNames : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _name;
        private ImageSource _source;
        public string Path { get; set; }
        public string Name { get { return _name; } set { _name = value; NotifyPropertyChanged(); } }
        public ImageSource Image { get { return _source; } set { _source = value; NotifyPropertyChanged(); } }
    }
}
