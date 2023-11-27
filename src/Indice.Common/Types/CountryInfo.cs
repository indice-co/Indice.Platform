using System.Globalization;
using System.Linq;

namespace Indice.Globalization;

/// <summary>Contains information about the countries. Based on 2018-2019.</summary>
public class CountryInfo
{
    private static readonly Dictionary<string, string> _continentNamesMap = new Dictionary<string, string> {
        ["AS"] = "Asia",
        ["AF"] = "Africa",
        ["NA"] = "North America",
        ["SA"] = "South America",
        ["AN"] = "Antartica",
        ["EU"] = "Europe",
        ["OC"] = "Australia",
    };

    private static readonly Dictionary<string, string> _callingCodeMap;
    private static readonly Dictionary<string, CountryInfo> _countries;

    /// <summary>Collection of countries</summary>
    public static ICollection<CountryInfo> Countries => _countries.Values;

    internal CountryInfo(string continent, string twoLetterCode, string threeLetterCode, int numericCode, string fullName, string capital, string twoLetterLanguageCode, string callingCode) {
        Name = fullName;
        Capital = capital;
        ContinentCode = continent;
        TwoLetterCode = twoLetterCode;
        ThreeLetterCode = threeLetterCode;
        NumericCode = numericCode;
        TwoLetterLanguageCode = string.IsNullOrWhiteSpace(twoLetterLanguageCode) ? null : twoLetterLanguageCode;
        CallingCode = string.IsNullOrWhiteSpace(callingCode) ? null : callingCode;
    }

