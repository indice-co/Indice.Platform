using System.Reflection;
using System.Text.RegularExpressions;

namespace Indice.Validation;

/// <summary>
/// Helper class. Provides utility methods to check the validity of Tax Identification codes. 
/// Mostly covers the country members of the EU but has some other countries too.
/// </summary>
public static class TaxCodeValidator
{
    static readonly IDictionary<string, string[]> supportedCountryMap = new Dictionary<string, string[]> {
           // Note - VAT codes without the "**" in the comment do not have check digit checking.
           ["AT"] = ["^(AT)?U(\\d{8})$" ],                     //** Austria
           ["BE"] = ["^(BE)?(0?\\d{9})$"],                     //** Belgium 
           ["BG"] = ["^(BG)?(\\d{9,10})$"],                    //** Bulgaria 
           ["CH"] = ["^(CHE)?(\\d{9})(MWST|TVA|IVA)?$"],       //** Switzerland
           ["CY"] = ["^(CY)?([0-59]\\d{7}[A-Z])$"],            //** Cyprus
           ["CZ"] = ["^(CZ)?(\\d{8,10})(\\d{3})?$"],           //** Czech Republic
           ["DE"] = ["^(DE)?([1-9]\\d{8})$"],                  //** Germany 
           ["DK"] = ["^(DK)?(\\d{8})$"],                       //** Denmark 
           ["EE"] = ["^(EE)?(10\\d{7})$"],                     //** Estonia 
           ["GR"] = ["^(EL)?(\\d{9})$"],                       //** Greece 
           ["ES"] = ["^(ES)?([A-Z]\\d{8})$",                   //** Spain (National juridical entities)
                     "^(ES)?([A-HN-SW]\\d{7}[A-J])$",          //** Spain (Other juridical entities)
                     "^(ES)?([0-9YZ]\\d{7}[A-Z])$",            //** Spain (Personal entities type 1)
                     "^(ES)?([KLMX]\\d{7}[A-Z])$"],            //** Spain (Personal entities type 2)
           ["EU"] = ["^(EU)?(\\d{9})$"],                       //** EU-type 
           ["FI"] = ["^(FI)?(\\d{8})$"],                       //** Finland 
           ["FR"] = ["^(FR)?(\\d{11})$",                       //** France (1)
                     "^(FR)?([A-HJ-NP-Z]\\d{10})$",            //   France (2)
                     "^(FR)?(\\d[A-HJ-NP-Z]\\d{9})$",          //   France (3)
                     "^(FR)?([A-HJ-NP-Z]{2}\\d{9})$"],         //   France (4)
           ["UK"] = ["^(GB)?(\\d{9})$",                        //** UK (Standard)
                     "^(GB)?(\\d{12})$",                       //** UK (Branches)
                     "^(GB)?(GD\\d{3})$",                      //** UK (Government)
                     "^(GB)?(HA\\d{3})$"],                     //** UK (Health authority)
           ["HR"] = ["^(HR)?(\\d{11})$"],                      //** Croatia 
           ["HU"] = ["^(HU)?(\\d{8})$"],                       //** Hungary 
           ["IE"] = ["^(IE)?(\\d{7}[A-W])$",                   //** Ireland (1)
                     "^(IE)?([7-9][A-Z\\*\\+)]\\d{5}[A-W])$",  //** Ireland (2)
                     "^(IE)?(\\d{7}[A-W][AH])$"],              //** Ireland (3)
           ["IT"] = ["^(IT)?(\\d{11})$"],                      //** Italy 
           ["LV"] = ["^(LV)?(\\d{11})$"],                      //** Latvia 
           ["LT"] = ["^(LT)?(\\d{9}|\\d{12})$"],               //** Lithunia
           ["LU"] = ["^(LU)?(\\d{8})$"],                       //** Luxembourg 
           ["MT"] = ["^(MT)?([1-9]\\d{7})$"],                  //** Malta
           ["NL"] = ["^(NL)?(\\d{9})B\\d{2}$"],                //** Netherlands
           ["NO"] = ["^(NO)?(\\d{9})$"],                       //** Norway (not EU)
           ["PL"] = ["^(PL)?(\\d{10})$"],                      //** Poland
           ["PT"] = ["^(PT)?(\\d{9})$"],                       //** Portugal
           ["RO"] = ["^(RO)?([1-9]\\d{1,9})$"],                //** Romania
           ["RU"] = ["^(RU)?(\\d{10}|\\d{12})$"],              //** Russia
           ["RS"] = ["^(RS)?(\\d{9})$"],                       //** Serbia
           ["SI"] = ["^(SI)?([1-9]\\d{7})$"],                  //** Slovenia
           ["SK"] = ["^(SK)?([1-9]\\d[2346-9]\\d{7})$"],       //** Slovakia Republic
           ["SE"] = ["^(SE)?(\\d{10}01)$"]                     //** Sweden
    };

