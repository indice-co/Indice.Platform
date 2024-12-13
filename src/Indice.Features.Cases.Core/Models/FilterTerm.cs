namespace Indice.Features.Cases.Core.Models;

/// <summary>The Filter Term model.</summary>
public class FilterTerm
{
    /// <summary>FilterTerm's Key</summary>
    public string? Key { get; set; }

    /// <summary>FilterTerm's Value</summary>
    public string? Value { get; set; }

    /// <summary>
    /// Parse method can parse form ui
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static FilterTerm Parse(string input) => new FilterTerm { Key = input.Split(':')[0], Value = input.Split(':')[0] };
}