    static CountryInfo() {
        _countries = new (StringComparer.OrdinalIgnoreCase)
        {
            ["AF"] = new CountryInfo("AS", "AF", "AFG", 4, "Afghanistan", "Kabul", "prs,ps,uz", "93") ,
            ["AX"] = new CountryInfo("EU", "AX", "ALA", 248, "Åland", "Mariehamn", "sv", "358-18") ,
            ["AL"] = new CountryInfo("EU", "AL", "ALB", 8, "Albania", "Tirana", "sq", "355") ,
            ["DZ"] = new CountryInfo("AF", "DZ", "DZA", 12, "Algeria", "Algiers", "ar,fr,kab,tzm", "213") ,
            ["AS"] = new CountryInfo("OC", "AS", "ASM", 16, "American Samoa", "Pago Pago", "en", "1-684") ,
            ["AD"] = new CountryInfo("EU", "AD", "AND", 20, "Andorra", "Andorra la Vella", "ca", "376") ,
            ["AO"] = new CountryInfo("AF", "AO", "AGO", 24, "Angola", "Luanda", "ln,pt", "244") ,
            ["AI"] = new CountryInfo("NA", "AI", "AIA", 660, "Anguilla", "The Valley", "en", "1-264") ,
            ["AQ"] = new CountryInfo("AN", "AQ", "ATA", 10, "Antarctica", "", "en", "672") ,
            ["AG"] = new CountryInfo("NA", "AG", "ATG", 28, "Antigua and Barbuda", "St. John's", "en", "1-268") ,
            ["AR"] = new CountryInfo("SA", "AR", "ARG", 32, "Argentina", "Buenos Aires", "es", "54") ,
            ["AM"] = new CountryInfo("AS", "AM", "ARM", 51, "Armenia", "Yerevan", "hy", "374") ,
            ["AW"] = new CountryInfo("NA", "AW", "ABW", 533, "Aruba", "Oranjestad", "nl", "297") ,
            ["AU"] = new CountryInfo("OC", "AU", "AUS", 36, "Australia", "Canberra", "en", "61") ,
            ["AT"] = new CountryInfo("EU", "AT", "AUT", 40, "Austria", "Vienna", "de,en", "43") ,
            ["AZ"] = new CountryInfo("AS", "AZ", "AZE", 31, "Azerbaijan", "Baku", "az", "994") ,
            ["BS"] = new CountryInfo("NA", "BS", "BHS", 44, "Bahamas", "Nassau", "en", "1-242") ,
            ["BH"] = new CountryInfo("AS", "BH", "BHR", 48, "Bahrain", "Manama", "ar", "973") ,
            ["BD"] = new CountryInfo("AS", "BD", "BGD", 50, "Bangladesh", "Dhaka", "bn", "880") ,
            ["BB"] = new CountryInfo("NA", "BB", "BRB", 52, "Barbados", "Bridgetown", "en", "1-246") ,
            ["BY"] = new CountryInfo("EU", "BY", "BLR", 112, "Belarus", "Minsk", "be,ru", "375") ,
            ["BE"] = new CountryInfo("EU", "BE", "BEL", 56, "Belgium", "Brussels", "fr,de,en,nl", "32") ,
            ["BZ"] = new CountryInfo("NA", "BZ", "BLZ", 84, "Belize", "Belmopan", "en", "501") ,
            ["BJ"] = new CountryInfo("AF", "BJ", "BEN", 204, "Benin", "Porto-Novo", "fr,yo", "229") ,
            ["BM"] = new CountryInfo("NA", "BM", "BMU", 60, "Bermuda", "Hamilton", "en", "1-441") ,
            ["BT"] = new CountryInfo("AS", "BT", "BTN", 64, "Bhutan", "Thimphu", "dz", "975") ,
            ["BO"] = new CountryInfo("SA", "BO", "BOL", 68, "Bolivia", "Sucre", "es,quz", "591") ,
            ["BQ"] = new CountryInfo("NA", "BQ", "BES", 535, "Bonaire, Sint Eustatius and Saba", "", "nl", "599-7") ,
            ["BA"] = new CountryInfo("EU", "BA", "BIH", 70, "Bosnia and Herzegovina", "Sarajevo", "bs,hr,sr", "387") ,
            ["BW"] = new CountryInfo("AF", "BW", "BWA", 72, "Botswana", "Gaborone", "en,tn", "267") ,
            ["BV"] = new CountryInfo("AN", "BV", "BVT", 74, "Bouvet Island", "", "no", "47") ,
            ["BR"] = new CountryInfo("SA", "BR", "BRA", 76, "Brazil", "Brasilia", "pt,es", "55") ,
            ["IO"] = new CountryInfo("AS", "IO", "IOT", 86, "British Indian Ocean Territory", "Diego Garcia", "en", "246") ,
            ["VG"] = new CountryInfo("NA", "VG", "VGB", 92, "British Virgin Islands", "Road Town", "en", "1-284") ,
            ["BN"] = new CountryInfo("AS", "BN", "BRN", 96, "Brunei Darussalam", "Bandar Seri Begawan", "ms", "673") ,
            ["BG"] = new CountryInfo("EU", "BG", "BGR", 100, "Bulgaria", "Sofia", "bg", "359") ,
            ["BF"] = new CountryInfo("AF", "BF", "BFA", 854, "Burkina Faso", "Ouagadougou", "fr", "226") ,
            ["BI"] = new CountryInfo("AF", "BI", "BDI", 108, "Burundi", "Bujumbura", "rn,en,fr", "257") ,
            ["KH"] = new CountryInfo("AS", "KH", "KHM", 116, "Cambodia", "Phnom Penh", "km", "855") ,
            ["CM"] = new CountryInfo("AF", "CM", "CMR", 120, "Cameroon", "Yaounde", "agq,bas,dua,en,ewo,ff,fr,jgo,kkj,ksf,mgo,mua,nmg,nnh,yav", "237") ,
            ["CA"] = new CountryInfo("NA", "CA", "CAN", 124, "Canada", "Ottawa", "en,fr,iu,iu,moh", "1") ,
            ["CV"] = new CountryInfo("AF", "CV", "CPV", 132, "Cabo Verde", "Praia", "kea,pt", "238") ,
            ["KY"] = new CountryInfo("NA", "KY", "CYM", 136, "Cayman Islands", "George Town", "en", "1-345") ,
            ["CF"] = new CountryInfo("AF", "CF", "CAF", 140, "Central African Republic", "Bangui", "fr,ln,sg", "236") ,
            ["TD"] = new CountryInfo("AF", "TD", "TCD", 148, "Chad", "N'Djamena", "ar,fr", "235") ,
            ["CL"] = new CountryInfo("SA", "CL", "CHL", 152, "Chile", "Santiago", "arn,es", "56") ,
            ["CN"] = new CountryInfo("AS", "CN", "CHN", 156, "China", "Beijing", "zh,mn,bo,ii,ug", "86") ,
            ["CX"] = new CountryInfo("OC", "CX", "CXR", 162, "Christmas Island", "Flying Fish Cove", "en", "61") ,
            ["CC"] = new CountryInfo("AS", "CC", "CCK", 166, "Cocos [Keeling] Islands", "West Island", "en", "61") ,
            ["CO"] = new CountryInfo("SA", "CO", "COL", 170, "Colombia", "Bogota", "es", "57") ,
            ["KM"] = new CountryInfo("AF", "KM", "COM", 174, "Comoros", "Moroni", "ar,fr", "269") ,
            ["CK"] = new CountryInfo("OC", "CK", "COK", 184, "Cook Islands", "Avarua", "en", "682") ,
            ["CR"] = new CountryInfo("NA", "CR", "CRI", 188, "Costa Rica", "San Jose", "es", "506") ,
            ["HR"] = new CountryInfo("EU", "HR", "HRV", 191, "Croatia", "Zagreb", "hr", "385") ,
            ["CU"] = new CountryInfo("NA", "CU", "CUB", 192, "Cuba", "Havana", "es", "53") ,
            ["CW"] = new CountryInfo("NA", "CW", "CUW", 531, "Curacao", "Willemstad", "nl", "599") ,
            ["CY"] = new CountryInfo("EU", "CY", "CYP", 196, "Cyprus", "Nicosia", "el,en,tr", "357") ,
            ["CZ"] = new CountryInfo("EU", "CZ", "CZE", 203, "Czechia", "Prague", "cs", "420") ,
            ["CD"] = new CountryInfo("AF", "CD", "COD", 180, "Democratic Republic of the Congo", "Kinshasa", "fr,ln,lu,sw", "243") ,
            ["DK"] = new CountryInfo("EU", "DK", "DNK", 208, "Denmark", "Copenhagen", "da,en,fo", "45") ,
            ["DJ"] = new CountryInfo("AF", "DJ", "DJI", 262, "Djibouti", "Djibouti", "aa,ar,fr,so", "253") ,
            ["DM"] = new CountryInfo("NA", "DM", "DMA", 212, "Dominica", "Roseau", "en", "1-767") ,
            ["DO"] = new CountryInfo("NA", "DO", "DOM", 214, "Dominican Republic", "Santo Domingo", "es", "1-809, 1-829, 1-849") ,
            ["TL"] = new CountryInfo("OC", "TL", "TLS", 626, "Timor-Leste", "Dili", "pt", "670") ,
            ["EC"] = new CountryInfo("SA", "EC", "ECU", 218, "Ecuador", "Quito", "es,quz", "593") ,
            ["EG"] = new CountryInfo("AF", "EG", "EGY", 818, "Egypt", "Cairo", "ar", "20") ,
            ["SV"] = new CountryInfo("NA", "SV", "SLV", 222, "El Salvador", "San Salvador", "es", "503") ,
            ["GQ"] = new CountryInfo("AF", "GQ", "GNQ", 226, "Equatorial Guinea", "Malabo", "es,fr,pt", "240") ,
            ["ER"] = new CountryInfo("AF", "ER", "ERI", 232, "Eritrea", "Asmara", "aa,ar,byn,en,ssy,ti,tig", "291") ,
            ["EE"] = new CountryInfo("EU", "EE", "EST", 233, "Estonia", "Tallinn", "et", "372") ,
            ["ET"] = new CountryInfo("AF", "ET", "ETH", 231, "Ethiopia", "Addis Ababa", "aa,am,om,so,ti,wal", "251") ,
            ["FK"] = new CountryInfo("SA", "FK", "FLK", 238, "Falkland Islands", "Stanley", "en", "500") ,
            ["FO"] = new CountryInfo("EU", "FO", "FRO", 234, "Faroe Islands", "Torshavn", "fo", "298") ,
            ["FJ"] = new CountryInfo("OC", "FJ", "FJI", 242, "Fiji", "Suva", "en", "679") ,
            ["FI"] = new CountryInfo("EU", "FI", "FIN", 246, "Finland", "Helsinki", "en,fi,se,smn,sms,sv", "358") ,
            ["FR"] = new CountryInfo("EU", "FR", "FRA", 250, "France", "Paris", "fr,br,ca,co,gsw,ia,oc", "33") ,
            ["GF"] = new CountryInfo("SA", "GF", "GUF", 254, "French Guiana", "Cayenne", "fr", "594") ,
            ["PF"] = new CountryInfo("OC", "PF", "PYF", 258, "French Polynesia", "Papeete", "fr", "689") ,
            ["TF"] = new CountryInfo("AN", "TF", "ATF", 260, "French Southern Territories", "Port-aux-Francais", "fr", "33") ,
            ["GA"] = new CountryInfo("AF", "GA", "GAB", 266, "Gabon", "Libreville", "fr", "241") ,
            ["GM"] = new CountryInfo("AF", "GM", "GMB", 270, "Gambia", "Banjul", "en", "220") ,
            ["GE"] = new CountryInfo("AS", "GE", "GEO", 268, "Georgia", "Tbilisi", "ka,os", "995") ,
            ["DE"] = new CountryInfo("EU", "DE", "DEU", 276, "Germany", "Berlin", "de,dsb,en,hsb,ksh,nds", "49") ,
            ["GH"] = new CountryInfo("AF", "GH", "GHA", 288, "Ghana", "Accra", "ak,ee,en,ha", "233") ,
            ["GI"] = new CountryInfo("EU", "GI", "GIB", 292, "Gibraltar", "Gibraltar", "en", "350") ,
            ["GR"] = new CountryInfo("EU", "GR", "GRC", 300, "Greece", "Athens", "el", "30") ,
            ["GL"] = new CountryInfo("NA", "GL", "GRL", 304, "Greenland", "Nuuk", "da,kl", "299") ,
            ["GD"] = new CountryInfo("NA", "GD", "GRD", 308, "Grenada", "St. George's", "en", "1-473") ,
            ["GP"] = new CountryInfo("NA", "GP", "GLP", 312, "Guadeloupe", "Basse-Terre", "fr", "590") ,
            ["GU"] = new CountryInfo("OC", "GU", "GUM", 316, "Guam", "Hagatna", "en", "1-671") ,
            ["GT"] = new CountryInfo("NA", "GT", "GTM", 320, "Guatemala", "Guatemala City", "es,quc", "502") ,
            ["GG"] = new CountryInfo("EU", "GG", "GGY", 831, "Guernsey", "St Peter Port", "en", "44-1481") ,
            ["GN"] = new CountryInfo("AF", "GN", "GIN", 324, "Guinea", "Conakry", "fr,ff,nqo", "224") ,
            ["GW"] = new CountryInfo("AF", "GW", "GNB", 624, "Guinea-Bissau", "Bissau", "pt", "245") ,
            ["GY"] = new CountryInfo("SA", "GY", "GUY", 328, "Guyana", "Georgetown", "en", "592") ,
            ["HT"] = new CountryInfo("NA", "HT", "HTI", 332, "Haiti", "Port-au-Prince", "fr,zh", "509") ,
            ["HM"] = new CountryInfo("AN", "HM", "HMD", 334, "Heard Island and McDonald Islands", "", "en", "61") ,
            ["HN"] = new CountryInfo("NA", "HN", "HND", 340, "Honduras", "Tegucigalpa", "es", "504") ,
            ["HK"] = new CountryInfo("AS", "HK", "HKG", 344, "Hong Kong", "Hong Kong", "en,zh,zh", "852") ,
            ["HU"] = new CountryInfo("EU", "HU", "HUN", 348, "Hungary", "Budapest", "hu", "36") ,
            ["IS"] = new CountryInfo("EU", "IS", "ISL", 352, "Iceland", "Reykjavik", "is", "354") ,
            ["IN"] = new CountryInfo("AS", "IN", "IND", 356, "India", "New Delhi", "as,bn,bo,brx,en,gu,hi,kn,kok,ks,ks,ml,mni,mr,ne,or,pa,sa,sd,ta,te,ur", "91") ,
            ["ID"] = new CountryInfo("AS", "ID", "IDN", 360, "Indonesia", "Jakarta", "en,id,jv,jv", "62") ,
            ["IR"] = new CountryInfo("AS", "IR", "IRN", 364, "Iran", "Tehran", "fa,ku,lrc,mzn", "98") ,
            ["IQ"] = new CountryInfo("AS", "IQ", "IRQ", 368, "Iraq", "Baghdad", "ar,ku,lrc", "964") ,
            ["IE"] = new CountryInfo("EU", "IE", "IRL", 372, "Ireland", "Dublin", "en,ga", "353") ,
            ["IM"] = new CountryInfo("EU", "IM", "IMN", 833, "Isle of Man", "Douglas", "en,gv", "44-1624") ,
            ["IL"] = new CountryInfo("AS", "IL", "ISR", 376, "Israel", "Jerusalem", "he,en,ar,en", "972") ,
            ["IT"] = new CountryInfo("EU", "IT", "ITA", 380, "Italy", "Rome", "it,ca,de,fur", "39") ,
            ["CI"] = new CountryInfo("AF", "CI", "CIV", 384, "Côte d'Ivoire", "Yamoussoukro", "fr", "225") ,
            ["JM"] = new CountryInfo("NA", "JM", "JAM", 388, "Jamaica", "Kingston", "en", "1-876") ,
            ["JP"] = new CountryInfo("AS", "JP", "JPN", 392, "Japan", "Tokyo", "ja", "81") ,
            ["JE"] = new CountryInfo("EU", "JE", "JEY", 832, "Jersey", "Saint Helier", "en", "44-1534") ,
            ["JO"] = new CountryInfo("AS", "JO", "JOR", 400, "Jordan", "Amman", "ar", "962") ,
            ["KZ"] = new CountryInfo("AS", "KZ", "KAZ", 398, "Kazakhstan", "Astana", "kk,ru", "7") ,
            ["KE"] = new CountryInfo("AF", "KE", "KEN", 404, "Kenya", "Nairobi", "dav,ebu,en,guz,kam,ki,kln,luo,luy,mas,mer,om,saq,so,sw,teo", "254") ,
            ["KI"] = new CountryInfo("OC", "KI", "KIR", 296, "Kiribati", "Tarawa", "en", "686") ,
            ["XK"] = new CountryInfo("EU", "XK", "XKX", 0, "Kosovo", "Pristina", "sq,sr", "383") ,
            ["KW"] = new CountryInfo("AS", "KW", "KWT", 414, "Kuwait", "Kuwait City", "ar", "965") ,
            ["KG"] = new CountryInfo("AS", "KG", "KGZ", 417, "Kyrgyzstan", "Bishkek", "ky,ru", "996") ,
            ["LA"] = new CountryInfo("AS", "LA", "LAO", 418, "Lao People's Democratic Republic", "Vientiane", "lo", "856") ,
            ["LV"] = new CountryInfo("EU", "LV", "LVA", 428, "Latvia", "Riga", "lv", "371") ,
            ["LB"] = new CountryInfo("AS", "LB", "LBN", 422, "Lebanon", "Beirut", "ar", "961") ,
            ["LS"] = new CountryInfo("AF", "LS", "LSO", 426, "Lesotho", "Maseru", "en,st", "266") ,
            ["LR"] = new CountryInfo("AF", "LR", "LBR", 430, "Liberia", "Monrovia", "en,vai,vai", "231") ,
            ["LY"] = new CountryInfo("AF", "LY", "LBY", 434, "Libya", "Tripoli", "ar", "218") ,
            ["LI"] = new CountryInfo("EU", "LI", "LIE", 438, "Liechtenstein", "Vaduz", "de,gsw", "423") ,
            ["LT"] = new CountryInfo("EU", "LT", "LTU", 440, "Lithuania", "Vilnius", "lt", "370") ,
            ["LU"] = new CountryInfo("EU", "LU", "LUX", 442, "Luxembourg", "Luxembourg", "de,fr,lb,pt", "352") ,
            ["MO"] = new CountryInfo("AS", "MO", "MAC", 446, "Macao", "Macao", "en,pt,zh,zh", "853") ,
            ["MK"] = new CountryInfo("EU", "MK", "MKD", 807, "Republic of North Macedonia", "Skopje", "mk,sq", "389") ,
            ["MG"] = new CountryInfo("AF", "MG", "MDG", 450, "Madagascar", "Antananarivo", "en,fr,mg", "261") ,
            ["MW"] = new CountryInfo("AF", "MW", "MWI", 454, "Malawi", "Lilongwe", "en", "265") ,
            ["MY"] = new CountryInfo("AS", "MY", "MYS", 458, "Malaysia", "Kuala Lumpur", "en,ms,ta", "60") ,
            ["MV"] = new CountryInfo("AS", "MV", "MDV", 462, "Maldives", "Male", "dv", "960") ,
            ["ML"] = new CountryInfo("AF", "ML", "MLI", 466, "Mali", "Bamako", "bm,fr,khq,ses", "223") ,
            ["MT"] = new CountryInfo("EU", "MT", "MLT", 470, "Malta", "Valletta", "en,mt", "356") ,
            ["MH"] = new CountryInfo("OC", "MH", "MHL", 584, "Marshall Islands", "Majuro", "en", "692") ,
            ["MQ"] = new CountryInfo("NA", "MQ", "MTQ", 474, "Martinique", "Fort-de-France", "fr", "596") ,
            ["MR"] = new CountryInfo("AF", "MR", "MRT", 478, "Mauritania", "Nouakchott", "ar,ff,fr", "222") ,
            ["MU"] = new CountryInfo("AF", "MU", "MUS", 480, "Mauritius", "Port Louis", "en,fr,mfe", "230") ,
            ["YT"] = new CountryInfo("AF", "YT", "MYT", 175, "Mayotte", "Mamoudzou", "fr", "262") ,
            ["MX"] = new CountryInfo("NA", "MX", "MEX", 484, "Mexico", "Mexico City", "es", "52") ,
            ["FM"] = new CountryInfo("OC", "FM", "FSM", 583, "Micronesia", "Palikir", "en", "691") ,
            ["MD"] = new CountryInfo("EU", "MD", "MDA", 498, "Moldova", "Chisinau", "ro,ru", "373") ,
            ["MC"] = new CountryInfo("EU", "MC", "MCO", 492, "Monaco", "Monaco", "fr", "377") ,
            ["MN"] = new CountryInfo("AS", "MN", "MNG", 496, "Mongolia", "Ulan Bator", "mn,mn", "976") ,
            ["ME"] = new CountryInfo("EU", "ME", "MNE", 499, "Montenegro", "Podgorica", "cu,hu,sq,sr", "382") ,
            ["MS"] = new CountryInfo("NA", "MS", "MSR", 500, "Montserrat", "Plymouth", "en", "1-664") ,
            ["MA"] = new CountryInfo("AF", "MA", "MAR", 504, "Morocco", "Rabat", "ar,fr,shi,shi,tzm,tzm,tzm,zgh", "212") ,
            ["MZ"] = new CountryInfo("AF", "MZ", "MOZ", 508, "Mozambique", "Maputo", "mgh,pt,seh", "258") ,
            ["MM"] = new CountryInfo("AS", "MM", "MMR", 104, "Myanmar", "Nay Pyi Taw", "my", "95") ,
            ["NA"] = new CountryInfo("AF", "NA", "NAM", 516, "Namibia", "Windhoek", "af,en,naq", "264") ,
            ["NR"] = new CountryInfo("OC", "NR", "NRU", 520, "Nauru", "Yaren", "en", "674") ,
            ["NP"] = new CountryInfo("AS", "NP", "NPL", 524, "Nepal", "Kathmandu", "ne", "977") ,
            ["NL"] = new CountryInfo("EU", "NL", "NLD", 528, "Netherlands", "Amsterdam", "en,fy,nds,nl", "31") ,
            ["AN"] = new CountryInfo("NA", "AN", "ANT", 530, "Netherlands Antilles", "Willemstad", "nl,en,es", "599") ,
            ["NC"] = new CountryInfo("OC", "NC", "NCL", 540, "New Caledonia", "Noumea", "fr", "687") ,
            ["NZ"] = new CountryInfo("OC", "NZ", "NZL", 554, "New Zealand", "Wellington", "en,mi", "64") ,
            ["NI"] = new CountryInfo("NA", "NI", "NIC", 558, "Nicaragua", "Managua", "es", "505") ,
            ["NE"] = new CountryInfo("AF", "NE", "NER", 562, "Niger", "Niamey", "dje,fr,ha,twq", "227") ,
            ["NG"] = new CountryInfo("AF", "NG", "NGA", 566, "Nigeria", "Abuja", "bin,en,ff,ha,ibb,ig,kr,yo", "234") ,
            ["NU"] = new CountryInfo("OC", "NU", "NIU", 570, "Niue", "Alofi", "en", "683") ,
            ["NF"] = new CountryInfo("OC", "NF", "NFK", 574, "Norfolk Island", "Kingston", "en", "672-3") ,
            ["KP"] = new CountryInfo("AS", "KP", "PRK", 408, "Democratic People's Republic of Korea", "Pyongyang", "ko", "850") ,
            ["MP"] = new CountryInfo("OC", "MP", "MNP", 580, "Northern Mariana Islands", "Saipan", "en", "1-670") ,
            ["NO"] = new CountryInfo("EU", "NO", "NOR", 578, "Norway", "Oslo", "nb,nn,se,sma,smj", "47") ,
            ["OM"] = new CountryInfo("AS", "OM", "OMN", 512, "Oman", "Muscat", "ar", "968") ,
            ["PK"] = new CountryInfo("AS", "PK", "PAK", 586, "Pakistan", "Islamabad", "en,pa,sd,ur", "92") ,
            ["PW"] = new CountryInfo("OC", "PW", "PLW", 585, "Palau", "Melekeok", "en", "680") ,
            ["PS"] = new CountryInfo("AS", "PS", "PSE", 275, "Palestine", "East Jerusalem", "ar", "970") ,
            ["PA"] = new CountryInfo("NA", "PA", "PAN", 591, "Panama", "Panama City", "es", "507") ,
            ["PG"] = new CountryInfo("OC", "PG", "PNG", 598, "Papua New Guinea", "Port Moresby", "en", "675") ,
            ["PY"] = new CountryInfo("SA", "PY", "PRY", 600, "Paraguay", "Asuncion", "es,gn", "595") ,
            ["PE"] = new CountryInfo("SA", "PE", "PER", 604, "Peru", "Lima", "es,quz", "51") ,
            ["PH"] = new CountryInfo("AS", "PH", "PHL", 608, "Philippines", "Manila", "en,es,fil", "63") ,
            ["PN"] = new CountryInfo("OC", "PN", "PCN", 612, "Pitcairn", "Adamstown", "en", "64") ,
            ["PL"] = new CountryInfo("EU", "PL", "POL", 616, "Poland", "Warsaw", "pl", "48") ,
            ["PT"] = new CountryInfo("EU", "PT", "PRT", 620, "Portugal", "Lisbon", "pt", "351") ,
            ["PR"] = new CountryInfo("NA", "PR", "PRI", 630, "Puerto Rico", "San Juan", "en,es", "1-787, 1-939") ,
            ["QA"] = new CountryInfo("AS", "QA", "QAT", 634, "Qatar", "Doha", "ar", "974") ,
            ["CG"] = new CountryInfo("AF", "CG", "COG", 178, "Republic of the Congo", "Brazzaville", "fr,ln", "242") ,
            ["RE"] = new CountryInfo("AF", "RE", "REU", 638, "Réunion", "Saint-Denis", "fr", "262") ,
            ["RO"] = new CountryInfo("EU", "RO", "ROU", 642, "Romania", "Bucharest", "ro", "40") ,
            ["RU"] = new CountryInfo("EU", "RU", "RUS", 643, "Russian Federation", "Moscow", "ru,ba,ce,cu,os,sah,tt", "7") ,
            ["RW"] = new CountryInfo("AF", "RW", "RWA", 646, "Rwanda", "Kigali", "en,fr,rw", "250") ,
            ["BL"] = new CountryInfo("NA", "BL", "BLM", 652, "Saint Barthélemy", "Gustavia", "fr", "590") ,
            ["SH"] = new CountryInfo("AF", "SH", "SHN", 654, "Saint Helena, Ascension and Tristan da Cunha", "Jamestown", "en", "290") ,
            ["KN"] = new CountryInfo("NA", "KN", "KNA", 659, "Saint Kitts and Nevis", "Basseterre", "en", "1-869") ,
            ["LC"] = new CountryInfo("NA", "LC", "LCA", 662, "Saint Lucia", "Castries", "en", "1-758") ,
            ["MF"] = new CountryInfo("NA", "MF", "MAF", 663, "Saint Martin", "Marigot", "fr", "590") ,
            ["PM"] = new CountryInfo("NA", "PM", "SPM", 666, "Saint Pierre and Miquelon", "Saint-Pierre", "fr", "508") ,
            ["VC"] = new CountryInfo("NA", "VC", "VCT", 670, "Saint Vincent and the Grenadines", "Kingstown", "en", "1-784") ,
            ["WS"] = new CountryInfo("OC", "WS", "WSM", 882, "Samoa", "Apia", "en", "685") ,
            ["SM"] = new CountryInfo("EU", "SM", "SMR", 674, "San Marino", "San Marino", "it", "378") ,
            ["ST"] = new CountryInfo("AF", "ST", "STP", 678, "São Tomé and Príncipe", "Sao Tome", "pt", "239") ,
            ["SA"] = new CountryInfo("AS", "SA", "SAU", 682, "Saudi Arabia", "Riyadh", "ar", "966") ,
            ["SN"] = new CountryInfo("AF", "SN", "SEN", 686, "Senegal", "Dakar", "fr,dyo,ff,wo", "221") ,
            ["RS"] = new CountryInfo("EU", "RS", "SRB", 688, "Serbia", "Belgrade", "sr,sr", "381") ,
            ["SC"] = new CountryInfo("AF", "SC", "SYC", 690, "Seychelles", "Victoria", "en,fr", "248") ,
            ["SL"] = new CountryInfo("AF", "SL", "SLE", 694, "Sierra Leone", "Freetown", "en", "232") ,
            ["SG"] = new CountryInfo("AS", "SG", "SGP", 702, "Singapore", "Singapore", "en,ms,ta,zh", "65") ,
            ["SX"] = new CountryInfo("NA", "SX", "SXM", 534, "Sint Maarten", "Philipsburg", "en,nl", "1-721") ,
            ["SK"] = new CountryInfo("EU", "SK", "SVK", 703, "Slovakia", "Bratislava", "sk", "421") ,
            ["SI"] = new CountryInfo("EU", "SI", "SVN", 705, "Slovenia", "Ljubljana", "en,sl", "386") ,
            ["SB"] = new CountryInfo("OC", "SB", "SLB", 90, "Solomon Islands", "Honiara", "en", "677") ,
            ["SO"] = new CountryInfo("AF", "SO", "SOM", 706, "Somalia", "Mogadishu", "ar,so", "252") ,
            ["ZA"] = new CountryInfo("AF", "ZA", "ZAF", 710, "South Africa", "Pretoria", "af,en,nr,nso,ss,st,tn,ts,ve,xh,zu", "27") ,
            ["GS"] = new CountryInfo("AN", "GS", "SGS", 239, "South Georgia and the South Sandwich Islands", "Grytviken", "en", "500") ,
            ["KR"] = new CountryInfo("AS", "KR", "KOR", 410, "Republic of Korea", "Seoul", "ko", "82") ,
            ["SS"] = new CountryInfo("AF", "SS", "SSD", 728, "South Sudan", "Juba", "ar,en,nus", "211") ,
            ["ES"] = new CountryInfo("EU", "ES", "ESP", 724, "Spain", "Madrid", "es,ca,ast,eu,gl", "34") ,
            ["LK"] = new CountryInfo("AS", "LK", "LKA", 144, "Sri Lanka", "Colombo", "si,ta", "94") ,
            ["SD"] = new CountryInfo("AF", "SD", "SDN", 729, "Sudan", "Khartoum", "ar,en", "249") ,
            ["SR"] = new CountryInfo("SA", "SR", "SUR", 740, "Suriname", "Paramaribo", "nl", "597") ,
            ["SJ"] = new CountryInfo("EU", "SJ", "SJM", 744, "Svalbard and Jan Mayen", "Longyearbyen", "nb", "47") ,
            ["SZ"] = new CountryInfo("AF", "SZ", "SWZ", 748, "Eswatini", "Mbabane", "en,ss", "268") ,
            ["SE"] = new CountryInfo("EU", "SE", "SWE", 752, "Sweden", "Stockholm", "en,se,sma,smj,sv", "46") ,
            ["CH"] = new CountryInfo("EU", "CH", "CHE", 756, "Switzerland", "Bern", "de,en,fr,gsw,it,pt,rm,wae", "41") ,
            ["SY"] = new CountryInfo("AS", "SY", "SYR", 760, "Syrian Arab Republic", "Damascus", "ar,fr,syr", "963") ,
            ["TW"] = new CountryInfo("AS", "TW", "TWN", 158, "Taiwan", "Taipei", "zh", "886") ,
            ["TJ"] = new CountryInfo("AS", "TJ", "TJK", 762, "Tajikistan", "Dushanbe", "tg", "992") ,
            ["TZ"] = new CountryInfo("AF", "TZ", "TZA", 834, "United Republic of Tanzania", "Dodoma", "asa,bez,en,jmc,kde,ksb,lag,mas,rof,rwk,sbp,sw,vun", "255") ,
            ["TH"] = new CountryInfo("AS", "TH", "THA", 764, "Thailand", "Bangkok", "th", "66") ,
            ["TG"] = new CountryInfo("AF", "TG", "TGO", 768, "Togo", "Lome", "ee,fr", "228") ,
            ["TK"] = new CountryInfo("OC", "TK", "TKL", 772, "Tokelau", "", "en", "690") ,
            ["TO"] = new CountryInfo("OC", "TO", "TON", 776, "Tonga", "Nuku'alofa", "en,to", "676") ,
            ["TT"] = new CountryInfo("NA", "TT", "TTO", 780, "Trinidad and Tobago", "Port of Spain", "en", "1-868") ,
            ["TN"] = new CountryInfo("AF", "TN", "TUN", 788, "Tunisia", "Tunis", "ar,fr", "216") ,
            ["TR"] = new CountryInfo("AS", "TR", "TUR", 792, "Turkey", "Ankara", "tr", "90") ,
            ["TM"] = new CountryInfo("AS", "TM", "TKM", 795, "Turkmenistan", "Ashgabat", "tk", "993") ,
            ["TC"] = new CountryInfo("NA", "TC", "TCA", 796, "Turks and Caicos Islands", "Cockburn Town", "en", "1-649") ,
            ["TV"] = new CountryInfo("OC", "TV", "TUV", 798, "Tuvalu", "Funafuti", "en", "688") ,
            ["UM"] = new CountryInfo("OC", "UM", "UMI", 581, "United States Minor Outlying Islands", "", "en", "246") ,
            ["VI"] = new CountryInfo("NA", "VI", "VIR", 850, "Virgin Islands", "Charlotte Amalie", "en", "1-340") ,
            ["UG"] = new CountryInfo("AF", "UG", "UGA", 800, "Uganda", "Kampala", "cgg,en,lg,nyn,sw,teo,xog", "256") ,
            ["UA"] = new CountryInfo("EU", "UA", "UKR", 804, "Ukraine", "Kiev", "uk,ru", "380") ,
            ["AE"] = new CountryInfo("AS", "AE", "ARE", 784, "United Arab Emirates", "Abu Dhabi", "ar", "971") ,
            ["GB"] = new CountryInfo("EU", "GB", "GBR", 826, "United Kingdom of Great Britain and Northern Ireland", "London", "en,cy,gd,kw", "44") ,
            ["US"] = new CountryInfo("NA", "US", "USA", 840, "United States of America", "Washington", "en,es,chr,haw,lkt", "1") ,
            ["UY"] = new CountryInfo("SA", "UY", "URY", 858, "Uruguay", "Montevideo", "es", "598") ,
            ["UZ"] = new CountryInfo("AS", "UZ", "UZB", 860, "Uzbekistan", "Tashkent", "uz,uz", "998") ,
            ["VU"] = new CountryInfo("OC", "VU", "VUT", 548, "Vanuatu", "Port Vila", "en,fr", "678") ,
            ["VA"] = new CountryInfo("EU", "VA", "VAT", 336, "Vatican City", "Vatican City", "it,fr,la", "379") ,
            ["VE"] = new CountryInfo("SA", "VE", "VEN", 862, "Bolivarian Republic of Venezuela", "Caracas", "es", "58") ,
            ["VN"] = new CountryInfo("AS", "VN", "VNM", 704, "Viet Nam", "Hanoi", "vi", "84") ,
            ["WF"] = new CountryInfo("OC", "WF", "WLF", 876, "Wallis and Futuna", "Mata Utu", "fr", "681") ,
            ["EH"] = new CountryInfo("AF", "EH", "ESH", 732, "Western Sahara", "El-Aaiun", "ar,mey", "212") ,
            ["YE"] = new CountryInfo("AS", "YE", "YEM", 887, "Yemen", "Sanaa", "ar", "967") ,
            ["ZM"] = new CountryInfo("AF", "ZM", "ZMB", 894, "Zambia", "Lusaka", "bem,en", "260") ,
            ["ZW"] = new CountryInfo("AF", "ZW", "ZWE", 716, "Zimbabwe", "Harare", "en,nd,sn", "263")
        };
        _callingCodeMap = new();
        foreach (var callingCode in Countries.SelectMany(x => x.CallingCode.Split(',').Select(code => new { CountryTwoLetterCode = x.TwoLetterCode, Code = code }))) {
            if (_callingCodeMap.ContainsKey(callingCode.Code)) {
                continue;
            }
            _callingCodeMap.Add(callingCode.Code, callingCode.CountryTwoLetterCode);
        }
    }

