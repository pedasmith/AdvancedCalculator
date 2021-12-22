using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.Storage.Streams;

namespace AdvancedCalculator.Notepad
{
    class Rtf
    {
        // Converts a text to RTF
        public static async Task<string> FromText(string text)
        {
            string Retval = "";
            var rtf = new RichEditBox();
            //var run = new Run() { Text = text };
            //var paragraph = new Paragraph();
            //paragraph.Inlines.Add(run);
            rtf.Document.SetText(Windows.UI.Text.TextSetOptions.ApplyRtfDocumentDefaults, text);
            Retval = rtf.Document.ToString();
            rtf.Document.GetText(Windows.UI.Text.TextGetOptions.None, out Retval);
            
            var stream = new InMemoryRandomAccessStream ();
            rtf.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, stream);
            stream.Seek(0);
            DataReader dr = new DataReader(stream);
            ulong size = stream.Size;
            await dr.LoadAsync((uint)size);
            Retval = dr.ReadString(dr.UnconsumedBufferLength);
            
            return Retval;
        }

        public static async Task<InMemoryRandomAccessStream> RtfToStream(string rtfContent)
        {
            var stream = new InMemoryRandomAccessStream();
            DataWriter dw = new DataWriter(stream);
            dw.WriteString(rtfContent);
            await dw.StoreAsync();
            dw.DetachStream();
            stream.Seek(0);
            return stream;
        }
    }
}
