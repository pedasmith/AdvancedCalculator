using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace AdvancedCalculator.Notepad
{
    public class AllNotes : INotifyPropertyChanged
    {
        public AllNotes()
        {
            GroupList = new ObservableCollection<NotesGroup>();
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public async void Save()
        {
            string str = JSon.Serialize(this);
            StorageFolder storageFolder = KnownFolders.DocumentsLibrary;
            StorageFile sampleFile = await storageFolder.CreateFileAsync("BestCalculatorNotes.bcnote", CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(sampleFile, str);
        }

        public async void Restore()
        {
            try
            {
                //StorageFolder storageFolder = KnownFolders.DocumentsLibrary;
                StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.RoamingFolder;
                StorageFile sampleFile = await storageFolder.GetFileAsync("BestCalculatorNotes.bcnote");
                var str = await Windows.Storage.FileIO.ReadTextAsync(sampleFile);
                var obj = JSon.Deserialize<AllNotes>(str);
                GroupList.Clear();
                foreach (var group in obj.GroupList)
                {
                    GroupList.Add(group);
                }
            }
            catch (Exception)
            {
                // "Something" happened, and the notes aren't available.
                // The something might be innocuous (no notes on first run)
                // or it might be really bad.
            }
        }


        public async void InitStock()
        {
            var group = new NotesGroup() { Title = "Help" };
            var stack = new NoteStack() { Title = "Getting Started" };
            stack.NoteList.Add(new Note() { Title = "Your first note", Rtf = await Rtf.FromText ("Here is text for your first note") });
            stack.NoteList.Add(new Note() { Title = "Your second note", Rtf = await Rtf.FromText ("second notes are good") });
            group.StackList.Add(stack);
            GroupList.Add(group);

            group = new NotesGroup() { Title = "Personal Notes" };
            stack = new NoteStack() { Title = "To Do" };
            stack.NoteList.Add(new Note() { Title = "Grocery", Rtf = await Rtf.FromText ("Eggs\nMilk") });
            stack.NoteList.Add(new Note() { Title = "Putter", Rtf = await Rtf.FromText ("Fix washing machine\nOil Hinges") });
            group.StackList.Add(stack);

            stack = new NoteStack() { Title = "Remember" };
            stack.NoteList.Add(new Note() { Title = "Birthdays", Rtf = await Rtf.FromText ("Peter: October 19, 1963") });
            group.StackList.Add(stack);
            GroupList.Add(group);
        }
        public ObservableCollection<NotesGroup> GroupList { get; set; }
    }
}
