using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections;
using System.Collections.ObjectModel;


// Need to update the UnicodeData.txt file?  Here's how!
// ftp://ftp.unicode.org/Public/UNIDATA/UnicodeData.txt
// ftp://ftp.unicode.org/Public/UNIDATA/Blocks.txt
// There you go!
namespace AdvancedCalculator
{
    public enum CharacterType { Normal, Symbol };



    public class BlockData
    {
        public uint Start { get; set; }
        public uint End { get; set; }
        public string Name { get; set; }
        public string NameUC { get; set; }
        public bool Match(uint value)
        {
            bool Retval = (value >= Start) && (value <= End);
            return Retval;
        }
        public static BlockData StringToBlockData(string str)
        {
            if (str.StartsWith("#")) return null; // e.g. # Blocks-7.0.0.txt
            if (str.Trim().Length == 0) return null; // blank line
            int dotPosition = str.IndexOf("..");
            int semicolonPosition = str.IndexOf(";");
            if (dotPosition < 0 || semicolonPosition < 0 || semicolonPosition < dotPosition) return null;
            // 0000..007F; Basic Latin
            // 0123456789_123456789_123
            // 
            var start = str.Substring(0, dotPosition);
            var end = str.Substring(dotPosition + 2, semicolonPosition - dotPosition - 2);
            var name = str.Substring(semicolonPosition + 1).Trim();

            try
            {
                var Retval = new BlockData();
                Retval.Start = Convert.ToUInt32(start, 16);
                Retval.End = Convert.ToUInt32(end, 16);
                Retval.Name = name;
                Retval.NameUC = name.ToUpper();
                return Retval;
            }
            catch (Exception)
            {
            }
            return null;
        }
    }
    public class BlockDataDictionary : List<BlockData>
    {
        public async Task InitAsync(string filename = "Blocks.txt")
        {
            var installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var folder = await installedLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync(filename);
            IList<string> lines = await Windows.Storage.FileIO.ReadLinesAsync(file);
            //var Retval = new List<BlockData>();
            this.Clear();
            foreach (var line in lines)
            {
                var item = BlockData.StringToBlockData(line);
                if (item != null) this.Add(item);
            }
            //return Retval;
        }
        static BlockData cachedBlock = null;
        public BlockData BlockOf(uint value)
        {
            if (cachedBlock != null && cachedBlock.Match(value)) return cachedBlock;

            foreach (var item in this)
            {
                if (item.Match(value))
                {
                    cachedBlock = item;
                    return item;
                }
            }
            return null;
        }
    }

    public class UnicodeData : IEnumerable<string>
    {
        public string Block { get; set; }
        public string BlockUC { get; set; }
        public string Alias { get; set; }
        public string Name { get; set; }
        public string NameUC { get; set; }
        public string UnicodeString { get; set; }
        public string UPlusName { get; set; }
        public CharacterType CharacterType { get; set;  }
        //public string UPlusName { get { return "U+" + Convert.ToString(UTF32, 16).ToUpper(); } }
        public string UPlusNameWindows
        {
            get {
                if (UTF32 < 0xFFFF) return UPlusName;
                UInt32 valoffset = UTF32 - 0x10000;
                UInt16 loval = (UInt16)(valoffset & 0x3FF); // low 10 bits
                UInt16 hival = (UInt16)((valoffset >> 10) & 0x3FF); // high 10 bits

                Int32 ch1 = (hival + 0xD800);
                Int32 ch2 = (loval + 0xDC00);
                var str = string.Format("U+{0} U+{1}", Convert.ToString(ch1, 16), Convert.ToString (ch2, 16));
                return str;
            }
        }
        public bool NeedsUPlusNameWindows { get { return UTF32 > 0xFFFF; } }
        private UInt32 _UTF32;
        public UInt32 UTF32 { get { return _UTF32; } set { if (value == _UTF32 && UPlusName!=null) return; _UTF32 = value; UPlusName = "U+" + Convert.ToString(UTF32, 16).ToUpper(); } }

