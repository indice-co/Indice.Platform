namespace Indice.Types;

/// <summary>Represents an object that contains data about a translated object. </summary>
/// <typeparam name="T">The translated type.</typeparam>
public class Translation<T> where T : notnull
{
    /// <summary>Creates a new instance of <see cref="Translation{T}"/>.</summary>
    /// <param name="culture">The culture.</param>
    /// <param name="value">The translation value.</param>
    public Translation(string culture, T value) {
        Culture = culture;
        Value = value;
    }

    /// <summary>The culture.</summary>
    public string Culture { get; }
    /// <summary>The translation value.</summary>
    public T Value { get; }
}
