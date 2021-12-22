using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace Utilities
{
    class BluetoothUtilities
    {
        public const string BluetoothGuidSuffix = "-0000-1000-8000-00805f9b34fb";
        public static Guid Bluetooth2901 = new Guid("00002901" + BluetoothGuidSuffix);
        public static Guid Bluetooth2902 = new Guid("00002902" + BluetoothGuidSuffix);
        public static Guid Bluetooth2904 = new Guid("00002904" + BluetoothGuidSuffix);

        public static string BluetoothUuidToMinimalString(Guid uid)
        {
            var str = uid.ToString();
            if (str.EndsWith(BluetoothGuidSuffix))
            {
                str = str.Replace(BluetoothGuidSuffix, "");
                if (str.StartsWith("0000")) str = str.Substring(4);
            }
            return str;
        }

        // e.g., "1800" is "generic access"
        public static string BluetoothServiceName(string str)
        {
            string Retval = "";
            switch (str)
            {
                case "1800": Retval = "Generic access"; break;
                #region LongListOfData
                case "1801": Retval = "Generic attribute"; break;
                case "180a": Retval = "Device info"; break;

                case "2901": Retval = "User description"; break;
                case "2902": Retval = "Client char."; break;

                case "2a00": Retval = "Device Name"; break;
                case "2a01": Retval = "Appearance"; break;
                case "2a02": Retval = "Privacy"; break;
                case "2a03": Retval = "Reconnect Addr"; break;
                case "2a04": Retval = "Conn param."; break;
                case "2a05": Retval = "Svc changed"; break;

                case "2a23": Retval = "System id"; break;
                case "2a24": Retval = "Model no."; break;
                case "2a25": Retval = "Serial no."; break;
                case "2a26": Retval = "Firmware"; break;
                case "2a27": Retval = "Hardware rev."; break;
                case "2a28": Retval = "Software rev."; break;
                case "2a29": Retval = "Manufacturer"; break;
                case "2a2a": Retval = "Cert."; break;

                case "2a50": Retval = "PNP id"; break;

                case "e": Retval = ""; break;
                case "f": Retval = ""; break;
                case "g": Retval = ""; break;
                case "h": Retval = ""; break;
                case "i": Retval = ""; break;
                #endregion
                default:
                    break;
            }
            return Retval;

        }
        public static string BluetoothUuidToString(Guid uid)
        {
            // Bluetooth UUIDs are e.g. 00002902-0000-1000-8000-00805f9b34fb
            // Last part is always the same, and first digits are zero.
            // See https://www.bluetooth.org/en-us/specification/assigned-numbers for assigned numbers
            var str = BluetoothUuidToMinimalString(uid);
            var name = BluetoothServiceName(str);
            if (name != "") str = $"{name} [{str}]";
            return str;
        }

        public static string BluetoothAppearanceToString(ushort value)
        {
            switch (value)
            {
                case 0: return "Unknown";
                case 64: return "Phone";
                #region LongListOfData
                case 128: return "Computer";
                case 192: return "Watch";
                case 193: return "Sports Watch";
                case 256: return "Clock";
                case 320: return "Display";
                case 384: return "Remote Control";
                case 448: return "Eye-glasses";
                case 512: return "Tag";
                case 576: return "Keyring";
                case 640: return "Media Player";
                case 704: return "Barcode Scanner";
                case 768: return "Thermometer";
                case 769: return "Thermometer: Ear";
                case 832: return "Heart rate Sensor";
                case 833: return "Heart Rate Sensor: Heart Rate Belt";
                case 896: return "Blood Pressure";
                case 897: return "Blood Pressure: Arm";
                case 898: return "Blood Pressure: Wrist";
                case 960: return "Human Interface Device (HID)";
                case 961: return "Keyboard";
                case 962: return "Mouse";
                case 963: return "Joystick";
                case 964: return "Gamepad";
                case 965: return "Digitizer Tablet";
                case 966: return "Card Reader";
                case 967: return "Digital Pen";
                case 968: return "Barcode Scanner";
                case 1024: return "Glucose Meter";
                case 1088: return "Running Walking Sensor";
                case 1089: return "Running Walking Sensor: In-Shoe";
                case 1090: return "Running Walking Sensor: On-Shoe";
                case 1091: return "Running Walking Sensor: On-Hip";
                case 1152: return "Cycling";
                case 1153: return "Cycling: Cycling Computer";
                case 1154: return "Cycling: Speed Sensor";
                case 1155: return "Cycling: Cadence Sensor";
                case 1156: return "Cycling: Power Sensor";
                case 1157: return "Cycling: Speed and Cadence Sensor";
                case 3136: return "Pulse Oximeter";
                case 3137: return "Fingertip";
                case 3138: return "Wrist Worn";
                case 3200: return "Weight Scale";
                case 5184: return "Outdoor Sports Activity";
                case 5185: return "Location Display Device";
                case 5186: return "Location and Navigation Display Device";
                case 5187: return "Location Pod";
                case 5188: return "Location and Navigation Pod";
                    #endregion
            }
            return $"Unknown ({value})";
        }

        // From https://www.bluetooth.org/en-us/specification/assigned-numbers/company-identifiers 2015-11-10
        // Manufacturer value
        public static string BluetoothCompanyIdentifier(ushort companyId)
        {
            switch (companyId)
            {
                case 0: return "Ericsson Technology Licensing";
                case 1: return "Nokia Mobile Phones";
                #region LongCompanyList
                case 2: return "Intel Corp.";
                case 3: return "IBM Corp.";
                case 4: return "Toshiba Corp.";
                case 5: return "3Com";
                case 6: return "Microsoft";
                case 7: return "Lucent";
                case 8: return "Motorola";
                case 9: return "Infineon Technologies AG";
                case 10: return "Cambridge Silicon Radio";
                case 11: return "Silicon Wave";
                case 12: return "Digianswer A/S";
                case 13: return "Texas Instruments Inc.";
                case 14: return "Ceva, Inc. (formerly Parthus Technologies, Inc.)";
                case 15: return "Broadcom Corporation";
                case 16: return "Mitel Semiconductor";
                case 17: return "Widcomm, Inc";
                case 18: return "Zeevo, Inc.";
                case 19: return "Atmel Corporation";
                case 20: return "Mitsubishi Electric Corporation";
                case 21: return "RTX Telecom A/S";
                case 22: return "KC Technology Inc.";
                case 23: return "NewLogic";
                case 24: return "Transilica, Inc.";
                case 25: return "Rohde & Schwarz GmbH & Co. KG";
                case 26: return "TTPCom Limited";
                case 27: return "Signia Technologies, Inc.";
                case 28: return "Conexant Systems Inc.";
                case 29: return "Qualcomm";
                case 30: return "Inventel";
                case 31: return "AVM Berlin";
                case 32: return "BandSpeed, Inc.";
                case 33: return "Mansella Ltd";
                case 34: return "NEC Corporation";
                case 35: return "WavePlus Technology Co., Ltd.";
                case 36: return "Alcatel";
                case 37: return "NXP Semiconductors (formerly Philips Semiconductors)";
                case 38: return "C Technologies";
                case 39: return "Open Interface";
                case 40: return "R F Micro Devices";
                case 41: return "Hitachi Ltd";
                case 42: return "Symbol Technologies, Inc.";
                case 43: return "Tenovis";
                case 44: return "Macronix International Co. Ltd.";
                case 45: return "GCT Semiconductor";
                case 46: return "Norwood Systems";
                case 47: return "MewTel Technology Inc.";
                case 48: return "ST Microelectronics";
                case 49: return "Synopsis";
                case 50: return "Red-M (Communications) Ltd";
                case 51: return "Commil Ltd";
                case 52: return "Computer Access Technology Corporation (CATC)";
                case 53: return "Eclipse (HQ Espana) S.L.";
                case 54: return "Renesas Electronics Corporation";
                case 55: return "Mobilian Corporation";
                case 56: return "Terax";
                case 57: return "Integrated System Solution Corp.";
                case 58: return "Matsushita Electric Industrial Co., Ltd.";
                case 59: return "Gennum Corporation";
                case 60: return "BlackBerry Limited (formerly Research In Motion)";
                case 61: return "IPextreme, Inc.";
                case 62: return "Systems and Chips, Inc.";
                case 63: return "Bluetooth SIG, Inc.";
                case 64: return "Seiko Epson Corporation";
                case 65: return "Integrated Silicon Solution Taiwan, Inc.";
                case 66: return "CONWISE Technology Corporation Ltd";
                case 67: return "PARROT SA";
                case 68: return "Socket Mobile";
                case 69: return "Atheros Communications, Inc.";
                case 70: return "MediaTek, Inc.";
                case 71: return "Bluegiga";
                case 72: return "Marvell Technology Group Ltd.";
                case 73: return "3DSP Corporation";
                case 74: return "Accel Semiconductor Ltd.";
                case 75: return "Continental Automotive Systems";
                case 76: return "Apple, Inc.";
                case 77: return "Staccato Communications, Inc.";
                case 78: return "Avago Technologies";
                case 79: return "APT Licensing Ltd.";
                case 80: return "SiRF Technology";
                case 81: return "Tzero Technologies, Inc.";
                case 82: return "J&M Corporation";
                case 83: return "Free2move AB";
                case 84: return "3DiJoy Corporation";
                case 85: return "Plantronics, Inc.";
                case 86: return "Sony Ericsson Mobile Communications";
                case 87: return "Harman International Industries, Inc.";
                case 88: return "Vizio, Inc.";
                case 89: return "Nordic Semiconductor ASA";
                case 90: return "EM Microelectronic-Marin SA";
                case 91: return "Ralink Technology Corporation";
                case 92: return "Belkin International, Inc.";
                case 93: return "Realtek Semiconductor Corporation";
                case 94: return "Stonestreet One, LLC";
                case 95: return "Wicentric, Inc.";
                case 96: return "RivieraWaves S.A.S";
                case 97: return "RDA Microelectronics";
                case 98: return "Gibson Guitars";
                case 99: return "MiCommand Inc.";
                case 100: return "Band XI International, LLC";
                case 101: return "Hewlett-Packard Company";
                case 102: return "9Solutions Oy";
                case 103: return "GN Netcom A/S";
                case 104: return "General Motors";
                case 105: return "A&D Engineering, Inc.";
                case 106: return "MindTree Ltd.";
                case 107: return "Polar Electro OY";
                case 108: return "Beautiful Enterprise Co., Ltd.";
                case 109: return "BriarTek, Inc.";
                case 110: return "Summit Data Communications, Inc.";
                case 111: return "Sound ID";
                case 112: return "Monster, LLC";
                case 113: return "connectBlue AB";
                case 114: return "ShangHai Super Smart Electronics Co. Ltd.";
                case 115: return "Group Sense Ltd.";
                case 116: return "Zomm, LLC";
                case 117: return "Samsung Electronics Co. Ltd.";
                case 118: return "Creative Technology Ltd.";
                case 119: return "Laird Technologies";
                case 120: return "Nike, Inc.";
                case 121: return "lesswire AG";
                case 122: return "MStar Semiconductor, Inc.";
                case 123: return "Hanlynn Technologies";
                case 124: return "A & R Cambridge";
                case 125: return "Seers Technology Co. Ltd";
                case 126: return "Sports Tracking Technologies Ltd.";
                case 127: return "Autonet Mobile";
                case 128: return "DeLorme Publishing Company, Inc.";
                case 129: return "WuXi Vimicro";
                case 130: return "Sennheiser Communications A/S";
                case 131: return "TimeKeeping Systems, Inc.";
                case 132: return "Ludus Helsinki Ltd.";
                case 133: return "BlueRadios, Inc.";
                case 134: return "equinox AG";
                case 135: return "Garmin International, Inc.";
                case 136: return "Ecotest";
                case 137: return "GN ReSound A/S";
                case 138: return "Jawbone";
                case 139: return "Topcorn Positioning Systems, LLC";
                case 140: return "Gimbal Inc. (formerly Qualcomm Labs, Inc. and Qualcomm Retail Solutions, Inc.)";
                case 141: return "Zscan Software";
                case 142: return "Quintic Corp.";
                case 143: return "Stollman E+V GmbH";
                case 144: return "Funai Electric Co., Ltd.";
                case 145: return "Advanced PANMOBIL Systems GmbH & Co. KG";
                case 146: return "ThinkOptics, Inc.";
                case 147: return "Universal Electronics, Inc.";
                case 148: return "Airoha Technology Corp.";
                case 149: return "NEC Lighting, Ltd.";
                case 150: return "ODM Technology, Inc.";
                case 151: return "ConnecteDevice Ltd.";
                case 152: return "zer01.tv GmbH";
                case 153: return "i.Tech Dynamic Global Distribution Ltd.";
                case 154: return "Alpwise";
                case 155: return "Jiangsu Toppower Automotive Electronics Co., Ltd.";
                case 156: return "Colorfy, Inc.";
                case 157: return "Geoforce Inc.";
                case 158: return "Bose Corporation";
                case 159: return "Suunto Oy";
                case 160: return "Kensington Computer Products Group";
                case 161: return "SR-Medizinelektronik";
                case 162: return "Vertu Corporation Limited";
                case 163: return "Meta Watch Ltd.";
                case 164: return "LINAK A/S";
                case 165: return "OTL Dynamics LLC";
                case 166: return "Panda Ocean Inc.";
                case 167: return "Visteon Corporation";
                case 168: return "ARP Devices Limited";
                case 169: return "Magneti Marelli S.p.A";
                case 170: return "CAEN RFID srl";
                case 171: return "Ingenieur-Systemgruppe Zahn GmbH";
                case 172: return "Green Throttle Games";
                case 173: return "Peter Systemtechnik GmbH";
                case 174: return "Omegawave Oy";
                case 175: return "Cinetix";
                case 176: return "Passif Semiconductor Corp";
                case 177: return "Saris Cycling Group, Inc";
                case 178: return "Bekey A/S";
                case 179: return "Clarinox Technologies Pty. Ltd.";
                case 180: return "BDE Technology Co., Ltd.";
                case 181: return "Swirl Networks";
                case 182: return "Meso international";
                case 183: return "TreLab Ltd";
                case 184: return "Qualcomm Innovation Center, Inc. (QuIC)";
                case 185: return "Johnson Controls, Inc.";
                case 186: return "Starkey Laboratories Inc.";
                case 187: return "S-Power Electronics Limited";
                case 188: return "Ace Sensor Inc";
                case 189: return "Aplix Corporation";
                case 190: return "AAMP of America";
                case 191: return "Stalmart Technology Limited";
                case 192: return "AMICCOM Electronics Corporation";
                case 193: return "Shenzhen Excelsecu Data Technology Co.,Ltd";
                case 194: return "Geneq Inc.";
                case 195: return "adidas AG";
                case 196: return "LG Electronics";
                case 197: return "Onset Computer Corporation";
                case 198: return "Selfly BV";
                case 199: return "Quuppa Oy.";
                case 200: return "GeLo Inc";
                case 201: return "Evluma";
                case 202: return "MC10";
                case 203: return "Binauric SE";
                case 204: return "Beats Electronics";
                case 205: return "Microchip Technology Inc.";
                case 206: return "Elgato Systems GmbH";
                case 207: return "ARCHOS SA";
                case 208: return "Dexcom, Inc.";
                case 209: return "Polar Electro Europe B.V.";
                case 210: return "Dialog Semiconductor B.V.";
                case 211: return "Taixingbang Technology (HK) Co,. LTD.";
                case 212: return "Kawantech";
                case 213: return "Austco Communication Systems";
                case 214: return "Timex Group USA, Inc.";
                case 215: return "Qualcomm Technologies, Inc.";
                case 216: return "Qualcomm Connected Experiences, Inc.";
                case 217: return "Voyetra Turtle Beach";
                case 218: return "txtr GmbH";
                case 219: return "Biosentronics";
                case 220: return "Procter & Gamble";
                case 221: return "Hosiden Corporation";
                case 222: return "Muzik LLC";
                case 223: return "Misfit Wearables Corp";
                case 224: return "Google";
                case 225: return "Danlers Ltd";
                case 226: return "Semilink Inc";
                case 227: return "inMusic Brands, Inc";
                case 228: return "L.S. Research Inc.";
                case 229: return "Eden Software Consultants Ltd.";
                case 230: return "Freshtemp";
                case 231: return "KS Technologies";
                case 232: return "ACTS Technologies";
                case 233: return "Vtrack Systems";
                case 234: return "Nielsen-Kellerman Company";
                case 235: return "Server Technology, Inc.";
                case 236: return "BioResearch Associates";
                case 237: return "Jolly Logic, LLC";
                case 238: return "Above Average Outcomes, Inc.";
                case 239: return "Bitsplitters GmbH";
                case 240: return "PayPal, Inc.";
                case 241: return "Witron Technology Limited";
                case 242: return "Aether Things Inc. (formerly Morse Project Inc.)";
                case 243: return "Kent Displays Inc.";
                case 244: return "Nautilus Inc.";
                case 245: return "Smartifier Oy";
                case 246: return "Elcometer Limited";
                case 247: return "VSN Technologies Inc.";
                case 248: return "AceUni Corp., Ltd.";
                case 249: return "StickNFind";
                case 250: return "Crystal Code AB";
                case 251: return "KOUKAAM a.s.";
                case 252: return "Delphi Corporation";
                case 253: return "ValenceTech Limited";
                case 254: return "Reserved";
                case 255: return "Typo Products, LLC";
                case 256: return "TomTom International BV";
                case 257: return "Fugoo, Inc";
                case 258: return "Keiser Corporation";
                case 259: return "Bang & Olufsen A/S";
                case 260: return "PLUS Locations Systems Pty Ltd";
                case 261: return "Ubiquitous Computing Technology Corporation";
                case 262: return "Innovative Yachtter Solutions";
                case 263: return "William Demant Holding A/S";
                case 264: return "Chicony Electronics Co., Ltd.";
                case 265: return "Atus BV";
                case 266: return "Codegate Ltd.";
                case 267: return "ERi, Inc.";
                case 268: return "Transducers Direct, LLC";
                case 269: return "Fujitsu Ten Limited";
                case 270: return "Audi AG";
                case 271: return "HiSilicon Technologies Co., Ltd.";
                case 272: return "Nippon Seiki Co., Ltd.";
                case 273: return "Steelseries ApS";
                case 274: return "Visybl Inc.";
                case 275: return "Openbrain Technologies, Co., Ltd.";
                case 276: return "Xensr";
                case 277: return "e.solutions";
                case 278: return "1OAK Technologies";
                case 279: return "Wimoto Technologies Inc";
                case 280: return "Radius Networks, Inc.";
                case 281: return "Wize Technology Co., Ltd.";
                case 282: return "Qualcomm Labs, Inc.";
                case 283: return "Aruba Networks";
                case 284: return "Baidu";
                case 285: return "Arendi AG";
                case 286: return "Skoda Auto a.s.";
                case 287: return "Volkswagon AG";
                case 288: return "Porsche AG";
                case 289: return "Sino Wealth Electronic Ltd.";
                case 290: return "AirTurn, Inc.";
                case 291: return "Kinsa, Inc.";
                case 292: return "HID Global";
                case 293: return "SEAT es";
                case 294: return "Promethean Ltd.";
                case 295: return "Salutica Allied Solutions";
                case 296: return "GPSI Group Pty Ltd";
                case 297: return "Nimble Devices Oy";
                case 298: return "Changzhou Yongse Infotech Co., Ltd";
                case 299: return "SportIQ";
                case 300: return "TEMEC Instruments B.V.";
                case 301: return "Sony Corporation";
                case 302: return "ASSA ABLOY";
                case 303: return "Clarion Co., Ltd.";
                case 304: return "Warehouse Innovations";
                case 305: return "Cypress Semiconductor Corporation";
                case 306: return "MADS Inc";
                case 307: return "Blue Maestro Limited";
                case 308: return "Resolution Products, Inc.";
                case 309: return "Airewear LLC";
                case 310: return "Seed Labs, Inc. (formerly ETC sp. z.o.o.)";
                case 311: return "Prestigio Plaza Ltd.";
                case 312: return "NTEO Inc.";
                case 313: return "Focus Systems Corporation";
                case 314: return "Tencent Holdings Limited";
                case 315: return "Allegion";
                case 316: return "Murata Manufacuring Co., Ltd.";
                case 318: return "Nod, Inc.";
                case 319: return "B&B Manufacturing Company";
                case 320: return "Alpine Electronics (China) Co., Ltd";
                case 321: return "FedEx Services";
                case 322: return "Grape Systems Inc.";
                case 323: return "Bkon Connect";
                case 324: return "Lintech GmbH";
                case 325: return "Novatel Wireless";
                case 326: return "Ciright";
                case 327: return "Mighty Cast, Inc.";
                case 328: return "Ambimat Electronics";
                case 329: return "Perytons Ltd.";
                case 330: return "Tivoli Audio, LLC";
                case 331: return "Master Lock";
                case 332: return "Mesh-Net Ltd";
                case 333: return "Huizhou Desay SV Automotive CO., LTD.";
                case 334: return "Tangerine, Inc.";
                case 335: return "B&W Group Ltd.";
                case 336: return "Pioneer Corporation";
                case 337: return "OnBeep";
                case 338: return "Vernier Software & Technology";
                case 339: return "ROL Ergo";
                case 340: return "Pebble Technology";
                case 341: return "NETATMO";
                case 342: return "Accumulate AB";
                case 343: return "Anhui Huami Information Technology Co., Ltd.";
                case 344: return "Inmite s.r.o.";
                case 345: return "ChefSteps, Inc.";
                case 346: return "micas AG";
                case 347: return "Biomedical Research Ltd.";
                case 348: return "Pitius Tec S.L.";
                case 349: return "Estimote, Inc.";
                case 350: return "Unikey Technologies, Inc.";
                case 351: return "Timer Cap Co.";
                case 352: return "AwoX";
                case 353: return "yikes";
                case 354: return "MADSGlobal NZ Ltd.";
                case 355: return "PCH International";
                case 356: return "Qingdao Yeelink Information Technology Co., Ltd.";
                case 357: return "Milwaukee Tool (formerly Milwaukee Electric Tools)";
                case 358: return "MISHIK Pte Ltd";
                case 359: return "Bayer HealthCare";
                case 360: return "Spicebox LLC";
                case 361: return "emberlight";
                case 362: return "Cooper-Atkins Corporation";
                case 363: return "Qblinks";
                case 364: return "MYSPHERA";
                case 365: return "LifeScan Inc";
                case 366: return "Volantic AB";
                case 367: return "Podo Labs, Inc";
                case 368: return "Roche Diabetes Care AG";
                case 369: return "Amazon Fulfillment Service";
                case 370: return "Connovate Technology Private Limited";
                case 371: return "Kocomojo, LLC";
                case 372: return "Everykey LLC";
                case 373: return "Dynamic Controls";
                case 374: return "SentriLock";
                case 375: return "I-SYST inc.";
                case 376: return "CASIO COMPUTER CO., LTD.";
                case 377: return "LAPIS Semiconductor Co., Ltd.";
                case 378: return "Telemonitor, Inc.";
                case 379: return "taskit GmbH";
                case 380: return "Daimler AG";
                case 381: return "BatAndCat";
                case 382: return "BluDotz Ltd";
                case 383: return "XTel ApS";
                case 384: return "Gigaset Communications GmbH";
                case 385: return "Gecko Health Innovations, Inc.";
                case 386: return "HOP Ubiquitous";
                case 387: return "To Be Assigned";
                case 388: return "Nectar";
                case 389: return "bel'apps LLC";
                case 390: return "CORE Lighting Ltd";
                case 391: return "Seraphim Sense Ltd";
                case 392: return "Unico RBC";
                case 393: return "Physical Enterprises Inc.";
                case 394: return "Able Trend Technology Limited";
                case 395: return "Konica Minolta, Inc.";
                case 396: return "Wilo SE";
                case 397: return "Extron Design Services";
                case 398: return "Fitbit, Inc.";
                case 399: return "Fireflies Systems";
                case 400: return "Intelletto Technologies Inc.";
                case 401: return "FDK CORPORATION";
                case 402: return "Cloudleaf, Inc";
                case 403: return "Maveric Automation LLC";
                case 404: return "Acoustic Stream Corporation";
                case 405: return "Zuli";
                case 406: return "Paxton Access Ltd";
                case 407: return "WiSilica Inc";
                case 408: return "Vengit Limited";
                case 409: return "SALTO SYSTEMS S.L.";
                case 410: return "TRON Forum (formerly T-Engine Forum)";
                case 411: return "CUBETECH s.r.o.";
                case 412: return "Cokiya Incorporated";
                case 413: return "CVS Health";
                case 414: return "Ceruus";
                case 415: return "Strainstall Ltd";
                case 416: return "Channel Enterprises (HK) Ltd.";
                case 417: return "FIAMM";
                case 418: return "GIGALANE.CO.,LTD";
                case 419: return "EROAD";
                case 420: return "Mine Safety Appliances";
                case 421: return "Icon Health and Fitness";
                case 422: return "Asandoo GmbH";
                case 423: return "ENERGOUS CORPORATION";
                case 424: return "Taobao";
                case 425: return "Canon Inc.";
                case 426: return "Geophysical Technology Inc.";
                case 427: return "Facebook, Inc.";
                case 428: return "Nipro Diagnostics, Inc.";
                case 429: return "FlightSafety International";
                case 430: return "Earlens Corporation";
                case 431: return "Sunrise Micro Devices, Inc.";
                case 432: return "Star Micronics Co., Ltd.";
                case 433: return "Netizens Sp. z o.o.";
                case 434: return "Nymi Inc.";
                case 435: return "Nytec, Inc.";
                case 436: return "Trineo Sp. z o.o.";
                case 437: return "Nest Labs Inc.";
                case 438: return "LM Technologies Ltd";
                case 439: return "General Electric Company";
                case 440: return "i+D3 S.L.";
                case 441: return "HANA Micron";
                case 442: return "Stages Cycling LLC";
                case 443: return "Cochlear Bone Anchored Solutions AB";
                case 444: return "SenionLab AB";
                case 445: return "Syszone Co., Ltd";
                case 446: return "Pulsate Mobile Ltd.";
                case 447: return "Hong Kong HunterSun Electronic Limited";
                case 448: return "pironex GmbH";
                case 449: return "BRADATECH Corp.";
                case 450: return "Transenergooil AG";
                case 451: return "Bunch";
                case 452: return "DME Microelectronics";
                case 453: return "Bitcraze AB";
                case 454: return "HASWARE Inc.";
                case 455: return "Abiogenix Inc.";
                case 456: return "Poly-Control ApS";
                case 457: return "Avi-on";
                case 458: return "Laerdal Medical AS";
                case 459: return "Fetch My Pet";
                case 460: return "Sam Labs Ltd.";
                case 461: return "Chengdu Synwing Technology Ltd";
                case 462: return "HOUWA SYSTEM DESIGN, k.k.";
                case 463: return "BSH";
                case 464: return "Primus Inter Pares Ltd";
                case 465: return "August";
                case 466: return "Gill Electronics";
                case 467: return "Sky Wave Design";
                case 468: return "Newlab S.r.l.";
                case 469: return "ELAD srl";
                case 470: return "G-wearables inc.";
                case 471: return "Squadrone Systems Inc.";
                case 472: return "Code Corporation";
                case 473: return "Savant Systems LLC";
                case 474: return "Logitech International SA";
                case 475: return "Innblue Consulting";
                case 476: return "iParking Ltd.";
                case 477: return "Koninklijke Philips Electronics N.V.";
                case 478: return "Minelab Electronics Pty Limited";
                case 479: return "Bison Group Ltd.";
                case 480: return "Widex A/S";
                case 481: return "Jolla Ltd";
                case 482: return "Lectronix, Inc.";
                case 483: return "Caterpillar Inc";
                case 484: return "Freedom Innovations";
                case 485: return "Dynamic Devices Ltd";
                case 486: return "Technology Solutions (UK) Ltd";
                case 487: return "IPS Group Inc.";
                case 488: return "STIR";
                case 489: return "Sano, Inc";
                case 490: return "Advanced Application Design, Inc.";
                case 491: return "AutoMap LLC";
                case 492: return "Spreadtrum Communications Shanghai Ltd";
                case 493: return "CuteCircuit LTD";
                case 494: return "Valeo Service";
                case 495: return "Fullpower Technologies, Inc.";
                case 496: return "KloudNation";
                case 497: return "Zebra Technologies Corporation";
                case 498: return "Itron, Inc.";
                case 499: return "The University of Tokyo";
                case 500: return "UTC Fire and Security";
                case 501: return "Cool Webthings Limited";
                case 502: return "DJO Global";
                case 503: return "Gelliner Limited";
                case 504: return "Anyka (Guangzhou) Microelectronics Technology Co, LTD";
                case 505: return "Medtronic, Inc.";
                case 506: return "Gozio, Inc.";
                case 507: return "Form Lifting, LLC";
                case 508: return "Wahoo Fitness, LLC";
                case 509: return "Kontakt Micro-Location Sp. z o.o.";
                case 510: return "Radio System Corporation";
                case 511: return "Freescale Semiconductor, Inc.";
                case 512: return "Verifone Systems PTe Ltd. Taiwan Branch";
                case 513: return "AR Timing";
                case 514: return "Rigado LLC";
                case 515: return "Kemppi Oy";
                case 516: return "Tapcentive Inc.";
                case 517: return "Smartbotics Inc.";
                case 518: return "Otter Products, LLC";
                case 519: return "STEMP Inc.";
                case 520: return "LumiGeek LLC";
                case 521: return "InvisionHeart Inc.";
                case 522: return "Macnica Inc. ";
                case 523: return "Jaguar Land Rover Limited";
                case 524: return "CoroWare Technologies, Inc";
                case 525: return "Simplo Technology Co., LTD";
                case 526: return "Omron Healthcare Co., LTD";
                case 527: return "Comodule GMBH";
                case 528: return "ikeGPS";
                case 529: return "Telink Semiconductor Co. Ltd";
                case 530: return "0x0212	Interplan Co., Ltd";
                case 531: return "Wyler AG";
                case 532: return "IK Multimedia Production srl";
                case 533: return "Lukoton Experience Oy";
                case 534: return "MTI Ltd";
                case 535: return "Tech4home, Lda";
                case 536: return "Hiotech AB";
                case 537: return "DOTT Limited";
                case 538: return "Blue Speck Labs, LLC";
                case 539: return "Cisco Systems Inc";
                case 540: return "Mobicomm Inc";
                case 541: return "Edamic";
                case 542: return "Goodnet Ltd";
                case 543: return "Luster Leaf Products Inc";
                case 544: return "Manus Machina BV";
                case 545: return "Mobiquity Networks Inc";
                case 546: return "Praxis Dynamics";
                case 547: return "Philip Morris Products S.A.";
                case 548: return "Comarch SA";
                case 549: return "Nestlé Nespresso S.A.";
                case 550: return "Merlinia A/S";
                case 551: return "LifeBEAM Technologies";
                case 552: return "Twocanoes Labs, LLC";
                case 553: return "Muoverti Limited";
                case 554: return "Stamer Musikanlagen GMBH";
                case 555: return "Tesla Motors";
                case 556: return "Pharynks Corporation";
                case 557: return "Lupine";
                case 558: return "Siemens AG";
                case 559: return "Huami (Shanghai) Culture Communication CO., LTD";
                case 560: return "Foster Electric Company, Ltd";
                case 561: return "ETA SA";
                case 562: return " x-Senso Solutions Kft";
                case 563: return " Shenzhen SuLong Communication Ltd";
                case 564: return " FengFan (BeiJing) Technology Co, Ltd";
                case 565: return " Qrio Inc";
                case 566: return " Pitpatpet Ltd";
                case 567: return " MSHeli s.r.l.";
                case 568: return "Trakm8 Ltd";
                case 569: return "JIN CO, Ltd";
                case 570: return "Alatech Technology";
                case 571: return "Beijing CarePulse Electronic Technology Co, Ltd";
                case 572: return "Awarepoint";
                case 573: return "ViCentra B.V.";
                case 574: return "Raven Industries";
                case 575: return "WaveWare Technologies";
                case 576: return "Argenox Technologies";
                case 577: return "Bragi GmbH";
                case 578: return "16Lab Inc";
                case 579: return "Masimo Corp";
                case 580: return "Iotera Inc.";
                case 581: return "Endress+Hauser";
                case 582: return "ACKme Networks, Inc.";
                case 583: return "FiftyThree Inc.";
                case 584: return "Parker Hannifin Corp";
                case 585: return "Transcranial Ltd";
                case 586: return "Uwatec AG";
                case 587: return "Orlan LLC";
                case 588: return "Blue Clover Devices";
                case 589: return "M-Way Solutions GmbH";
                case 590: return "Microtronics Engineering GmbH";
                case 591: return "Schneider Schreibgeräte GmbH";
                case 592: return "Sapphire Circuits LLC";
                case 593: return "Lumo Bodytech Inc.";
                case 594: return "UKC Technosolution";
                case 595: return "Xicato Inc.";
                case 596: return "Playbrush";
                case 597: return "Dai Nippon Printing Co., Ltd.";
                case 598: return "G24 Power Limited";
                case 599: return "AdBabble Local Commerce Inc.";
                case 600: return "Devialet SA";
                case 601: return "ALTYOR";
                case 602: return "University of Applied Sciences Valais/Haute Ecole Valaisanne";
                case 603: return "Five Interactive, LLC dba Zendo";
                case 604: return "NetEase (Hangzhou) Network co.Ltd.";
                case 605: return "Lexmark International Inc.";
                case 606: return "Fluke Corporation";
                case 607: return "Yardarm Technologies";
                case 608: return "SensaRx";
                case 609: return "SECVRE GmbH";
                case 610: return "Glacial Ridge Technologies";
                case 611: return "Identiv, Inc.";
                case 612: return "DDS, Inc.";
                case 613: return "SMK Corporation";
                case 614: return "Schawbel Technologies LLC";
                case 615: return "XMI Systems SA";
                case 616: return "Cerevo";
                case 617: return "Torrox GmbH & Co KG";
                case 618: return "Gemalto";
                case 619: return "DEKA Research & Development Corp.";
                case 620: return "Domster Tadeusz Szydlowski";
                case 621: return "Technogym SPA";
                case 622: return "FLEURBAEY BVBA";
                case 623: return "Aptcode Solutions";
                case 624: return "LSI ADL Technology";
                case 625: return "Animas Corp";
                case 626: return "Alps Electric Co., Ltd.";
                case 627: return "OCEASOFT";
                case 628: return "Motsai Research";
                case 629: return "Geotab";
                case 630: return "E.G.O. Elektro-Gerätebau GmbH";
                case 631: return "bewhere inc";
                case 632: return "Johnson Outdoors Inc";
                case 633: return "steute Schaltgerate GmbH & Co. KG";
                case 634: return "Ekomini inc.";
                case 635: return "DEFA AS";
                case 636: return "Aseptika Ltd";
                case 637: return "HUAWEI Technologies Co., Ltd. (  )";
                case 638: return "HabitAware, LLC";
                case 639: return "ruwido austria gmbh";
                case 640: return "ITEC corporation";
                case 641: return "StoneL";
                case 642: return "Sonova AG";
                case 643: return "Maven Machines, Inc.";
                case 644: return "Synapse Electronics";
                case 645: return "Standard Innovation Inc.";
                case 646: return "RF Code, Inc.";
                case 647: return "Wally Ventures S.L.";
                case 648: return "Willowbank Electronics Ltd";
                case 649: return "SK Telecom";
                case 650: return "Jetro AS";
                case 651: return "Code Gears LTD";
                case 652: return "NANOLINK APS";
                case 653: return "IF, LLC";
                case 654: return "RF Digital Corp";
                case 655: return "Church & Dwight Co., Inc";
                case 656: return "Multibit Oy";
                case 657: return "CliniCloud Inc";
                case 658: return "SwiftSensors";
                case 659: return "Blue Bite";
                case 660: return "ELIAS GmbH";
                case 661: return "Sivantos GmbH";
                case 662: return "Petzl";
                case 663: return "storm power ltd";
                case 664: return "EISST Ltd";
                case 665: return "Inexess Technology Simma KG";
                case 666: return "Currant, Inc.";
                case 667: return "C2 Development, Inc.";
                case 668: return "Blue Sky Scientific, LLC";
                case 669: return "ALOTTAZS LABS, LLC";
                case 670: return "Kupson spol. s r.o.";
                case 671: return "Areus Engineering GmbH";
                case 672: return "Impossible Camera GmbH";
                case 673: return "InventureTrack Systems";
                case 674: return "LockedUp";
                case 675: return "Itude";
                case 676: return "Pacific Lock Company";
                case 677: return "Tendyron Corporation (  )";
                case 678: return "Robert Bosch GmbH";
                case 679: return "Illuxtron international B.V.";
                case 680: return "miSport Ltd.";
                case 681: return "Chargelib";
                case 682: return "Doppler Lab";
                case 683: return "BBPOS Limited";
                case 684: return "RTB Elektronik GmbH & Co. KG";
                case 685: return "Rx Networks, Inc.";
                case 686: return "WeatherFlow, Inc.";
                case 687: return "Technicolor USA Inc.";
                case 688: return "Bestechnic(Shanghai),Ltd";
                case 689: return "Raden Inc";
                case 690: return "JouZen Oy";
                case 691: return "CLABER S.P.A.";
                case 692: return "Hyginex, Inc.";
                case 693: return "HANSHIN ELECTRIC RAILWAY CO.,LTD.";
                #endregion
                case 65535: return "This value has special meaning depending on the context in which it used.";
            }
            return $"CompanyId={companyId}";
        }

        // From https://www.bluetooth.org/en-us/specification/assigned-numbers/generic-access-profile
        public static string BluetoothDataType(ushort dataType)
        {
            switch (dataType)
            {
                case 0x01: return "Flags";
                #region LongListOfData
                case 0x02: return "Incomplete List of 16-bit Service Class UUIDs";
                case 0x03: return "Complete List of 16-bit Service Class UUIDs";
                case 0x04: return "Incomplete List of 32-bit Service Class UUIDs";
                case 0x05: return "Complete List of 32-bit Service Class UUIDs";
                case 0x06: return "Incomplete List of 128-bit Service Class UUIDs";
                case 0x07: return "Complete List of 128-bit Service Class UUIDs";
                case 0x08: return "Shortened Local Name";
                case 0x09: return "Complete Local Name";
                case 0x0A: return "Tx Power Level";
                case 0x0B: return "Transport Discovery Data";
                case 0x0D: return "Class of Device";
                case 0x0E: return "Simple Pairing Hash C";
                //case 0x0E: return "Simple Pairing Hash C-192";
                case 0x0F: return "Simple Pairing Randomizer R";
                case 0x10: return "Device ID";
                //case 0x10: return "Security Manager TK Value";
                case 0x11: return "Security Manager Out of Band Flags";
                case 0x12: return "Slave Connection Interval Range";
                case 0x14: return "List of 16-bit Service Solicitation UUIDs";
                case 0x1F: return "List of 32-bit Service Solicitation UUIDs";
                case 0x15: return "List of 128-bit Service Solicitation UUIDs";
                //case 0x16: return "Service Data";
                case 0x16: return "Service Data - 16-bit UUID";
                case 0x20: return "Service Data - 32-bit UUID";
                case 0x21: return "Service Data - 128-bit UUID";
                case 0x22: return "​LE Secure Connections Confirmation Value";
                case 0x23: return "​​LE Secure Connections Random Value";
                case 0x24: return "​​​URI";
                case 0x17: return "Public Target Address";
                case 0x18: return "Random Target Address";
                case 0x19: return "Appearance";
                case 0x1A: return "«Advertising Interval";
                case 0x1B: return "​LE Bluetooth Device Address";
                case 0x1C: return "​LE Role";
                case 0x1D: return "​Simple Pairing Hash C-256";
                case 0x1E: return "​Simple Pairing Randomizer R-256";
                case 0x3D: return "3D Information Data";
                case 0xFF: return "Manufacturer Specific Data";
                    #endregion
            }
            return $"value={dataType}";
        }

        public static string BluetoothAdvertismentFlag(byte value)
        {
            var Retval = "";
            if ((value & 0x01) != 0) Retval += "Limited Discoverable Mode; ";
            if ((value & 0x02) != 0) Retval += "General Discoverable Mode; ";
            if ((value & 0x04) != 0) Retval += "BR/EDR Not Supported; ";
            if ((value & 0x08) != 0) Retval += "Simultaneous LE and BR/EDR to Same Device Capable (Controller); ";
            if ((value & 0x10) != 0) Retval += "Simultaneous LE and BR/EDR to Same Device Capable (Host); ";
            if (Retval != "") Retval = Retval.Substring(0, Retval.Length - 2);
            return Retval;

            /*
             0 0 LE Limited Discoverable Mode 
            0 1 LE General Discoverable Mode
            0 2 BR/EDR Not Supported. Bit 37 of LMP Feature Mask Definitions (Page 0)
            0 3 Simultaneous LE and BR/EDR to Same Device Capable (Controller). Bit 49 of LMP Feature Mask Definitions (Page 0)
            0 4 Simultaneous LE and BR/EDR to Same Device Capable (Host). Bit 66 of LMP Feature Mask Definitions (Page 1)

            */
        }
        public static string GattCharacteristicPropertiesToString(GattCharacteristicProperties value)
        {
            string Retval = "";
            if ((value & GattCharacteristicProperties.Read) == GattCharacteristicProperties.Read) Retval += "Read ";
            if ((value & GattCharacteristicProperties.Write) == GattCharacteristicProperties.Write) Retval += "Write ";
            if ((value & GattCharacteristicProperties.Notify) == GattCharacteristicProperties.Notify) Retval += "Notify ";
            if ((value & GattCharacteristicProperties.Indicate) == GattCharacteristicProperties.Indicate) Retval += "Indicate ";
            if ((value & GattCharacteristicProperties.Broadcast) == GattCharacteristicProperties.Broadcast) Retval += "Broadcast ";
            if ((value & GattCharacteristicProperties.ExtendedProperties) == GattCharacteristicProperties.ExtendedProperties) Retval += "ExtendedProperties ";
            if ((value & GattCharacteristicProperties.WriteWithoutResponse) == GattCharacteristicProperties.WriteWithoutResponse) Retval += "WriteWithoutResponse ";
            if ((value & GattCharacteristicProperties.ReliableWrites) == GattCharacteristicProperties.ReliableWrites) Retval += "ReliableWrites ";
            if ((value & GattCharacteristicProperties.AuthenticatedSignedWrites) == GattCharacteristicProperties.AuthenticatedSignedWrites) Retval += "AuthenticatedSignedWrites ";
            if ((value & GattCharacteristicProperties.WritableAuxiliaries) == GattCharacteristicProperties.WritableAuxiliaries) Retval += "WritableAuxiliaries ";
            if (Retval == "") Retval = "None";
            Retval = Retval.TrimEnd();
            return Retval;
        }



        public static string GattReadResultToHexString(GattReadResult value)
        {
            return BufferToHexString(value.Value);
        }

        public static string GattReadResultToUnicodeString(GattReadResult value)
        {
            return BufferToUnicodeString(value.Value);
        }

        public static string GattReadResultToUTF8String(GattReadResult value)
        {
            if (value.Value == null) return "";
            if (value.Value.Length == 0) return "";
            return BufferToUTF8String(value.Value);
        }
        public static byte GattReadResultToByte(GattReadResult value)
        {
            byte b0 = value.Value.GetByte(0);
            return b0;
        }

        public static ushort GattReadResultToUShort(GattReadResult value)
        {
            ushort s = BufferToUShort(value.Value);
            return s;
        }

        public static ushort BufferToUShort(IBuffer value, uint startIndex = 0)
        {
            byte b0 = value.GetByte(startIndex + 0);
            byte b1 = value.GetByte(startIndex + 1);
            ushort s = (ushort)(b0 | (b1 << 8));
            return s;
        }

        public static string BufferTo16BitServiceUUIDs(IBuffer buffer)
        {
            string Retval = "";
            for (uint idx = 0; idx < buffer.Length; idx += 2)
            {
                try
                {
                    var uuid = BufferToUShort(buffer, idx);
                    Retval += $"{uuid:X4}, ";
                }
                catch (Exception)
                {
                    Retval += $"??, ";
                }
            }
            if (Retval.Length == 0)
            {
                Retval = "[]";
            }
            else
            {
                Retval = Retval.Substring(0, Retval.Length - 2); // remove the ", " at the end.
                Retval = "[" + Retval + "]";
            }
            return Retval;
        }

        public static string BufferToHexString(IBuffer buffer)
        {
            var sb = new StringBuilder();
            bool IsAscii = true;
            string Retval = "[";

            for (uint i = 0; i < buffer.Length; i++)
            {
                //var index = buffer.Length - i - 1; // top to bottom
                var index = i; // go from first to last, not last to first.
                var b = buffer.GetByte(index);
                if (i > 0) Retval += " ";
                Retval += string.Format("{0:X2}", b);
                bool byteIsAscii = (b >= 32 && b < 127);
                char ch = byteIsAscii ? (char)b : '.';
                sb.Append(ch);
                IsAscii = IsAscii && byteIsAscii;

            }
            Retval += "]";
            if (buffer.Length > 4) // && IsAscii) // assume it's a string!
                Retval += $" = '{sb}'";
            return Retval;
        }

        public static string BufferToUnicodeString(IBuffer buffer)
        {
            var array = buffer.ToArray();
            string Retval = "";
            try
            {
                Retval = System.Text.Encoding.Unicode.GetString(array, 0, array.Length);
            }
            catch (Exception)
            {
                Retval = BufferToHexString(buffer);
            }

            return Retval;
        }

        public static string BufferToUTF8String(IBuffer buffer)
        {
            var array = buffer.ToArray();
            string Retval = "";
            try
            {
                Retval = System.Text.Encoding.UTF8.GetString(array, 0, array.Length);
            }
            catch (Exception)
            {
                Retval = BufferToHexString(buffer);
            }

            return Retval;
        }

        // General routine: splits the array into a list of sub-arrays, each of which is no more than 'maxlen' long.
        public static List<T[]> Split<T>(T[] array, int maxlen)
        {
            var Retval = new List<T[]>();
            // NOTE: could do an optimization where if the maxlen >= array.count, just return the array.
            for (int startIndex = 0; startIndex < array.Length; startIndex += maxlen)
            {
                var count = Math.Min(maxlen, array.Length - startIndex);
                var newarray = new T[count];
                Array.Copy(array, startIndex, newarray, 0, count);
                Retval.Add(newarray);
            }
            return Retval;
        }

        // Specific routine: converts a string into a list of
        // byte buffers that can be sent to the Display (Watch)
        public static List<byte[]> TextToByteArray(string value, int maxlen = 16)
        {
            var text = value.Replace("\\n", "\n"); // NOTE: any other escape sequences?
            var buffer = System.Text.UTF8Encoding.UTF8.GetBytes(text);
            var Retval = Utilities.BluetoothUtilities.Split(buffer, maxlen);
            return Retval;
        }


    }
}
