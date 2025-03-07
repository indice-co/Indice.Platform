#nullable enable
using Indice.Validation;
using Xunit;

namespace Indice.Common.Tests;
public class TaxCodeTests
{
    [InlineData(null, "NL005085444B42")]
    [Theory]
    public void TaxCodeValidationTest(string? countryISO, string taxCode) {
        var ok = TaxCodeValidator.CheckNumber(taxCode, countryISO);
        Assert.True(ok);
    }
}
#nullable disable