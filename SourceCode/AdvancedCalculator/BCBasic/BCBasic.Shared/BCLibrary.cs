using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml;
using System.Linq;
using System.Threading;

namespace BCBasic
{
    public class BCLibrary
    {
        public ObservableCollection<BCPackage> Packages { get; internal set; }

        public BCLibrary()
        {
            Packages = new ObservableCollection<BCPackage>();
        }

        // Packages are .bcbasic files and are defined with Markdown (or JSON)
        public async Task InitAsync(ButtonToProgramList buttonlist)
        {
            var roamingdir = PreferredDataStorageDirectory();
            await Init(roamingdir, true, buttonlist); // true = is editable

            try
            {
                StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var basicDir = await InstallationFolder.GetFolderAsync(@"Assets\BCBasic\");
                await Init(basicDir, false, buttonlist); // not editable
            }
            catch (Exception)
            {
                // Just in case someone's gone and deleted every single package...
            }

#if BLUETOOTH
            // Get the Bluetooth packages
            try
            {
                StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var basicDir = await InstallationFolder.GetFolderAsync(@"Assets\BCBasic-BT\");
                await Init(basicDir, false, buttonlist); // not editable
            }
            catch (Exception)
            {
                // Just in case someone's gone and deleted every single package...
            }
#endif

            //
            // Now do the sort
            //
            var slist = (from package in Packages
                             orderby package.IsEditable descending, package.Name
                             select package).ToList();
                Packages.Clear();
                foreach (var package in slist) Packages.Add(package);
        }

        private async Task Init(StorageFolder folder, bool editable, ButtonToProgramList buttonlist)
        {
            var files = await folder.GetFilesAsync();
            foreach (var f in files)
            {
                if (f.Name.EndsWith(".bcbasic"))
                {
                    //var text = await Windows.Storage.FileIO.ReadTextAsync(f);
                    var newPackage = new BCPackage() { Filename = f.Name, Directory = folder };
                    var packageOk = await BCPackageImport.InitFromFile(f, newPackage);
                    newPackage.IsEditable = editable;
                    Packages.Add(newPackage);
                }
                else if (f.Name.EndsWith(".bcbasic_buttondefinitions"))
                {
                    if (buttonlist != null)
                    {
                        var text = await Windows.Storage.FileIO.ReadTextAsync(f);
                        buttonlist.Init(text);
                    }
                }
            }

        }

        public bool PackageExists (string name)
        {
            var p = GetPackage(name);
            return p == null ? false : true;
        }

        public string NewPackageName(string firstTemplate = "New Package", string backupTemplate = "New Package ({0})")
        {
            //string name = "New Package";
            var name = string.Format(firstTemplate, 1); // Act like a template for consistancy.
            if (!PackageNameExists(name)) return firstTemplate;
            //string template = "New Package ({0})";
            for (int i = 2; i < 99999; i++)
            {
                name = string.Format(backupTemplate, i);
                if (!PackageNameExists(name)) return name;
            }
            return "New Package (dup)";
        }

        public Windows.Storage.StorageFolder PreferredDataStorageDirectory()
        {
            var quota = Windows.Storage.ApplicationData.Current.RoamingStorageQuota;
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.RoamingFolder;
            return localFolder;
        }

        public async Task<string> NewPackageFilenameAsync(StorageFolder folder, string packageName)
        {
            String name = "";
            // Sanitize the package name -- azAZ09-_
            foreach (var ch in packageName)
            {
                if (charOkForFilename(ch)) name += ch;
            }
            var ext = ".bcbasic";

            //var folder = PreferredDataStorageDirectory();
            var files = await folder.GetFilesAsync();

            var Retval = name + ext;

            StorageFile result;
            for (int i = 1; i < 99999; i++ )
            {
                if (i == 1) Retval = name + ext;
                else Retval = string.Format ("{0} ({1}){2}", name, i, ext);
                result = files.FirstOrDefault(p => p.Name == Retval);
                if (result == null) break; // Got the answer!
            }

            return Retval;
        }

        private bool InRange(char ch, char start, char end)
        {
            return ch >= start && ch <= end;
        }
        private bool charOkForFilename(char ch)
        {
            bool Retval = InRange(ch, 'a', 'z') || InRange(ch, 'A', 'Z') || InRange(ch, '0', '9') || (ch == '-' || ch == '_');
            return Retval;
        }

        private bool PackageNameExists(string name)
        {
            foreach (var item in Packages)
            {
                if (item.Name == name) return true;
            }
            return false;
        }

        public bool FilenameExists(string name)
        {
            foreach (var item in Packages)
            {
                if (item.Filename == name) return true;
            }
            return false;
        }

        public BCPackage GetPackage (string name)
       {
            foreach (var item in Packages)
            {
                if (item.Name == name) return item;
            }
            return null;
        }

    }

    public class BCPackage: INotifyPropertyChanged
    {
        public BCPackage()
        {
            This = this;
            Programs = new ObservableCollection<BCProgram>();
            Directory = null;

            Programs.CollectionChanged += (s, e) =>
            {
                if (e.NewItems == null) return;
                foreach (var item in e.NewItems)
                {
                    var program = item as BCProgram;
                    if (program == null) continue;
                    program.Package = this;

                    program.PropertyChanged += (ps, pe) => { MustSave = true; };
                }
            };
            PropertyChanged += (s, e) => 
            { 
                switch (e.PropertyName)
                {
                    case "MustSave": break;
                    case "IsEditable": break;
                    case "EditableString": break;
                    case "This": break;

                    case "Name": MustSave = true; break;
                    case "Description": MustSave = true; break;
                    case "Filename": MustSave = true; break;
                    case "Directory": MustSave = true; break;

                    default: MustSave = true; break;
                }
            };
            MustSave = false;
        }

        // Merges metadata from the source to the destination.  Copied fields are
        //     Name, Description, IsEditable
        // Yes, that's really not much :-)
        // Not filename (it's not user-available)
        // Not directory (it's also not user-available)
        // Not Programs (those are merged seperately)
        public void MergeMetadata(BCPackage source)
        {
            if (Name != source.Name) Name = source.Name;
            if (Description != source.Description) Description = source.Description;
            if (IsEditable != source.IsEditable) IsEditable = source.IsEditable;
        }

        public BCPackage Duplicate()
        {
            var Retval = new BCPackage();
            Retval.MergeMetadata(this);
            foreach (var program in Programs)
            {
                var newProgram = program.Duplicate(Retval);
                Retval.Programs.Add(newProgram);
            }
            return Retval;
        }

        private bool _MustSave = false;
        public bool MustSave { get { return _MustSave; } set { if (_MustSave == value) return; _MustSave = value; NotifyPropertyChanged(); } }


        private string _Name = "";
        public string Name { get { return _Name; } set { if (_Name == value) return; _Name = value; NotifyPropertyChanged(); } }

        private string _Description = "";
        public string Description { get { return _Description; } set { if (_Description == value) return; _Description = value; NotifyPropertyChanged(); } }

        private string _PackagePreComment = "";
        public string PackagePreComment { get { return _PackagePreComment; } set { if (_PackagePreComment == value) return; _PackagePreComment = value; NotifyPropertyChanged(); } }

        private string _Filename = "";
        public string Filename { get { return _Filename; } set { if (_Filename == value) return; _Filename = value; NotifyPropertyChanged(); } }

        public Windows.Storage.StorageFolder Directory { get; set; }

        public ObservableCollection<BCProgram> Programs { get; internal set; }

        public override string ToString() { return Name; }


        //
        // All of the editable flags and values are grouped together.
        //
        private bool _IsEditable = false;
        public bool IsEditable { get { return _IsEditable; } set { if (_IsEditable == value) return; _IsEditable = value; NotifyPropertyChanged(); NotifyPropertyChanged("EditableString"); } }

        public Visibility EditableVisible { get { return IsEditable ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility EditableCollapsed { get { return IsEditable ? Visibility.Collapsed : Visibility.Visible; } }

        public string EditableString { get { return IsEditable ? "GEAR" : "LOCKED";  } } 

        private BCPackage _This = null;
        public BCPackage This { get { return _This; } set { if (_This == value) return; _This = value; NotifyPropertyChanged(); } }




        public JsonObject ToJson()
        {
            var Retval = new JsonObject();
            Retval.SetNamedValue("Name", JsonValue.CreateStringValue(this.Name));
            Retval.SetNamedValue("Description", JsonValue.CreateStringValue(this.Description));

            var list = new JsonArray();
            foreach (var program in Programs)
            {
                var obj = new JsonObject();
                obj.SetNamedValue("Name", JsonValue.CreateStringValue(program.Name));
                obj.SetNamedValue("Description", JsonValue.CreateStringValue(program.Description));
                obj.SetNamedValue("KeyName", JsonValue.CreateStringValue(program.KeyName));

                // Old files just had code and not codelines.  But that results in awkward to read JSON files
                // when they are exported.  So now we just save CodeLines (but on import, we still read in Code
                // in case it's an old files and the CodeLines doesn't exist)
                //obj.SetNamedValue("Code", JsonValue.CreateStringValue(program.Code));

                // Add in CodeLines
                // Code needs to be split by \r.
                var lines = program.CodeAsLines();
                var codeLines = new JsonArray();
                foreach (var line in lines)
                {
                    codeLines.Add(JsonValue.CreateStringValue(line));
                }
                obj.SetNamedValue("CodeLines", codeLines);
                list.Add(obj);
            }
            Retval.SetNamedValue("Programs", list);
            return Retval;
        }

        public bool ProgramAlreadyRunning()
        {
            bool Retval = NPrograms > 0;
            return Retval;
        }

        // This is the one true choke point for all running programs
        int NPrograms = 0;
        public async Task<BCValue> RunAsync(BCGlobalConnections externalConnections, BCBasicProgram program, CancellationTokenSource cts, bool keepVariables = false)
        {
            BCValue Retval;

            lock (this) { NPrograms++; }
            if (NPrograms == 1)
            {
                externalConnections.Package = this;
                externalConnections.DoStopProgram.SetStopProgramCancellationTokenSource(cts);
                externalConnections.DoStopProgram.SetStopProgramVisibility(Visibility.Visible);
                await program.RunAsync(cts.Token, keepVariables); // Never throws
                externalConnections.DoStopProgram.SetStopProgramVisibility(Visibility.Collapsed);
            }
            else
            {
                program.BCContext.ReturnValue = BCValue.MakeError(NPrograms, "There is already a program running");

            }
            Retval = program.BCContext.ReturnValue;
            lock (this) { NPrograms--; }
            return Retval;
        }

        public async Task<BCValue> RunByNameAsync(BCGlobalConnections externalConnections, string programname, CancellationTokenSource cts)
        {
            var prog = GetProgram(programname);
            if (prog == null)
            {
                var msg = $"ERROR: unable to find program {programname}";
                externalConnections.DoConsole.Console(msg);
                return BCValue.MakeError(2, msg);
            }
            string code = prog.Code;
            externalConnections.Package = this;
            var compileResults = await BCBasicProgram.CompileAsync(externalConnections, externalConnections.DoConsole, prog, code); // will get a new context.
            var program = compileResults.Program;
            if (program != null)
            {
                return await RunAsync(externalConnections, program, cts);
            }
            else
            {
                externalConnections.DoConsole.Console("ERROR: unable to compile that program");
                return BCValue.MakeError(1, "ERROR: unable to compile that program");
            }
        }


        public async Task SaveAsync(BCLibrary library)
        {
            if (!IsEditable) return; // locked packages are read-only and don't get saved.

            // Old way: JSON
            // New way (December 2017): MD (Markdown) format!
            // But for 3.14, keep as JSON for back compat

            //var fulltext = MdWriter.PackageToMd(this);

            var package = ToJson();
            var fulltext = package.Stringify();

            if (Directory == null) Directory = library.PreferredDataStorageDirectory();
            if (Filename == "" || Filename == null) Filename = await library.NewPackageFilenameAsync(Directory, Name);
            //var file = await Directory.GetFileAsync(Filename); Not correct; can't use a "file" here because it might not exist yet.
            //var file = await Directory.CreateFileAsync(Filename, CreationCollisionOption.ReplaceExisting);


            if (fulltext.Length >= 10)
            {
                var fullpath = Directory.Path + @"\" + Filename;
                var bytes = Edit.Lexer.ConvertToUtfRobust(fulltext);
                bool wroteOk = false;
                try
                {
                    if (bytes != null)
                    {
                        await Windows.Storage.PathIO.WriteBytesAsync(fullpath, bytes);
                        wroteOk = true;
                    }
                }
                catch (Exception)
                {
                    wroteOk = false;
                }

                //await Windows.Storage.FileIO.WriteTextAsync(file, json);
                if (!wroteOk && bytes != null)
                {
                    var file = await Directory.CreateFileAsync(Filename, CreationCollisionOption.ReplaceExisting);
                    using (var fs = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        using (var s = fs.GetOutputStreamAt(0))
                        {
                            using (var dw = new Windows.Storage.Streams.DataWriter(s))
                            {
                                dw.WriteBytes (bytes);
                                await dw.StoreAsync();
                                await dw.FlushAsync();
                            }
                        }
                        //await Windows.Storage.FileIO.WriteTextAsync(file, json);
                    }
                }
            }
            else
            {
                ; // json = json + json;
            }

            //var fullfile = Directory.Path + @"\" + Filename;
            //await Windows.Storage.PathIO.WriteTextAsync(fullfile, json);
            //await Windows.Storage.FileIO.WriteTextAsync(file, json);
            MustSave = false;
        }

        public BCProgram GetProgram (string name)
        {
            foreach (var item in Programs)
            {
                if (item.Name == name) return item;
            }
            return null;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class BCProgram: INotifyPropertyChanged
    {
        public BCProgram()
        {
            This = this;
        }
        public BCProgram Duplicate (BCPackage newPackage)
        {
            var Retval = new BCProgram();
            Retval.Name = Name;
            Retval.Description = Description;
            Retval.Code = Code;
            Retval.ProgramPostComment = ProgramPostComment;
            // Differences
            Retval.This = Retval;
            Retval.Package = newPackage;
            return Retval;
        }
        private string _Name = "";
        public string Name { get { return _Name; } set { if (_Name == value) return; _Name = value; NotifyPropertyChanged();  } }

        private string _Description = "";
        public string Description { get { return _Description; } set { if (_Description == value) return; _Description = value; NotifyPropertyChanged(); } }

        private string _KeyName = "";
        public string KeyName { get { return _KeyName; } set { if (_KeyName == value) return; _KeyName = value; NotifyPropertyChanged(); } }

        private string _Code = "";
        public string Code { get { return _Code; } set { if (_Code == value) return; _Code = value; NotifyPropertyChanged(); } }

        private string _ProgramPostComment = "";
        public string ProgramPostComment { get { return _ProgramPostComment; } set { if (_ProgramPostComment == value) return; _ProgramPostComment = value; NotifyPropertyChanged(); } }

        public IList<string> CodeAsLines()
        {
            char[] nl = new char[] { '\r' };
            var codeCR = Code.Replace("\r\n", "\r").Replace("\n\r", "\r").Replace("\n", "\r").Replace("\v", "\r"); // Word likes to insert vertical tabs.
            var lines = codeCR.Split(nl);
            return lines;
        }
        public static bool ContainsFlag (string code)
        {
            char[] singleCharFlags = "⚐⚑⛳⛿".ToCharArray();
            if (code.IndexOfAny(singleCharFlags) != -1) return true;

            //POssible optimization that might be needed.
            //char[] extStart = "\ud83c\ud83d".ToCharArray();
            //bool hasAnyExtendedChars = code.IndexOfAny(extStart) != -1;
            //if (!hasAnyExtendedChars) return false;

            // Might have one of the extended flags.  This takes more work.
            if (code.Contains("🎌")) return true;
            if (code.Contains("🏁")) return true;
            if (code.Contains("🏳")) return true;
            if (code.Contains("🏴")) return true;
            if (code.Contains("📪")) return true;
            if (code.Contains("📫")) return true;
            if (code.Contains("📬")) return true;
            if (code.Contains("📭")) return true;
            if (code.Contains("🚩")) return true;
            return false;
#if LIST_OF_FLAGS
⚐ WHITE FLAG U+2690
⚑ BLACK FLAG U+2691
⛳ FLAG IN HOLE U+26F3
⛿ WHITE FLAG WITH HORIZONTAL MIDDLE BLACK STRIPE U+26FF

🎌 CROSSED FLAGS U+d83c U+df8c
🏁 CHEQUERED FLAG U+d83c U+dfc1
🏳 WAVING WHITE FLAG U+d83c U+dff3
🏴 WAVING BLACK FLAG U+d83c U+dff4
📪 CLOSED MAILBOX WITH LOWERED FLAG U+d83d U+dcea
📫 CLOSED MAILBOX WITH RAISED FLAG U+d83d U+dceb
📬 OPEN MAILBOX WITH RAISED FLAG U+d83d U+dcec
📭 OPEN MAILBOX WITH LOWERED FLAG U+d83d U+dced
🚩 TRIANGULAR FLAG ON POST U+d83d U+dea9
#endif
        }

        private BCProgram _This = null;
        public BCProgram This { get { return _This; } set { if (_This == value) return; _This = value; NotifyPropertyChanged(); } }

        private BCPackage _Parent = null;
        public BCPackage Package { get { return _Parent; } set { if (_Parent == value) return; _Parent = value; NotifyPropertyChanged(); } }

        public override string ToString() { return Name; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
