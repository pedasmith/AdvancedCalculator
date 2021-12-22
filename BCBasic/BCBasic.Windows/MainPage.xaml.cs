using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using TinyPG;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BCBasic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //BCLibrary Library;
        public MainPage()
        {
            this.InitializeComponent();
            uiCalculatorConnection.Calculator = uiSampleCalculator;

            uiCalculatorConnection.ExternalConnections.AddExternal (uiSampleCalculator);
            uiCalculatorConnection.ExternalConnections.AddExternal(new SampleMemory());


            // All junk
            uiCalculatorConnection.ExternalConnections.Externals.Add("CALCULATOR", uiSampleCalculator); // TODO: get rid of this uppercase version


            uiSampleCalculator.CalculatorConnection = uiCalculatorConnection;

            //uiProgram.Text = "10 I=10\n20 J=20\n30 K=I+3\n40 DUMP\n";
            //uiProgram.Text = "10 I=10\n";
#if NEVER_EVER_DEFINED
            this.Loaded += async (s, e) =>
            {
                BCContext = new Context() { DoConsole = uiScreen };
                Library = new BCLibrary();

                await Library.InitAsync();
                uiLibrary.Library = Library;
                uiLibrary.BCContext = BCContext;
            };
#endif
        }

#if NEVER_EVER_DEFINED
        private Context BCContext = null;
        private BCBasicProgram Compile(string code)
        {
            //var ctx = new Context() { DoConsole = uiScreen };
            return BCBasicProgram.Compile(BCContext, code);

            /*
            var scanner = new Scanner();
            var p = new Parser(scanner);
            var result = p.Parse(code);
            var ctx = new Context() { DoConsole = uiScreen };

            ctx.WriteLine(string.Format("PARSE: {0} errors", result.Errors.Count));
            foreach (var error in result.Errors)
            {
                var lp = PosToLineRow(code, error.Column);
                ctx.WriteLine(string.Format("ERROR: line {0} column {1} error {2}", lp.Item1, lp.Item2, error.Message));
            }
            if (result.Errors.Count == 0)
            {
                return result.Eval(null) as BCBasic.BCBasicProgram;
            }
            return null;
             */
        }



        BCBasicProgram MyProgram = null;
        private async void OnRun(object sender, RoutedEventArgs e)
        {
            uiScreen.SetRunMode();


            string code = uiScreen.GetBCBasicProgram();
            var program = BCBasicProgram.Compile(BCContext, code);
            if (program != null)
            {
                MyProgram = program;
                MyProgram.BCContext.DoConsole = BCContext.DoConsole;
                await MyProgram.RunAsync();
            }

        }




        private void OnEdit(object sender, RoutedEventArgs e)
        {
            uiScreen.SetEditMode();

        }

        private async Task DoInteractiveAsync()
        {
            var code = uiInteractiveLine.Text + "\r\n"; // lines must always end in a CR
            if (MyProgram == null)
            {
                MyProgram = new BCBasicProgram();
                MyProgram.BCContext.DoConsole = uiScreen;
            }
            var program = Compile(code);
            if (program == null)
            {
                uiScreen.SetConsoleMode(); // there was an error, show it!
            }
            else
            {
                uiInteractiveLine.Text = "";
                uiScreen.SetRunMode();
                await MyProgram.RunInteractiveAsync(program.GetStatements());
            }

        }
        private async void OnInteractiveRun(object sender, RoutedEventArgs e)
        {
            await DoInteractiveAsync();

        }

        private async void OnInteractiveChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBox).Text.EndsWith ("\r\n"))
            {
                await DoInteractiveAsync();
            }
        }

        private void OnLibrary(object sender, RoutedEventArgs e)
        {
            var tb = sender as ToggleButton;
            if (tb == null) return;
            uiScreen.Visibility = (tb.IsChecked.Value) ? Visibility.Collapsed : Visibility.Visible;

        }
#endif
    }
}
