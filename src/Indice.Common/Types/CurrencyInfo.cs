using System.Collections.ObjectModel;
using System.Globalization;

namespace Indice.Globalization;

/// <summary>Represents a currency (ie Dollar $).</summary>
public class CurrencyInfo : IFormatProvider, ICustomFormatter
{
    private static readonly HashSet<string> CurrencySymbols;
    private static readonly HashSet<string> commonIsoSymbols = new HashSet<string> { "EUR", "USD", "GBP", "CAD", "CHF", "AUD", "INR", "TND", "AED", "JPY" };
    /// <summary>Collection of available currencies</summary>
    public static readonly IReadOnlyCollection<CurrencyInfo> Currencies;

    static CurrencyInfo() {
        var fractualCurrencySymbols = new ReadOnlyDictionary<string, FractionInfo>(new Dictionary<string, FractionInfo> {
            {"£", new FractionInfo("penny", "p", 100) }
        });
        var currencies = new Dictionary<string, CurrencyInfo>() {
            { "USD", new CurrencyInfo { Name = "US Dollar", NativeName = "United States, Dollar", ISOSymbol = "USD", Symbol = "$", Fraction = new FractionInfo("cent", "¢", 100) } },
            { "EUR", new CurrencyInfo { Name = "Euro", NativeName = "Euro Member Countries, Euro", ISOSymbol = "EUR", Symbol = "€", Fraction = new FractionInfo("cent", "c", 100), AlignRight = true } },
            { "GBP", new CurrencyInfo { Name = "British Pound", NativeName = "United Kingdom, Pound", ISOSymbol = "GBP", Symbol = "£", Fraction = new FractionInfo("penny", "p", 100) } }
        };
        var symbols = currencies.Values.Select(c => c.Symbol).ToList();
        foreach (var country in CountryInfo.Countries) {
            try {
                var region = new RegionInfo(country.TwoLetterCode);
                if (!currencies.ContainsKey(region.ISOCurrencySymbol)) {
                    var currency = currencies[region.ISOCurrencySymbol] = new CurrencyInfo() {
                        Name = region.EnglishName,
                        NativeName = region.DisplayName,
                        ISOSymbol = region.ISOCurrencySymbol,
                        Symbol = region.CurrencySymbol,
                        Fraction = fractualCurrencySymbols.ContainsKey(region.CurrencySymbol) ? fractualCurrencySymbols[region.CurrencySymbol] : new FractionInfo("cent", "c", 100),
                    };
                    symbols.Add(currency.Symbol);
                    if (!fractualCurrencySymbols.ContainsKey(region.CurrencySymbol)) {
                        symbols.Add(currency.Fraction.Symbol);
                    }
                }
            } catch {
                ;
            }
        }
        Currencies = currencies.Values.OrderByDescending(ci => ci.IsCommon).ThenBy(ci => ci.IsCommon ? "0" : ci.Name).ToArray();
        CurrencySymbols = new HashSet<string>(symbols.Distinct());
    }

    /// <summary>The display name.</summary>
    public string Name { get; protected set; }
    /// <summary>The native name.</summary>
    public string NativeName { get; protected set; }
    /// <summary>The symbol.</summary>
    public string Symbol { get; protected set; }
    /// <summary>The position beside the number.</summary>
    public bool AlignRight { get; protected set; }
    /// <summary>This is a common currency among countries or not.</summary>
    public bool IsCommon => commonIsoSymbols.Contains(ISOSymbol);
    /// <summary>Three letter ISO symbol.</summary>
    public string ISOSymbol { get; protected set; }
    /// <summary>The fraction information.</summary>
    public FractionInfo Fraction { get; protected set; }

    /// <summary>Utility that checks for existance of a symbol (ie $) in the list.</summary>
    /// <param name="symbol"></param>
    public static bool ContainsSymbol(string symbol) => CurrencySymbols.Contains(symbol);

    /// <summary>Utility that checks for existance of an ISO symbol (ie USD) in the list.</summary>
    /// <param name="isoSymbol"></param>
    /// <returns></returns>
    public static bool ContainsISOSymbol(string isoSymbol) => Currencies.Where(c => c.ISOSymbol == isoSymbol).Any();

    /// <summary>Utility the searches the currency list by ISO symbol (ie USD).</summary>
    /// <param name="iso"></param>
    public static CurrencyInfo GetByISOSymbol(string iso) => Currencies.Where(c => c.ISOSymbol == iso).SingleOrDefault() ?? throw new ArgumentOutOfRangeException(nameof(iso));

    /// <summary>Utility the searches the currency list by symbol (ie $).</summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static CurrencyInfo GetBySymbol(string symbol) => Currencies.Where(c => c.Symbol == symbol || c.Fraction.Symbol == symbol).FirstOrDefault() ?? throw new ArgumentOutOfRangeException(nameof(symbol));

