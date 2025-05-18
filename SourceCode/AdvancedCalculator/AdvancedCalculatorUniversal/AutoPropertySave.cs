using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Reflection;
using System.Collections.ObjectModel;

namespace AdvancedCalculator
{
    public interface DoSaveMemory
    {
        void SaveMemory();
    }
    public class AutoPropertySave: DoSaveMemory
    {
        static Windows.Storage.ApplicationDataContainer Settings = Windows.Storage.ApplicationData.Current.RoamingSettings;
        public void OnSuspend()
        {
            SaveMemory();
        }

        public void SaveMemory()
        {
            string memory = Notepad.JSon.Serialize(simpleCalculator.Memory);
            Settings.Values["Memory"] = memory;

            string memoryNames = Notepad.JSon.Serialize(simpleCalculator.MemoryNames);
            Settings.Values["MemoryNames"] = memoryNames;
        }

        SimpleCalculator simpleCalculator;
        public void Init(SimpleCalculator simpleCalculator)
        {
            this.simpleCalculator = simpleCalculator;
            simpleCalculator.PropertyChanged += simpleCalculator_PropertyChanged;
            string InView = mainPage?.CalculatorInView() ?? "";
            Set(simpleCalculator, "DisplaySpecifier", InView + "DisplaySpecifier");
            Set(simpleCalculator, "DisplayPrecision");
            Set(simpleCalculator, "DisplaySize");
            Set(simpleCalculator, "StringTrigUnits");

            SetDouble(simpleCalculator, "ResultDouble");

            string value = Settings.Values["Memory"] as string;
            if (value != null && value != "")
            {
                ObservableCollection<string> memory = Notepad.JSon.Deserialize<ObservableCollection<string>>(value);
                for (int i = 0; i < Math.Min(memory.Count, simpleCalculator.Memory.Count); i++)
                {
                    simpleCalculator.Memory[i] = memory[i];
                }
            }
            value = Settings.Values["MemoryNames"] as string;
            if (value != null && value != "")
            {
                ObservableCollection<string> memoryNames = Notepad.JSon.Deserialize<ObservableCollection<string>>(value);
                for (int i = 0; i < Math.Min(memoryNames.Count, simpleCalculator.MemoryNames.Count); i++)
                {
                    simpleCalculator.MemoryNames[i] = memoryNames[i];
                }
            }
        }

        MainPage mainPage;
        CalculatorLog Log = new CalculatorLog();
        public void Init(MainPage mainPage)
        {
            Log.WriteWithTime("APS: InitAsync: MainPage: called\r\n");
            this.mainPage = mainPage;
            mainPage.PropertyChanged += mainPage_PropertyChanged;
            Log.WriteWithTime("APS: InitAsync: MainPage: doing Set SelectMain\r\n");
            Set(mainPage, "SelectMain");
            Log.WriteWithTime("APS: InitAsync: MainPage: doing Set SelectSub\r\n");
            if (mainPage.SelectMain != null && mainPage.SelectMain.Contains("SubMenu"))
            {
                Set(mainPage, "SelectSub");
            }
            Log.WriteWithTime("APS: InitAsync: MainPage: doing Alignment\r\n");
            Set(mainPage, "Alignment");
            Log.WriteWithTime("APS: InitAsync: MainPage: doing ColorScheme\r\n");
            Set(mainPage, "ColorScheme");
            Log.WriteWithTime("APS: InitAsync: MainPage: doing BackgroundColor\r\n");
            Set(mainPage, "BackgroundColor");
            Log.WriteWithTime("APS: InitAsync: MainPage: doing Font\r\n");
            Set(mainPage, "Font");
            Log.WriteWithTime("APS: InitAsync: MainPage: return\r\n");
        }

        Background background;
        public void Init(Background background)
        {
            Log.WriteWithTime("APS: InitAsync: Background: called\r\n");
            this.background = background;
            background.PropertyChanged += background_PropertyChanged;
            Set(background, "TextColor");
            Set(background, "BackgroundText");
            Set(background, "NumberOfRows");
            Set(background, "NumberMargin");
            Set(background, "NumberOpacity");
            Log.WriteWithTime("APS: InitAsync: Background: return\r\n");
        }

        // e.g. GetSetting (CalculatorInView()+"DisplaySpecifier") for the specialty base10/base16 etc setting for the programmable calculator.
        public string GetSetting(string SaveName)
        {
            string val = Settings.Values[SaveName] as string;
            return val;
        }

