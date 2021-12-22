using BCBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace AdvancedCalculator.BCBasic.RunTimeLibrary
{
    class DataLocation
    {
        /*
         * Data is from http://download.geonames.org/export/dump/ and is the Cities15000.txt file (cities with population >= 15K)
                The main 'geoname' table has the following fields :
        ---------------------------------------------------
        geonameid         : integer id of record in geonames database
        name              : name of geographical point(utf8) varchar(200)
        asciiname         : name of geographical point in plain ascii characters, varchar(200)
        alternatenames    : alternatenames, comma separated, ascii names automatically transliterated, convenience attribute from alternatename table, varchar(10000)
        latitude          : latitude in decimal degrees(wgs84)
        longitude         : longitude in decimal degrees(wgs84)
        feature class     : see http://www.geonames.org/export/codes.html, char(1)
        feature code      : see http://www.geonames.org/export/codes.html, varchar(10)
        country code      : ISO-3166 2-letter country code, 2 characters
        cc2               : alternate country codes, comma separated, ISO-3166 2-letter country code, 200 characters
        admin1 code       : fipscode (subject to change to iso code), see exceptions below, see file admin1Codes.txt for display names of this code; varchar(20)
        admin2 code       : code for the second administrative division, a county in the US, see file admin2Codes.txt; varchar(80) 
        admin3 code       : code for third level administrative division, varchar(20)
        admin4 code       : code for fourth level administrative division, varchar(20)
        population        : bigint (8 byte int) 
        elevation         : in meters, integer
        dem               : digital elevation model, srtm3 or gtopo30, average elevation of 3''x3'' (ca 90mx90m) or 30''x30'' (ca 900mx900m) area in meters, integer.srtm processed by cgiar/ciat.
        blanks
        timezone          : the iana timezone id(see file timeZone.txt) varchar(40)
        modification date : date of last modification in yyyy-MM-dd format
         */
        public class City
        {
            public string FullName
            {
                get
                {
                    if (admin1 != "") return $"{name} ({admin1}, {countryCode})";
                    return $"{name} ({countryCode})";
                }
            }
            public int geonameid { get; internal set; } = 0;
            public string name { get; internal set; } = "(no name)";
            public string asciiname { get; internal set; } = "(no name)";
            // skip alternate names public string alternatenames { get; internal set; } = "(no alternate names)";
            public double latitudeDD { get; internal set; } = 0.0;
            public double longitudeDD { get; internal set; } = 0.0; //index=5
            public string featureClass { get; internal set; } = "?";
            public string featureCode { get; internal set; } = "???";
            public string countryCode { get; internal set; } = "ZZ";
            public string cc2 { get; internal set; } = ""; 
            public string admin1 { get; internal set; } = ""; // is like the state; index=10
            // skip over the admin1 admin2 admin3 admin4 codes //index=10 for admin1
            public double population { get; internal set; } = 0.0; // index=14
            public double elevation { get; internal set; } = 0.0; // index=16
            // skip over dem
            // skip over blanks
            // NOTE: do I need this? skip over public string timezone { get; internal set; } // index=18
            // skip over public DateTime modificationDate { get; internal set; } // index=19

            public BCValueList AsValueList()
            {
                // Only includes a subset of possible values
                BCValueList Retval = new BCValueList();
                Retval.SetProperty("GeoNameId", new BCValue(geonameid));
                Retval.SetProperty("FullName", new BCValue(FullName));
                Retval.SetProperty("Name", new BCValue(name));
                Retval.SetProperty("Admin1", new BCValue(admin1));
                Retval.SetProperty("Country", new BCValue(countryCode));
                Retval.SetProperty("LatitudeDD", new BCValue(latitudeDD));
                Retval.SetProperty("LongitudeDD", new BCValue(longitudeDD));
                Retval.SetProperty("Population", new BCValue(population));
                Retval.SetProperty("Elevation", new BCValue(elevation));
                return Retval;
            }
            public static City FromCityLine(string text)
            {
                var data = text.Split(new char[] { '\t' });
                if (data.Length != 19)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR reading cities15000.txt in line {text}; field count is {data.Length}");
                    return null;
                }
                City Retval = new City();

                int intval;
                double dval;
                var parseOk = double.TryParse(data[14], out dval);
                if (!parseOk)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR reading cities15000.txt in line {text}; population is not a double {data[14]}");
                    return null;
                }
                if (dval < 100000)
                {
                    // Short circuit; we only want BIG cities
                    return null;
                }
                Retval.population = dval;

                parseOk = int.TryParse(data[0], out intval);
                if (!parseOk)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR reading cities15000.txt in line {text}; geonameid is not an int {data[0]}");
                    return null;
                }
                Retval.geonameid = intval;
                Retval.name = data[1];
                Retval.asciiname = data[2];
                parseOk = double.TryParse(data[4], out dval);
                if (!parseOk)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR reading cities15000.txt in line {text}; latitude is not an doule {data[4]}");
                    return null;
                }
                Retval.latitudeDD = dval;
                parseOk = double.TryParse(data[5], out dval);
                if (!parseOk)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR reading cities15000.txt in line {text}; longitude is not an double {data[5]}");
                    return null;
                }
                Retval.longitudeDD = dval;
                Retval.featureClass = data[6];
                Retval.featureCode = data[7];
                Retval.countryCode = data[8];
                Retval.cc2 = data[9];
                Retval.admin1 = data[10];
                parseOk = double.TryParse(data[16], out dval);
                if (!parseOk)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR reading cities15000.txt in line {text}; elevation is not a double {data[15]}");
                    return null;
                }
                Retval.elevation = dval;
                return Retval;
            }
        }

        static List<City> Cities = new List<City>();
        enum InitStatus { NotInit, Reading, Init };
        static InitStatus CitiesStatus = DataLocation.InitStatus.NotInit;

        private static async Task InitAsync()
        {
            if (CitiesStatus == InitStatus.Init) return;
            if (CitiesStatus == InitStatus.Reading)
            {
                while (CitiesStatus == InitStatus.Reading)
                {
                    await Task.Delay(50); // Wait a bit
                }
                return;
            }

            CitiesStatus = InitStatus.Reading;
            StorageFile sFile = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Assets\" + "cities15000.txt");
            var lines = await FileIO.ReadLinesAsync(sFile);
            foreach (var line in lines)
            {
                var city = City.FromCityLine(line);
                if (city != null) Cities.Add(city);
            }
            Cities = Cities.OrderBy(item => item.FullName).ToList();
            CitiesStatus = InitStatus.Init;
        }

        public static async Task<BCValueList> GetLocationsAsync(string match)
        {
            var Retval = new BCValueList();
            var matchlower = match.ToLower();
            await InitAsync();
            foreach (var city in Cities)
            {
                bool isMatch = city.asciiname.ToLower().Contains(matchlower);
                if (isMatch)
                {
                    BCValue value = new BCValue(city.AsValueList());
                    Retval.Append(value);
                }
            }
            return Retval;
        }

        public static async Task<BCValueList> PickAsync()
        {
            await InitAsync();

            var dlg = new ContentDialog()
            {
                Title = "Pick a city",
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = "OK",
                IsSecondaryButtonEnabled = true,
                SecondaryButtonText = "Cancel",
            };
            var border = new Border() { Background = new SolidColorBrush(Colors.AntiqueWhite) };
            var panel = new StackPanel();
            border.Child = panel;
            dlg.Content = border;

            var combo = new ComboBox() { MinWidth = 300 };
            panel.Children.Add(combo);
            foreach (var city in Cities)
            {
                var cbi = new ComboBoxItem()
                {
                    Content = city.FullName,
                    Tag = city
                };
                combo.Items.Add(cbi);
            }
            combo.SelectedIndex = 0;

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var Retval = (combo.SelectedItem as ComboBoxItem)?.Tag as City;
                return Retval.AsValueList();
            }

            
            return null;
        }
    }
}