    /// <summary>The name.</summary>
    public string Name { get; private set; }
    /// <summary>The capital if available.</summary>
    public string Capital { get; private set; }
    /// <summary>The two letter continent code.</summary>
    public string ContinentCode { get; private set; }
    /// <summary>The name of the continent in English.</summary>
    public string ContinentName => _continentNamesMap[ContinentCode];
    /// <summary>ISO two letter country code.</summary>
    public string TwoLetterCode { get; private set; }
    /// <summary>ISO three letter country code.</summary>
    public string ThreeLetterCode { get; set; }
    /// <summary>ISO three letter country code.</summary>
    public string TwoLetterLanguageCode { get; private set; }
    /// <summary>Numeric country code.</summary>
    public int NumericCode { get; private set; }
    /// <summary>Country Calling code.</summary>
    public string CallingCode { get; private set; }
    /// <summary>Default Country Calling code</summary>
    public int CallingCodeDefault => string.IsNullOrWhiteSpace(CallingCode) ? -1 : int.Parse(CallingCode.Split(',').First().Replace("-", string.Empty));
    /// <summary>Culture code locale.</summary>
    public string Locale => TwoLetterLanguageCode != null ? $"{TwoLetterLanguageCode?.Split(',')[0]}-{TwoLetterCode}" : null;