        private Windows.UI.Color StringToColor(string colorString)
        {
            colorString = colorString.Substring(1, colorString.Length - 1);//remove the #
            var style = System.Globalization.NumberStyles.HexNumber;
            int hexColorAsInteger = int.Parse(colorString, style);
            byte[] colorData = BitConverter.GetBytes(hexColorAsInteger);

            //Mind the order.
            byte alpha = colorData[3];
            byte red = colorData[2];
            byte green = colorData[1];
            byte blue = colorData[0];

            var color = Windows.UI.Color.FromArgb(alpha, red, green, blue);
            return color;
        }

        private void Set(object obj, string PropertyName, string SaveName=null)
        {
            if (SaveName == null) SaveName = PropertyName;
            string val = Settings.Values[SaveName] as string;
            if (val != null && val != "")
            {
                var rtp = obj.GetType().GetRuntimeProperty(PropertyName);
                if (rtp != null)
                {
                    Log.WriteWithTime("APS: Set: start:  " + PropertyName + "\r\n");
                    if (PropertyName.EndsWith("Color"))
                    {
                        var color = StringToColor(val);
                        rtp.SetValue(obj, color);
                    }
                    else if (PropertyName.StartsWith("Number"))
                    {
                        double value = 1.0;
                        bool ok = Double.TryParse(val, out value);
                        if (ok)
                        {
                            rtp.SetValue(obj, value);
                        }
                        else
                        {
                            ok = false; 
                        }
                    }
                    else if (PropertyName == "Font")
                    {
                        rtp.SetValue(obj, val);
                    }
                    else
                    {
                        rtp.SetValue(obj, val);
                    }
                    Log.WriteWithTime("APS: Set: end:  " + PropertyName + "\r\n");
                }
                else
                {
                    Log.WriteWithTime("APS: Set: failed to get runtime property for name " + PropertyName + "\r\n");
                }
            }
        }

        // Call is, e.g. SetDouble(simpleCalculator, "ResultDouble");
        private void SetDouble(object obj, string Name)
        {
            string val = Settings.Values[Name] as string;
            if (val != null && val != "")
            {
                double dval = 0.0;
                bool Ok = double.TryParse (val, out dval);
                if (Ok)
                {
                    obj.GetType().GetRuntimeProperty(Name).SetValue(obj, dval);
                }
            }
        }



        void simpleCalculator_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SimpleCalculator simpleCalculator = sender as SimpleCalculator;
            if (simpleCalculator == null) return;
            if (mainPage == null) return;

            // The DisplaySpecifier is per-calculator because the programmer's calc
            // uses base8, base10, etc, and the regular calculator uses G, N, etc.
            switch (e.PropertyName)
            {
                case "DisplaySpecifier":
                    string InView = mainPage.CalculatorInView();
                    Settings.Values[InView + "DisplaySpecifier"] = simpleCalculator.DisplaySpecifier;
                    break;
                case "DisplayPrecision": Settings.Values["DisplayPrecision"] = simpleCalculator.DisplayPrecision; break;
                case "DisplaySize": Settings.Values["DisplaySize"] = simpleCalculator.DisplaySize; break;
                case "StringTrigUnits": Settings.Values["StringTrigUnits"] = simpleCalculator.StringTrigUnits; break;

                case "ResultDouble": Settings.Values["ResultDouble"] = String.Format ("{0:R}", simpleCalculator.ResultDouble); break;
            }
        }


        void mainPage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            MainPage mainPage = sender as MainPage;
            if (mainPage == null) return;

            switch (e.PropertyName)
            {
                case "SelectMain": Settings.Values[e.PropertyName] = mainPage.SelectMain; break;
                case "SelectSub": Settings.Values[e.PropertyName] = mainPage.SelectSub; break;
                case "Alignment": Settings.Values[e.PropertyName] = mainPage.Alignment; break;
                case "cbc": Settings.Values["ColorScheme"] = mainPage.ColorScheme.ToString(); break;
                case "Font": Settings.Values["Font"] = mainPage.GetFont(); break;
                case "BackgroundColor": Settings.Values[e.PropertyName] = mainPage.BackgroundColor.ToString(); break;
            }
        }

        void background_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Background background = sender as Background;
            if (background == null) return;

            switch (e.PropertyName)
            {
                case "TextColor": Settings.Values[e.PropertyName] = background.TextColor.ToString(); break;
                case "BackgroundText": Settings.Values[e.PropertyName] = background.BackgroundText; break;
                case "NumberOfRows": Settings.Values[e.PropertyName] = background.NumberOfRows.ToString(); break;
                case "NumberMargin": Settings.Values[e.PropertyName] = background.NumberMargin.ToString(); break;
                case "NumberOpacity": Settings.Values[e.PropertyName] = background.NumberOpacity.ToString(); break;
            }
        }
    }
}
