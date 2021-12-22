using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using Windows.Data.Json;
using System.Runtime.Serialization;
using System.IO;

namespace AdvancedCalculator
{
    public class WordList : ObservableCollection<WordDefinition>
    {
        public static async Task<WordList> Create()
        {
            var folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync("simpleDictionary.json");
            var fulltext = await Windows.Storage.FileIO.ReadTextAsync (file);

            WordList Retval = new WordList();

            string def = "";
            try
            {
                var json = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(WordList));
                var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(fulltext));
                
                //json.WriteObject(ms, Retval);
                var wl = json.ReadObject(ms) as WordList;
                if (wl != null)
                {
                    foreach (var item in wl)
                    {
                        Retval.Add(item);
                    }
                }

                //ms.Seek(0, System.IO.SeekOrigin.Begin);
                //var sr = new StreamReader(ms);
                //def = sr.ReadToEnd();
                //var dict = json.ReadObject(ms);
            }
            catch (Exception e)
            {
                Retval.Add(new WordDefinition()
                {
                    Word = "Exception",
                    Definition = e.ToString()
                });

            }

            try
            {
                var json = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(WordList));
                var ms = new System.IO.MemoryStream();
                json.WriteObject(ms, Retval);
                ms.Seek(0, System.IO.SeekOrigin.Begin);
                var sr = new StreamReader(ms);
                def = sr.ReadToEnd();
                //var dict = json.ReadObject(ms);
            }
            catch (Exception e)
            {
                def = "EXCEPTION: " + e.ToString();
            }
            //Console.Write(dict);

            //json.SerializeReadOnlyTypes (

            Retval.Add(new WordDefinition()
            {
                Word = "JSON Sample",
                Definition = def
            });
            return Retval;
        }
    }
}