    /// <summary>Tries to get the <see cref="CurrencyInfo"/> by symbol (ie $).</summary>
    /// <param name="symbol"></param>
    /// <param name="currencyInfo"></param>
    /// <returns></returns>
    public static bool TryGetBySymbol(string symbol, out CurrencyInfo currencyInfo) {
        currencyInfo = null;
        var success = false;
        if (ContainsSymbol(symbol)) {
            success = true;
            currencyInfo = GetBySymbol(symbol);
        }
        return success;
    }

    /// <summary>Tries to get the <see cref="CurrencyInfo"/> by ISO symbol (ie USD).</summary>
    /// <param name="isoSymbol"></param>
    /// <param name="currencyInfo"></param>
    public static bool TryGetByIsoSymbol(string isoSymbol, out CurrencyInfo currencyInfo) {
        currencyInfo = Currencies.Where(c => c.ISOSymbol == isoSymbol).SingleOrDefault();
        var success = currencyInfo != null;
        return success;
    }

    /// <summary>String representation.</summary>
    /// <returns></returns>
    public override string ToString() => $"{ISOSymbol} - {Name} ({Symbol}), {Fraction}";

    /// <summary>Short vertion of the string representation</summary>
    /// <returns></returns>
    public string ToShortString() => $"{ISOSymbol} - {Name} ({Symbol})";

    /// <summary>Format</summary>
    /// <param name="format"></param>
    /// <param name="arg"></param>
    /// <param name="formatProvider"></param>
    public string Format(string format, object arg, IFormatProvider formatProvider) {
        if (arg is double || arg is decimal || arg is int || arg is long || arg is short) {
            var numberFormatInfo = (NumberFormatInfo)GetFormat(typeof(NumberFormatInfo));
            if (string.IsNullOrEmpty(format)) {
                // By default, format doubles to 3 decimal places.
                return string.Format(numberFormatInfo, "C", arg);
            } else {
                // If user supplied own format use it.
                return ((double)arg).ToString(format, numberFormatInfo);
            }
        }
        // Format everything else normally.
        if (arg is IFormattable formattable) {
            return formattable.ToString(format, formatProvider);
        } else {
            return arg.ToString();
        }
    }

    /// <summary>Get the format according to the formatType.</summary>
    /// <param name="formatType"></param>
    public object GetFormat(Type formatType) {
        if (formatType == typeof(ICustomFormatter)) {
            return this;
        } else if (formatType == typeof(NumberFormatInfo)) {
            var rtl = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
            var numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
            numberFormatInfo.CurrencySymbol = Symbol; // Replace with "$" or "£" or whatever you need.
            var positiveLeftPattern = !rtl ? 2 : 3; // https://msdn.microsoft.com/en-us/library/system.globalization.numberformatinfo.currencynegativepattern(v=vs.110).aspx
            var positiveRightPattern = !rtl ? 3 : 2;
            var negativeLeftPattern = !rtl ? 14 : 15; //https://msdn.microsoft.com/en-us/library/system.globalization.numberformatinfo.currencynegativepattern(v=vs.110).aspx
            var negativeRightPattern = !rtl ? 8 : 11;
            numberFormatInfo.CurrencyPositivePattern = AlignRight ? positiveRightPattern : positiveLeftPattern;
            numberFormatInfo.CurrencyNegativePattern = AlignRight ? negativeRightPattern : negativeLeftPattern;
            return numberFormatInfo;
        }
        return null;
    }

    /// <summary>Rounds a given decimal number.</summary>
    /// <param name="money"></param>
    /// <param name="truncate"></param>
    public decimal Round(decimal money, bool truncate = false) {
        if (truncate) {
            var denominator = Fraction.Denominator > 0 ? Fraction.Denominator : 100;
            return Math.Truncate(denominator * money) / denominator;
        }
        return Math.Round(money, Fraction?.DecimalPlaces ?? 2);
    }

    /// <summary>Rounds a given decimal number.</summary>
    /// <param name="money"></param>
    /// <param name="midpointRounding"></param>
    public decimal Round(decimal money, MidpointRounding midpointRounding) => Math.Round(money, Fraction?.DecimalPlaces ?? 2, midpointRounding);
}

/// <summary>Represents a fraction of a currency (ie cent c).</summary>
public class FractionInfo
{
    /// <summary>Contructs a <see cref="FractionInfo" /></summary>
    /// <param name="name"></param>
    /// <param name="symbol"></param>
    /// <param name="denominator"></param>
    public FractionInfo(string name, string symbol, int denominator) {
        Name = name;
        Symbol = symbol;
        Denominator = denominator;
    }

    /// <summary>The display name.</summary>
    public string Name { get; protected set; }
    /// <summary>The symbol.</summary>
    public string Symbol { get; protected set; }
    /// <summary>The denominator.</summary>
    public int Denominator { get; protected set; }
    /// <summary>Number of decimal places.</summary>
    public int DecimalPlaces => (int)Math.Log10(Denominator);
    /// <summary>String representation.</summary>
    /// <returns></returns>
    public override string ToString() => string.Format("{0} ({1})", Name, Symbol, Denominator);
}
