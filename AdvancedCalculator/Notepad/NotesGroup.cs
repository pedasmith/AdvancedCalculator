using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;

namespace AdvancedCalculator.Notepad
{
    public class NotesGroup : INotifyPropertyChanged
    {
        public NotesGroup()
        {
            Title="";
            StackList= new ObservableCollection<NoteStack>();
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

        public ObservableCollection<NoteStack> StackList { get; set; }
    }
}
