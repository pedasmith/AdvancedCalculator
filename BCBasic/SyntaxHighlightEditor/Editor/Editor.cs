using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Edit
{
    public class EditorProgram
    {
        public Lexer Lexer { get; } = new Lexer();
        public LanguageColors Language { get; } = new LanguageColors();
        public ObservableCollection<Statement> Statements = new ObservableCollection<Statement>();
    }
}
