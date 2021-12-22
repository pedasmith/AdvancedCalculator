using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace AdvancedCalculator
{
    public class Formula
    {
        public string Description { get; set; }
        public string Value { get; set; }
        public string Conversion { get; set; }
    }

    public class WordDefinition
    {
        public WordDefinition()
        {
            Formulas = new ObservableCollection<Formula>();
        }
        public string Word { get; set; }
        public string Symbol { get; set; }
        public string Definition { get; set; }
        public string Example { get; set; }
        public string Tags { get; set; }
        public Uri Link { get; set; }
        public ObservableCollection<Formula> Formulas { get; set; }
    }
}
