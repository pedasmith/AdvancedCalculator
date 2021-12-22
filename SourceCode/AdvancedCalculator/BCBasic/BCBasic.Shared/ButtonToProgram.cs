using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace BCBasic
{
    public class ButtonToProgram : INotifyPropertyChanged
    {
        public ButtonToProgram() { }
        public ButtonToProgram(string button, string package, string program, string keyName)
        {
            Button = button;
            Package = package;
            Program = program;
            KeyName = keyName;
        }
        private string _Button = "A";
        public string Button { get { return _Button; } set { if (value == _Button) return; _Button = value; NotifyPropertyChanged(); } }

        private string _Package = "Samples";
        public string Package { get { return _Package; } set { if (value == _Package) return; _Package = value; NotifyPropertyChanged(); } }

        private string _Program = "mpg";
        public string Program { get { return _Program; } set { if (value == _Program) return; _Program = value; NotifyPropertyChanged(); } }

        private string _KeyName = "mpg";
        public string KeyName { get { return _KeyName; } set { if (value == _KeyName) return; _KeyName = value; NotifyPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //public BCPackage AssociatedPackage { get; internal set; }
        //public BCProgram AssociatedProgram { get; internal set; }
        private void Attach (BCPackage package, BCProgram program)
        {
            //AssociatedPackage = package;
            //AssociatedProgram = program;
            Package = package.Name;
            Program = program.Name;
            package.PropertyChanged += (s,e) => { if (e.PropertyName == "Name") Package = (s as BCPackage).Name;  };
            program.PropertyChanged += (s,e) =>{ if (e.PropertyName == "Name") Program = (s as BCProgram).Name;  };
        }

        public void Attach (BCLibrary library)
        {
            if (library == null) return;
            foreach (var package in library.Packages)
            {
                if (package.Name == Package)
                {
                    foreach (var program in package.Programs)
                    {
                        if (program.Name == Program)
                        {
                            Attach(package, program);
                        }
                    }
                }
            }
        }
    }

    public class ButtonToProgramList
    {
        public ButtonToProgramList()
        {
            ButtonList = new ObservableCollection<ButtonToProgram>();

            // Pick the programs to bind:
            Set("P1", "BC BASIC Quick Samples", "Welcome to BC BASIC", "Welcome");
            Set("P2", "BC BASIC Quick Samples", "Grams of Fat to Calories", "Cal.");
            Set("P3", "BC BASIC Quick Samples", "Miles per Gallon", "MPG");
            Set("P4", "BC BASIC Quick Samples", "Tip Calculator", "Tip");
            Set("P5", "BC BASIC Quick Samples", "Colorful Countdown", "Timer");
        }
        public ObservableCollection<ButtonToProgram> ButtonList { get; internal set; }

        public delegate void SaveHandler(object sender);
        public event SaveHandler OnUpdate;

        private ButtonToProgram Get(string button)
        {
            foreach (var item in ButtonList)
            {
                if (item.Button == button) return item;
            }
            return null;
        }
        public void Set(string button, string package, string program, string keyName)
        {
            var bl = Get(button);
            if (bl == null) ButtonList.Add(new ButtonToProgram(button, package, program, keyName));
            else
            {
                if (keyName == null || keyName == "") keyName = button;
                bl.Button = button;
                bl.Package  = package;
                bl.Program = program;
                bl.KeyName = keyName;
                bl.Attach(CurrentLibrary);
                bl.PropertyChanged += async (s, e) => { await SaveAsync(null); }; // save all changes.
            }

            if (OnUpdate != null) OnUpdate.Invoke(this);
        }

        BCLibrary CurrentLibrary;
        public void Attach(BCLibrary library)
        {
            CurrentLibrary = library;
            foreach (var item in ButtonList)
            {
                item.Attach(CurrentLibrary);
            }
        }

        public void Init(string contents) // in JSON format
        {
            try
            {
                JsonObject json = JsonObject.Parse(contents);
                // Don't clear the button list.  ButtonList.Clear(); 

                var programs = json.GetNamedArray("Buttons", null);
                if (programs != null)
                {
                    for (uint i = 0; i < programs.Count; i++)
                    {
                        var f = programs.GetObjectAt(i);
                        if (f != null)
                        {
                            var pbutton = f.GetNamedString("Button", "");
                            switch (pbutton)
                            {
                                case "A": pbutton = "P1"; break;
                                case "B": pbutton = "P2"; break;
                                case "C": pbutton = "P3"; break;
                                case "D": pbutton = "P4"; break;
                                case "E": pbutton = "P5"; break;
                            }
                            var ppackage = f.GetNamedString("Package", "");
                            var pprogram = f.GetNamedString("Program", "");
                            var pkeyname = f.GetNamedString("KeyName", pprogram);
                            if (pbutton != null && ppackage != null && pprogram != null)
                            {
                                Set(pbutton, ppackage, pprogram, pkeyname);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private JsonObject ToJson()
        {
            var Retval = new JsonObject();
            var list = new JsonArray();
            foreach (var item in ButtonList)
            {
                var obj = new JsonObject();
                obj.SetNamedValue("Button", JsonValue.CreateStringValue(item.Button));
                obj.SetNamedValue("Package", JsonValue.CreateStringValue(item.Package));
                obj.SetNamedValue("Program", JsonValue.CreateStringValue(item.Program));
                obj.SetNamedValue("KeyName", JsonValue.CreateStringValue(item.KeyName));
                list.Add(obj);
            }
            Retval.Add("Buttons", list);
            return Retval;
        }

        public async Task SaveAsync(BCLibrary library = null)
        {
            if (library == null) library = CurrentLibrary;
            if (library == null) return; // Without a library, saving isn't possible.

            var Directory = library.PreferredDataStorageDirectory();
            var Filename = "buttonlist.bcbasic_buttondefinitions";
            
            var file = await Directory.CreateFileAsync(Filename, CreationCollisionOption.ReplaceExisting);
            var package = ToJson();
            var json = package.Stringify();
            try
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, json);
                nok++;
            }
            catch (Exception)
            {
                nfail++;
            }
        }
        int nok = 0;
        int nfail = 0;
    }
}