    /// <summary>Check the tax id against format and check sum where available.</summary>
    /// <param name="taxIdentificationNumber">The number to check</param>
    /// <param name="countryISO">Optionaly pass the country iso in order to cover tax numbers not providing their country prefix.</param>
    /// <returns></returns>
    public static bool CheckNumber(string taxIdentificationNumber, string? countryISO = null) {
        if (countryISO != null && !supportedCountryMap.ContainsKey(countryISO)) {
            throw new NotSupportedException($"country iso \"{countryISO}\" is not supported for VAT number validation.");
        }
        var countryCode = countryISO;
        // rewrite vat country code from iso 
        switch (countryISO?.ToUpper()) {
            case "GR": countryCode = "EL"; break;
            case "UK": countryCode = "GB"; break;
            case "CH": countryCode = "CHE"; break;
        }
        // Array holds the regular expressions for the valid VAT number.
        var vatExpression = !string.IsNullOrEmpty(countryISO) ? supportedCountryMap[countryISO] : supportedCountryMap.Values.SelectMany(x => x);
        // Load up the string to check.
        var vatNumber = taxIdentificationNumber.ToUpperInvariant();
        // Remove spaces etc. From the VAT number to help validation.
        vatNumber = new Regex("(\\s|-|\\.)+").Replace(vatNumber, string.Empty);
        // Assume we're not going to find a valid VAT number.
        var valid = false;
        // Check the string against the regular expressions for all types of VAT numbers.
        foreach (var expression in vatExpression) {
            // Have we recognised the VAT number.
            var match = Regex.Match(vatNumber, expression);
            if (match.Success) {
                // Yes - we have
                var cCode = match.Groups[1].Value;   // Isolate country code.
                var cNumber = match.Groups[2].Value; // Isolate the number.
                if (cCode.Length == 0) {
                    cCode = countryCode;             // Set up default country code. 
                }
                if (cCode == null) {
                    continue;
                }
                // Call the appropriate country VAT validation routine depending on the country code
                var methodName = cCode + "CheckDigit";
#if !NETSTANDARD14
                var method = typeof(TaxCodeValidator).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
#else
                var method = typeof(TaxCodeValidator).GetTypeInfo().GetDeclaredMethod(methodName);
#endif
                if (method != null && (bool)method.Invoke(null, [cNumber])!)
                    valid = true;

                // Having processed the number, we break from the loop
                break;
            }
        }

        // Return with either an error or the reformatted VAT number
        return valid;
    }

