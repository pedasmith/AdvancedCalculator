using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedCalculator
{
    // 1 tonne = 1000 kgram
    // 1 ounce = 28.3495231 grams (16 ounces=1lb 1 short ton [us]-->2000lb; 1 long ton [uk]-->2240 
    // troy ounce= 480 grains (regular ounce=437.5 grains).  12 troy onces-->1 troy pound
    // celsius to fahrenheit; kelvin=c+273.15; rankine=k*9/5

    // 1 acre = 43 560 square feet
    // gallon (us wet)=231 cu inches
    // fathom=2 yards
    public class Conversions
    {
        public static double INCHES_PER_FOOT = 12;
        public static double FEET_PER_MILE = 5280;
        public static double FEET_PER_YARD = 3;
        public static double CENTIMETERS_PER_INCH = 2.54;

        public static double AU_PER_PARSEC = 64800 / Math.PI;
        public static double METERS_PER_AU = 149597870700;
        public static double METERS_PER_LIGHTYEAR = 9460730472580800;

        public static double ACRES_PER_SQUARE_MILE = 640.0;
        public static double SQUARE_METERS_PER_HECTARE = 10000.0;

        public static double DEGREESF_PER_DEGREEC = 9.0 / 5.0;
        public static double DEGREESF_OFFSET = 32;
        public static double DEGREESKELVIN_OFFSET = 273.15;

        public static double OUNCES_PER_POUND = 16;
        public static double POUNDS_PER_SHORT_TON = 2000;
        public static double POUNDS_PER_LONG_TON = 2240;
        public static double GRAMS_PER_OUNCE = 28.3495231;
        public static double KILOGRAMS_PER_TONNE = 1000.0; // Tonne=metric ton
        public static double TONNES_PER_MMT = 1000000.0; // mmt=million of metric tons
        public static double GRAINS_PER_OUNCE = 437.5;
        public static double GRAINS_PER_TROY_OUNCE = 480;
        public static double TROY_OUNCES_PER_TROY_POUND = 12;
        // http://en.wikipedia.org/wiki/Indian_weights_and_measures 
        public static double GRAMS_PER_TOLA = 11.6638038; // Tolä
        public static double TOLA_PER_SER = 80; // Sèr
        public static double SER_PER_MAUND = 40; 


        public static double DEGREES_PER_RADIAN = (360.0 / (2.0 * Math.PI));

        public static double CUPS_PER_GALLON_DRY_US = 16.0;
        public static double PINTS_PER_GALLON_DRY_US = 8.0;
        public static double QUARTS_PER_GALLON_DRY_US = 4.0;
        public static double GALLONS_PER_BUSHEL_DRY_US = 8.0;
        public static double PECK_PER_BUSHEL_DRY_US = 4.0;

        public static double DRY_US_QUARTS_PER_LITER = 0.908082984;

        public static double ERGS_PER_JOULE = 10000000.0;
        public static double JOULES_PER_KILOWATTHOUR = 3600000;
        public static double JOULES_PER_BTU = 1055.056; // ISO BTU
        public static double BTUS_PER_THERM = 100000.0;
        public static double JOULES_PER_CALORIE = 4.184;
        public static double JOULES_PER_KCAL = 4184.0; // food calorie.
        public static double KCALS_PER_GRAM_FAT = 9.0;
        public static double KCALS_PER_DOUNT = 198; // medium sized donut; 10.8g fat, 23.4g carbo, 2.4g protein
        //public static double KCALS_PER_GRAM_CARBOHYDRATE = 4.0;
    }
}
