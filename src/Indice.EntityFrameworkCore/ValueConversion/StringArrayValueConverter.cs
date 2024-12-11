using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Indice.EntityFrameworkCore.ValueConversion;

/// <summary>Value converter for EF core that takes an <see cref="Array"/> of strings and saves it as comma separated values string and vice versa.</summary>
public class StringArrayValueConverter : ValueConverter<string[]?, string?>
{
    /// <inheritdoc/>
    public StringArrayValueConverter() : base(
        array => array != null && array.Length > 0 ? string.Join(',', array) : null,
        commaSeparatedValues => !string.IsNullOrWhiteSpace(commaSeparatedValues) ? commaSeparatedValues.Split(',', StringSplitOptions.None).ToArray() : null
    ) { }
}
