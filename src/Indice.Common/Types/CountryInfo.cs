using System.Globalization;

namespace Indice.Globalization;

/// <summary>Contains information about the countries. Based on 2018-2019.</summary>
public class CountryInfo
{
    private static readonly Dictionary<string, string> _continentNamesMap = new Dictionary<string, string> {
        { "AS", "Asia" },
        { "AF", "Africa" },
        { "NA", "North America" },
        { "SA", "South America" },
        { "AN", "Antartica" },
        { "EU", "Europe" },
        { "OC", "Australia" },
    };

    /// <summary>Collection of countries</summary>
    public static readonly ICollection<CountryInfo> Countries;

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
        Countries = new List<CountryInfo>
        {
            { new CountryInfo("AS", "AF", "AFG", 4, "Afghanistan", "Kabul", "prs,ps,uz", "93") },
            { new CountryInfo("EU", "AX", "ALA", 248, "Åland", "Mariehamn", "sv", "358-18") },
            { new CountryInfo("EU", "AL", "ALB", 8, "Albania", "Tirana", "sq", "355") },
            { new CountryInfo("AF", "DZ", "DZA", 12, "Algeria", "Algiers", "ar,fr,kab,tzm", "213") },
            { new CountryInfo("OC", "AS", "ASM", 16, "American Samoa", "Pago Pago", "en", "1-684") },
            { new CountryInfo("EU", "AD", "AND", 20, "Andorra", "Andorra la Vella", "ca", "376") },
            { new CountryInfo("AF", "AO", "AGO", 24, "Angola", "Luanda", "ln,pt", "244") },
            { new CountryInfo("NA", "AI", "AIA", 660, "Anguilla", "The Valley", "en", "1-264") },
            { new CountryInfo("AN", "AQ", "ATA", 10, "Antarctica", "", "en", "672") },
            { new CountryInfo("NA", "AG", "ATG", 28, "Antigua and Barbuda", "St. John's", "en", "1-268") },
            { new CountryInfo("SA", "AR", "ARG", 32, "Argentina", "Buenos Aires", "es", "54") },
            { new CountryInfo("AS", "AM", "ARM", 51, "Armenia", "Yerevan", "hy", "374") },
            { new CountryInfo("NA", "AW", "ABW", 533, "Aruba", "Oranjestad", "nl", "297") },
            { new CountryInfo("OC", "AU", "AUS", 36, "Australia", "Canberra", "en", "61") },
            { new CountryInfo("EU", "AT", "AUT", 40, "Austria", "Vienna", "de,en", "43") },
            { new CountryInfo("AS", "AZ", "AZE", 31, "Azerbaijan", "Baku", "az", "994") },
            { new CountryInfo("NA", "BS", "BHS", 44, "Bahamas", "Nassau", "en", "1-242") },
            { new CountryInfo("AS", "BH", "BHR", 48, "Bahrain", "Manama", "ar", "973") },
            { new CountryInfo("AS", "BD", "BGD", 50, "Bangladesh", "Dhaka", "bn", "880") },
            { new CountryInfo("NA", "BB", "BRB", 52, "Barbados", "Bridgetown", "en", "1-246") },
            { new CountryInfo("EU", "BY", "BLR", 112, "Belarus", "Minsk", "be,ru", "375") },
            { new CountryInfo("EU", "BE", "BEL", 56, "Belgium", "Brussels", "fr,de,en,nl", "32") },
            { new CountryInfo("NA", "BZ", "BLZ", 84, "Belize", "Belmopan", "en", "501") },
            { new CountryInfo("AF", "BJ", "BEN", 204, "Benin", "Porto-Novo", "fr,yo", "229") },
            { new CountryInfo("NA", "BM", "BMU", 60, "Bermuda", "Hamilton", "en", "1-441") },
            { new CountryInfo("AS", "BT", "BTN", 64, "Bhutan", "Thimphu", "dz", "975") },
            { new CountryInfo("SA", "BO", "BOL", 68, "Bolivia", "Sucre", "es,quz", "591") },
            { new CountryInfo("NA", "BQ", "BES", 535, "Bonaire, Sint Eustatius and Saba", "", "nl", "599-7") },
            { new CountryInfo("EU", "BA", "BIH", 70, "Bosnia and Herzegovina", "Sarajevo", "bs,hr,sr", "387") },
            { new CountryInfo("AF", "BW", "BWA", 72, "Botswana", "Gaborone", "en,tn", "267") },
            { new CountryInfo("AN", "BV", "BVT", 74, "Bouvet Island", "", "no", "47") },
            { new CountryInfo("SA", "BR", "BRA", 76, "Brazil", "Brasilia", "pt,es", "55") },
            { new CountryInfo("AS", "IO", "IOT", 86, "British Indian Ocean Territory", "Diego Garcia", "en", "246") },
            { new CountryInfo("NA", "VG", "VGB", 92, "British Virgin Islands", "Road Town", "en", "1-284") },
            { new CountryInfo("AS", "BN", "BRN", 96, "Brunei Darussalam", "Bandar Seri Begawan", "ms", "673") },
            { new CountryInfo("EU", "BG", "BGR", 100, "Bulgaria", "Sofia", "bg", "359") },
            { new CountryInfo("AF", "BF", "BFA", 854, "Burkina Faso", "Ouagadougou", "fr", "226") },
            { new CountryInfo("AF", "BI", "BDI", 108, "Burundi", "Bujumbura", "rn,en,fr", "257") },
            { new CountryInfo("AS", "KH", "KHM", 116, "Cambodia", "Phnom Penh", "km", "855") },
            { new CountryInfo("AF", "CM", "CMR", 120, "Cameroon", "Yaounde", "agq,bas,dua,en,ewo,ff,fr,jgo,kkj,ksf,mgo,mua,nmg,nnh,yav", "237") },
            { new CountryInfo("NA", "CA", "CAN", 124, "Canada", "Ottawa", "en,fr,iu,iu,moh", "1") },
            { new CountryInfo("AF", "CV", "CPV", 132, "Cabo Verde", "Praia", "kea,pt", "238") },
            { new CountryInfo("NA", "KY", "CYM", 136, "Cayman Islands", "George Town", "en", "1-345") },
            { new CountryInfo("AF", "CF", "CAF", 140, "Central African Republic", "Bangui", "fr,ln,sg", "236") },
            { new CountryInfo("AF", "TD", "TCD", 148, "Chad", "N'Djamena", "ar,fr", "235") },
            { new CountryInfo("SA", "CL", "CHL", 152, "Chile", "Santiago", "arn,es", "56") },
            { new CountryInfo("AS", "CN", "CHN", 156, "China", "Beijing", "zh,mn,bo,ii,ug", "86") },
            { new CountryInfo("OC", "CX", "CXR", 162, "Christmas Island", "Flying Fish Cove", "en", "61") },
            { new CountryInfo("AS", "CC", "CCK", 166, "Cocos [Keeling] Islands", "West Island", "en", "61") },
            { new CountryInfo("SA", "CO", "COL", 170, "Colombia", "Bogota", "es", "57") },
            { new CountryInfo("AF", "KM", "COM", 174, "Comoros", "Moroni", "ar,fr", "269") },
            { new CountryInfo("OC", "CK", "COK", 184, "Cook Islands", "Avarua", "en", "682") },
            { new CountryInfo("NA", "CR", "CRI", 188, "Costa Rica", "San Jose", "es", "506") },
            { new CountryInfo("EU", "HR", "HRV", 191, "Croatia", "Zagreb", "hr", "385") },
            { new CountryInfo("NA", "CU", "CUB", 192, "Cuba", "Havana", "es", "53") },
            { new CountryInfo("NA", "CW", "CUW", 531, "Curacao", "Willemstad", "nl", "599") },
            { new CountryInfo("EU", "CY", "CYP", 196, "Cyprus", "Nicosia", "el,en,tr", "357") },
            { new CountryInfo("EU", "CZ", "CZE", 203, "Czechia", "Prague", "cs", "420") },
            { new CountryInfo("AF", "CD", "COD", 180, "Democratic Republic of the Congo", "Kinshasa", "fr,ln,lu,sw", "243") },
            { new CountryInfo("EU", "DK", "DNK", 208, "Denmark", "Copenhagen", "da,en,fo", "45") },
            { new CountryInfo("AF", "DJ", "DJI", 262, "Djibouti", "Djibouti", "aa,ar,fr,so", "253") },
            { new CountryInfo("NA", "DM", "DMA", 212, "Dominica", "Roseau", "en", "1-767") },
            { new CountryInfo("NA", "DO", "DOM", 214, "Dominican Republic", "Santo Domingo", "es", "1-809, 1-829, 1-849") },
            { new CountryInfo("OC", "TL", "TLS", 626, "Timor-Leste", "Dili", "pt", "670") },
            { new CountryInfo("SA", "EC", "ECU", 218, "Ecuador", "Quito", "es,quz", "593") },
            { new CountryInfo("AF", "EG", "EGY", 818, "Egypt", "Cairo", "ar", "20") },
            { new CountryInfo("NA", "SV", "SLV", 222, "El Salvador", "San Salvador", "es", "503") },
            { new CountryInfo("AF", "GQ", "GNQ", 226, "Equatorial Guinea", "Malabo", "es,fr,pt", "240") },
            { new CountryInfo("AF", "ER", "ERI", 232, "Eritrea", "Asmara", "aa,ar,byn,en,ssy,ti,tig", "291") },
            { new CountryInfo("EU", "EE", "EST", 233, "Estonia", "Tallinn", "et", "372") },
            { new CountryInfo("AF", "ET", "ETH", 231, "Ethiopia", "Addis Ababa", "aa,am,om,so,ti,wal", "251") },
            { new CountryInfo("SA", "FK", "FLK", 238, "Falkland Islands", "Stanley", "en", "500") },
            { new CountryInfo("EU", "FO", "FRO", 234, "Faroe Islands", "Torshavn", "fo", "298") },
            { new CountryInfo("OC", "FJ", "FJI", 242, "Fiji", "Suva", "en", "679") },
            { new CountryInfo("EU", "FI", "FIN", 246, "Finland", "Helsinki", "en,fi,se,smn,sms,sv", "358") },
            { new CountryInfo("EU", "FR", "FRA", 250, "France", "Paris", "fr,br,ca,co,gsw,ia,oc", "33") },
            { new CountryInfo("SA", "GF", "GUF", 254, "French Guiana", "Cayenne", "fr", "594") },
            { new CountryInfo("OC", "PF", "PYF", 258, "French Polynesia", "Papeete", "fr", "689") },
            { new CountryInfo("AN", "TF", "ATF", 260, "French Southern Territories", "Port-aux-Francais", "fr", "33") },
            { new CountryInfo("AF", "GA", "GAB", 266, "Gabon", "Libreville", "fr", "241") },
            { new CountryInfo("AF", "GM", "GMB", 270, "Gambia", "Banjul", "en", "220") },
            { new CountryInfo("AS", "GE", "GEO", 268, "Georgia", "Tbilisi", "ka,os", "995") },
            { new CountryInfo("EU", "DE", "DEU", 276, "Germany", "Berlin", "de,dsb,en,hsb,ksh,nds", "49") },
            { new CountryInfo("AF", "GH", "GHA", 288, "Ghana", "Accra", "ak,ee,en,ha", "233") },
            { new CountryInfo("EU", "GI", "GIB", 292, "Gibraltar", "Gibraltar", "en", "350") },
            { new CountryInfo("EU", "GR", "GRC", 300, "Greece", "Athens", "el", "30") },
            { new CountryInfo("NA", "GL", "GRL", 304, "Greenland", "Nuuk", "da,kl", "299") },
            { new CountryInfo("NA", "GD", "GRD", 308, "Grenada", "St. George's", "en", "1-473") },
            { new CountryInfo("NA", "GP", "GLP", 312, "Guadeloupe", "Basse-Terre", "fr", "590") },
            { new CountryInfo("OC", "GU", "GUM", 316, "Guam", "Hagatna", "en", "1-671") },
            { new CountryInfo("NA", "GT", "GTM", 320, "Guatemala", "Guatemala City", "es,quc", "502") },
            { new CountryInfo("EU", "GG", "GGY", 831, "Guernsey", "St Peter Port", "en", "44-1481") },
            { new CountryInfo("AF", "GN", "GIN", 324, "Guinea", "Conakry", "fr,ff,nqo", "224") },
            { new CountryInfo("AF", "GW", "GNB", 624, "Guinea-Bissau", "Bissau", "pt", "245") },
            { new CountryInfo("SA", "GY", "GUY", 328, "Guyana", "Georgetown", "en", "592") },
            { new CountryInfo("NA", "HT", "HTI", 332, "Haiti", "Port-au-Prince", "fr,zh", "509") },
            { new CountryInfo("AN", "HM", "HMD", 334, "Heard Island and McDonald Islands", "", "en", "61") },
            { new CountryInfo("NA", "HN", "HND", 340, "Honduras", "Tegucigalpa", "es", "504") },
            { new CountryInfo("AS", "HK", "HKG", 344, "Hong Kong", "Hong Kong", "en,zh,zh", "852") },
            { new CountryInfo("EU", "HU", "HUN", 348, "Hungary", "Budapest", "hu", "36") },
            { new CountryInfo("EU", "IS", "ISL", 352, "Iceland", "Reykjavik", "is", "354") },
            { new CountryInfo("AS", "IN", "IND", 356, "India", "New Delhi", "as,bn,bo,brx,en,gu,hi,kn,kok,ks,ks,ml,mni,mr,ne,or,pa,sa,sd,ta,te,ur", "91") },
            { new CountryInfo("AS", "ID", "IDN", 360, "Indonesia", "Jakarta", "en,id,jv,jv", "62") },
            { new CountryInfo("AS", "IR", "IRN", 364, "Iran", "Tehran", "fa,ku,lrc,mzn", "98") },
            { new CountryInfo("AS", "IQ", "IRQ", 368, "Iraq", "Baghdad", "ar,ku,lrc", "964") },
            { new CountryInfo("EU", "IE", "IRL", 372, "Ireland", "Dublin", "en,ga", "353") },
            { new CountryInfo("EU", "IM", "IMN", 833, "Isle of Man", "Douglas", "en,gv", "44-1624") },
            { new CountryInfo("AS", "IL", "ISR", 376, "Israel", "Jerusalem", "he,en,ar,en", "972") },
            { new CountryInfo("EU", "IT", "ITA", 380, "Italy", "Rome", "it,ca,de,fur", "39") },
            { new CountryInfo("AF", "CI", "CIV", 384, "Côte d'Ivoire", "Yamoussoukro", "fr", "225") },
            { new CountryInfo("NA", "JM", "JAM", 388, "Jamaica", "Kingston", "en", "1-876") },
            { new CountryInfo("AS", "JP", "JPN", 392, "Japan", "Tokyo", "ja", "81") },
            { new CountryInfo("EU", "JE", "JEY", 832, "Jersey", "Saint Helier", "en", "44-1534") },
            { new CountryInfo("AS", "JO", "JOR", 400, "Jordan", "Amman", "ar", "962") },
            { new CountryInfo("AS", "KZ", "KAZ", 398, "Kazakhstan", "Astana", "kk,ru", "7") },
            { new CountryInfo("AF", "KE", "KEN", 404, "Kenya", "Nairobi", "dav,ebu,en,guz,kam,ki,kln,luo,luy,mas,mer,om,saq,so,sw,teo", "254") },
            { new CountryInfo("OC", "KI", "KIR", 296, "Kiribati", "Tarawa", "en", "686") },
            { new CountryInfo("EU", "XK", "XKX", 0, "Kosovo", "Pristina", "sq,sr", "383") },
            { new CountryInfo("AS", "KW", "KWT", 414, "Kuwait", "Kuwait City", "ar", "965") },
            { new CountryInfo("AS", "KG", "KGZ", 417, "Kyrgyzstan", "Bishkek", "ky,ru", "996") },
            { new CountryInfo("AS", "LA", "LAO", 418, "Lao People's Democratic Republic", "Vientiane", "lo", "856") },
            { new CountryInfo("EU", "LV", "LVA", 428, "Latvia", "Riga", "lv", "371") },
            { new CountryInfo("AS", "LB", "LBN", 422, "Lebanon", "Beirut", "ar", "961") },
            { new CountryInfo("AF", "LS", "LSO", 426, "Lesotho", "Maseru", "en,st", "266") },
            { new CountryInfo("AF", "LR", "LBR", 430, "Liberia", "Monrovia", "en,vai,vai", "231") },
            { new CountryInfo("AF", "LY", "LBY", 434, "Libya", "Tripoli", "ar", "218") },
            { new CountryInfo("EU", "LI", "LIE", 438, "Liechtenstein", "Vaduz", "de,gsw", "423") },
            { new CountryInfo("EU", "LT", "LTU", 440, "Lithuania", "Vilnius", "lt", "370") },
            { new CountryInfo("EU", "LU", "LUX", 442, "Luxembourg", "Luxembourg", "de,fr,lb,pt", "352") },
            { new CountryInfo("AS", "MO", "MAC", 446, "Macao", "Macao", "en,pt,zh,zh", "853") },
            { new CountryInfo("EU", "MK", "MKD", 807, "Republic of North Macedonia", "Skopje", "mk,sq", "389") },
            { new CountryInfo("AF", "MG", "MDG", 450, "Madagascar", "Antananarivo", "en,fr,mg", "261") },
            { new CountryInfo("AF", "MW", "MWI", 454, "Malawi", "Lilongwe", "en", "265") },
            { new CountryInfo("AS", "MY", "MYS", 458, "Malaysia", "Kuala Lumpur", "en,ms,ta", "60") },
            { new CountryInfo("AS", "MV", "MDV", 462, "Maldives", "Male", "dv", "960") },
            { new CountryInfo("AF", "ML", "MLI", 466, "Mali", "Bamako", "bm,fr,khq,ses", "223") },
            { new CountryInfo("EU", "MT", "MLT", 470, "Malta", "Valletta", "en,mt", "356") },
            { new CountryInfo("OC", "MH", "MHL", 584, "Marshall Islands", "Majuro", "en", "692") },
            { new CountryInfo("NA", "MQ", "MTQ", 474, "Martinique", "Fort-de-France", "fr", "596") },
            { new CountryInfo("AF", "MR", "MRT", 478, "Mauritania", "Nouakchott", "ar,ff,fr", "222") },
            { new CountryInfo("AF", "MU", "MUS", 480, "Mauritius", "Port Louis", "en,fr,mfe", "230") },
            { new CountryInfo("AF", "YT", "MYT", 175, "Mayotte", "Mamoudzou", "fr", "262") },
            { new CountryInfo("NA", "MX", "MEX", 484, "Mexico", "Mexico City", "es", "52") },
            { new CountryInfo("OC", "FM", "FSM", 583, "Micronesia", "Palikir", "en", "691") },
            { new CountryInfo("EU", "MD", "MDA", 498, "Moldova", "Chisinau", "ro,ru", "373") },
            { new CountryInfo("EU", "MC", "MCO", 492, "Monaco", "Monaco", "fr", "377") },
            { new CountryInfo("AS", "MN", "MNG", 496, "Mongolia", "Ulan Bator", "mn,mn", "976") },
            { new CountryInfo("EU", "ME", "MNE", 499, "Montenegro", "Podgorica", "cu,hu,sq,sr", "382") },
            { new CountryInfo("NA", "MS", "MSR", 500, "Montserrat", "Plymouth", "en", "1-664") },
            { new CountryInfo("AF", "MA", "MAR", 504, "Morocco", "Rabat", "ar,fr,shi,shi,tzm,tzm,tzm,zgh", "212") },
            { new CountryInfo("AF", "MZ", "MOZ", 508, "Mozambique", "Maputo", "mgh,pt,seh", "258") },
            { new CountryInfo("AS", "MM", "MMR", 104, "Myanmar", "Nay Pyi Taw", "my", "95") },
            { new CountryInfo("AF", "NA", "NAM", 516, "Namibia", "Windhoek", "af,en,naq", "264") },
            { new CountryInfo("OC", "NR", "NRU", 520, "Nauru", "Yaren", "en", "674") },
            { new CountryInfo("AS", "NP", "NPL", 524, "Nepal", "Kathmandu", "ne", "977") },
            { new CountryInfo("EU", "NL", "NLD", 528, "Netherlands", "Amsterdam", "en,fy,nds,nl", "31") },
            { new CountryInfo("NA", "AN", "ANT", 530, "Netherlands Antilles", "Willemstad", "nl,en,es", "599") },
            { new CountryInfo("OC", "NC", "NCL", 540, "New Caledonia", "Noumea", "fr", "687") },
            { new CountryInfo("OC", "NZ", "NZL", 554, "New Zealand", "Wellington", "en,mi", "64") },
            { new CountryInfo("NA", "NI", "NIC", 558, "Nicaragua", "Managua", "es", "505") },
            { new CountryInfo("AF", "NE", "NER", 562, "Niger", "Niamey", "dje,fr,ha,twq", "227") },
            { new CountryInfo("AF", "NG", "NGA", 566, "Nigeria", "Abuja", "bin,en,ff,ha,ibb,ig,kr,yo", "234") },
            { new CountryInfo("OC", "NU", "NIU", 570, "Niue", "Alofi", "en", "683") },
            { new CountryInfo("OC", "NF", "NFK", 574, "Norfolk Island", "Kingston", "en", "672-3") },
            { new CountryInfo("AS", "KP", "PRK", 408, " Democratic People's Republic of Korea", "Pyongyang", "ko", "850") },
            { new CountryInfo("OC", "MP", "MNP", 580, "Northern Mariana Islands", "Saipan", "en", "1-670") },
            { new CountryInfo("EU", "NO", "NOR", 578, "Norway", "Oslo", "nb,nn,se,sma,smj", "47") },
            { new CountryInfo("AS", "OM", "OMN", 512, "Oman", "Muscat", "ar", "968") },
            { new CountryInfo("AS", "PK", "PAK", 586, "Pakistan", "Islamabad", "en,pa,sd,ur", "92") },
            { new CountryInfo("OC", "PW", "PLW", 585, "Palau", "Melekeok", "en", "680") },
            { new CountryInfo("AS", "PS", "PSE", 275, "Palestine", "East Jerusalem", "ar", "970") },
            { new CountryInfo("NA", "PA", "PAN", 591, "Panama", "Panama City", "es", "507") },
            { new CountryInfo("OC", "PG", "PNG", 598, "Papua New Guinea", "Port Moresby", "en", "675") },
            { new CountryInfo("SA", "PY", "PRY", 600, "Paraguay", "Asuncion", "es,gn", "595") },
            { new CountryInfo("SA", "PE", "PER", 604, "Peru", "Lima", "es,quz", "51") },
            { new CountryInfo("AS", "PH", "PHL", 608, "Philippines", "Manila", "en,es,fil", "63") },
            { new CountryInfo("OC", "PN", "PCN", 612, "Pitcairn", "Adamstown", "en", "64") },
            { new CountryInfo("EU", "PL", "POL", 616, "Poland", "Warsaw", "pl", "48") },
            { new CountryInfo("EU", "PT", "PRT", 620, "Portugal", "Lisbon", "pt", "351") },
            { new CountryInfo("NA", "PR", "PRI", 630, "Puerto Rico", "San Juan", "en,es", "1-787, 1-939") },
            { new CountryInfo("AS", "QA", "QAT", 634, "Qatar", "Doha", "ar", "974") },
            { new CountryInfo("AF", "CG", "COG", 178, "Republic of the Congo", "Brazzaville", "fr,ln", "242") },
            { new CountryInfo("AF", "RE", "REU", 638, "Réunion", "Saint-Denis", "fr", "262") },
            { new CountryInfo("EU", "RO", "ROU", 642, "Romania", "Bucharest", "ro", "40") },
            { new CountryInfo("EU", "RU", "RUS", 643, "Russian Federation", "Moscow", "ru,ba,ce,cu,os,sah,tt", "7") },
            { new CountryInfo("AF", "RW", "RWA", 646, "Rwanda", "Kigali", "en,fr,rw", "250") },
            { new CountryInfo("NA", "BL", "BLM", 652, "Saint Barthélemy", "Gustavia", "fr", "590") },
            { new CountryInfo("AF", "SH", "SHN", 654, "Saint Helena, Ascension and Tristan da Cunha", "Jamestown", "en", "290") },
            { new CountryInfo("NA", "KN", "KNA", 659, "Saint Kitts and Nevis", "Basseterre", "en", "1-869") },
            { new CountryInfo("NA", "LC", "LCA", 662, "Saint Lucia", "Castries", "en", "1-758") },
            { new CountryInfo("NA", "MF", "MAF", 663, "Saint Martin", "Marigot", "fr", "590") },
            { new CountryInfo("NA", "PM", "SPM", 666, "Saint Pierre and Miquelon", "Saint-Pierre", "fr", "508") },
            { new CountryInfo("NA", "VC", "VCT", 670, "Saint Vincent and the Grenadines", "Kingstown", "en", "1-784") },
            { new CountryInfo("OC", "WS", "WSM", 882, "Samoa", "Apia", "en", "685") },
            { new CountryInfo("EU", "SM", "SMR", 674, "San Marino", "San Marino", "it", "378") },
            { new CountryInfo("AF", "ST", "STP", 678, "São Tomé and Príncipe", "Sao Tome", "pt", "239") },
            { new CountryInfo("AS", "SA", "SAU", 682, "Saudi Arabia", "Riyadh", "ar", "966") },
            { new CountryInfo("AF", "SN", "SEN", 686, "Senegal", "Dakar", "fr,dyo,ff,wo", "221") },
            { new CountryInfo("EU", "RS", "SRB", 688, "Serbia", "Belgrade", "sr,sr", "381") },
            { new CountryInfo("AF", "SC", "SYC", 690, "Seychelles", "Victoria", "en,fr", "248") },
            { new CountryInfo("AF", "SL", "SLE", 694, "Sierra Leone", "Freetown", "en", "232") },
            { new CountryInfo("AS", "SG", "SGP", 702, "Singapore", "Singapore", "en,ms,ta,zh", "65") },
            { new CountryInfo("NA", "SX", "SXM", 534, "Sint Maarten", "Philipsburg", "en,nl", "1-721") },
            { new CountryInfo("EU", "SK", "SVK", 703, "Slovakia", "Bratislava", "sk", "421") },
            { new CountryInfo("EU", "SI", "SVN", 705, "Slovenia", "Ljubljana", "en,sl", "386") },
            { new CountryInfo("OC", "SB", "SLB", 90, "Solomon Islands", "Honiara", "en", "677") },
            { new CountryInfo("AF", "SO", "SOM", 706, "Somalia", "Mogadishu", "ar,so", "252") },
            { new CountryInfo("AF", "ZA", "ZAF", 710, "South Africa", "Pretoria", "af,en,nr,nso,ss,st,tn,ts,ve,xh,zu", "27") },
            { new CountryInfo("AN", "GS", "SGS", 239, "South Georgia and the South Sandwich Islands", "Grytviken", "en", "500") },
            { new CountryInfo("AS", "KR", "KOR", 410, "Republic of Korea", "Seoul", "ko", "82") },
            { new CountryInfo("AF", "SS", "SSD", 728, "South Sudan", "Juba", "ar,en,nus", "211") },
            { new CountryInfo("EU", "ES", "ESP", 724, "Spain", "Madrid", "es,ca,ast,eu,gl", "34") },
            { new CountryInfo("AS", "LK", "LKA", 144, "Sri Lanka", "Colombo", "si,ta", "94") },
            { new CountryInfo("AF", "SD", "SDN", 729, "Sudan", "Khartoum", "ar,en", "249") },
            { new CountryInfo("SA", "SR", "SUR", 740, "Suriname", "Paramaribo", "nl", "597") },
            { new CountryInfo("EU", "SJ", "SJM", 744, "Svalbard and Jan Mayen", "Longyearbyen", "nb", "47") },
            { new CountryInfo("AF", "SZ", "SWZ", 748, "Eswatini", "Mbabane", "en,ss", "268") },
            { new CountryInfo("EU", "SE", "SWE", 752, "Sweden", "Stockholm", "en,se,sma,smj,sv", "46") },
            { new CountryInfo("EU", "CH", "CHE", 756, "Switzerland", "Bern", "de,en,fr,gsw,it,pt,rm,wae", "41") },
            { new CountryInfo("AS", "SY", "SYR", 760, "Syrian Arab Republic", "Damascus", "ar,fr,syr", "963") },
            { new CountryInfo("AS", "TW", "TWN", 158, "Taiwan", "Taipei", "zh", "886") },
            { new CountryInfo("AS", "TJ", "TJK", 762, "Tajikistan", "Dushanbe", "tg", "992") },
            { new CountryInfo("AF", "TZ", "TZA", 834, "United Republic of Tanzania", "Dodoma", "asa,bez,en,jmc,kde,ksb,lag,mas,rof,rwk,sbp,sw,vun", "255") },
            { new CountryInfo("AS", "TH", "THA", 764, "Thailand", "Bangkok", "th", "66") },
            { new CountryInfo("AF", "TG", "TGO", 768, "Togo", "Lome", "ee,fr", "228") },
            { new CountryInfo("OC", "TK", "TKL", 772, "Tokelau", "", "en", "690") },
            { new CountryInfo("OC", "TO", "TON", 776, "Tonga", "Nuku'alofa", "en,to", "676") },
            { new CountryInfo("NA", "TT", "TTO", 780, "Trinidad and Tobago", "Port of Spain", "en", "1-868") },
            { new CountryInfo("AF", "TN", "TUN", 788, "Tunisia", "Tunis", "ar,fr", "216") },
            { new CountryInfo("AS", "TR", "TUR", 792, "Turkey", "Ankara", "tr", "90") },
            { new CountryInfo("AS", "TM", "TKM", 795, "Turkmenistan", "Ashgabat", "tk", "993") },
            { new CountryInfo("NA", "TC", "TCA", 796, "Turks and Caicos Islands", "Cockburn Town", "en", "1-649") },
            { new CountryInfo("OC", "TV", "TUV", 798, "Tuvalu", "Funafuti", "en", "688") },
            { new CountryInfo("OC", "UM", "UMI", 581, "United States Minor Outlying Islands", "", "en", "246") },
            { new CountryInfo("NA", "VI", "VIR", 850, "Virgin Islands", "Charlotte Amalie", "en", "1-340") },
            { new CountryInfo("AF", "UG", "UGA", 800, "Uganda", "Kampala", "cgg,en,lg,nyn,sw,teo,xog", "256") },
            { new CountryInfo("EU", "UA", "UKR", 804, "Ukraine", "Kiev", "uk,ru", "380") },
            { new CountryInfo("AS", "AE", "ARE", 784, "United Arab Emirates", "Abu Dhabi", "ar", "971") },
            { new CountryInfo("EU", "GB", "GBR", 826, "United Kingdom of Great Britain and Northern Ireland", "London", "en,cy,gd,kw", "44") },
            { new CountryInfo("NA", "US", "USA", 840, "United States of America", "Washington", "en,es,chr,haw,lkt", "1") },
            { new CountryInfo("SA", "UY", "URY", 858, "Uruguay", "Montevideo", "es", "598") },
            { new CountryInfo("AS", "UZ", "UZB", 860, "Uzbekistan", "Tashkent", "uz,uz", "998") },
            { new CountryInfo("OC", "VU", "VUT", 548, "Vanuatu", "Port Vila", "en,fr", "678") },
            { new CountryInfo("EU", "VA", "VAT", 336, "Vatican City", "Vatican City", "it,fr,la", "379") },
            { new CountryInfo("SA", "VE", "VEN", 862, "Bolivarian Republic of Venezuela", "Caracas", "es", "58") },
            { new CountryInfo("AS", "VN", "VNM", 704, "Viet Nam", "Hanoi", "vi", "84") },
            { new CountryInfo("OC", "WF", "WLF", 876, "Wallis and Futuna", "Mata Utu", "fr", "681") },
            { new CountryInfo("AF", "EH", "ESH", 732, "Western Sahara", "El-Aaiun", "ar,mey", "212") },
            { new CountryInfo("AS", "YE", "YEM", 887, "Yemen", "Sanaa", "ar", "967") },
            { new CountryInfo("AF", "ZM", "ZMB", 894, "Zambia", "Lusaka", "bem,en", "260") },
            { new CountryInfo("AF", "ZW", "ZWE", 716, "Zimbabwe", "Harare", "en,nd,sn", "263") }
        };
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
        var data = Countries.FirstOrDefault(cd => string.Equals(cd.Name, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase) || string.Equals(cd.TwoLetterCode, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase));
        if (data == null) {
            throw new ArgumentException(string.Format("CountryData with name or two letter code {0} does not exist.", nameOrTwoLetterCode), nameof(nameOrTwoLetterCode));
        }
        return data;
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
        Countries.Where(cd => string.Equals(cd.Name, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase) || string.Equals(cd.TwoLetterCode, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase)).Any();
}
