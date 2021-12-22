using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Windows.UI.Xaml;

namespace AdvancedCalculator
{
    class TakeScreenshots
    {
        public static async Task TakeScreenshotsAsync(MainPage mainpage, AllPagesControl allPages)
        {
            //NOTE: you have to first run the app and set the size.  Then run again,
            // and get the nice images.

            const double SMALL_CALC_W = 2.1;
            const double WIDE_CALC_W = 12.0; // very wide!
            const double SCREEN_W = 800;
            const double SCREEN_H = 815;

            const double SCREEN_M = 1.0;

            mainpage.Width = SCREEN_W * SCREEN_M;
            mainpage.Height = SCREEN_H * SCREEN_M;

            //allPages.DisplayPage("uiCalculatorAlign");
            mainpage.DoSelectMain("uiCalculatorAlign");
            mainpage.DoButton("n6.9551");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiCalculatorAlign.png");

            mainpage.DoSelectMain("uiAdvancedWideAlign");
            mainpage.DoButton("adegrees");
            mainpage.DoButton("n0.7071");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiAdvancedWideAlign.png");

            mainpage.DoSelectMain("uiFormatAlign");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiFormatAlign.png");


            mainpage.DoSelectMain("uiFormatAlign");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiFormatAlign.png");


            mainpage.DoSelectMain("uiConstantsAlign");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiConstantsAlign.png");


            mainpage.DoSelectMain("uiMemoryAlign");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiMemoryAlign.png");


            mainpage.DoSelectMain("uiCalculatorProgrammerAlign");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiCalculatorProgrammerAlign.png");



            mainpage.DoSelectMain("uiColumnStatsAlign");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiColumnStatsAlign.png");

            mainpage.DoSelectMain("uiColumnStatsAlign");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, WIDE_CALC_W, double.NaN, null, "WIDE_uiColumnStatsAlign.png");


            mainpage.Width = 1000;
            mainpage.DoSelectMain("uiConversionsAsciiTableAlign");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiConversionsAsciiTableAlign.png");


            mainpage.DoSelectMain("uiConversionsUnicodeDataAlign");
            await allPages.SetStringAsync("uiConversionsUnicodeDataAlign", "search", "calcu");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiConversionsUnicodeDataAlign.png");


            mainpage.DoSelectMain("uiCalculatorQuickConnectionPopupAlign");
            await allPages.SetStringAsync("uiCalculatorQuickConnectionPopupAlign", "program", "x = 1/SIN(Calculator.Value)\nSTOP x\nREM CoSecant Calculation\nREM STOP x sets the calculator value.\n");
            await Task.Delay(2000); // have to wait for the color coding
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiCalculatorQuickConnectionPopupAlign.png");

            mainpage.DoSelectMain("uiCalculatorSigmaConnectionPopupAlign");
            await allPages.SetStringAsync("uiCalculatorSigmaConnectionPopupAlign", "sigma", "=n*n\n");
            await Task.Delay(2000); // have to wait for the color coding
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, SMALL_CALC_W, double.NaN, null, "uiCalculatorSigmaConnectionPopupAlign.png");




            mainpage.Width = 1200;
            mainpage.DoSelectMain("uiConversionsSubMenu");
            mainpage.DoSelectMain("uiConversionsArea");
            await Utilities.Screenshot.TakeScreenshotAsync(mainpage, 3.5, double.NaN, null, "area.png");
            await Utilities.Screenshot.TakeScreenshotAsync(allPages, 3.5, double.NaN, null, "area_zoom.png");



            mainpage.DoSelectMain("uiCalculatorAlign");
        }
    }
}
