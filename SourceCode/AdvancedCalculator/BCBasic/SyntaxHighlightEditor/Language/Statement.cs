using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents;

namespace Edit
{
    public class Statement
    {
        //
        // Because of how data-binding works, sometimes we have a Statement
        // that will shortly have a XamlStatement, but doesn't yet. But we
        // need to set a little bit of information about what the XamlStatement
        // should have set.
        //
        public Edit.LanguageEditorStatement XamlStatement;
        public bool XamlHaveSettings = false;
        public bool XamlShouldBeFocused = false;
        public int XamlShouldSetCursorPos = -1;
        public int XamlShouldSetEndCursorPos = -1; // EG, to make a selection work.
        public bool? XamlShouldSetForceCaretTextInvisible { get; set; } = null;

        // 
        // Things we're going to need
        //
        public ILanguageEditor Editor;
        public LanguageColors LanguageColors;
        public Lexer Lexer;

        public Statement(ILanguageEditor editor, LanguageColors language, Lexer lexer, string text)
        {
            Editor = editor;
            LanguageColors = language;
            Lexer = lexer;
            SetText(text);
        }
        public void SetText(string text)
        {
            NLine = text.Count(ch => ch == '\n') + 1; // the final \n isn't present.
            Words = Lexer.LexInput(new ROS(text)); // old way using Read Only Span had weird casting for the arguments. new ArraySegment<char>(text.ToCharArray()));
        }
        public int NLine = 1;
        public IList<Word> Words = new List<Word>();
        static int NAsSimpleText = 0;
        public string AsSimpleText
        {
            get
            {
                NAsSimpleText++;
                string Retval = "";
                foreach (var word in Words)
                {
                    Retval += word.ToString();
                }
                return Retval;
            }
        }

        public static double PreferredFontSize { get { return 16; } }
        public static double PreferredTextBlockHeight { get { return PreferredFontSize * 1.6; } }
        public List<Inline> AsInlines
        {
            get
            {
                List<Inline> Retval = new List<Inline>();
                foreach (var word in Words)
                {
                    Run run = new Run();
                    if (word.SymbolType == Lexer.SymbolType.NewLine)
                    {
                        // Newline is a little special!
                        Retval.Add(new LineBreak());
                    }
                    else
                    {
                        run.Text = word.ToString();
                        run.Foreground = LanguageColors.SymbolTypeToBrush(word.SymbolType);
                        Retval.Add(run);
                    }
                }
                return Retval;
            }
        }

        public override string ToString()
        {
            return AsSimpleText;
        }
    }
}