    /// <summary>Currency Symbol if available.</summary>
    public string GetCurrencyISO() {
        try {
            var region = new RegionInfo(TwoLetterCode);
            return region.ISOCurrencySymbol;
        } catch { };
        return null;
    }

    /// <summary>String representation.</summary>
    /// <returns></returns>
    public override string ToString() => $"{TwoLetterCode} {Name}";

    /// <summary>Get <see cref="CountryInfo"/> by numeric code.</summary>
    /// <param name="code"></param>
    public static CountryInfo GetCountryByNumericCode(int code) {
        var data = Countries.FirstOrDefault(cd => cd.NumericCode == code);
        if (data == null) {
            throw new ArgumentException(string.Format("{0} is an invalid numeric country code.", code), nameof(code));
        }
        return data;
    }

    /// <summary>Search the first match <see cref="CountryInfo"/> by name or twoletter ISO. Throws if none found.</summary>
    /// <param name="nameOrTwoLetterCode"></param>
    public static CountryInfo GetCountryByNameOrCode(string nameOrTwoLetterCode) {
        if (nameOrTwoLetterCode?.Length == 2 && _countries.ContainsKey(nameOrTwoLetterCode)) {
            return _countries[nameOrTwoLetterCode];
        }
        var data = Countries.FirstOrDefault(cd => string.Equals(cd.Name, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase));
        if (data == null) {
            throw new ArgumentException(string.Format("CountryData with name or two letter code {0} does not exist.", nameOrTwoLetterCode), nameof(nameOrTwoLetterCode));
        }
        return data;
    }

