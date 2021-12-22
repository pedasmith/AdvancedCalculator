
using BCBasic;
using System;
using System.ComponentModel;
using Windows.UI.Xaml;

namespace Edit
{

    public class SyntaxHighlightEditorBCBasicAdapter : IKeyDown
    {
        public event EventHandler Escape;
        public event EventHandler F5;
        public event EventHandler F7;

        // These were used in the original complex implementation.
        // The code is kept to remind me of how to hook up properties
        // that can be called from Xaml.
        //public event PropertyChangedEventHandler PropertyChanged;
        //public static DependencyProperty TextProperty;
        // Binding and UserControls:
        // http://blog.jerrynixon.com/2013/07/solved-two-way-binding-inside-user.html

        Edit.LanguageEditor Editor = null;
        public SyntaxHighlightEditorBCBasicAdapter(Edit.LanguageEditor editor, string editorName)
        {
            Editor = editor;
            editor.EditorName = editorName;
            Editor.AdapterKeyDown = this;
        }

        // These are all of the externals (like Bluetooth and Math.)
        // They need to be colored correctly.
        public void InitializeExternals(BCGlobalConnections externalConnections)
        {
            var list = Editor.EditorProgram.Lexer.Externals;

            foreach (var kvp in externalConnections.Externals)
            {
                list.Add(kvp.Key);
            }
        }

        public void Insert (string str)
        {
            Editor.InsertText(str);
        }

        // Gets called by the editor when keys are pressed.
        // Many keys are routed to here, but they should almost all 
        // be completely ignored. Only system-type keys (like ESCAPE
        // which cancels the running of a program) or F5 (starts the
        // program) should actually do anything.
        public void OnKeyDown(MyKeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.F5: F5?.Invoke(this, new EventArgs()); break;
                case Windows.System.VirtualKey.F7: F7?.Invoke(this, new EventArgs()); break;
                case Windows.System.VirtualKey.Escape: Escape?.Invoke(this, new EventArgs()); break;
            }
        }

        public string Text
        {
            get
            {
                if (Editor == null) return "";
                var str = Editor.GetText();
                return str;
            }

            set
            {
                if (Editor == null) return;
                Editor.SetText(value);
            }
        }

        public bool SetFocusOnEditor(bool focus)
        {
            if (Editor == null) return false; // no editor means we we're focused :-)
            var oldvalue = Editor.SetFocusOnEditor(focus);
            return oldvalue;
        }

    }
}