        public class Enumerator: IEnumerator<string>
        {
            private int curr = -1;
            UnicodeData parent;
            public Enumerator(UnicodeData data) { parent = data; }

            public string Current
            {
                get {
                    switch (curr)
                    {
                        case 0: return parent.Name;
                        case 1: return parent.UnicodeString;
                        case 2:
                            if (parent.UTF32 == 0x29FB)
                            {
                                parent.UTF32 = 0x29FB;
                            }
                            return parent.UPlusName; // "U+" + Convert.ToString(parent.UTF32, 16);
                        case 3: return parent.Alias;
                        case 4: return parent.BlockUC != null ? parent.BlockUC : "";
                    }
                    return "";
                }
            }

            public bool MoveNext()
            {
                curr++;

                bool Retval = (curr <= 4); // 4 matches the switch statement in Current
                return Retval;
            }

            public void Dispose() { }
            object System.Collections.IEnumerator.Current { get { return Current; } }
            void System.Collections.IEnumerator.Reset() { curr = -1; }

        }
        public IEnumerator<string> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    public class UnicodeDictionary
    {
        public UnicodeDictionary()
        {
            Data = new ObservableCollection<UnicodeData>();
        }

        public ObservableCollection<UnicodeData> Data { get; set; }
        private enum MatchType { exact, contains, containsWord, notContains, notContainsWord }

        // If any of the IEnumerable matches, it's a match.
        // Example: "symbol" will match anything in the symbol plane
        // However, "-linear" will not match if it ever fails.
        private bool Match(IEnumerable<string> enumerable, string Search, MatchType matchType)
        {
            bool anyMatched = false;
            bool allMatched = true;
            foreach (string Data in enumerable)
            {
                var isMatch = WordMatch(Data, Search, matchType);
                if (isMatch) anyMatched = true;
                else allMatched = false;
            }
            bool Retval = anyMatched;
            if (matchType == MatchType.notContains || matchType == MatchType.notContainsWord) Retval = allMatched;
            return Retval;
        }

        private bool WordMatch(string Data, string Search, MatchType matchType)
        {
            var Retval = false;
            switch (matchType)
            {
                case MatchType.exact: if (Data == Search) Retval = true; break;
                case MatchType.contains: if (Data.Contains(Search)) Retval = true; break;
                case MatchType.containsWord:
                    if (Data.Contains(Search))
                    {
                        bool isStart = Data.StartsWith(Search + " ");
                        bool isEnd = Data.EndsWith(" " + Search);
                        bool isMiddle = Data.Contains(" " + Search + " ");
                        bool isEntire = Data == Search;
                        if (isStart || isMiddle || isEnd || isEntire) Retval = true;
                    }
                    break;
                case MatchType.notContains:
                    Retval = !WordMatch(Data, Search, MatchType.contains);
                    break;
                case MatchType.notContainsWord:
                    Retval = !WordMatch(Data, Search, MatchType.containsWord);
                    break;
            }
            return Retval;
        }



        // example: search for <<face ton with>>  finds <<face with stuck-out tongue>>

        public void Search (string searchstr, ObservableCollection<UnicodeData> Results)
        {
            searchstr = searchstr.ToUpper();
            char[] space = new char[] { ' ' };
            var searchlist = searchstr.Split(space);
            var nSearch = searchlist.Count();
            if (nSearch > 1 && searchlist[nSearch - 1] == "") nSearch--; // Ignore trailing spaces
            MatchType[] matchType = new MatchType[nSearch];
            for (int i = 0; i < nSearch; i++)
            {
                bool isNot = false;
                bool isEqual = false;
                if (searchlist[i].StartsWith("-") && searchlist[i] != "-")
                {
                    // The user can search for just a plain '-' sign
                    isNot = true;
                    searchlist[i] = searchlist[i].Substring(1); // remove the '-'
                } else if (searchlist[i].StartsWith("=") && searchlist[i] != "=")
                {
                    isEqual = true;
                    searchlist[i] = searchlist[i].Substring(1); // remove the '='
                }
                matchType[i] = isNot ? MatchType.notContains : (isEqual ? MatchType.containsWord : MatchType.contains);
                if (searchlist[i].StartsWith("U+")) matchType[i] = MatchType.exact;
                else if (searchlist[i].Length <= 2) matchType[i] = isNot ? MatchType.notContainsWord : MatchType.containsWord;
            }

                
            foreach (var item in Data)
            {
                bool match = true;
                for (int i=0; i<nSearch && match; i++)
                {
                    bool matches = Match(item, searchlist[i], matchType[i]);
                    if (!matches) match = false;
                }
                if (match)
                {
                    Results.Add(item);
                }
            }
        }

        BlockDataDictionary UnicodeBlockData = null;
        public async Task Init()
        {
            Data.Clear();
            //UnicodeBlockData = await BlockData.Init();
            UnicodeBlockData = new BlockDataDictionary();
            await UnicodeBlockData.InitAsync();
            await InitFile("UnicodeData.txt");
            // Where does the UnicodeData_MicrosoftSymbol.txt file come from?
            // https://msdn.microsoft.com/en-us/windows/uwp/controls-and-patterns/segoe-ui-symbol-font
            await InitFile("UnicodeData_MicrosoftSymbol.txt", "MICROSOFT SYMBOL ", CharacterType.Symbol);
        }

        public async Task InitFile(string filename, string prefix = "", CharacterType type = CharacterType.Normal)
        {
            var installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var folder = await installedLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync (filename);
            IList<string> lines = await Windows.Storage.FileIO.ReadLinesAsync(file);
            ObservableCollection<UnicodeData> data = new ObservableCollection<UnicodeData>();
            char[] semi = new char[] { ';' };
            foreach (var line in lines)
            {
                try
                {
                    if (line.StartsWith("#")) continue; // # is for comments
                    var words = line.Split(semi);
                    UInt32 value;
                    bool ok = UInt32.TryParse(words[0], System.Globalization.NumberStyles.HexNumber, null, out value);
                    if (ok)
                    {
                        var name = prefix + words[1];
                        var alias = prefix + words[10];
                        if (name == "<control>")
                        {
                            if (alias != "")
                            {
                                var temp = name;
                                name = alias;
                                alias = temp.ToUpper();
                            }
                            else
                            {
                                // Name is <control> and alias is empty (e.g., U+80).
                                // We need to show something to the user.
                                alias = name;
                                name = String.Format("U+{0:X}", value);
                            }
                        }
                        else if (name.StartsWith("<"))
                        {
                            // Not interesting at all!
                        }
                        else if (name != name.ToUpper())
                        {
                            var junk = name;
                        }

                        string str;
                        if (value <= 0xFFFF)
                        {
                            char ch = (char)value;
                            str = Convert.ToString(ch);
                        }
                        else
                        {
                            UInt32 valoffset = value - 0x10000;
                            UInt16 loval = (UInt16)(valoffset & 0x3FF); // low 10 bits
                            UInt16 hival = (UInt16)((valoffset >> 10) & 0x3FF); // high 10 bits

                            char ch1 = (char)(hival + 0xD800);
                            char ch2 = (char)(loval + 0xDC00);
                            str = string.Format("{0}{1}", ch1, ch2);
                        }
                        var item = new UnicodeData() { Alias = alias, Name = name, NameUC = name.ToUpper(), UnicodeString = str, UTF32 = value, CharacterType = type };
                        var block = UnicodeBlockData.BlockOf(value);
                        item.Block = block.Name;
                        item.BlockUC = block.NameUC;
                        data.Add(item);
                    }
                    else
                    {
                        // ?
                        value = 2;
                    }
                }
                catch (Exception)
                {
                    folder = null;
                }
            }
            foreach (var item in data)
            {
                Data.Add(item);
            }
        }
    }
}