    /// <summary>Search for <see cref="CountryInfo"/> by international calling code . Throws if none found.</summary>
    /// <param name="callingCode">The international country code without the plus sign</param>
    /// <returns>The country information found</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ArgumentNullException"/>
    public static CountryInfo GetCountryByCallingCode(string callingCode) {
        if (string.IsNullOrWhiteSpace(callingCode)) { 
            throw new ArgumentNullException(nameof(callingCode));
        }
        if (!_callingCodeMap.ContainsKey(callingCode)) {
            throw new ArgumentOutOfRangeException(nameof(callingCode), $"Unknown calling code {callingCode}");
        }
        return _countries[_callingCodeMap[callingCode.TrimStart('+')]];
    }

    /// <summary>Search for <see cref="CountryInfo"/> by international calling code. All candidates returned.</summary>
    /// <param name="phoneNumber">The international phonenumber code without the plus sign</param>
    /// <returns>The country information found</returns>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<CountryInfo> FindCountriesByPhoneNumber(string phoneNumber) {
        if (string.IsNullOrWhiteSpace(phoneNumber)) {
            throw new ArgumentNullException(nameof(phoneNumber));
        }
        phoneNumber = phoneNumber.TrimStart('0', '+');
        return _callingCodeMap.Keys.Where(k => phoneNumber.StartsWith(k)).Select(k => _countries[_callingCodeMap[k]]).OrderByDescending(x => x.CallingCodeDefault);
    }

    /// <summary>Search for <see cref="CountryInfo"/> by international calling code . Throws if none found.</summary>
    /// <param name="callingCode">The international country code without the plus sign</param>
    /// <param name="countryInfo">Output parameter</param>
    /// <returns>True if successful</returns>
    public static bool TryGetCountryByCallingCode(string callingCode, out CountryInfo countryInfo) {
        countryInfo = null;
        try {
            countryInfo = GetCountryByCallingCode(callingCode);
            return true;
        } catch {
            return false;
        }
    }

    /// <summary>Try <see cref="GetCountryByNameOrCode(string)"/>.</summary>
    /// <param name="nameOrTwoLetterCode"></param>
    /// <param name="countryInfo"></param>
    public static bool TryGetCountryByNameOrCode(string nameOrTwoLetterCode, out CountryInfo countryInfo) {
        var success = true;
        countryInfo = default;
        try {
            if (!string.IsNullOrEmpty(nameOrTwoLetterCode)) {
                countryInfo = GetCountryByNameOrCode(nameOrTwoLetterCode.ToUpper());
            }
        } catch {
            success = false;
        }
        return success;
    }

    /// <summary>Check the name or two letter code for existence</summary>
    /// <param name="nameOrTwoLetterCode"></param>
    public static bool ValidCountryNameOrCode(string nameOrTwoLetterCode) =>
        _countries.ContainsKey(nameOrTwoLetterCode) || Countries.Where(cd => string.Equals(cd.Name, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase)).Any();
}