    static bool ATCheckDigit(string vatnumber) {

        // Checks the check digits of an Austrian VAT number.

        var total = 0;
        var multipliers = new[] { 1, 2, 1, 2, 1, 2, 1 };
        var temp = 0;

        // Extract the next digit and multiply by the appropriate multiplier.  
        for (var i = 0; i < 7; i++) {
            temp = int.Parse(vatnumber[i].ToString()) * multipliers[i];
            if (temp > 9)
                total += temp / 10 + temp % 10;
            else
                total += temp;
        }

        // Establish check digit.
        total = 10 - (total + 4) % 10;
        if (total == 10) total = 0;

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(7, 1)))
            return true;
        else
            return false;
    }

    static bool BECheckDigit(string vatnumber) {

        // Checks the check digits of a Belgium VAT number.

        // Nine digit numbers have a 0 inserted at the front.
        if (vatnumber.Length == 9) vatnumber = "0" + vatnumber;

        if (int.Parse(vatnumber.Substring(1, 1)) == 0) return false;

        // Modulus 97 check on last nine digits
        if (97 - int.Parse(vatnumber.Substring(0, 8)) % 97 == int.Parse(vatnumber.Substring(8, 2)))
            return true;
        else
            return false;
    }

    static bool BGCheckDigit(string vatnumber) {
        var multipliers = default(int[]);
        var total = 0;
        // Checks the check digits of a Bulgarian VAT number.
        if (vatnumber.Length == 9) {
            // Check the check digit of 9 digit Bulgarian VAT numbers.
            total = 0;

            // First try to calculate the check digit using the first multipliers  
            var temp = 0;
            for (var i = 0; i < 8; i++) temp += int.Parse(vatnumber[i].ToString()) * (i + 1);

            // See if we have a check digit yet
            total = temp % 11;
            if (total != 10) {
                if (total == int.Parse(vatnumber.Substring(8)))
                    return true;
                else
                    return false;
            }

            // We got a modulus of 10 before so we have to keep going. Calculate the new check digit using  
            // the different multipliers  
            temp = 0;
            for (var i = 0; i < 8; i++) temp += int.Parse(vatnumber[i].ToString()) * (i + 3);

            // See if we have a check digit yet. If we still have a modulus of 10, set it to 0.
            total = temp % 11;
            if (total == 10) total = 0;
            if (total == int.Parse(vatnumber.Substring(8)))
                return true;
            else
                return false;
        }

        // 10 digit VAT code - see if it relates to a standard physical person

        if (Regex.IsMatch(vatnumber, "^\\d\\d[0-5]\\d[0-3]\\d\\d{4}$")) {

            // Check month
            var month = int.Parse(vatnumber.Substring(2, 2));
            if ((month > 0 && month < 13) || (month > 20 && month < 33) || (month > 40 && month < 53)) {

                // Extract the next digit and multiply by the counter.
                multipliers = [2, 4, 8, 5, 10, 9, 7, 3, 6];
                total = 0;
                for (var i = 0; i < 9; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

                // Establish check digit.
                total = total % 11;
                if (total == 10) total = 0;

                // Check to see if the check digit given is correct, If not, try next type of person
                if (total == int.Parse(vatnumber.Substring(9, 1))) return true;
            }
        }

        // It doesn't relate to a standard physical person - see if it relates to a foreigner.

        // Extract the next digit and multiply by the counter.
        multipliers = [21, 19, 17, 13, 11, 9, 7, 3, 1];
        total = 0;
        for (var i = 0; i < 9; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Check to see if the check digit given is correct, If not, try next type of person
        if (total % 10 == int.Parse(vatnumber.Substring(9, 1))) return true;

        // Finally, if not yet identified, see if it conforms to a miscellaneous VAT number

        // Extract the next digit and multiply by the counter.
        multipliers = [4, 3, 2, 7, 6, 5, 4, 3, 2];
        total = 0;
        for (var i = 0; i < 9; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digit.
        total = 11 - total % 11;
        if (total == 10) return false;
        if (total == 11) total = 0;

        // Check to see if the check digit given is correct, If not, we have an error with the VAT number
        if (total == int.Parse(vatnumber.Substring(9, 1)))
            return true;
        else
            return false;
    }

    static bool CHECheckDigit(string vatnumber) {

        // Checks the check digits of a Swiss VAT number.

        // Extract the next digit and multiply by the counter.
        var multipliers = new[] { 5, 4, 3, 2, 7, 6, 5, 4 };
        var total = 0;
        for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digit.
        total = 11 - total % 11;
        if (total == 10) return false;
        if (total == 11) total = 0;

        // Check to see if the check digit given is correct, If not, we have an error with the VAT number
        if (total == int.Parse(vatnumber.Substring(8, 1)))
            return true;
        else
            return false;
    }

    static bool CYCheckDigit(string vatnumber) {

        // Checks the check digits of a Cypriot VAT number.

        // Not allowed to start with '12'
        if (int.Parse(vatnumber.Substring(0, 2)) == 12) return false;

        // Extract the next digit and multiply by the counter.
        var total = 0;
        for (var i = 0; i < 8; i++) {
            var temp = int.Parse(vatnumber[i].ToString());
            if (i % 2 == 0) {
                switch (temp) {
                    case 0: temp = 1; break;
                    case 1: temp = 0; break;
                    case 2: temp = 5; break;
                    case 3: temp = 7; break;
                    case 4: temp = 9; break;
                    default: temp = temp * 2 + 3; break;
                }
            }
            total += temp;
        }

        // Establish check digit using modulus 26, and translate to char. equivalent.
        total = total % 26;
        total = total + 65;

        // Check to see if the check digit given is correct
        if ((char)total == vatnumber.Substring(8, 1)[0])
            return true;
        else
            return false;
    }

    static bool CZCheckDigit(string vatnumber) {

        // Checks the check digits of a Czech Republic VAT number.

        var total = 0;
        var multipliers = new[] { 8, 7, 6, 5, 4, 3, 2 };

        var czexp = new[] {
             "^\\d{8}$",
            "^[0-5][0-9][0|1|5|6]\\d[0-3]\\d\\d{3}$",
            "^6\\d{8}$",
            "^\\d{2}[0-3|5-8]\\d[0-3]\\d\\d{4}$"
        };
        var i = 0;

        // Legal entities
        if (Regex.IsMatch(vatnumber, czexp[0])) {

            // Extract the next digit and multiply by the counter.
            for (i = 0; i < 7; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

            // Establish check digit.
            total = 11 - total % 11;
            if (total == 10) total = 0;
            if (total == 11) total = 1;

            // Compare it with the last character of the VAT number. If it's the same, then it's valid.
            if (total == int.Parse(vatnumber.Substring(7, 1)))
                return true;
            else
                return false;
        }

        // Individuals type 1
        else if (Regex.IsMatch(vatnumber, czexp[1])) {
            var temp = int.Parse(vatnumber.Substring(0, 2));
            if (temp > 53) return false;
            return true;
        }

        // Individuals type 2
        else if (Regex.IsMatch(vatnumber, czexp[2])) {

            // Extract the next digit and multiply by the counter.
            for (i = 0; i < 7; i++) total += int.Parse(vatnumber[i + 1].ToString()) * multipliers[i];

            // Establish check digit.
            total = 11 - total % 11;
            if (total == 10) total = 0;
            if (total == 11) total = 1;

            // Convert calculated check digit according to a lookup table;
            var lookup = new[] { 8, 7, 6, 5, 4, 3, 2, 1, 0, 9, 10 };
            if (lookup[total - 1] == int.Parse(vatnumber.Substring(8, 1)))
                return true;
            else
                return false;
        }

        // Individuals type 3
        else if (Regex.IsMatch(vatnumber, czexp[3])) {
            var temp = int.Parse(vatnumber.Substring(0, 2)) +
                       int.Parse(vatnumber.Substring(2, 2)) +
                       int.Parse(vatnumber.Substring(4, 2)) +
                       int.Parse(vatnumber.Substring(6, 2)) +
                       int.Parse(vatnumber.Substring(8));
            if (temp % 11 == 0 && int.Parse(vatnumber) % 11 == 0)
                return true;
            else
                return false;
        }

        // else error
        return false;
    }

    static bool DECheckDigit(string vatnumber) {

        // Checks the check digits of a German VAT number.

        var product = 10;
        var sum = 0;
        var checkdigit = 0;
        for (var i = 0; i < 8; i++) {

            // Extract the next digit and implement peculiar algorithm!.
            sum = (int.Parse(vatnumber[i].ToString()) + product) % 10;
            if (sum == 0) { sum = 10; }
            product = (2 * sum) % 11;
        }

        // Establish check digit.  
        if (11 - product == 10) { checkdigit = 0; } else { checkdigit = 11 - product; }

        // Compare it with the last two characters of the VAT number. If the same, then it is a valid 
        // check digit.
        if (checkdigit == int.Parse(vatnumber.Substring(8, 1)))
            return true;
        else
            return false;
    }

    static bool DKCheckDigit(string vatnumber) {

        // Checks the check digits of a Danish VAT number.

        var total = 0;
        var multipliers = new[] { 2, 7, 6, 5, 4, 3, 2, 1 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digit.
        total = total % 11;

        // The remainder should be 0 for it to be valid..
        if (total == 0)
            return true;
        else
            return false;
    }

    static bool EECheckDigit(string vatnumber) {

        // Checks the check digits of an Estonian VAT number.

        var total = 0;
        var multipliers = new[] { 3, 7, 1, 3, 7, 1, 3, 7 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digits using modulus 10.
        total = 10 - total % 10;
        if (total == 10) total = 0;

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(8, 1)))
            return true;
        else
            return false;
    }

    static bool ELCheckDigit(string vatnumber) {
        // Checks the check digits of a Greek VAT number.
        var total = 0;
        var multipliers = new[] { 256, 128, 64, 32, 16, 8, 4, 2 };

        //eight character numbers should be prefixed with an 0.
        if (vatnumber.Length == 8) { vatnumber = "0" + vatnumber; }

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digit.
        total = total % 11;
        if (total > 9) { total = 0; };

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(8, 1)))
            return true;
        else
            return false;
    }

    static bool ESCheckDigit(string vatnumber) {
        // Checks the check digits of a Spanish VAT number.
        var total = 0;
        var temp = 0;
        var multipliers = new[] { 2, 1, 2, 1, 2, 1, 2 };
        var esexp = new[] {
            "^[A-H|J|U|V]\\d{8}$",
            "^[A-H|N-S|W]\\d{7}[A-J]$",
            "^[0-9|Y|Z]\\d{7}[A-Z]$",
            "^[K|L|M|X]\\d{7}[A-Z]$"
        };
        var i = 0;

        // National juridical entities
        if (Regex.IsMatch(vatnumber, esexp[0])) {
            // Extract the next digit and multiply by the counter.
            for (i = 0; i < 7; i++) {
                temp = int.Parse(vatnumber[i + 1].ToString()) * multipliers[i];
                if (temp > 9)
                    total += (temp / 10) + (temp % 10);
                else
                    total += temp;
            }
            // Now calculate the check digit itself. 
            total = 10 - total % 10;
            if (total == 10) { total = 0; }

            // Compare it with the last character of the VAT number. If it's the same, then it's valid.
            if (total == int.Parse(vatnumber.Substring(8, 1)))
                return true;
            else
                return false;
        }

        // Juridical entities other than national ones
        else if (Regex.IsMatch(vatnumber, esexp[1])) {

            // Extract the next digit and multiply by the counter.
            for (i = 0; i < 7; i++) {
                temp = int.Parse(vatnumber[i + 1].ToString()) * multipliers[i];
                if (temp > 9)
                    total += (temp / 10) + (temp % 10);
                else
                    total += temp;
            }

            // Now calculate the check digit itself.
            total = 10 - total % 10;
            total = total + 64;

            // Compare it with the last character of the VAT number. If it's the same, then it's valid.
            if ((char)total == vatnumber.Substring(8, 1)[0])
                return true;
            else
                return false;
        }

        // Personal number (NIF) (starting with numeric of Y or Z)
        else if (Regex.IsMatch(vatnumber, esexp[2])) {
            var tempnumber = vatnumber;
            if (tempnumber.Substring(0, 1) == "Y") tempnumber = tempnumber.Replace('Y', '1');
            if (tempnumber.Substring(0, 1) == "Z") tempnumber = tempnumber.Replace('Z', '2');
            return tempnumber[8] == "TRWAGMYFPDXBNJZSQVHLCKE"[int.Parse(tempnumber.Substring(0, 8)) % 23];
        }

        // Personal number (NIF) (starting with K, L, M, or X)
        else if (Regex.IsMatch(vatnumber, esexp[3])) {
            return vatnumber[8] == "TRWAGMYFPDXBNJZSQVHLCKE"[int.Parse(vatnumber.Substring(1, 7)) % 23];
        } else
            return false;
    }

    static bool EUCheckDigit(string vatnumber) {

        // We know little about EU numbers apart from the fact that the first 3 digits represent the 
        // country, and that there are nine digits in total.
        return true;
    }

    static bool FICheckDigit(string vatnumber) {

        // Checks the check digits of a Finnish VAT number.

        var total = 0;
        var multipliers = new[] { 7, 9, 10, 5, 8, 4, 2 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 7; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digit.
        total = 11 - total % 11;
        if (total > 9) { total = 0; };

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(7, 1)))
            return true;
        else
            return false;
    }

    static bool FRCheckDigit(string vatnumber) {
        // Checks the check digits of a French VAT number.
        if (!Regex.IsMatch(vatnumber, "^\\d{11}$")) return true;
        // Extract the last nine digits as an integer.
        var total = long.Parse(vatnumber.Substring(2));
        // Establish check digit.
        total = (total * 100 + 12) % 97;

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(0, 2)))
            return true;
        else
            return false;
    }

    static bool GBCheckDigit(string vatnumber) {

        // Checks the check digits of a UK VAT number
        var multipliers = new[] { 8, 7, 6, 5, 4, 3, 2 };
        // Government departments
        if (vatnumber.Substring(0, 2) == "GD") {
            if (int.Parse(vatnumber.Substring(2, 3)) < 500)
                return true;
            else
                return false;
        }

        // Health authorities
        if (vatnumber.Substring(0, 2) == "HA") {
            if (int.Parse(vatnumber.Substring(2, 3)) > 499)
                return true;
            else
                return false;
        }

        // Standard and commercial numbers
        var total = 0L;

        // 0 VAT numbers disallowed!
        if (long.Parse(vatnumber.Substring(0)) == 0) return false;

        // Check range is OK for modulus 97 calculation
        var no = long.Parse(vatnumber.Substring(0, 7));

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 7; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Old numbers use a simple 97 modulus, but new numbers use an adaptation of that (less 55). Our 
        // VAT number could use either system, so we check it against both.

        // Establish check digits by subtracting 97 from total until negative.
        var cd = total;
        while (cd > 0) { cd = cd - 97; }

        // Get the absolute value and compare it with the last two characters of the VAT number. If the 
        // same, then it is a valid traditional check digit. However, even then the number must fit within
        // certain specified ranges.
        cd = Math.Abs(cd);
        if (cd == int.Parse(vatnumber.Substring(7, 2)) && no < 9990001 && (no < 100000 || no > 999999) && (no < 9490001 || no > 9700000)) return true;

        // Now try the new method by subtracting 55 from the check digit if we can - else add 42
        if (cd >= 55)
            cd = cd - 55;
        else
            cd = cd + 42;
        if (cd == int.Parse(vatnumber.Substring(7, 2)) && no > 1000000)
            return true;
        else
            return false;
    }

    static bool HRCheckDigit(string vatnumber) {

        // Checks the check digits of a Croatian VAT number using ISO 7064, MOD 11-10 for check digit.
        var product = 10;
        var sum = 0;
        for (var i = 0; i < 10; i++) {
            // Extract the next digit and implement the algorithm
            sum = (int.Parse(vatnumber[i].ToString()) + product) % 10;
            if (sum == 0) { sum = 10; }
            product = (2 * sum) % 11;
        }

        // Now check that we have the right check digit
        if ((product + int.Parse(vatnumber.Substring(10, 1)) * 1) % 10 == 1)
            return true;
        else
            return false;
    }

    static bool HUCheckDigit(string vatnumber) {
        // Checks the check digits of a Hungarian VAT number.
        var total = 0;
        var multipliers = new[] { 9, 7, 3, 1, 9, 7, 3 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 7; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digit.
        total = 10 - total % 10;
        if (total == 10) total = 0;

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(7, 1)))
            return true;
        else
            return false;
    }

    static bool IECheckDigit(string vatnumber) {
        // Checks the check digits of an Irish VAT number.
        var total = 0;
        var multipliers = new[] { 8, 7, 6, 5, 4, 3, 2 };

        // If the code is type 1 format, we need to convert it to the new before performing the validation.
        if (Regex.IsMatch(vatnumber, "^\\d[A-Z\\*\\+]"))
            vatnumber = "0" + vatnumber.Substring(2, 5) + vatnumber.Substring(0, 1) + vatnumber.Substring(7, 1);

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 7; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // If the number is type 3 then we need to include the trailing A or H in the calculation
        if (Regex.IsMatch(vatnumber, "^\\d{7}[A-Z][AH]$")) {

            // Add in a multiplier for the character A (1*9=9) or H (8*9=72)
            if (vatnumber[8] == 'H')
                total += 72;
            else
                total += 9;
        }

        // Establish check digit using modulus 23, and translate to char. equivalent.
        total = total % 23;
        if (total == 0)
            total = 'W';
        else
            total = total + 64;

        // Compare it with the eighth character of the VAT number. If it's the same, then it's valid.
        if ((char)total == vatnumber.Substring(7, 1)[0])
            return true;
        else
            return false;
    }

    static bool ITCheckDigit(string vatnumber) {

        // Checks the check digits of an Italian VAT number.

        var total = 0;
        var multipliers = new[] { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };
        var temp = 0;

        // The last three digits are the issuing office, and cannot exceed more 201, unless 999 or 888
        if (int.Parse(vatnumber.Substring(0, 7)) == 0) return false;
        temp = int.Parse(vatnumber.Substring(7, 3));
        if ((temp < 1) || (temp > 201) && temp != 999 && temp != 888) return false;

        // Extract the next digit and multiply by the appropriate  
        for (var i = 0; i < 10; i++) {
            temp = int.Parse(vatnumber[i].ToString()) * multipliers[i];
            if (temp > 9)
                total += (temp / 10) + (temp % 10);
            else
                total += temp;
        }

        // Establish check digit.
        total = 10 - total % 10;
        if (total > 9) { total = 0; };

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(10, 1)))
            return true;
        else
            return false;
    }

    static bool LTCheckDigit(string vatnumber) {

        // Checks the check digits of a Lithuanian VAT number.

        // 9 character VAT numbers are for legal persons
        if (vatnumber.Length == 9) {

            // 8th character must be one
            if (!Regex.IsMatch(vatnumber, "^\\d{7}1")) return false;

            // Extract the next digit and multiply by the counter+1.
            var total = 0;
            for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * (i + 1);

            // Can have a double check digit calculation!
            if (total % 11 == 10) {
                var multipliers = new[] { 3, 4, 5, 6, 7, 8, 9, 1 };
                total = 0;
                for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];
            }

            // Establish check digit.
            total = total % 11;
            if (total == 10) { total = 0; };

            // Compare it with the last character of the VAT number. If it's the same, then it's valid.
            if (total == int.Parse(vatnumber.Substring(8, 1)))
                return true;
            else
                return false;
        }

        // 12 character VAT numbers are for temporarily registered taxpayers
        else {

            // 11th character must be one
            if (!Regex.IsMatch(vatnumber, "^\\d{10}1")) return false;

            // Extract the next digit and multiply by the counter+1.
            var total = 0;
            var multipliers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2 };
            for (var i = 0; i < 11; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

            // Can have a double check digit calculation!
            if (total % 11 == 10) {
                multipliers = [3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4];
                total = 0;
                for (var i = 0; i < 11; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];
            }

            // Establish check digit.
            total = total % 11;
            if (total == 10) { total = 0; };

            // Compare it with the last character of the VAT number. If it's the same, then it's valid.
            if (total == int.Parse(vatnumber.Substring(11, 1)))
                return true;
            else
                return false;
        }
    }

    static bool LUCheckDigit(string vatnumber) {

        // Checks the check digits of a Luxembourg VAT number.

        if (int.Parse(vatnumber.Substring(0, 6)) % 89 == int.Parse(vatnumber.Substring(6, 2)))
            return true;
        else
            return false;
    }

    static bool LVCheckDigit(string vatnumber) {

        // Checks the check digits of a Latvian VAT number.

        // Differentiate between legal entities and natural bodies. For the latter we simply check that
        // the first six digits correspond to valid DDMMYY dates.
        if (Regex.IsMatch(vatnumber, "^[0-3]")) {
            if (Regex.IsMatch(vatnumber, "^[0-3][0-9][0-1][0-9]"))
                return true;
            else
                return false;
        } else {
            var total = 0;
            var multipliers = new[] { 9, 1, 4, 8, 3, 10, 2, 5, 7, 6 };

            // Extract the next digit and multiply by the counter.
            for (var i = 0; i < 10; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

            // Establish check digits by getting modulus 11.
            if (total % 11 == 4 && vatnumber[0] == 9) total = total - 45;
            if (total % 11 == 4)
                total = 4 - total % 11;
            else if (total % 11 > 4)
                total = 14 - total % 11;
            else if (total % 11 < 4)
                total = 3 - total % 11;

            // Compare it with the last character of the VAT number. If it's the same, then it's valid.
            if (total == int.Parse(vatnumber.Substring(10, 1)))
                return true;
            else
                return false;
        }
    }

    static bool MTCheckDigit(string vatnumber) {
        // Checks the check digits of a Maltese VAT number.
        var total = 0;
        var multipliers = new[] { 3, 4, 6, 7, 8, 9 };
        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 6; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digits by getting modulus 37.
        total = 37 - total % 37;

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(6, 2)) * 1)
            return true;
        else
            return false;
    }

    static bool NLCheckDigit(string vatnumber) {

        // Checks the check digits of a Dutch VAT number.

        var total = 0;
        var multipliers = new[] { 9, 8, 7, 6, 5, 4, 3, 2 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digits by getting modulus 11.
        total = total % 11;
        if (total > 9) { total = 0; };

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(8, 1)))
            return true;
        else
            return false;
    }

    static bool NOCheckDigit(string vatnumber) {
        // Checks the check digits of a Norwegian VAT number.
        // See http://www.brreg.no/english/coordination/number.html
        var total = 0;
        var multipliers = new[] { 3, 2, 7, 6, 5, 4, 3, 2 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digits by getting modulus 11. Check digits > 9 are invalid
        total = 11 - total % 11;
        if (total == 11) { total = 0; }
        if (total < 10) {
            // Compare it with the last character of the VAT number. If it's the same, then it's valid.
            if (total == int.Parse(vatnumber.Substring(8, 1)))
                return true;
        }
        return false;
    }

    static bool PLCheckDigit(string vatnumber) {

        // Checks the check digits of a Polish VAT number.

        var total = 0;
        var multipliers = new[] { 6, 5, 7, 2, 3, 4, 5, 6, 7 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 9; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digits subtracting modulus 11 from 11.
        total = total % 11;
        if (total > 9) { total = 0; };

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(9, 1)))
            return true;
        else
            return false;
    }

    static bool PTCheckDigit(string vatnumber) {
        // Checks the check digits of a Portugese VAT number.
        var total = 0;
        var multipliers = new[] { 9, 8, 7, 6, 5, 4, 3, 2 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 8; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digits subtracting modulus 11 from 11.
        total = 11 - total % 11;
        if (total > 9) { total = 0; };

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(8, 1)))
            return true;
        else
            return false;
    }

    static bool ROCheckDigit(string vatnumber) {
        // Checks the check digits of a Romanian VAT number.
        var multipliersAll = new[] { 7, 5, 3, 2, 1, 7, 5, 3, 2 };

        // Extract the next digit and multiply by the counter.
        var VATlen = vatnumber.Length;

        var total = 0;
        var startIndex = 10 - VATlen;
        var multipliers = new int[multipliersAll.Length - startIndex];
        Array.Copy(multipliersAll, startIndex, multipliers, 0, multipliers.Length);
        for (var i = 0; i < vatnumber.Length - 1; i++) {
            total += int.Parse(vatnumber[i].ToString()) * multipliers[i];
        }

        // Establish check digits by getting modulus 11.
        total = (10 * total) % 11;
        if (total == 10) total = 0;

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (total == int.Parse(vatnumber.Substring(vatnumber.Length - 1, 1)))
            return true;
        else
            return false;
    }

    static bool RSCheckDigit(string vatnumber) {
        // Checks the check digits of a Serbian VAT number using ISO 7064, MOD 11-10 for check digit.
        var product = 10;
        var sum = 0;

        for (var i = 0; i < 8; i++) {
            // Extract the next digit and implement the algorithm
            sum = (int.Parse(vatnumber[i].ToString()) + product) % 10;
            if (sum == 0) { sum = 10; }
            product = (2 * sum) % 11;
        }

        // Now check that we have the right check digit
        if ((product + int.Parse(vatnumber.Substring(8, 1)) * 1) % 10 == 1)
            return true;
        else
            return false;
    }
    static bool RUCheckDigit(string vatnumber) {
        // Checks the check digits of a Russian INN number 
        // See http://russianpartner.biz/test_inn.html for algorithm 
        // 10 digit INN numbers
        if (vatnumber.Length == 10) {
            var total = 0;
            var multipliers = new[] { 2, 4, 10, 3, 5, 9, 4, 6, 8, 0 };

            for (var i = 0; i < 10; i++) {
                total += int.Parse(vatnumber[i].ToString()) * multipliers[i];
            }
            total = total % 11;

            if (total > 9) { total = total % 10; }
            // Compare it with the last character of the VAT number. If it is the same, then it's valid
            if (total == int.Parse(vatnumber.Substring(9, 1)))
                return true;
            else
                return false;
            // 12 digit INN numbers
        } else if (vatnumber.Length == 12) {
            var total1 = 0;
            var multipliers1 = new[] { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8, 0 };

            var total2 = 0;
            var multipliers2 = new[] { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8, 0 };

            for (var i = 0; i < 11; i++) total1 += int.Parse(vatnumber[i].ToString()) * multipliers1[i];
            total1 = total1 % 11;

            if (total1 > 9) { total1 = total1 % 10; }

            for (var i = 0; i < 11; i++) total2 += int.Parse(vatnumber[i].ToString()) * multipliers2[i];
            total2 = total2 % 11;

            if (total2 > 9) { total2 = total2 % 10; }

            // Compare the first check with the 11th character and the second check with the 12th and last 
            // character of the VAT number. If they're both the same, then it's valid
            if (total1 == int.Parse(vatnumber.Substring(10, 1)) &&
                total2 == int.Parse(vatnumber.Substring(11, 1)))
                return true;

            else
                return false;

        }
        return false;
    }

    static bool SECheckDigit(string vatnumber) {

        // Calculate R where R = R1 + R3 + R5 + R7 + R9, and Ri = INT(Ci/5) + (Ci*2) modulo 10
        var R = 0;
        var digit = 0;
        for (var i = 0; i < 9; i = i + 2) {
            digit = int.Parse(vatnumber[i].ToString());
            R += (digit / 5) + ((digit * 2) % 10);
        }

        // Calculate S where S = C2 + C4 + C6 + C8
        var S = 0;
        for (var i = 1; i < 9; i = i + 2) S += int.Parse(vatnumber[i].ToString());

        // Calculate the Check Digit
        var cd = (10 - (R + S) % 10) % 10;

        // Compare it with the last character of the VAT number. If it's the same, then it's valid.
        if (cd == int.Parse(vatnumber.Substring(9, 1)))
            return true;
        else
            return false;
    }

    static bool SICheckDigit(string vatnumber) {

        // Checks the check digits of a Slovenian VAT number.

        var total = 0;
        var multipliers = new[] { 8, 7, 6, 5, 4, 3, 2 };

        // Extract the next digit and multiply by the counter.
        for (var i = 0; i < 7; i++) total += int.Parse(vatnumber[i].ToString()) * multipliers[i];

        // Establish check digits using modulus 11
        total = 11 - total % 11;
        if (total == 10) { total = 0; };

        // Compare the number with the last character of the VAT number. If it is the 
        // same, then it's a valid check digit.
        if (total != 11 && total == int.Parse(vatnumber.Substring(7, 1)))
            return true;
        else
            return false;
    }

    static bool SKCheckDigit(string vatnumber) {

        // Checks the check digits of a Slovakian VAT number.

        // Check that the modulus of the whole VAT number is 0 - else error
        if ((long.Parse(vatnumber) % 11) == 0)
            return true;
        else
            return false;
    }


}
