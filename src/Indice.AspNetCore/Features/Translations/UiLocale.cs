namespace Indice.AspNetCore.Features.Translations;

/// <summary>
/// Holds information about each available language.
/// </summary>
public class UiLocale
{
    /// <summary>
    /// The culture name of the language
    /// </summary>
    public string? Lang { get; set; }
    /// <summary>
    /// The native name for the language
    /// </summary>
    public string? NativeName { get; set; }
    /// <summary>
    /// The English name for the language
    /// </summary>
    public string? EnglishName { get; set; }
}
