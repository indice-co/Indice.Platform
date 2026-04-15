using Indice.Globalization;
using Xunit;

namespace Indice.Common.Tests;

public class CountryInfoTests
{
    public CountryInfoTests() {

    }

    [Trait("Tag", "Country")]
    [Theory]
    [InlineData("GR")]
    [InlineData("AU")]
    [InlineData("GB")]
    [InlineData("Greece")]
    [InlineData("SG")]
    [InlineData("IE")]
    public void ByTwoLetterISOCodeValid_Test(string iso) {
        var country = CountryInfo.GetCountryByNameOrCode(iso);
        Assert.NotNull(country);
        var timezoneResolved = TimeZoneInfo.TryFindSystemTimeZoneById(country.TimeZoneId, out var timeZone);
        Assert.True(timezoneResolved);
        Assert.NotNull(timeZone);
    }
}
