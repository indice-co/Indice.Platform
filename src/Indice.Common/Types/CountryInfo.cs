using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.Globalization
{
    /// <summary>
    /// Contains information about the countries. Based on 2018-2019.
    /// </summary>
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

        /// <summary>
        /// Collection of countries
        /// </summary>
        public static readonly ICollection<CountryInfo> Countries;

        internal CountryInfo(string continent, string twoLetterCode, int numericCode, string fullName, string capital, string twoLetterLanguageCode, string callingCode) {
            Name = fullName;
            Capital = capital;
            ContinentCode = continent;
            TwoLetterCode = twoLetterCode;
            NumericCode = numericCode;
            TwoLetterLanguageCode = string.IsNullOrWhiteSpace(twoLetterLanguageCode) ? null : twoLetterLanguageCode;
            CallingCode = string.IsNullOrWhiteSpace(callingCode) ? null : callingCode;
        }

        static CountryInfo() {
            Countries = new List<CountryInfo>
            {
                { new CountryInfo("AS", "AF", 4, "Afghanistan", "Kabul", "prs,ps,uz", "93") },
                { new CountryInfo("EU", "AX", 248, "Åland", "Mariehamn", "sv", "358-18") },
                { new CountryInfo("EU", "AL", 8, "Albania", "Tirana", "sq", "355") },
                { new CountryInfo("AF", "DZ", 12, "Algeria", "Algiers", "ar,fr,kab,tzm", "213") },
                { new CountryInfo("OC", "AS", 16, "American Samoa", "Pago Pago", "en", "1-684") },
                { new CountryInfo("EU", "AD", 20, "Andorra", "Andorra la Vella", "ca", "376") },
                { new CountryInfo("AF", "AO", 24, "Angola", "Luanda", "ln,pt", "244") },
                { new CountryInfo("NA", "AI", 660, "Anguilla", "The Valley", "en", "1-264") },
                { new CountryInfo("AN", "AQ", 10, "Antarctica", "", "en", "672") },
                { new CountryInfo("NA", "AG", 28, "Antigua and Barbuda", "St. John's", "en", "1-268") },
                { new CountryInfo("SA", "AR", 32, "Argentina", "Buenos Aires", "es", "54") },
                { new CountryInfo("AS", "AM", 51, "Armenia", "Yerevan", "hy", "374") },
                { new CountryInfo("NA", "AW", 533, "Aruba", "Oranjestad", "nl", "297") },
                { new CountryInfo("OC", "AU", 36, "Australia", "Canberra", "en", "61") },
                { new CountryInfo("EU", "AT", 40, "Austria", "Vienna", "de,en", "43") },
                { new CountryInfo("AS", "AZ", 31, "Azerbaijan", "Baku", "az", "994") },
                { new CountryInfo("NA", "BS", 44, "Bahamas", "Nassau", "en", "1-242") },
                { new CountryInfo("AS", "BH", 48, "Bahrain", "Manama", "ar", "973") },
                { new CountryInfo("AS", "BD", 50, "Bangladesh", "Dhaka", "bn", "880") },
                { new CountryInfo("NA", "BB", 52, "Barbados", "Bridgetown", "en", "1-246") },
                { new CountryInfo("EU", "BY", 112, "Belarus", "Minsk", "be,ru", "375") },
                { new CountryInfo("EU", "BE", 56, "Belgium", "Brussels", "fr,de,en,nl", "32") },
                { new CountryInfo("NA", "BZ", 84, "Belize", "Belmopan", "en", "501") },
                { new CountryInfo("AF", "BJ", 204, "Benin", "Porto-Novo", "fr,yo", "229") },
                { new CountryInfo("NA", "BM", 60, "Bermuda", "Hamilton", "en", "1-441") },
                { new CountryInfo("AS", "BT", 64, "Bhutan", "Thimphu", "dz", "975") },
                { new CountryInfo("SA", "BO", 68, "Bolivia", "Sucre", "es,quz", "591") },
                { new CountryInfo("NA", "BQ", 535, "Bonaire", "", "nl", "599-7") },
                { new CountryInfo("EU", "BA", 70, "Bosnia and Herzegovina", "Sarajevo", "bs,hr,sr", "387") },
                { new CountryInfo("AF", "BW", 72, "Botswana", "Gaborone", "en,tn", "267") },
                { new CountryInfo("AN", "BV", 74, "Bouvet Island", "", "no", "47") },
                { new CountryInfo("SA", "BR", 76, "Brazil", "Brasilia", "pt,es", "55") },
                { new CountryInfo("AS", "IO", 86, "British Indian Ocean Territory", "Diego Garcia", "en", "246") },
                { new CountryInfo("NA", "VG", 92, "British Virgin Islands", "Road Town", "en", "1-284") },
                { new CountryInfo("AS", "BN", 96, "Brunei", "Bandar Seri Begawan", "ms", "673") },
                { new CountryInfo("EU", "BG", 100, "Bulgaria", "Sofia", "bg", "359") },
                { new CountryInfo("AF", "BF", 854, "Burkina Faso", "Ouagadougou", "fr", "226") },
                { new CountryInfo("AF", "BI", 108, "Burundi", "Bujumbura", "rn,en,fr", "257") },
                { new CountryInfo("AS", "KH", 116, "Cambodia", "Phnom Penh", "km", "855") },
                { new CountryInfo("AF", "CM", 120, "Cameroon", "Yaounde", "agq,bas,dua,en,ewo,ff,fr,jgo,kkj,ksf,mgo,mua,nmg,nnh,yav", "237") },
                { new CountryInfo("NA", "CA", 124, "Canada", "Ottawa", "en,fr,iu,iu,moh", "1") },
                { new CountryInfo("AF", "CV", 132, "Cape Verde", "Praia", "kea,pt", "238") },
                { new CountryInfo("NA", "KY", 136, "Cayman Islands", "George Town", "en", "1-345") },
                { new CountryInfo("AF", "CF", 140, "Central African Republic", "Bangui", "fr,ln,sg", "236") },
                { new CountryInfo("AF", "TD", 148, "Chad", "N'Djamena", "ar,fr", "235") },
                { new CountryInfo("SA", "CL", 152, "Chile", "Santiago", "arn,es", "56") },
                { new CountryInfo("AS", "CN", 156, "China", "Beijing", "zh,mn,bo,ii,ug", "86") },
                { new CountryInfo("OC", "CX", 162, "Christmas Island", "Flying Fish Cove", "en", "61") },
                { new CountryInfo("AS", "CC", 166, "Cocos [Keeling] Islands", "West Island", "en", "61") },
                { new CountryInfo("SA", "CO", 170, "Colombia", "Bogota", "es", "57") },
                { new CountryInfo("AF", "KM", 174, "Comoros", "Moroni", "ar,fr", "269") },
                { new CountryInfo("OC", "CK", 184, "Cook Islands", "Avarua", "en", "682") },
                { new CountryInfo("NA", "CR", 188, "Costa Rica", "San Jose", "es", "506") },
                { new CountryInfo("EU", "HR", 191, "Croatia", "Zagreb", "hr", "385") },
                { new CountryInfo("NA", "CU", 192, "Cuba", "Havana", "es", "53") },
                { new CountryInfo("NA", "CW", 531, "Curacao", "Willemstad", "nl", "599") },
                { new CountryInfo("EU", "CY", 196, "Cyprus", "Nicosia", "el,en,tr", "357") },
                { new CountryInfo("EU", "CZ", 203, "Czechia", "Prague", "cs", "420") },
                { new CountryInfo("AF", "CD", 180, "Democratic Republic of the Congo", "Kinshasa", "fr,ln,lu,sw", "243") },
                { new CountryInfo("EU", "DK", 208, "Denmark", "Copenhagen", "da,en,fo", "45") },
                { new CountryInfo("AF", "DJ", 262, "Djibouti", "Djibouti", "aa,ar,fr,so", "253") },
                { new CountryInfo("NA", "DM", 212, "Dominica", "Roseau", "en", "1-767") },
                { new CountryInfo("NA", "DO", 214, "Dominican Republic", "Santo Domingo", "es", "1-809, 1-829, 1-849") },
                { new CountryInfo("OC", "TL", 626, "East Timor", "Dili", "pt", "670") },
                { new CountryInfo("SA", "EC", 218, "Ecuador", "Quito", "es,quz", "593") },
                { new CountryInfo("AF", "EG", 818, "Egypt", "Cairo", "ar", "20") },
                { new CountryInfo("NA", "SV", 222, "El Salvador", "San Salvador", "es", "503") },
                { new CountryInfo("AF", "GQ", 226, "Equatorial Guinea", "Malabo", "es,fr,pt", "240") },
                { new CountryInfo("AF", "ER", 232, "Eritrea", "Asmara", "aa,ar,byn,en,ssy,ti,tig", "291") },
                { new CountryInfo("EU", "EE", 233, "Estonia", "Tallinn", "et", "372") },
                { new CountryInfo("AF", "ET", 231, "Ethiopia", "Addis Ababa", "aa,am,om,so,ti,wal", "251") },
                { new CountryInfo("SA", "FK", 238, "Falkland Islands", "Stanley", "en", "500") },
                { new CountryInfo("EU", "FO", 234, "Faroe Islands", "Torshavn", "fo", "298") },
                { new CountryInfo("OC", "FJ", 242, "Fiji", "Suva", "en", "679") },
                { new CountryInfo("EU", "FI", 246, "Finland", "Helsinki", "en,fi,se,smn,sms,sv", "358") },
                { new CountryInfo("EU", "FR", 250, "France", "Paris", "fr,br,ca,co,gsw,ia,oc", "33") },
                { new CountryInfo("SA", "GF", 254, "French Guiana", "Cayenne", "fr", "594") },
                { new CountryInfo("OC", "PF", 258, "French Polynesia", "Papeete", "fr", "689") },
                { new CountryInfo("AN", "TF", 260, "French Southern Territories", "Port-aux-Francais", "fr", "33") },
                { new CountryInfo("AF", "GA", 266, "Gabon", "Libreville", "fr", "241") },
                { new CountryInfo("AF", "GM", 270, "Gambia", "Banjul", "en", "220") },
                { new CountryInfo("AS", "GE", 268, "Georgia", "Tbilisi", "ka,os", "995") },
                { new CountryInfo("EU", "DE", 276, "Germany", "Berlin", "de,dsb,en,hsb,ksh,nds", "49") },
                { new CountryInfo("AF", "GH", 288, "Ghana", "Accra", "ak,ee,en,ha", "233") },
                { new CountryInfo("EU", "GI", 292, "Gibraltar", "Gibraltar", "en", "350") },
                { new CountryInfo("EU", "GR", 300, "Greece", "Athens", "el", "30") },
                { new CountryInfo("NA", "GL", 304, "Greenland", "Nuuk", "da,kl", "299") },
                { new CountryInfo("NA", "GD", 308, "Grenada", "St. George's", "en", "1-473") },
                { new CountryInfo("NA", "GP", 312, "Guadeloupe", "Basse-Terre", "fr", "590") },
                { new CountryInfo("OC", "GU", 316, "Guam", "Hagatna", "en", "1-671") },
                { new CountryInfo("NA", "GT", 320, "Guatemala", "Guatemala City", "es,quc", "502") },
                { new CountryInfo("EU", "GG", 831, "Guernsey", "St Peter Port", "en", "44-1481") },
                { new CountryInfo("AF", "GN", 324, "Guinea", "Conakry", "fr,ff,nqo", "224") },
                { new CountryInfo("AF", "GW", 624, "Guinea-Bissau", "Bissau", "pt", "245") },
                { new CountryInfo("SA", "GY", 328, "Guyana", "Georgetown", "en", "592") },
                { new CountryInfo("NA", "HT", 332, "Haiti", "Port-au-Prince", "fr,zh", "509") },
                { new CountryInfo("AN", "HM", 334, "Heard Island and McDonald Islands", "", "en", "61") },
                { new CountryInfo("NA", "HN", 340, "Honduras", "Tegucigalpa", "es", "504") },
                { new CountryInfo("AS", "HK", 344, "Hong Kong", "Hong Kong", "en,zh,zh", "852") },
                { new CountryInfo("EU", "HU", 348, "Hungary", "Budapest", "hu", "36") },
                { new CountryInfo("EU", "IS", 352, "Iceland", "Reykjavik", "is", "354") },
                { new CountryInfo("AS", "IN", 356, "India", "New Delhi", "as,bn,bo,brx,en,gu,hi,kn,kok,ks,ks,ml,mni,mr,ne,or,pa,sa,sd,ta,te,ur", "91") },
                { new CountryInfo("AS", "ID", 360, "Indonesia", "Jakarta", "en,id,jv,jv", "62") },
                { new CountryInfo("AS", "IR", 364, "Iran", "Tehran", "fa,ku,lrc,mzn", "98") },
                { new CountryInfo("AS", "IQ", 368, "Iraq", "Baghdad", "ar,ku,lrc", "964") },
                { new CountryInfo("EU", "IE", 372, "Ireland", "Dublin", "en,ga", "353") },
                { new CountryInfo("EU", "IM", 833, "Isle of Man", "Douglas", "en,gv", "44-1624") },
                { new CountryInfo("AS", "IL", 376, "Israel", "Jerusalem", "he,en,ar,en", "972") },
                { new CountryInfo("EU", "IT", 380, "Italy", "Rome", "it,ca,de,fur", "39") },
                { new CountryInfo("AF", "CI", 384, "Ivory Coast", "Yamoussoukro", "fr", "225") },
                { new CountryInfo("NA", "JM", 388, "Jamaica", "Kingston", "en", "1-876") },
                { new CountryInfo("AS", "JP", 392, "Japan", "Tokyo", "ja", "81") },
                { new CountryInfo("EU", "JE", 832, "Jersey", "Saint Helier", "en", "44-1534") },
                { new CountryInfo("AS", "JO", 400, "Jordan", "Amman", "ar", "962") },
                { new CountryInfo("AS", "KZ", 398, "Kazakhstan", "Astana", "kk,ru", "7") },
                { new CountryInfo("AF", "KE", 404, "Kenya", "Nairobi", "dav,ebu,en,guz,kam,ki,kln,luo,luy,mas,mer,om,saq,so,sw,teo", "254") },
                { new CountryInfo("OC", "KI", 296, "Kiribati", "Tarawa", "en", "686") },
                { new CountryInfo("EU", "XK", 0, "Kosovo", "Pristina", "sq,sr", "383") },
                { new CountryInfo("AS", "KW", 414, "Kuwait", "Kuwait City", "ar", "965") },
                { new CountryInfo("AS", "KG", 417, "Kyrgyzstan", "Bishkek", "ky,ru", "996") },
                { new CountryInfo("AS", "LA", 418, "Laos", "Vientiane", "lo", "856") },
                { new CountryInfo("EU", "LV", 428, "Latvia", "Riga", "lv", "371") },
                { new CountryInfo("AS", "LB", 422, "Lebanon", "Beirut", "ar", "961") },
                { new CountryInfo("AF", "LS", 426, "Lesotho", "Maseru", "en,st", "266") },
                { new CountryInfo("AF", "LR", 430, "Liberia", "Monrovia", "en,vai,vai", "231") },
                { new CountryInfo("AF", "LY", 434, "Libya", "Tripoli", "ar", "218") },
                { new CountryInfo("EU", "LI", 438, "Liechtenstein", "Vaduz", "de,gsw", "423") },
                { new CountryInfo("EU", "LT", 440, "Lithuania", "Vilnius", "lt", "370") },
                { new CountryInfo("EU", "LU", 442, "Luxembourg", "Luxembourg", "de,fr,lb,pt", "352") },
                { new CountryInfo("AS", "MO", 446, "Macao", "Macao", "en,pt,zh,zh", "853") },
                { new CountryInfo("EU", "MK", 807, "North Macedonia", "Skopje", "mk,sq", "389") },
                { new CountryInfo("AF", "MG", 450, "Madagascar", "Antananarivo", "en,fr,mg", "261") },
                { new CountryInfo("AF", "MW", 454, "Malawi", "Lilongwe", "en", "265") },
                { new CountryInfo("AS", "MY", 458, "Malaysia", "Kuala Lumpur", "en,ms,ta", "60") },
                { new CountryInfo("AS", "MV", 462, "Maldives", "Male", "dv", "960") },
                { new CountryInfo("AF", "ML", 466, "Mali", "Bamako", "bm,fr,khq,ses", "223") },
                { new CountryInfo("EU", "MT", 470, "Malta", "Valletta", "en,mt", "356") },
                { new CountryInfo("OC", "MH", 584, "Marshall Islands", "Majuro", "en", "692") },
                { new CountryInfo("NA", "MQ", 474, "Martinique", "Fort-de-France", "fr", "596") },
                { new CountryInfo("AF", "MR", 478, "Mauritania", "Nouakchott", "ar,ff,fr", "222") },
                { new CountryInfo("AF", "MU", 480, "Mauritius", "Port Louis", "en,fr,mfe", "230") },
                { new CountryInfo("AF", "YT", 175, "Mayotte", "Mamoudzou", "fr", "262") },
                { new CountryInfo("NA", "MX", 484, "Mexico", "Mexico City", "es", "52") },
                { new CountryInfo("OC", "FM", 583, "Micronesia", "Palikir", "en", "691") },
                { new CountryInfo("EU", "MD", 498, "Moldova", "Chisinau", "ro,ru", "373") },
                { new CountryInfo("EU", "MC", 492, "Monaco", "Monaco", "fr", "377") },
                { new CountryInfo("AS", "MN", 496, "Mongolia", "Ulan Bator", "mn,mn", "976") },
                { new CountryInfo("EU", "ME", 499, "Montenegro", "Podgorica", "sr,sr", "382") },
                { new CountryInfo("NA", "MS", 500, "Montserrat", "Plymouth", "en", "1-664") },
                { new CountryInfo("AF", "MA", 504, "Morocco", "Rabat", "ar,fr,shi,shi,tzm,tzm,tzm,zgh", "212") },
                { new CountryInfo("AF", "MZ", 508, "Mozambique", "Maputo", "mgh,pt,seh", "258") },
                { new CountryInfo("AS", "MM", 104, "Myanmar [Burma]", "Nay Pyi Taw", "my", "95") },
                { new CountryInfo("AF", "NA", 516, "Namibia", "Windhoek", "af,en,naq", "264") },
                { new CountryInfo("OC", "NR", 520, "Nauru", "Yaren", "en", "674") },
                { new CountryInfo("AS", "NP", 524, "Nepal", "Kathmandu", "ne", "977") },
                { new CountryInfo("EU", "NL", 528, "Netherlands", "Amsterdam", "en,fy,nds,nl", "31") },
                { new CountryInfo("NA", "AN", 530, "Netherlands Antilles", "Willemstad", "nl,en,es", "599") },
                { new CountryInfo("OC", "NC", 540, "New Caledonia", "Noumea", "fr", "687") },
                { new CountryInfo("OC", "NZ", 554, "New Zealand", "Wellington", "en,mi", "64") },
                { new CountryInfo("NA", "NI", 558, "Nicaragua", "Managua", "es", "505") },
                { new CountryInfo("AF", "NE", 562, "Niger", "Niamey", "dje,fr,ha,twq", "227") },
                { new CountryInfo("AF", "NG", 566, "Nigeria", "Abuja", "bin,en,ff,ha,ibb,ig,kr,yo", "234") },
                { new CountryInfo("OC", "NU", 570, "Niue", "Alofi", "en", "683") },
                { new CountryInfo("OC", "NF", 574, "Norfolk Island", "Kingston", "en", "672-3") },
                { new CountryInfo("AS", "KP", 408, "North Korea", "Pyongyang", "ko", "850") },
                { new CountryInfo("OC", "MP", 580, "Northern Mariana Islands", "Saipan", "en", "1-670") },
                { new CountryInfo("EU", "NO", 578, "Norway", "Oslo", "nb,nn,se,sma,smj", "47") },
                { new CountryInfo("AS", "OM", 512, "Oman", "Muscat", "ar", "968") },
                { new CountryInfo("AS", "PK", 586, "Pakistan", "Islamabad", "en,pa,sd,ur", "92") },
                { new CountryInfo("OC", "PW", 585, "Palau", "Melekeok", "en", "680") },
                { new CountryInfo("AS", "PS", 275, "Palestine", "East Jerusalem", "ar", "970") },
                { new CountryInfo("NA", "PA", 591, "Panama", "Panama City", "es", "507") },
                { new CountryInfo("OC", "PG", 598, "Papua New Guinea", "Port Moresby", "en", "675") },
                { new CountryInfo("SA", "PY", 600, "Paraguay", "Asuncion", "es,gn", "595") },
                { new CountryInfo("SA", "PE", 604, "Peru", "Lima", "es,quz", "51") },
                { new CountryInfo("AS", "PH", 608, "Philippines", "Manila", "en,es,fil", "63") },
                { new CountryInfo("OC", "PN", 612, "Pitcairn Islands", "Adamstown", "en", "64") },
                { new CountryInfo("EU", "PL", 616, "Poland", "Warsaw", "pl", "48") },
                { new CountryInfo("EU", "PT", 620, "Portugal", "Lisbon", "pt", "351") },
                { new CountryInfo("NA", "PR", 630, "Puerto Rico", "San Juan", "en,es", "1-787, 1-939") },
                { new CountryInfo("AS", "QA", 634, "Qatar", "Doha", "ar", "974") },
                { new CountryInfo("AF", "CG", 178, "Republic of the Congo", "Brazzaville", "fr,ln", "242") },
                { new CountryInfo("AF", "RE", 638, "Réunion", "Saint-Denis", "fr", "262") },
                { new CountryInfo("EU", "RO", 642, "Romania", "Bucharest", "ro", "40") },
                { new CountryInfo("EU", "RU", 643, "Russia", "Moscow", "ru,ba,ce,cu,os,sah,tt", "7") },
                { new CountryInfo("AF", "RW", 646, "Rwanda", "Kigali", "en,fr,rw", "250") },
                { new CountryInfo("NA", "BL", 652, "Saint Barthélemy", "Gustavia", "fr", "590") },
                { new CountryInfo("AF", "SH", 654, "Saint Helena", "Jamestown", "en", "290") },
                { new CountryInfo("NA", "KN", 659, "Saint Kitts and Nevis", "Basseterre", "en", "1-869") },
                { new CountryInfo("NA", "LC", 662, "Saint Lucia", "Castries", "en", "1-758") },
                { new CountryInfo("NA", "MF", 663, "Saint Martin", "Marigot", "fr", "590") },
                { new CountryInfo("NA", "PM", 666, "Saint Pierre and Miquelon", "Saint-Pierre", "fr", "508") },
                { new CountryInfo("NA", "VC", 670, "Saint Vincent and the Grenadines", "Kingstown", "en", "1-784") },
                { new CountryInfo("OC", "WS", 882, "Samoa", "Apia", "en", "685") },
                { new CountryInfo("EU", "SM", 674, "San Marino", "San Marino", "it", "378") },
                { new CountryInfo("AF", "ST", 678, "São Tomé and Príncipe", "Sao Tome", "pt", "239") },
                { new CountryInfo("AS", "SA", 682, "Saudi Arabia", "Riyadh", "ar", "966") },
                { new CountryInfo("AF", "SN", 686, "Senegal", "Dakar", "fr,dyo,ff,wo", "221") },
                { new CountryInfo("EU", "RS", 688, "Serbia", "Belgrade", "sr,sr", "381") },
                { new CountryInfo("EU", "CS", 891, "Serbia and Montenegro", "Belgrade", "cu,hu,sq,sr", "381") },
                { new CountryInfo("AF", "SC", 690, "Seychelles", "Victoria", "en,fr", "248") },
                { new CountryInfo("AF", "SL", 694, "Sierra Leone", "Freetown", "en", "232") },
                { new CountryInfo("AS", "SG", 702, "Singapore", "Singapore", "en,ms,ta,zh", "65") },
                { new CountryInfo("NA", "SX", 534, "Sint Maarten", "Philipsburg", "en,nl", "1-721") },
                { new CountryInfo("EU", "SK", 703, "Slovakia", "Bratislava", "sk", "421") },
                { new CountryInfo("EU", "SI", 705, "Slovenia", "Ljubljana", "en,sl", "386") },
                { new CountryInfo("OC", "SB", 90, "Solomon Islands", "Honiara", "en", "677") },
                { new CountryInfo("AF", "SO", 706, "Somalia", "Mogadishu", "ar,so", "252") },
                { new CountryInfo("AF", "ZA", 710, "South Africa", "Pretoria", "af,en,nr,nso,ss,st,tn,ts,ve,xh,zu", "27") },
                { new CountryInfo("AN", "GS", 239, "South Georgia and the South Sandwich Islands", "Grytviken", "en", "500") },
                { new CountryInfo("AS", "KR", 410, "South Korea", "Seoul", "ko", "82") },
                { new CountryInfo("AF", "SS", 728, "South Sudan", "Juba", "ar,en,nus", "211") },
                { new CountryInfo("EU", "ES", 724, "Spain", "Madrid", "es,ca,ast,eu,gl", "34") },
                { new CountryInfo("AS", "LK", 144, "Sri Lanka", "Colombo", "si,ta", "94") },
                { new CountryInfo("AF", "SD", 729, "Sudan", "Khartoum", "ar,en", "249") },
                { new CountryInfo("SA", "SR", 740, "Suriname", "Paramaribo", "nl", "597") },
                { new CountryInfo("EU", "SJ", 744, "Svalbard and Jan Mayen", "Longyearbyen", "nb", "47") },
                { new CountryInfo("AF", "SZ", 748, "Swaziland", "Mbabane", "en,ss", "268") },
                { new CountryInfo("EU", "SE", 752, "Sweden", "Stockholm", "en,se,sma,smj,sv", "46") },
                { new CountryInfo("EU", "CH", 756, "Switzerland", "Bern", "de,en,fr,gsw,it,pt,rm,wae", "41") },
                { new CountryInfo("AS", "SY", 760, "Syria", "Damascus", "ar,fr,syr", "963") },
                { new CountryInfo("AS", "TW", 158, "Taiwan", "Taipei", "zh", "886") },
                { new CountryInfo("AS", "TJ", 762, "Tajikistan", "Dushanbe", "tg", "992") },
                { new CountryInfo("AF", "TZ", 834, "Tanzania", "Dodoma", "asa,bez,en,jmc,kde,ksb,lag,mas,rof,rwk,sbp,sw,vun", "255") },
                { new CountryInfo("AS", "TH", 764, "Thailand", "Bangkok", "th", "66") },
                { new CountryInfo("AF", "TG", 768, "Togo", "Lome", "ee,fr", "228") },
                { new CountryInfo("OC", "TK", 772, "Tokelau", "", "en", "690") },
                { new CountryInfo("OC", "TO", 776, "Tonga", "Nuku'alofa", "en,to", "676") },
                { new CountryInfo("NA", "TT", 780, "Trinidad and Tobago", "Port of Spain", "en", "1-868") },
                { new CountryInfo("AF", "TN", 788, "Tunisia", "Tunis", "ar,fr", "216") },
                { new CountryInfo("AS", "TR", 792, "Turkey", "Ankara", "tr", "90") },
                { new CountryInfo("AS", "TM", 795, "Turkmenistan", "Ashgabat", "tk", "993") },
                { new CountryInfo("NA", "TC", 796, "Turks and Caicos Islands", "Cockburn Town", "en", "1-649") },
                { new CountryInfo("OC", "TV", 798, "Tuvalu", "Funafuti", "en", "688") },
                { new CountryInfo("OC", "UM", 581, "U.S. Minor Outlying Islands", "", "en", "246") },
                { new CountryInfo("NA", "VI", 850, "U.S. Virgin Islands", "Charlotte Amalie", "en", "1-340") },
                { new CountryInfo("AF", "UG", 800, "Uganda", "Kampala", "cgg,en,lg,nyn,sw,teo,xog", "256") },
                { new CountryInfo("EU", "UA", 804, "Ukraine", "Kiev", "uk,ru", "380") },
                { new CountryInfo("AS", "AE", 784, "United Arab Emirates", "Abu Dhabi", "ar", "971") },
                { new CountryInfo("EU", "GB", 826, "United Kingdom", "London", "en,cy,gd,kw", "44") },
                { new CountryInfo("NA", "US", 840, "United States", "Washington", "en,es,chr,haw,lkt", "1") },
                { new CountryInfo("SA", "UY", 858, "Uruguay", "Montevideo", "es", "598") },
                { new CountryInfo("AS", "UZ", 860, "Uzbekistan", "Tashkent", "uz,uz", "998") },
                { new CountryInfo("OC", "VU", 548, "Vanuatu", "Port Vila", "en,fr", "678") },
                { new CountryInfo("EU", "VA", 336, "Vatican City", "Vatican City", "it,fr,la", "379") },
                { new CountryInfo("SA", "VE", 862, "Venezuela", "Caracas", "es", "58") },
                { new CountryInfo("AS", "VN", 704, "Vietnam", "Hanoi", "vi", "84") },
                { new CountryInfo("OC", "WF", 876, "Wallis and Futuna", "Mata Utu", "fr", "681") },
                { new CountryInfo("AF", "EH", 732, "Western Sahara", "El-Aaiun", "ar,mey", "212") },
                { new CountryInfo("AS", "YE", 887, "Yemen", "Sanaa", "ar", "967") },
                { new CountryInfo("AF", "ZM", 894, "Zambia", "Lusaka", "bem,en", "260") },
                { new CountryInfo("AF", "ZW", 716, "Zimbabwe", "Harare", "en,nd,sn", "263") },
            };
        }

        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The capital if available.
        /// </summary>
        public string Capital { get; private set; }
        /// <summary>
        /// The two letter continent code.
        /// </summary>
        public string ContinentCode { get; private set; }
        /// <summary>
        /// The name of the continent in English.
        /// </summary>
        public string ContinentName => _continentNamesMap[ContinentCode];
        /// <summary>
        /// ISO two letter country code.
        /// </summary>
        public string TwoLetterCode { get; private set; }
        /// <summary>
        /// ISO three letter country code.
        /// </summary>
        public string TwoLetterLanguageCode { get; private set; }
        /// <summary>
        /// Numeric country code.
        /// </summary>
        public int NumericCode { get; private set; }
        /// <summary>
        /// Country Calling code.
        /// </summary>
        public string CallingCode { get; private set; }
        /// <summary>
        /// Default Country Calling code
        /// </summary>
        public int CallingCodeDefault => string.IsNullOrWhiteSpace(CallingCode) ? -1 : int.Parse(CallingCode.Split(',')[0].Trim().Replace("+", "").Replace(" ", ""));
        /// <summary>
        /// Culture code locale.
        /// </summary>
        public string Locale => TwoLetterLanguageCode != null ? $"{TwoLetterLanguageCode?.Split(',')[0]}-{TwoLetterCode}" : null;

        /// <summary>
        /// Curency Symbol if available.
        /// </summary>
        public string GetCurrencyISO() {
            try {
                var region = new System.Globalization.RegionInfo(TwoLetterCode);
                return region.ISOCurrencySymbol;
            } catch { };
            return null;
        }

        /// <summary>
        /// String representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{TwoLetterCode} {Name}";

        /// <summary>
        /// Get <see cref="CountryInfo"/> by numeric code.
        /// </summary>
        /// <param name="code"></param>
        public static CountryInfo GetCountryByNumericCode(int code) {
            var data = Countries.FirstOrDefault(cd => cd.NumericCode == code);
            if (data == null) {
                throw new ArgumentException(string.Format("{0} is an invalid numeric country code.", code), nameof(code));
            }
            return data;
        }

        /// <summary>
        /// Search the first match <see cref="CountryInfo"/> by name or twoletter ISO. Throws if none found.
        /// </summary>
        /// <param name="nameOrTwoLetterCode"></param>
        public static CountryInfo GetCountryByNameOrCode(string nameOrTwoLetterCode) {
            var data = Countries.FirstOrDefault(cd => string.Equals(cd.Name, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase) || string.Equals(cd.TwoLetterCode, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase));
            if (data == null) {
                throw new ArgumentException(string.Format("CountryData with name or two letter code {0} does not exist.", nameOrTwoLetterCode), nameof(nameOrTwoLetterCode));
            }
            return data;
        }

        /// <summary>
        /// Try <see cref="GetCountryByNameOrCode(string)"/>.
        /// </summary>
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

        /// <summary>
        /// Check the name or two letter code for existence
        /// </summary>
        /// <param name="nameOrTwoLetterCode"></param>
        public static bool ValidCountryNameOrCode(string nameOrTwoLetterCode) =>
            Countries.Where(cd => string.Equals(cd.Name, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase) || string.Equals(cd.TwoLetterCode, nameOrTwoLetterCode, StringComparison.CurrentCultureIgnoreCase)).Any();
    }
}
