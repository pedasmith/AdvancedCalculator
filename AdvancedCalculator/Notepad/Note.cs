using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;


namespace AdvancedCalculator.Notepad
{
    public class Note : INotifyPropertyChanged
    {
        public Note()
        {
            Title = "";
            Rtf = "";
            Links = new ObservableCollection<string>();
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private string _Title;
        public string Title { get { return _Title; } set { if (value != _Title) { _Title = value; NotifyPropertyChanged("Title"); } } }
        private string _Rtf;
        public string Rtf { get { return _Rtf; } set { if (value != _Rtf) { _Rtf = value; NotifyPropertyChanged("Text"); } } }
        public ObservableCollection<string> Links { get; set; }
    }
}
